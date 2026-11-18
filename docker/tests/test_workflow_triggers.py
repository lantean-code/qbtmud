"""Release workflow trigger and channel regressions without GitHub writes."""
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]


class WorkflowTriggerTests(unittest.TestCase):
    def test_supplemental_verification_is_manual_read_only_and_cannot_publish(self):
        workflow = (ROOT / ".github/workflows/verify-release-3.2.0.yml").read_text(encoding="utf-8")
        trigger = workflow.split("on:\n", 1)[1].split("permissions:", 1)[0]
        self.assertEqual(trigger.strip(), "workflow_dispatch:")
        self.assertIn("github.ref_name == github.event.repository.default_branch", workflow)
        self.assertNotIn(": write", workflow)
        self.assertNotIn("release create", workflow)
        self.assertNotIn("release upload", workflow)
        self.assertNotIn("release edit", workflow)
        self.assertNotIn("docker push", workflow)
        self.assertLess(workflow.index("dotnet test "), workflow.index("--base '3.2.0^'"))
        self.assertLess(workflow.index("--base '3.2.0^'"), workflow.index("release_verification.py record"))
        self.assertLess(workflow.index("release_verification.py record"), workflow.index("name: release-3.2.0-verification\n"))

    def test_supplemental_workload_install_uses_the_same_sdk_context_as_the_build(self):
        workflow = (ROOT / ".github/workflows/verify-release-3.2.0.yml").read_text(encoding="utf-8")
        for step_name in ("Install WASM workload", "Build the unchanged release with supplementary tests", "Test the tagged source"):
            with self.subTest(step=step_name):
                step = workflow.split("      - name: " + step_name + "\n", 1)[1].split("      - ", 1)[0]
                self.assertIn("working-directory: source", step)

    def test_container_publication_subscribes_only_to_stable_release_activity(self):
        workflow = (ROOT / ".github/workflows/container-publish.yml").read_text(encoding="utf-8")
        trigger = re.search(r"^  release:\s*\n    types: \[([^\]]+)\]", workflow, re.MULTILINE)
        self.assertIsNotNone(trigger)
        self.assertEqual(trigger.group(1), "released")

    def test_container_publication_excludes_prereleases_and_nonstable_tags_before_preparing(self):
        workflow = (ROOT / ".github/workflows/container-publish.yml").read_text(encoding="utf-8")
        condition = workflow.split("  prepare:\n", 1)[1].split("    runs-on:", 1)[0]
        self.assertIn("!github.event.release.draft", condition)
        self.assertIn("!github.event.release.prerelease", condition)
        self.assertIn("!contains(github.event.release.tag_name, '-')", condition)
        self.assertIn("!contains(github.event.release.tag_name, '+')", condition)
        self.assertIn("github.event_name == 'schedule'", condition)
        self.assertIn("github.event_name == 'workflow_dispatch' && github.ref_name == github.event.repository.default_branch", condition)

    def test_release_channels_classify_rcs_as_prereleases(self):
        workflow = (ROOT / ".github/workflows/dotnet.yml").read_text(encoding="utf-8")
        step = workflow.split("      - name: Set Release Channel\n", 1)[1].split("      - name:", 1)[0]
        script = "\n".join(line[10:] for line in step.split("        run: |\n", 1)[1].splitlines())
        git_bash = Path(os.environ.get("ProgramFiles", "C:/Program Files")) / "Git/bin/bash.exe"
        bash = str(git_bash) if os.name == "nt" and git_bash.is_file() else shutil.which("bash")
        self.assertIsNotNone(bash)
        cases = (
            ("master", "stable", "false"),
            ("release/3.2.0", "rc", "true"),
            ("hotfix/3.1.1", "rc", "true"),
            ("develop", "beta", "true"),
            ("feature/example", "alpha", "true"),
            ("other", "none", "false"),
        )
        for branch, channel, prerelease in cases:
            with self.subTest(branch=branch), tempfile.TemporaryDirectory() as temporary:
                output = Path(temporary) / "outputs"
                environment = dict(os.environ, GITHUB_REF="refs/heads/" + branch,
                                   GITHUB_OUTPUT=output.as_posix())
                subprocess.run([bash, "--noprofile", "--norc", "-c", script],
                               env=environment, check=True, capture_output=True, text=True)
                values = dict(line.split("=", 1) for line in output.read_text(encoding="utf-8").splitlines())
                self.assertEqual(values["channel"], channel)
                self.assertEqual(values["prerelease"], prerelease)


if __name__ == "__main__":
    unittest.main()
