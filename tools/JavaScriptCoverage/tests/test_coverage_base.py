import contextlib
import importlib.util
import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("coverage_base", ROOT / "coverage_base.py")
coverage_base = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(coverage_base)
REPORT_SPEC = importlib.util.spec_from_file_location("coverage_report", ROOT / "report.py")
report = importlib.util.module_from_spec(REPORT_SPEC)
REPORT_SPEC.loader.exec_module(report)


class CoverageBaseTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.root = Path(directory.name)
        self.git("init", "--initial-branch=master")
        self.git("config", "user.name", "Coverage Tests")
        self.git("config", "user.email", "coverage@example.test")
        self.git("config", "commit.gpgsign", "false")
        self.git("config", "core.autocrlf", "false")
        self.commit("initial.txt", "Stable release\n")
        self.stable = self.git("rev-parse", "HEAD")
        self.git("update-ref", "refs/remotes/origin/master", self.stable)
        self.git("checkout", "-b", "release/3.3.0")
        self.path = report.SCRIPT_PATHS["torrent-split-panel.module.js"]
        self.commit(self.path, "covered();\nmissed();\n")
        self.feature = self.git("rev-parse", "HEAD")
        self.commit("notes.txt", "Latest RC only changes documentation\n")

    def git(self, *arguments):
        return subprocess.check_output(["git", "-C", str(self.root), *arguments],
                                       text=True, encoding="utf-8", stderr=subprocess.PIPE).strip()

    def commit(self, name, content):
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
        self.git("add", "--", name)
        self.git("commit", "-m", "Update " + name)

    def test_given_release_and_hotfix_builds_when_selected_then_include_all_unreleased_commits(self):
        for branch in ("release/3.3.0", "hotfix/3.2.1"):
            with self.subTest(branch=branch):
                selected = coverage_base.select_base(self.root, "refs/heads/" + branch)
                self.assertEqual(selected["base"], self.stable)
                self.assertEqual(selected["comparison"], "refs/remotes/origin/master")
                self.assertIn("Full release", selected["reason"])
                self.assertEqual(report.changed_lines(self.root, selected["base"])[self.path], {1, 2})

    def test_given_pull_requests_into_or_from_rc_branches_when_selected_then_check_the_full_release(self):
        for target, source in (("release/3.3.0", "feature/details"), ("hotfix/3.2.1", "fix/details"),
                               ("master", "release/3.3.0"), ("master", "hotfix/3.2.1")):
            with self.subTest(target=target, source=source):
                self.assertEqual(coverage_base.select_base(self.root, "refs/pull/1/merge", target, source)["base"], self.stable)

    def test_given_ordinary_pull_request_when_selected_then_preserves_target_branch_comparison(self):
        self.git("update-ref", "refs/remotes/origin/develop", self.feature)
        selected = coverage_base.select_base(self.root, "refs/pull/1/merge", "develop", "feature/details")
        self.assertEqual(selected["base"], self.feature)
        self.assertEqual(selected["comparison"], "refs/remotes/origin/develop")

    def test_given_stable_merge_when_selected_then_compares_with_the_previous_master_commit(self):
        self.git("checkout", "master")
        self.git("merge", "--no-ff", "release/3.3.0", "-m", "Release 3.3.0")
        selected = coverage_base.select_base(self.root, "refs/heads/master")
        self.assertEqual(selected["base"], self.stable)
        self.assertEqual(selected["head"], self.git("rev-parse", "HEAD"))

    def test_given_non_rc_manual_build_when_selected_then_preserves_previous_commit_comparison(self):
        for branch in ("develop", "feature/details", "release-notes", "hotfixes"):
            with self.subTest(branch=branch):
                self.assertEqual(coverage_base.select_base(self.root, "refs/heads/" + branch)["base"], self.feature)

    def test_given_master_advanced_after_branching_when_selected_then_uses_shared_ancestor(self):
        self.git("checkout", "master")
        self.commit("another-release.txt", "Independent master change\n")
        self.git("update-ref", "refs/remotes/origin/master", "HEAD")
        self.git("checkout", "release/3.3.0")
        self.assertEqual(coverage_base.select_base(self.root, "refs/heads/release/3.3.0")["base"], self.stable)

    def test_given_earlier_uncovered_change_when_latest_commit_has_no_javascript_then_rc_gate_still_fails(self):
        scripts = {"torrent-split-panel.module.js": {"path": self.path, "lines": {1: 1, 2: 0}, "ranges": {"script": 1}}}
        previous_commit = report.changed_lines(self.root, self.git("rev-parse", "HEAD^"))
        self.assertTrue(report.make_summary(scripts, 1, previous_commit)["passed"])
        selected = coverage_base.select_base(self.root, "refs/heads/release/3.3.0")
        changes = report.changed_lines(self.root, selected["base"])
        result = report.make_summary(scripts, 1, changes)
        self.assertFalse(result["passed"])
        self.assertEqual(result["uncovered_changed_lines"], {self.path: [2]})

    def test_given_missing_master_when_selected_then_fails_without_falling_back_to_latest_commit(self):
        self.git("update-ref", "-d", "refs/remotes/origin/master")
        with self.assertRaises(subprocess.CalledProcessError):
            coverage_base.select_base(self.root, "refs/heads/release/3.3.0")

    def test_given_unrelated_master_history_when_selected_then_fails_without_a_narrower_comparison(self):
        self.git("checkout", "--orphan", "unrelated")
        self.git("rm", "-rf", ".")
        self.commit("unrelated.txt", "Unrelated history\n")
        self.git("update-ref", "refs/remotes/origin/master", "HEAD")
        self.git("checkout", "release/3.3.0")
        with self.assertRaises(subprocess.CalledProcessError):
            coverage_base.select_base(self.root, "refs/heads/release/3.3.0")

    def test_given_rc_cli_when_run_then_records_and_prints_the_selected_base(self):
        output = self.root / "report" / "base.json"
        stdout = io.StringIO()
        stderr = io.StringIO()
        environment = {"GITHUB_REF": "refs/heads/release/3.3.0", "GITHUB_BASE_REF": "", "GITHUB_HEAD_REF": ""}
        with patch.dict(os.environ, environment), contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            result = coverage_base.main(["--repo-root", str(self.root), "--output", str(output)])
        self.assertEqual(result, 0)
        self.assertEqual(stdout.getvalue().strip(), self.stable)
        self.assertIn("Full release", stderr.getvalue())
        self.assertEqual(json.loads(output.read_text(encoding="utf-8"))["base"], self.stable)

    def test_given_cli_without_required_history_when_run_then_returns_failure_and_no_base(self):
        self.git("update-ref", "-d", "refs/remotes/origin/master")
        stdout = io.StringIO()
        stderr = io.StringIO()
        environment = {"GITHUB_REF": "refs/heads/hotfix/3.2.1", "GITHUB_BASE_REF": "", "GITHUB_HEAD_REF": ""}
        with patch.dict(os.environ, environment), contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            self.assertEqual(coverage_base.main(["--repo-root", str(self.root)]), 1)
        self.assertEqual(stdout.getvalue(), "")
        self.assertIn("Unable to select", stderr.getvalue())


if __name__ == "__main__":
    unittest.main()
