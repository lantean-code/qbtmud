"""Select the complete release diff for RC JavaScript coverage checks."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys


def git(root, *arguments):
    return subprocess.check_output(["git", "-C", str(root), *arguments], text=True,
                                   encoding="utf-8", stderr=subprocess.PIPE).strip()


def release_branch(branch):
    return branch.startswith(("release/", "hotfix/"))


def select_base(root, ref, base_ref="", head_ref=""):
    branch = ref.removeprefix("refs/heads/")
    if any(release_branch(name) for name in (branch, base_ref, head_ref)):
        comparison = "refs/remotes/origin/master"
        base = git(root, "merge-base", "HEAD", comparison)
        reason = "Full release comparison against master"
    elif base_ref:
        comparison = "refs/remotes/origin/" + base_ref
        base = git(root, "merge-base", "HEAD", comparison)
        reason = "Pull request comparison against " + base_ref
    else:
        comparison = "HEAD^"
        base = git(root, "rev-parse", "--verify", "HEAD^{commit}^")
        reason = "Comparison against the previous commit"
    return {"base": base, "head": git(root, "rev-parse", "--verify", "HEAD^{commit}"),
            "comparison": comparison, "reason": reason}


def main(arguments=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--output", type=Path)
    options = parser.parse_args(arguments)
    try:
        selected = select_base(options.repo_root, os.environ.get("GITHUB_REF", ""),
                               os.environ.get("GITHUB_BASE_REF", ""), os.environ.get("GITHUB_HEAD_REF", ""))
        if options.output:
            options.output.parent.mkdir(parents=True, exist_ok=True)
            options.output.write_text(json.dumps(selected, indent=2) + "\n", encoding="utf-8")
        print(selected["reason"] + ": " + selected["base"], file=sys.stderr)
        print(selected["base"])
        return 0
    except (OSError, subprocess.CalledProcessError) as error:
        print("Unable to select JavaScript coverage base; full Git history and origin/master are required: "
              + str(error), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
