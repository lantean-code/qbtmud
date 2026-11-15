import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


SCREENSHOT_TOOLS = Path(__file__).resolve().parents[1]
RUNNER = SCREENSHOT_TOOLS / "run-readme-screenshots.sh"
GENERATED_FILES = (
    "welcome-wizard.png",
    "welcome-wizard-debug.png",
    "dashboard.png",
    "torrent-details.png",
    "theme-manager.png",
    "app-settings.png",
)


class ScreenshotCleanupTests(unittest.TestCase):
    def test_given_mixed_assets_when_cleanup_runs_then_preserves_non_generated_assets(self):
        with tempfile.TemporaryDirectory() as directory:
            workspace = Path(directory)
            repo_root = workspace / "checkout with spaces"
            output_dir = repo_root / "docs/content/readme-assets"
            output_dir.mkdir(parents=True)
            wordmark = (SCREENSHOT_TOOLS.parents[1] / "docs/content/readme-assets/qbtmud-wordmark.svg").read_bytes()
            preserved_files = {
                "qbtmud-wordmark.svg": wordmark,
                "unrelated.png": b"Unrelated image",
                "dashboard-old.png": b"Previous image",
                "notes.txt": b"Notes",
                "nested/dashboard.png": b"Nested image",
            }
            for name, content in preserved_files.items():
                path = output_dir / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(content)
            for name in GENERATED_FILES:
                (output_dir / name).write_bytes(b"Generated screenshot")
            stale_work = workspace / "temporary/qbtmud-readme-screenshots/stale.txt"
            stale_work.parent.mkdir(parents=True)
            stale_work.write_bytes(b"Stale work")

            self._run_cleanup(workspace, repo_root)

            self.assertFalse(stale_work.exists())
            for name in GENERATED_FILES:
                with self.subTest(generated_file=name):
                    self.assertFalse((output_dir / name).exists())
            for name, content in preserved_files.items():
                with self.subTest(preserved_file=name):
                    self.assertEqual((output_dir / name).read_bytes(), content)

            self._run_cleanup(workspace, repo_root)

            for name, content in preserved_files.items():
                with self.subTest(preserved_file=name):
                    self.assertEqual((output_dir / name).read_bytes(), content)

    def test_given_missing_output_directory_when_cleanup_runs_then_creates_output_directory(self):
        with tempfile.TemporaryDirectory() as directory:
            workspace = Path(directory)
            repo_root = workspace / "checkout with spaces"

            self._run_cleanup(workspace, repo_root)

            output_dir = repo_root / "docs/content/readme-assets"
            self.assertTrue(output_dir.is_dir())
            self.assertEqual(list(output_dir.iterdir()), [])

    def test_given_screenshot_profile_when_configured_then_requires_localhost_authentication(self):
        runner_text = RUNNER.read_text(encoding="utf-8")
        marker = 'cat >"$profile_config_path" <<EOF\n'
        self.assertIn(marker, runner_text)
        profile_text = runner_text.partition(marker)[2].partition("\nEOF")[0]

        self.assertIn(r"WebUI\\LocalHostAuth=true", profile_text.splitlines())
        self.assertNotIn(r"WebUI\\LocalHostAuth=false", profile_text.splitlines())

    def _run_cleanup(self, workspace, repo_root):
        self.assertTrue((workspace / "temporary/qbtmud-readme-screenshots").resolve().is_relative_to(workspace.resolve()))
        self.assertTrue(repo_root.resolve().is_relative_to(workspace.resolve()))
        runner_text = RUNNER.read_text(encoding="utf-8")
        marker = "\ndotnet publish "
        self.assertIn(marker, runner_text)
        setup_text = runner_text.partition(marker)[0] + "\n"
        tools_dir = repo_root / "tools/ReadmeScreenshots"
        tools_dir.mkdir(parents=True, exist_ok=True)
        runner_path = tools_dir / RUNNER.name
        runner_path.write_bytes(setup_text.encode("utf-8"))
        shutil.copyfile(SCREENSHOT_TOOLS / "ReadmeScreenshots.csproj", tools_dir / "ReadmeScreenshots.csproj")
        environment = dict(os.environ, TMPDIR=(workspace / "temporary").as_posix(), HOME=(workspace / "home").as_posix())
        bash = shutil.which("bash")
        self.assertIsNotNone(bash)

        result = subprocess.run([bash, runner_path.as_posix()], cwd=repo_root, env=environment, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
