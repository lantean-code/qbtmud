"""Release/tag policy regressions; no registry writes or network calls."""
import copy
import hashlib
import io
import importlib.util
import json
from pathlib import Path
import tempfile
import sys
import tarfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
spec = importlib.util.spec_from_file_location("release", Path(__file__).resolve().parents[1] / "release.py")
release = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release)


class PolicyTests(unittest.TestCase):
    def setUp(self):
        self.inputs = {"version": "3.0.0", "upstreamTag": "5.2.4_v2.0.15-ls480", "revision": 1,
                       "packagingCommit": "a" * 40, "upstreamDigest": "sha256:" + "b" * 64}

    def test_stable_versions_exclude_prereleases_and_ambiguous_tags(self):
        for tag in ("3.0.0-rc.1", "3.0.0-beta", "3.0", "03.0.0", "vv3.0.0", "3.0.0+build", "develop"):
            with self.subTest(tag=tag), self.assertRaises(ValueError):
                release.stable_version(tag)
        self.assertEqual(release.stable_version("v3.0.0"), "3.0.0")

    def test_semantic_order_is_numeric(self):
        self.assertGreater(release.version_key("3.10.0"), release.version_key("3.9.0"))

    def test_combination_and_unknown_upstream(self):
        self.assertEqual(release.combination("3.0.0", self.inputs["upstreamTag"], 1),
                         "3.0.0-qbt5.2.4-lt2.0.15-ls480-r1")
        for tag in ("latest", "5.2.4-ls480", "libtorrentv1", "5.2.4_v2.0.15-ls480-extra"):
            with self.subTest(tag=tag), self.assertRaises(ValueError):
                release.combination("3.0.0", tag, 1)

    def test_old_ui_cannot_replace_latest(self):
        newer = dict(self.inputs, version="3.1.0")
        self.assertFalse(release.can_promote(self.inputs, newer))
        self.assertTrue(release.can_promote(newer, self.inputs))

    def test_old_base_or_revision_cannot_replace_version_alias(self):
        self.assertFalse(release.can_promote(self.inputs, dict(self.inputs, upstreamTag="5.2.4_v2.0.15-ls481")))
        self.assertFalse(release.can_promote(self.inputs, dict(self.inputs, revision=2)))

    def test_changed_digest_same_revision_requires_new_revision(self):
        with self.assertRaises(ValueError):
            release.can_promote(self.inputs, dict(self.inputs, upstreamDigest="sha256:" + "c" * 64))

    def test_new_upstream_combination_can_start_at_revision_one(self):
        old = dict(self.inputs, revision=2)
        new = dict(self.inputs, upstreamTag="5.2.5_v2.0.15-ls481")
        self.assertTrue(release.can_promote(new, old))

    def base(self, tag="5.2.4_v2.0.15-ls480", digest=None):
        digest = digest or "sha256:" + "b" * 64
        return {"tag": tag, "digest": digest, "image": f"lscr.io/linuxserver/qbittorrent:{tag}@{digest}"}

    def test_upstream_resolves_both_architectures_to_the_same_pinned_version(self):
        base = self.base()
        labels = {"config": {"Labels": {"org.opencontainers.image.version": base["tag"],
                                        "build_version": f"Linuxserver.io version:- {base['tag']} Build-date:- 2026-10-13"}}}
        result = type("Result", (), {"stdout": json.dumps(labels)})()
        with patch.object(release, "inspect", return_value=({}, base["digest"])), \
                patch.object(release, "command", return_value=result) as commands, \
                patch.object(release, "validate_base", side_effect=lambda value: value):
            self.assertEqual(release.resolve_upstream(), base)
            self.assertEqual([call.args[call.args.index("--override-arch") + 1] for call in commands.call_args_list],
                             ["amd64", "arm64"])
            self.assertTrue(all("docker://lscr.io/linuxserver/qbittorrent@" + base["digest"] in call.args
                                for call in commands.call_args_list))

    def test_upstream_rejects_mixed_architecture_versions_and_moving_tag_races(self):
        base = self.base()
        def labels(tag):
            return type("Result", (), {"stdout": json.dumps({"config": {"Labels": {
                "org.opencontainers.image.version": tag,
                "build_version": f"Linuxserver.io version:- {tag} Build-date:- today"}}})})()
        with patch.object(release, "inspect", return_value=({}, base["digest"])), \
                patch.object(release, "command", side_effect=[labels(base["tag"]), labels("5.2.5_v2.0.15-ls481")]):
            with self.assertRaisesRegex(ValueError, "different versions"):
                release.resolve_upstream()
        with patch.object(release, "inspect", side_effect=[({}, base["digest"]), ({}, "sha256:" + "c" * 64)]), \
                patch.object(release, "command", return_value=labels(base["tag"])):
            with self.assertRaisesRegex(ValueError, "different indexes"):
                release.resolve_upstream()

    def test_base_policy_rejects_libtorrent_family_switch_and_automatic_downgrades(self):
        with self.assertRaisesRegex(ValueError, "libtorrent 2"):
            release.validate_base(self.base("5.2.4_v1.2.20-ls480"))
        with patch.object(release, "approved_base", return_value=self.base("5.2.5_v2.0.15-ls481")), \
                patch.object(release, "resolve_upstream", return_value=self.base()):
            with self.assertRaisesRegex(ValueError, "downgrade"):
                release.select_base({}, None, True)

    def test_later_ui_release_keeps_the_successfully_published_newer_base(self):
        base = self.base("5.2.5_v2.0.15-ls481")
        current = dict(self.inputs, upstreamTag=base["tag"], upstreamDigest=base["digest"], baseImage=base["image"])
        with patch.object(release, "read", return_value=self.base()):
            self.assertEqual(release.approved_base({"revision": 1}, current), base)

    def test_repackaging_does_not_restore_a_stale_same_tag_bootstrap_digest(self):
        base = self.base(digest="sha256:" + "c" * 64)
        current = dict(self.inputs, upstreamDigest=base["digest"], baseImage=base["image"], revision=2)
        with patch.object(release, "read", return_value=self.base()):
            self.assertEqual(release.approved_base({"revision": 3}, current), base)

    def test_unchanged_upstream_skips_build_and_registry_writes(self):
        base = self.base()
        ui = {"version": "3.0.0", "releaseTag": "3.0.0", "uiSha256": "1" * 64}
        current = dict(self.inputs, **{key: value for key, value in ui.items() if key != "version"}, baseImage=base["image"])
        config = {"image": "ghcr.io/lantean-code/qbtmud", "revision": 1}
        with tempfile.TemporaryDirectory() as folder, patch.object(release, "read", return_value=config), \
                patch.object(release, "latest_release", return_value=ui), \
                patch.object(release, "published_inputs", return_value=current), \
                patch.object(release, "select_base", return_value=base), \
                patch.object(release, "command") as commands, patch.object(release, "output") as outputs:
            release.prepare("lantean-code/qbtmud", "ignored", folder, True)
            outputs.assert_called_once_with("build_required", "false")
            commands.assert_not_called()

    def test_changed_same_tag_digest_allocates_a_new_immutable_revision(self):
        base = self.base(digest="sha256:" + "c" * 64)
        ui = {"version": "3.0.0", "releaseTag": "3.0.0", "uiSha256": "1" * 64}
        current = dict(self.inputs, releaseTag="3.0.0", uiSha256="1" * 64, baseImage=self.base()["image"])
        config = {"image": "ghcr.io/lantean-code/qbtmud", "revision": 1}
        def git(*args, **kwargs):
            return type("Result", (), {"stdout": "a" * 40 if "rev-parse" in args else ""})()
        with tempfile.TemporaryDirectory() as folder, patch.object(release, "read", return_value=config), \
                patch.object(release, "latest_release", return_value=ui), \
                patch.object(release, "published_inputs", return_value=current), \
                patch.object(release, "select_base", return_value=base), \
                patch.object(release, "command", side_effect=git), \
                patch.object(release, "inspect", return_value=None) as lookup, \
                patch.object(release, "output"):
            release.prepare("lantean-code/qbtmud", None, folder, True)
            inputs = json.loads((Path(folder) / "publication-inputs.json").read_text())
            self.assertEqual(inputs["revision"], 2)
            self.assertEqual(inputs["upstreamDigest"], base["digest"])
            self.assertTrue(json.loads((Path(folder) / "selection.json").read_text())["upstream"])
            self.assertTrue(lookup.call_args.args[0].endswith("-r2"))

    def test_upstream_change_after_testing_prevents_alias_promotion(self):
        changed = self.base(digest="sha256:" + "c" * 64)
        inputs = dict(self.inputs, baseImage=self.base()["image"])
        with patch.object(release, "gh", side_effect=[{"default_branch": "develop"}, {"sha": "a" * 40}]), \
                patch.object(release, "read", return_value={"image": "ghcr.io/lantean-code/qbtmud"}), \
                patch.object(release, "published_inputs", return_value=inputs), \
                patch.object(release, "select_base", return_value=changed):
            self.assertFalse(release.approval_current("lantean-code/qbtmud", inputs, {"upstream": True}))

    def test_new_ui_published_during_base_checks_prevents_stale_latest_promotion(self):
        base = self.base()
        inputs = dict(self.inputs, releaseTag="3.0.0", baseImage=base["image"])
        with patch.object(release, "gh", side_effect=[{"default_branch": "develop"}, {"sha": "a" * 40}]), \
                patch.object(release, "read", return_value={"image": "ghcr.io/lantean-code/qbtmud"}), \
                patch.object(release, "published_inputs", return_value=inputs), \
                patch.object(release, "select_base", return_value=base), \
                patch.object(release, "current_stable", return_value={"tag_name": "3.1.0"}):
            self.assertFalse(release.approval_current("lantean-code/qbtmud", inputs, {"upstream": True}, latest=True))

    def test_packaging_changed_during_upstream_checks_prevents_promotion(self):
        base = self.base()
        inputs = dict(self.inputs, baseImage=base["image"])
        with patch.object(release, "gh", side_effect=[{"default_branch": "develop"}, {"sha": "a" * 40}, {"sha": "c" * 40}]), \
                patch.object(release, "read", return_value={"image": "ghcr.io/lantean-code/qbtmud"}), \
                patch.object(release, "published_inputs", return_value=inputs), \
                patch.object(release, "select_base", return_value=base):
            self.assertFalse(release.approval_current("lantean-code/qbtmud", inputs, {"upstream": True}))

    def test_retry_rejects_changed_index_even_with_identical_annotations(self):
        with tempfile.TemporaryDirectory() as folder:
            release.write(Path(folder) / "publication-inputs.json", self.inputs)
            release.write(Path(folder) / "selection.json", {"reuseDigest": "sha256:" + "1" * 64})
            index = {"annotations": {release.ANNOTATION: json.dumps(self.inputs)}}
            with patch.object(release, "inspect", return_value=(index, "sha256:" + "2" * 64)):
                with self.assertRaisesRegex(ValueError, "changed since native testing"):
                    release.publish("lantean-code/qbtmud", folder, True)

    def test_approval_change_after_release_download_skips_aliases(self):
        inputs = dict(self.inputs, releaseTag="3.0.0")
        digest = "sha256:" + "1" * 64
        index = {"annotations": {release.ANNOTATION: json.dumps(inputs)},
                 "manifests": [{"platform": {"os": "linux", "architecture": arch}} for arch in ("amd64", "arm64")]}
        with tempfile.TemporaryDirectory() as folder:
            release.write(Path(folder) / "publication-inputs.json", inputs)
            release.write(Path(folder) / "selection.json", {"reuseDigest": digest})
            result = type("Result", (), {"stdout": json.dumps([[{"tag_name": "3.0.0", "draft": False,
                                                                   "prerelease": False, "published_at": "today"}]])})()
            with patch.object(release, "inspect", side_effect=[(index, digest), (index, digest), None]), \
                    patch.object(release, "command", return_value=result) as commands, \
                    patch.object(release, "resolve_release", return_value={"releaseTag": "3.0.0"}), \
                    patch.object(release, "approval_current", side_effect=[True, False]):
                release.publish("lantean-code/qbtmud", folder, True)
                self.assertFalse(any("create" in call.args for call in commands.call_args_list))

    def test_existing_immutable_requires_exact_inputs_and_platforms(self):
        index = {"annotations": {release.ANNOTATION: json.dumps(self.inputs)},
                 "manifests": [{"platform": {"os": "linux", "architecture": arch}} for arch in ("amd64", "arm64")]}
        release.validate_index(index, self.inputs)
        with self.assertRaises(ValueError):
            release.validate_index(index, dict(self.inputs, packagingCommit="d" * 40))
        duplicate = copy.deepcopy(index)
        duplicate["manifests"][1] = duplicate["manifests"][0]
        with self.assertRaises(ValueError):
            release.validate_index(duplicate, self.inputs)

    def test_registry_errors_do_not_become_missing_tags(self):
        for error in ("unauthorized", "connection refused", "TLS error"):
            result = type("Result", (), {"returncode": 1, "stderr": error})()
            with patch.object(release, "command", return_value=result), self.assertRaises(RuntimeError):
                release.inspect("ghcr.io/test/image:tag", missing=True)

    def test_skopeo_reads_pinned_references_without_the_optional_tag(self):
        digest = "sha256:" + "1" * 64
        responses = [type("Result", (), {"returncode": 0, "stdout": "{}"})(),
                     type("Result", (), {"stdout": digest})()]
        with patch.object(release, "command", side_effect=responses) as commands:
            self.assertEqual(release.inspect("localhost:5000/repo:version@" + digest), ({}, digest))
            self.assertTrue(all(call.args[-1] == "docker://localhost:5000/repo@" + digest
                                for call in commands.call_args_list))

    def test_oci_index_preserves_platform_manifest_digests_and_annotations(self):
        with tempfile.TemporaryDirectory() as directory:
            originals = []
            for arch in ("amd64", "arm64"):
                folder = Path(directory) / arch
                folder.mkdir()
                config = json.dumps({"architecture": arch, "os": "linux"}).encode()
                config_digest = "sha256:" + hashlib.sha256(config).hexdigest()
                manifest = json.dumps({"schemaVersion": 2, "config": {"digest": config_digest}, "layers": []}).encode()
                digest = "sha256:" + hashlib.sha256(manifest).hexdigest()
                originals.append(digest)
                descriptor = {"mediaType": "application/vnd.docker.distribution.manifest.v2+json",
                              "digest": digest, "size": len(manifest)}
                blobs = {"index.json": json.dumps({"manifests": [descriptor]}).encode(),
                         "oci-layout": b'{"imageLayoutVersion":"1.0.0"}',
                         "blobs/sha256/" + config_digest[7:]: config,
                         "blobs/sha256/" + digest[7:]: manifest}
                with tarfile.open(folder / "image.tar", "w") as archive:
                    for name, data in blobs.items():
                        member = tarfile.TarInfo(name)
                        member.size = len(data)
                        archive.addfile(member, io.BytesIO(data))
                release.write(folder / "tested.json", {"inputs": self.inputs, "architecture": arch,
                                                       "manifestDigest": digest, "imageId": config_digest})
            layout, digest = release.assemble(directory, self.inputs, release.ANNOTATION)
            data = (layout / "blobs/sha256" / digest[7:]).read_bytes()
            self.assertEqual("sha256:" + hashlib.sha256(data).hexdigest(), digest)
            index = json.loads(data)
            release.validate_index(index, self.inputs)
            self.assertEqual([item["digest"] for item in index["manifests"]], originals)
            # Byte corruption is refused even if test-evidence JSON still agrees.
            evidence = release.read(Path(directory) / "amd64/tested.json")
            evidence["imageId"] = "sha256:" + "0" * 64
            release.write(Path(directory) / "amd64/tested.json", evidence)
            with self.assertRaisesRegex(ValueError, "configuration differs"):
                release.assemble(directory, self.inputs, release.ANNOTATION)

    def test_release_requires_matching_stable_provenance_and_successful_run(self):
        data = b"verified zip fixture"
        digest = __import__("hashlib").sha256(data).hexdigest()
        asset = {"id": 2, "name": "qbt-mud-v3.0.0-intl.zip", "digest": "sha256:" + digest}
        document = {"draft": False, "prerelease": False, "published_at": "2026-10-13",
                    "id": 1, "assets": [asset, {"name": "qbt-mud-v3.0.0-provenance.json"}]}
        provenance = {"channel": "stable", "tag": "3.0.0", "version": "3.0.0", "commit": "a" * 40,
                      "archive": asset["name"], "sha256": digest, "runId": "123"}
        run = {"conclusion": "success", "head_sha": "a" * 40, "head_branch": "master",
               "event": "push", "path": ".github/workflows/dotnet.yml"}

        def api(path):
            if "/releases/tags/" in path:
                return document
            if "/commits/" in path:
                return {"sha": "a" * 40}
            return run

        with tempfile.TemporaryDirectory() as directory:
            def download(repo, tag, name, folder):
                path = Path(directory) / name
                path.write_bytes(json.dumps(provenance).encode() if name.endswith("json") else data)
                return path
            with patch.object(release, "gh", side_effect=api), patch.object(release, "download", side_effect=download):
                self.assertEqual(release.resolve_release("owner/repo", "3.0.0", directory)["uiCommit"], "a" * 40)
                provenance["channel"] = "rc"
                with self.assertRaises(ValueError):
                    release.resolve_release("owner/repo", "3.0.0", directory)
                provenance["channel"] = "stable"
                run["conclusion"] = "failure"
                with self.assertRaises(ValueError):
                    release.resolve_release("owner/repo", "3.0.0", directory)


if __name__ == "__main__":
    unittest.main()
