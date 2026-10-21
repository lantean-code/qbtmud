#!/usr/bin/env python3
"""Verified stable release inputs and immutable GHCR promotion policy."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
from oci_index import assemble

ROOT = Path(__file__).resolve().parent
ANNOTATION = "io.github.lantean-code.qbtmud.inputs"


def command(*args, check=True):
    result = subprocess.run(args, capture_output=True, text=True, encoding="utf-8")
    if check and result.returncode:
        raise RuntimeError(result.stderr.strip() or "Command failed: " + args[0])
    return result


def gh(path):
    return json.loads(command("gh", "api", path).stdout)


def stable_version(tag):
    match = re.fullmatch(r"v?((?:0|[1-9]\d*)\.(?:0|[1-9]\d*)\.(?:0|[1-9]\d*))", tag)
    if not match:
        raise ValueError("Only stable major.minor.patch releases are eligible")
    return match[1]


def version_key(version):
    return tuple(map(int, stable_version(version).split(".")))


def upstream_key(tag):
    match = re.fullmatch(r"(\d+\.\d+\.\d+)_v(\d+\.\d+\.\d+)-ls(\d+)", tag)
    if not match:
        raise ValueError("Unsupported LinuxServer tag syntax; update the explicit mapping")
    return (*map(int, match[1].split(".")), *map(int, match[2].split(".")), int(match[3]))


def combination(version, base, revision):
    upstream_key(base)
    if type(revision) is not int or revision < 1:
        raise ValueError("Packaging revision must be a positive integer")
    qbt, rest = base.split("_v")
    lt, build = rest.split("-ls")
    return f"{stable_version(version)}-qbt{qbt}-lt{lt}-ls{build}-r{revision}"


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def write(path, value):
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    Path(path).write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def download(repo, tag, name, directory):
    command("gh", "release", "download", tag, "--repo", repo, "--pattern", name,
            "--dir", str(directory), "--clobber")
    return Path(directory) / name


def resolve_release(repo, tag, directory):
    version = stable_version(tag)
    release = gh(f"repos/{repo}/releases/tags/{tag}")
    if release["draft"] or release["prerelease"] or not release["published_at"]:
        raise ValueError("Release must be published and stable")
    commit = gh(f"repos/{repo}/commits/{tag}")["sha"]
    assets = [a for a in release["assets"] if a["name"] == f"qbt-mud-v{version}-intl.zip"]
    if len(assets) != 1 or not re.fullmatch(r"sha256:[a-f0-9]{64}", assets[0].get("digest") or ""):
        raise ValueError("Expected one international ZIP with a GitHub SHA-256 digest")
    asset = assets[0]
    provenance_name = f"qbt-mud-v{version}-provenance.json"
    manifests = [a for a in release["assets"] if a["name"] == provenance_name]
    if len(manifests) == 1:
        provenance = read(download(repo, tag, provenance_name, directory))
        if (provenance.get("channel") != "stable" or provenance.get("tag") != tag
                or provenance.get("commit") != commit or provenance.get("version") != version
                or provenance.get("archive") != asset["name"]
                or "sha256:" + provenance.get("sha256", "") != asset["digest"]):
            raise ValueError("Release provenance does not match the stable asset and tag")
        run_id = provenance["runId"]
    else:
        # Explicitly reviewed bootstrap evidence for the pre-manifest 3.0.0 release.
        legacy = read(ROOT / "ui-release.json")
        if (version != legacy["version"] or commit != legacy["commit"]
                or asset["digest"] != "sha256:" + legacy["sha256"]):
            raise ValueError("Release has no verified build provenance")
        run_id = legacy["buildRun"].rsplit("/", 1)[1]
    run = gh(f"repos/{repo}/actions/runs/{run_id}")
    if (run["conclusion"] != "success" or run["head_sha"] != commit
            or run["head_branch"] != "master" or run["event"] != "push"
            or run["path"] != ".github/workflows/dotnet.yml"):
        raise ValueError("Provenance must identify a successful stable master UI build")
    archive = download(repo, tag, asset["name"], directory)
    if hashlib.sha256(archive.read_bytes()).hexdigest() != asset["digest"][7:]:
        raise ValueError("Downloaded release ZIP checksum mismatch")
    return {"version": version, "releaseTag": tag, "releaseId": release["id"],
            "assetId": asset["id"], "archive": asset["name"], "uiSha256": asset["digest"][7:],
            "uiCommit": commit, "uiBuildRun": str(run_id)}


def inspect(reference, missing=False, anonymous=False):
    # Docker accepts tag@digest, but Skopeo's docker transport requires name@digest.
    if "@" in reference:
        name, digest = reference.rsplit("@", 1)
        if ":" in name.rsplit("/", 1)[-1]:
            name = name.rsplit(":", 1)[0]
        reference = name + "@" + digest
    with tempfile.TemporaryDirectory() as directory:
        auth = Path(directory) / "auth.json" if anonymous else Path.home() / ".docker/config.json"
        if anonymous:
            write(auth, {"auths": {}})
        args = ["skopeo", "inspect"]
        if auth.exists():
            args += ["--authfile", str(auth)]
        result = command(*args, "--raw", "docker://" + reference, check=False)
    if result.returncode:
        if missing and re.search(r"manifest unknown|name unknown", result.stderr, re.I):
            return None
        raise RuntimeError("Registry lookup failed: " + result.stderr.strip())
    raw = result.stdout
    # Ask the registry transport for its digest; CLI whitespace is not manifest data.
    args = ["skopeo", "inspect"]
    if anonymous:
        with tempfile.TemporaryDirectory() as directory:
            auth = Path(directory) / "auth.json"
            write(auth, {"auths": {}})
            digest = command(*args, "--authfile", str(auth), "--format", "{{.Digest}}", "docker://" + reference).stdout.strip()
    else:
        auth = Path.home() / ".docker/config.json"
        if auth.exists():
            args += ["--authfile", str(auth)]
        digest = command(*args, "--format", "{{.Digest}}", "docker://" + reference).stdout.strip()
    if not re.fullmatch(r"sha256:[a-f0-9]{64}", digest):
        raise ValueError("Registry did not return a valid image digest")
    return json.loads(raw), digest


def validate_index(index, expected):
    recorded = json.loads(index.get("annotations", {}).get(ANNOTATION, "null"))
    if recorded != expected:
        raise ValueError("Immutable tag inputs differ; increment the packaging revision")
    platforms = [(m.get("platform", {}).get("os"), m.get("platform", {}).get("architecture"))
                 for m in index.get("manifests", [])]
    if sorted(platforms) != [("linux", "amd64"), ("linux", "arm64")]:
        raise ValueError("Expected exactly the tested amd64 and arm64 manifests")


def can_promote(candidate, current):
    if current is None:
        return True
    if version_key(current["version"]) > version_key(candidate["version"]):
        return False
    if current["version"] == candidate["version"]:
        old, new = upstream_key(current["upstreamTag"]), upstream_key(candidate["upstreamTag"])
        if old > new or (old == new and current["revision"] > candidate["revision"]):
            return False
        if old == new and current["revision"] == candidate["revision"] and current != candidate:
            raise ValueError("Moving alias has conflicting inputs at the same revision")
    return True


def output(name, value):
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as stream:
            stream.write(f"{name}={value}\n")


def validate_base(base):
    if base["image"] != f"lscr.io/linuxserver/qbittorrent:{base['tag']}@{base['digest']}":
        raise ValueError("Base image pin is inconsistent")
    key = upstream_key(base["tag"])
    if key[3] != 2 or not re.fullmatch(r"sha256:[a-f0-9]{64}", base["digest"]):
        raise ValueError("Only stable libtorrent 2 bases are supported")
    index, digest = inspect(base["image"])
    platforms = [(m.get("platform", {}).get("os"), m.get("platform", {}).get("architecture"))
                 for m in index.get("manifests", [])]
    if digest != base["digest"] or any(platforms.count(("linux", arch)) != 1 for arch in ("amd64", "arm64")):
        raise ValueError("Pinned base must resolve to an index supporting both requested platforms")
    return base


def resolve_upstream():
    reference = "lscr.io/linuxserver/qbittorrent"
    _, digest = inspect(reference + ":latest")
    versions = []
    for arch in ("amd64", "arm64"):
        config = json.loads(command("skopeo", "inspect", "--override-os", "linux", "--override-arch", arch,
                                    "--config", "docker://" + reference + "@" + digest).stdout)
        labels = config["config"]["Labels"]
        version = labels.get("org.opencontainers.image.version", "")
        build = re.fullmatch(r"Linuxserver\.io version:- (\S+) Build-date:- \S+", labels.get("build_version", ""))
        if not build or build[1] != version:
            raise ValueError("Upstream version/build labels do not agree")
        upstream_key(version)
        versions.append(version)
    if versions[0] != versions[1]:
        raise ValueError("Upstream architectures identify different versions")
    version = versions[0]
    _, named_digest = inspect(reference + ":" + version)
    if named_digest != digest:
        raise ValueError("Latest and the upstream version tag identify different indexes; retry later")
    return validate_base({"tag": version, "digest": digest, "image": f"{reference}:{version}@{digest}"})


def published_inputs(image):
    existing = inspect(image, missing=True)
    if existing is None:
        return None
    inputs = json.loads(existing[0].get("annotations", {}).get(ANNOTATION, "null"))
    if inputs is None:
        raise ValueError("Published image has no verified source inputs")
    validate_index(existing[0], inputs)
    return inputs


def approved_base(config, current):
    base = read(ROOT / "base-image.json")
    if current:
        old, new = upstream_key(current["upstreamTag"]), upstream_key(base["tag"])
        # Preserve the tested pin even when repackaging; a stale bootstrap digest
        # must not undo an automatic same-tag upstream update.
        if old >= new:
            base = {"tag": current["upstreamTag"], "digest": current["upstreamDigest"], "image": current["baseImage"]}
    return base


def select_base(config, current, upstream=False):
    approved = approved_base(config, current)
    base = resolve_upstream() if upstream else validate_base(approved)
    if upstream_key(base["tag"]) < upstream_key(approved["tag"]):
        raise ValueError("Upstream latest moved backwards; automatic downgrade refused")
    return base


def current_stable(repo):
    releases = json.loads(command("gh", "api", "--paginate", "--slurp", f"repos/{repo}/releases?per_page=100").stdout)
    stable = []
    for item in sum(releases, []):
        try:
            version = stable_version(item["tag_name"])
        except ValueError:
            continue
        if not item["draft"] and not item["prerelease"] and item["published_at"]:
            stable.append((version_key(version), item["tag_name"], item))
    if not stable:
        raise ValueError("No published stable UI release is available")
    return max(stable, key=lambda value: value[:2])[2]


def latest_release(repo, directory):
    return resolve_release(repo, current_stable(repo)["tag_name"], directory)


def approval_current(repo, inputs, selection=None, latest=False):
    repository = gh(f"repos/{repo}")
    head = gh(f"repos/{repo}/commits/{repository['default_branch']}")["sha"]
    if head != inputs["packagingCommit"]:
        return False
    if selection is not None:
        config = read(ROOT / "publication.json")
        current = published_inputs(config["image"] + ":latest")
        base = select_base(config, current, selection.get("upstream", False))
        if base["image"] != inputs["baseImage"]:
            return False
    if latest and current_stable(repo)["tag_name"] != inputs["releaseTag"]:
        return False
    # Upstream/registry lookups can take time; recheck packaging after them too.
    return gh(f"repos/{repo}/commits/{repository['default_branch']}")["sha"] == inputs["packagingCommit"]


def prepare(repo, tag, directory, upstream=False):
    config = read(ROOT / "publication.json")
    if repo != "lantean-code/qbtmud" or config["image"] != "ghcr.io/" + repo:
        raise ValueError("Publication is restricted to the approved repository/package")
    inputs = latest_release(repo, directory) if upstream else resolve_release(repo, tag, directory)
    current = published_inputs(config["image"] + ":latest")
    base = select_base(config, current, upstream)
    if upstream and current and all(current.get(key) == value for key, value in inputs.items()) and base["image"] == current["baseImage"]:
        output("build_required", "false")
        print("LinuxServer base and stable UI are unchanged; no build or publication required")
        return
    inputs.update(baseImage=base["image"], upstreamTag=base["tag"], upstreamDigest=base["digest"],
                  revision=config["revision"], packagingCommit=command("git", "rev-parse", "HEAD").stdout.strip())
    if command("git", "status", "--porcelain", "--ignore-submodules").stdout.strip():
        raise ValueError("Publication requires a clean committed packaging tree")
    if upstream and current and current["version"] == inputs["version"] and current["upstreamTag"] == base["tag"]:
        # Never reuse a lower revision merely because old registry tags were deleted.
        inputs["revision"] = max(inputs["revision"], current["revision"] + 1)
    tag = combination(inputs["version"], base["tag"], inputs["revision"])
    existing = inspect(config["image"] + ":" + tag, missing=True)
    if upstream:
        # Same-tag digest updates and conflicting retained combinations get a new
        # identity automatically; matching interrupted publications are reused.
        for _ in range(1000):
            if existing is None:
                break
            recorded = json.loads(existing[0].get("annotations", {}).get(ANNOTATION, "null"))
            if recorded == inputs:
                validate_index(existing[0], inputs)
                break
            inputs["revision"] += 1
            tag = combination(inputs["version"], base["tag"], inputs["revision"])
            existing = inspect(config["image"] + ":" + tag, missing=True)
        else:
            raise ValueError("No free packaging revision found; manual intervention required")
    elif existing:
        validate_index(existing[0], inputs)
    write(Path(directory) / "publication-inputs.json", inputs)
    write(Path(directory) / "selection.json", {"reuseDigest": existing[1] if existing else None, "upstream": upstream})
    output("build_required", "true")
    output("reuse", "true" if existing else "false")
    output("digest", existing[1] if existing else "")
    output("packaging_commit", inputs["packagingCommit"])
    output("tag", tag)


def publish(repo, directory, reuse):
    directory = Path(directory)
    inputs = read(directory / "publication-inputs.json")
    image = read(ROOT / "publication.json")["image"]
    tag = combination(inputs["version"], inputs["upstreamTag"], inputs["revision"])
    existing = inspect(image + ":" + tag, missing=True)
    selection = read(directory / "selection.json")
    selected_digest = selection["reuseDigest"]
    if existing:
        if not reuse or existing[1] != selected_digest:
            raise ValueError("Immutable index changed since native testing; rerun preparation and tests")
        validate_index(existing[0], inputs)
        digest = existing[1]
    else:
        if reuse:
            raise ValueError("Previously published immutable index disappeared")
        layout, retained_digest = assemble(directory, inputs, ANNOTATION)
        command("skopeo", "copy", "--all", "--preserve-digests", "--authfile", str(Path.home() / ".docker/config.json"),
                "oci:" + str(layout) + ":public", "docker://" + image + ":" + tag)
        index, digest = inspect(image + ":" + tag)
        if digest != retained_digest:
            raise ValueError("Registry transfer changed the tested index")
        validate_index(index, inputs)
    write(directory / "published.json", {"image": image, "tag": tag, "digest": digest, "inputs": inputs})
    # Re-test an existing immutable index on retries; each native job does that before this job.
    # Require anonymous access before advertising either convenience alias.
    index, anonymous_digest = inspect(image + "@" + digest, anonymous=True)
    if anonymous_digest != digest:
        raise ValueError("Anonymous manifest lookup changed the digest")
    with tempfile.TemporaryDirectory() as config:
        command("docker", "--config", config, "pull", "--platform", "linux/amd64", image + "@" + digest)
    fresh = resolve_release(repo, inputs["releaseTag"], directory / "recheck")
    if any(inputs[key] != value for key, value in fresh.items()):
        raise ValueError("Release inputs changed during publication; aliases were not updated")
    if not approval_current(repo, inputs, selection):
        print("Packaging approval changed; immutable image retained, aliases skipped")
        return
    newest = latest_release(repo, directory / "latest")
    for alias in (inputs["version"], "latest"):
        if alias == "latest" and newest["releaseTag"] != inputs["releaseTag"]:
            print("A newer stable UI is selected; latest skipped")
            continue
        current = inspect(image + ":" + alias, missing=True)
        recorded = None
        if current:
            recorded = json.loads(current[0].get("annotations", {}).get(ANNOTATION, "null"))
            if recorded is None:
                raise ValueError("Existing alias has no provenance; explicit migration is required")
        if can_promote(inputs, recorded):
            # Repeat after release downloads and registry lookups, immediately before each write.
            if not approval_current(repo, inputs, selection, latest=(alias == "latest")):
                print("Publication selection changed; remaining aliases skipped")
                break
            command("docker", "buildx", "imagetools", "create", "--tag", image + ":" + alias, image + "@" + digest)
            if inspect(image + ":" + alias)[1] != digest:
                raise ValueError("Alias digest differs from the tested immutable index")
            print(f"Published {image}:{alias}@{digest}")
        else:
            print(f"Skipped {alias}: a newer selection is already published")
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as stream:
            stream.write(f"Published `{image}:{tag}@{digest}`\n\n```json\n{json.dumps(inputs, indent=2)}\n```\n")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("operation", choices=("prepare", "publish"))
    parser.add_argument("--repo", required=True)
    parser.add_argument("--tag")
    parser.add_argument("--directory", default="docker/.build/publication")
    parser.add_argument("--reuse", action="store_true")
    parser.add_argument("--upstream", action="store_true")
    args = parser.parse_args()
    if args.operation == "prepare":
        prepare(args.repo, args.tag, args.directory, args.upstream)
    else:
        publish(args.repo, args.directory, args.reuse)
