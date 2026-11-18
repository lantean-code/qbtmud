"""Supplemental verification of the unchanged, manually packaged 3.2.0 release."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile
from urllib.parse import quote

REPOSITORY = "lantean-code/qbtmud"
WORKFLOW = ".github/workflows/verify-release-3.2.0.yml"
ARTIFACT = "release-3.2.0-verification"
ROOT = Path(__file__).resolve().parent


def command(*args):
    return subprocess.check_output(args, text=True, encoding="utf-8").strip()


def gh(path):
    return json.loads(command("gh", "api", path))


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def policy():
    return read(ROOT / "verification-3.2.0.json")


def check_identity(repo, tag, commit, assets):
    expected = policy()
    if repo != REPOSITORY or tag != expected["tag"] or commit != expected["commit"]:
        raise ValueError("Supplemental verification is restricted to the pinned 3.2.0 release")
    selected = {}
    for name, checksum in expected["archives"].items():
        matches = [asset for asset in assets if asset["name"] == name]
        if len(matches) != 1 or matches[0].get("digest") != "sha256:" + checksum:
            raise ValueError("Release archive differs from the approved 3.2.0 checksum")
        selected[name] = {"id": matches[0]["id"], "sha256": checksum}
    return selected


def test_bytes(source):
    expected = policy()
    return subprocess.check_output(["git", "-C", str(source), "show",
                                    expected["testsCommit"] + ":" + expected["testFile"]])


def check_source(source, staged=True):
    expected = policy()
    if command("git", "-C", str(source), "rev-parse", "HEAD") != expected["commit"]:
        raise ValueError("Verification source must remain at the pinned release commit")
    changed = command("git", "-C", str(source), "diff", "HEAD", "--name-only", "--ignore-submodules").splitlines()
    if changed != ([expected["testFile"]] if staged else []):
        raise ValueError("Only the pinned supplementary test file may differ from the release")
    if staged:
        actual = (Path(source) / expected["testFile"]).read_bytes().replace(b"\r\n", b"\n")
        if actual != test_bytes(source).replace(b"\r\n", b"\n"):
            raise ValueError("Supplementary tests differ from the pinned test commit")


def stage(source):
    check_source(source, staged=False)
    (Path(source) / policy()["testFile"]).write_bytes(test_bytes(source))
    check_source(source)


def record(source, output):
    check_source(source)
    expected = policy()
    repo = os.environ["GITHUB_REPOSITORY"]
    document = gh(f"repos/{repo}/releases/{expected['releaseId']}")
    commit = gh(f"repos/{repo}/commits/{expected['tag']}")["sha"]
    if document["prerelease"] or document["tag_name"] != expected["tag"] or document["id"] != expected["releaseId"]:
        raise ValueError("Verification requires the stable 3.2.0 release")
    assets = check_identity(repo, expected["tag"], commit, document["assets"])
    with tempfile.TemporaryDirectory() as directory:
        for name, asset in assets.items():
            command("gh", "release", "download", expected["tag"], "--repo", repo,
                    "--pattern", name, "--dir", directory)
            if hashlib.sha256((Path(directory) / name).read_bytes()).hexdigest() != asset["sha256"]:
                raise ValueError("Downloaded verification archive checksum mismatch")
    evidence = dict(expected, kind="supplemental-verification", releaseId=document["id"], assets=assets,
                    runId=os.environ["GITHUB_RUN_ID"], runAttempt=os.environ["GITHUB_RUN_ATTEMPT"],
                    verificationCommit=os.environ["GITHUB_SHA"])
    Path(output).parent.mkdir(parents=True, exist_ok=True)
    Path(output).write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")


def resolve(repo, tag, commit, document):
    assets = check_identity(repo, tag, commit, document["assets"])
    if document["id"] != policy()["releaseId"]:
        raise ValueError("Supplemental verification requires the original 3.2.0 release identity")
    branch = gh(f"repos/{repo}")["default_branch"]
    runs = gh(f"repos/{repo}/actions/workflows/verify-release-3.2.0.yml/runs"
              f"?event=workflow_dispatch&status=success&branch={quote(branch, safe='')}&per_page=1")["workflow_runs"]
    if not runs:
        raise ValueError("Run Verify unchanged 3.2.0 release successfully from the default branch first")
    run = runs[0]
    if (run["conclusion"] != "success" or run["head_branch"] != branch
            or run["event"] != "workflow_dispatch" or run["path"] != WORKFLOW
            or run["repository"]["full_name"] != repo or run["head_repository"]["full_name"] != repo):
        raise ValueError("Supplemental evidence requires a successful trusted verification workflow")
    artifacts = gh(f"repos/{repo}/actions/runs/{run['id']}/artifacts?name={ARTIFACT}")["artifacts"]
    if len(artifacts) != 1 or artifacts[0]["expired"]:
        raise ValueError("Supplemental verification evidence is missing or expired; rerun verification")
    with tempfile.TemporaryDirectory() as directory:
        command("gh", "run", "download", str(run["id"]), "--repo", repo,
                "--name", ARTIFACT, "--dir", directory)
        evidence = read(Path(directory) / "verification.json")
    expected = dict(policy(), kind="supplemental-verification", releaseId=document["id"], assets=assets,
                    runId=str(run["id"]), runAttempt=str(run["run_attempt"]), verificationCommit=run["head_sha"])
    if evidence != expected:
        raise ValueError("Supplemental evidence does not match the release, tests and workflow attempt")
    return {"uiVerificationRun": str(run["id"]), "uiVerificationCommit": run["head_sha"],
            "uiVerificationTestsCommit": expected["testsCommit"]}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("operation", choices=("stage", "record"))
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--output", default="verification-evidence/verification.json")
    args = parser.parse_args()
    if args.operation == "stage":
        stage(args.source)
    else:
        record(args.source, args.output)
