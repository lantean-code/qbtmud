"""Pinned supplemental evidence never changes or replaces release build provenance."""
import copy
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import release
import release_verification as verification


class VerificationTests(unittest.TestCase):
    def setUp(self):
        self.policy = verification.policy()
        self.assets = [{"id": index, "name": name, "digest": "sha256:" + checksum}
                       for index, (name, checksum) in enumerate(self.policy["archives"].items(), 10)]
        self.document = {"id": 123, "draft": False, "prerelease": False, "published_at": "today",
                         "assets": self.assets}
        self.run = {"id": 456, "run_attempt": 2, "head_sha": "a" * 40, "conclusion": "success",
                    "head_branch": "develop", "event": "workflow_dispatch", "path": verification.WORKFLOW,
                    "repository": {"full_name": verification.REPOSITORY},
                    "head_repository": {"full_name": verification.REPOSITORY}}
        self.artifacts = {"artifacts": [{"id": 789, "expired": False}]}
        self.evidence = dict(self.policy, kind="supplemental-verification", releaseId=123,
                             assets={asset["name"]: {"id": asset["id"], "sha256": asset["digest"][7:]}
                                     for asset in self.assets}, runId="456", runAttempt="2", verificationCommit="a" * 40)

    def resolve(self):
        return verification.resolve(verification.REPOSITORY, "3.2.0", self.policy["commit"], self.document)

    def test_successful_separate_evidence_is_not_reported_as_a_ui_build(self):
        with patch.object(verification, "gh", side_effect=[{"default_branch": "develop"},
                          {"workflow_runs": [self.run]}, self.artifacts]), \
                patch.object(verification, "command") as download, \
                patch.object(verification, "read", side_effect=[self.policy, self.evidence, self.policy]):
            result = self.resolve()
        self.assertEqual(result, {"uiVerificationRun": "456", "uiVerificationCommit": "a" * 40,
                                  "uiVerificationTestsCommit": self.policy["testsCommit"]})
        self.assertNotIn("uiBuildRun", result)
        self.assertIn("456", download.call_args.args)
        self.assertIn(verification.ARTIFACT, download.call_args.args)

    def test_only_the_exact_repository_tag_commit_and_archives_are_eligible(self):
        for field, value in (("repo", "another/repo"), ("tag", "3.2.1"), ("commit", "b" * 40),
                             ("digest", "sha256:" + "b" * 64), ("missing", None), ("duplicate", None)):
            with self.subTest(field=field):
                repo, tag, commit, assets = verification.REPOSITORY, "3.2.0", self.policy["commit"], copy.deepcopy(self.assets)
                if field == "repo":
                    repo = value
                elif field == "tag":
                    tag = value
                elif field == "commit":
                    commit = value
                elif field == "digest":
                    assets[0]["digest"] = value
                elif field == "missing":
                    assets.pop()
                else:
                    assets.append(assets[0])
                with self.assertRaises(ValueError):
                    verification.check_identity(repo, tag, commit, assets)

    def test_missing_or_untrusted_successful_runs_are_rejected(self):
        for field, value in (("conclusion", "failure"), ("head_branch", "feature/untrusted"),
                             ("event", "pull_request"), ("path", ".github/workflows/dotnet.yml"),
                             ("repository", {"full_name": "another/repo"}),
                             ("head_repository", {"full_name": "another/repo"})):
            with self.subTest(field=field):
                run = dict(self.run, **{field: value})
                with patch.object(verification, "gh", side_effect=[{"default_branch": "develop"},
                                  {"workflow_runs": [run]}]), self.assertRaisesRegex(ValueError, "trusted"):
                    self.resolve()
        with patch.object(verification, "gh", side_effect=[{"default_branch": "develop"},
                          {"workflow_runs": []}]), self.assertRaisesRegex(ValueError, "successfully"):
            self.resolve()

    def test_missing_duplicate_and_expired_artifacts_are_rejected(self):
        for artifacts in ([], [{"expired": True}], [{"expired": False}, {"expired": False}]):
            with self.subTest(artifacts=artifacts), \
                    patch.object(verification, "gh", side_effect=[{"default_branch": "develop"},
                                 {"workflow_runs": [self.run]}, {"artifacts": artifacts}]), \
                    self.assertRaisesRegex(ValueError, "missing or expired"):
                self.resolve()

    def test_evidence_must_match_every_pinned_field_and_the_successful_attempt(self):
        for field in self.evidence:
            with self.subTest(field=field):
                altered = dict(self.evidence, **{field: "incorrect"})
                with patch.object(verification, "gh", side_effect=[{"default_branch": "develop"},
                                  {"workflow_runs": [self.run]}, self.artifacts]), \
                        patch.object(verification, "command"), \
                        patch.object(verification, "read", side_effect=[self.policy, altered, self.policy]), \
                        self.assertRaisesRegex(ValueError, "does not match"):
                    self.resolve()

    def test_staging_writes_only_the_pinned_test_file(self):
        with tempfile.TemporaryDirectory() as directory:
            test = Path(directory) / self.policy["testFile"]
            test.parent.mkdir(parents=True)
            test.write_bytes(b"old tests")
            with patch.object(verification, "check_source") as check, \
                    patch.object(verification, "test_bytes", return_value=b"pinned tests"):
                verification.stage(directory)
                self.assertEqual(test.read_bytes(), b"pinned tests")
                self.assertEqual(check.call_args_list[0].kwargs, {"staged": False})
                self.assertEqual(check.call_count, 2)

    def test_source_checks_reject_changed_head_or_other_tracked_files(self):
        for responses in (("b" * 40,), (self.policy["commit"], "src/changed.cs"),
                          (self.policy["commit"], self.policy["testFile"] + "\nsrc/changed.cs")):
            with self.subTest(responses=responses), patch.object(verification, "command", side_effect=responses), \
                    self.assertRaises(ValueError):
                verification.check_source("unused")

    def test_staging_requires_a_clean_release_checkout(self):
        with patch.object(verification, "command", side_effect=[self.policy["commit"], ""]):
            verification.check_source("unused", staged=False)
        with patch.object(verification, "command", side_effect=[self.policy["commit"], self.policy["testFile"]]), \
                self.assertRaisesRegex(ValueError, "Only the pinned"):
            verification.check_source("unused", staged=False)

    def test_source_checks_compare_exact_test_content_and_accept_checkout_line_endings(self):
        with tempfile.TemporaryDirectory() as directory:
            test = Path(directory) / self.policy["testFile"]
            test.parent.mkdir(parents=True)
            test.write_bytes(b"pinned\r\ntests\r\n")
            with patch.object(verification, "command", side_effect=[self.policy["commit"], self.policy["testFile"]]), \
                    patch.object(verification, "test_bytes", return_value=b"pinned\ntests\n"):
                verification.check_source(directory)
            test.write_bytes(b"changed tests")
            with patch.object(verification, "command", side_effect=[self.policy["commit"], self.policy["testFile"]]), \
                    patch.object(verification, "test_bytes", return_value=b"pinned\ntests\n"), \
                    self.assertRaisesRegex(ValueError, "tests differ"):
                verification.check_source(directory)

    def test_record_checks_both_downloaded_archives_and_keeps_evidence_separate(self):
        environment = {"GITHUB_REPOSITORY": verification.REPOSITORY, "GITHUB_RUN_ID": "456",
                       "GITHUB_RUN_ATTEMPT": "2", "GITHUB_SHA": "a" * 40}
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "evidence" / "verification.json"
            def download(*args):
                name = args[args.index("--pattern") + 1]
                folder = args[args.index("--dir") + 1]
                (Path(folder) / name).write_bytes(b"archive")
            with patch.dict(os.environ, environment), patch.object(verification, "check_source"), \
                    patch.object(verification, "gh", side_effect=[dict(self.document, draft=True),
                                  {"sha": self.policy["commit"]}]), \
                    patch.object(verification, "command", side_effect=download) as commands, \
                    patch.object(verification.hashlib, "sha256") as checksum:
                checksum.return_value.hexdigest.side_effect = list(self.policy["archives"].values())
                verification.record("unused", output)
            self.assertEqual(json.loads(output.read_text(encoding="utf-8")), self.evidence)
            self.assertEqual(commands.call_count, 2)
            self.assertTrue(all(call.args[:3] == ("gh", "release", "download") for call in commands.call_args_list))

    def test_record_rejects_mismatched_archive_bytes(self):
        with tempfile.TemporaryDirectory() as directory:
            def download(*args):
                (Path(args[-1]) / args[args.index("--pattern") + 1]).write_bytes(b"wrong archive")
            with patch.dict(os.environ, {"GITHUB_REPOSITORY": verification.REPOSITORY}), \
                    patch.object(verification, "check_source"), \
                    patch.object(verification, "gh", side_effect=[self.document, {"sha": self.policy["commit"]}]), \
                    patch.object(verification, "command", side_effect=download), \
                    self.assertRaisesRegex(ValueError, "checksum mismatch"):
                verification.record("unused", Path(directory) / "verification.json")

    def test_record_rejects_a_prerelease(self):
        with patch.dict(os.environ, {"GITHUB_REPOSITORY": verification.REPOSITORY}), \
                patch.object(verification, "check_source"), \
                patch.object(verification, "gh", side_effect=[dict(self.document, prerelease=True),
                              {"sha": self.policy["commit"]}]), \
                self.assertRaisesRegex(ValueError, "stable"):
            verification.record("unused", "unused")

    def test_publisher_requires_published_stable_release_even_with_verification(self):
        for changed in ({"draft": True}, {"prerelease": True}, {"published_at": None}):
            with self.subTest(changed=changed), \
                    patch.object(release, "gh", return_value=dict(self.document, **changed)), \
                    patch.object(release, "resolve_verification") as resolve, self.assertRaisesRegex(ValueError, "published"):
                release.resolve_release(verification.REPOSITORY, "3.2.0", "unused")
            resolve.assert_not_called()

    def test_publisher_records_verification_and_still_checks_downloaded_zip(self):
        asset = self.assets[1]
        for correct in (True, False):
            with self.subTest(correct=correct), tempfile.TemporaryDirectory() as directory:
                archive = Path(directory) / asset["name"]
                archive.write_bytes(b"archive")
                separate = {"uiVerificationRun": "456"}
                with patch.object(release, "gh", side_effect=[self.document, {"sha": self.policy["commit"]}]), \
                        patch.object(release, "resolve_verification", return_value=separate), \
                        patch.object(release, "download", return_value=archive), \
                        patch.object(release.hashlib, "sha256") as checksum:
                    checksum.return_value.hexdigest.return_value = asset["digest"][7:] if correct else "b" * 64
                    if correct:
                        result = release.resolve_release(verification.REPOSITORY, "3.2.0", directory)
                        self.assertEqual(result["uiVerificationRun"], "456")
                        self.assertNotIn("uiBuildRun", result)
                    else:
                        with self.assertRaisesRegex(ValueError, "checksum mismatch"):
                            release.resolve_release(verification.REPOSITORY, "3.2.0", directory)


if __name__ == "__main__":
    unittest.main()
