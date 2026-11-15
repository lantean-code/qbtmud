import contextlib
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch


SPEC = importlib.util.spec_from_file_location("javascript_coverage_report", Path(__file__).resolve().parents[1] / "report.py")
report = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(report)


def coverage_range(start, end, count):
    return {"startOffset": start, "endOffset": end, "count": count}


def function_ranges(ranges):
    return {"functionName": "Function", "isBlockCoverage": True, "ranges": ranges}


class ReportTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.root = Path(directory.name)
        self.inputs = self.root / "reports"
        self.inputs.mkdir()
        self.source = "first();\r\nsecond();\r\n"
        for path in report.SCRIPT_PATHS.values():
            source_path = self.root / path
            source_path.parent.mkdir(parents=True, exist_ok=True)
            source_path.write_bytes(self.source.encode("utf-8"))

    def entry(self, name, ranges=None, source=None):
        source = self.source if source is None else source
        ranges = ranges if ranges is not None else [coverage_range(0, report.utf16_length(source), 1)]
        return {"url": f"https://qbtmud.test/js/{name}?v=Version", "source": source, "functions": [function_ranges(ranges)]}

    def write_report(self, filename, entries):
        (self.inputs / filename).write_text(json.dumps({"scripts": entries}), encoding="utf-8")

    def test_given_unexecuted_hole_when_counted_then_enclosing_hit_does_not_cover_hole(self):
        start = report.utf16_length("first();\r\n")
        ranges = [(0, report.utf16_length(self.source), 4), (start, report.utf16_length(self.source), 0)]

        counts = report.line_counts(report.executable_positions(self.source), ranges)

        self.assertEqual(counts, {1: 4, 2: 0})

    def test_given_nested_uninvoked_function_when_counted_then_script_hit_does_not_cover_body(self):
        source = "outer();\nfunction unused() {\n  missed();\n}\ntail();\n"
        start = source.index("function")
        end = source.index("}\n") + 1
        ranges, identities = report.read_ranges([
            function_ranges([coverage_range(0, len(source), 1)]),
            function_ranges([coverage_range(start, end, 0)]),
        ], len(source))

        counts = report.line_counts(report.executable_positions(source), ranges)

        self.assertEqual(counts, {1: 1, 2: 0, 3: 0, 5: 1})
        self.assertEqual(len(identities), 2)

    def test_given_complementary_tests_when_aggregated_then_unions_line_and_range_hits(self):
        start = report.utf16_length("first();\r\n")
        length = report.utf16_length(self.source)
        first = [coverage_range(0, length, 1), coverage_range(start, length, 0)]
        second = [coverage_range(0, length, 0), coverage_range(start, length, 3)]
        self.write_report("first.json", [self.entry(name, first) for name in report.SCRIPT_PATHS])
        self.write_report("second.json", [self.entry(name, second) for name in report.SCRIPT_PATHS])

        scripts, count = report.collect_coverage(self.inputs, self.root)

        self.assertEqual(count, 2)
        for script in scripts.values():
            self.assertEqual(script["lines"], {1: 1, 2: 3})
            self.assertEqual(sorted(script["ranges"].values()), [1, 3])

    def test_given_astral_unicode_and_crlf_when_offsets_are_utf16_then_maps_hole_to_correct_line(self):
        source = 'const face = "😀";\r\nmissed();\r\n'
        start = report.utf16_length(source[:source.index("missed")])
        length = report.utf16_length(source)

        counts = report.line_counts(report.executable_positions(source), [(0, length, 1), (start, length, 0)])

        self.assertGreater(length, len(source))
        self.assertEqual(counts, {1: 1, 2: 0})

    def test_given_comments_braces_and_template_expression_when_classified_then_excludes_only_non_code_lines(self):
        source = '// Comment\n{\n/* Block\nComment */\nconst url = "https://example.test";\nconst text = `Text\n${\ncall();\n}\n`;\n}\n'

        counts = report.line_counts(report.executable_positions(source), [(0, report.utf16_length(source), 1)])

        self.assertEqual(set(counts), {5, 6, 8})

    def test_given_executable_text_outside_ranges_when_counted_then_does_not_silently_drop_line(self):
        counts = report.line_counts(report.executable_positions(self.source), [(0, 8, 1)])

        self.assertEqual(counts, {1: 1, 2: 0})

    def test_given_comment_delimiters_inside_regex_literals_when_classified_then_keeps_following_code(self):
        source = r"const pattern = /[/*]/;" + "\n" + r"const escaped = /\/\//;" + "\nlast();\n"

        counts = report.line_counts(report.executable_positions(source), [(0, report.utf16_length(source), 1)])

        self.assertEqual(counts, {1: 1, 2: 1, 3: 1})

    def test_given_stale_source_when_collected_then_fails(self):
        self.write_report("test.json", [self.entry(name, source=self.source.replace("\r\n", "\n")) for name in report.SCRIPT_PATHS])

        with self.assertRaisesRegex(ValueError, "Stale or mismatched source"):
            report.collect_coverage(self.inputs, self.root)

    def test_given_missing_script_when_collected_then_fails(self):
        self.write_report("test.json", [self.entry("boot.resources.js")])

        with self.assertRaisesRegex(ValueError, "Missing script coverage: interop.js"):
            report.collect_coverage(self.inputs, self.root)

    def test_given_no_reports_when_collected_then_fails(self):
        with self.assertRaisesRegex(ValueError, "No JavaScript test coverage reports"):
            report.collect_coverage(self.inputs, self.root)

    def test_given_empty_script_report_when_collected_then_fails(self):
        self.write_report("test.json", [])

        with self.assertRaisesRegex(ValueError, "No scripts were recorded"):
            report.collect_coverage(self.inputs, self.root)

    def test_given_missing_functions_or_invalid_ranges_when_read_then_fails(self):
        invalid_functions = [
            [],
            [function_ranges([])],
            [function_ranges([coverage_range(0, 1000, 1)])],
            [function_ranges([coverage_range(0, 10, -1)])],
            [function_ranges([coverage_range(0, 10, "1")])],
            [function_ranges([coverage_range(2, 8, 1), coverage_range(0, 4, 1)])],
        ]
        for functions in invalid_functions:
            with self.subTest(functions=functions), self.assertRaises(ValueError):
                report.read_ranges(functions, 20)

    def test_given_diff_with_deletion_and_additions_when_parsed_then_tracks_new_line_numbers(self):
        path = report.SCRIPT_PATHS["interop.js"]
        diff = f"diff --git a/{path} b/{path}\n--- a/{path}\n+++ b/{path}\n@@ -4,2 +4,3 @@ Function\n-old();\n-oldAgain();\n+first();\n+second();\n+third();\n@@ -12 +13,0 @@\n-deleted();\n"

        changed = report.parse_changed_lines(diff)

        self.assertEqual(changed[path], {4, 5, 6})

    def test_given_added_increment_when_diff_parsed_then_keeps_following_uncovered_changes(self):
        path = report.SCRIPT_PATHS["interop.js"]
        diff = f"diff --git a/{path} b/{path}\n--- a/{path}\n+++ b/{path}\n@@ -0,0 +1,2 @@\n+++ counter;\n+missed();\n"
        self.write_report("test.json", [self.entry(name, [coverage_range(0, report.utf16_length(self.source), 0)]) for name in report.SCRIPT_PATHS])
        scripts, count = report.collect_coverage(self.inputs, self.root)

        changed = report.parse_changed_lines(diff)
        summary = report.make_summary(scripts, count, changed)

        self.assertEqual(changed[path], {1, 2})
        self.assertFalse(summary["passed"])
        self.assertEqual(summary["uncovered_changed_lines"], {path: [1, 2]})

    def test_given_uncovered_changed_code_when_gated_then_fails_and_writes_line_only_lcov(self):
        start = report.utf16_length("first();\r\n")
        self.write_report("test.json", [self.entry(name, [coverage_range(0, report.utf16_length(self.source), 1), coverage_range(start, report.utf16_length(self.source), 0)]) for name in report.SCRIPT_PATHS])
        scripts, count = report.collect_coverage(self.inputs, self.root)
        path = report.SCRIPT_PATHS["boot.resources.js"]

        summary = report.make_summary(scripts, count, {path: {2, 3}})
        text = report.write_reports(self.root / "output", scripts, summary)

        self.assertFalse(summary["passed"])
        self.assertEqual(summary["uncovered_changed_lines"], {path: [2]})
        self.assertEqual(summary["scripts"]["boot.resources.js"]["changed_executable_lines"], {"covered": 0, "total": 1})
        lcov = (self.root / "output/coverage.lcov").read_text(encoding="utf-8")
        self.assertIn(f"SF:{path}\nDA:1,1\nDA:2,0\nLF:2\nLH:1", lcov)
        self.assertNotIn("BRDA:", lcov)
        self.assertIn("V8 ranges 1/2", text)
        self.assertIn(f"UNCOVERED {path}:2", text)
        self.assertEqual(json.loads((self.root / "output/summary.json").read_text(encoding="utf-8")), summary)

    def test_given_covered_changes_and_non_code_when_gated_then_succeeds(self):
        self.write_report("test.json", [self.entry(name) for name in report.SCRIPT_PATHS])
        scripts, count = report.collect_coverage(self.inputs, self.root)
        path = report.SCRIPT_PATHS["interop.js"]

        summary = report.make_summary(scripts, count, {path: {1, 2, 3}})

        self.assertTrue(summary["passed"])
        self.assertEqual(summary["scripts"]["interop.js"]["changed_executable_lines"], {"covered": 2, "total": 2})

    def test_given_cli_when_gate_fails_or_succeeds_then_returns_expected_exit_status(self):
        start = report.utf16_length("first();\r\n")
        self.write_report("test.json", [self.entry(name, [coverage_range(0, report.utf16_length(self.source), 1), coverage_range(start, report.utf16_length(self.source), 0)]) for name in report.SCRIPT_PATHS])
        arguments = ["--input", str(self.inputs), "--output", str(self.root / "output"), "--base", "HEAD", "--repo-root", str(self.root)]
        path = report.SCRIPT_PATHS["interop.js"]
        for line, expected in ((1, 0), (2, 1)):
            with self.subTest(line=line), patch.object(report, "changed_lines", return_value={path: {line}}), contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(report.main(arguments), expected)

    def test_given_cli_without_reports_when_run_then_fails_before_git(self):
        arguments = ["--input", str(self.inputs), "--output", str(self.root / "output"), "--base", "HEAD", "--repo-root", str(self.root)]
        with patch.object(report, "changed_lines") as git, contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(report.main(arguments), 1)
            git.assert_not_called()

    def test_given_git_option_instead_of_revision_when_requested_then_rejects_it(self):
        with patch.object(report.subprocess, "run") as git, self.assertRaises(ValueError):
            report.changed_lines(self.root, "--output=UnexpectedFile")
        git.assert_not_called()


if __name__ == "__main__":
    unittest.main()
