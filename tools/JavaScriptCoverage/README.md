# JavaScript coverage comparisons

The UI build runs browser regressions against the production scripts and requires coverage of every changed executable line.

Release and hotfix builds compare the full candidate against its common ancestor with `origin/master`. Pull requests into or from `release/` and `hotfix/` branches use the same comparison. An uncovered change from an earlier RC therefore remains part of the check when the latest commit only changes documentation or tests.

Other pull requests compare against their target branch. Other branch builds, including stable master builds, compare against the previous commit. On a stable release merge, that parent is the preceding master commit.

`coverage_base.py` selects the base from GitHub's branch context. It requires full Git history and the relevant remote branch; a missing reference or common ancestor fails the check rather than narrowing the comparison. Checkout in the UI workflow fetches the full history.

The selected base, checked commit, comparison reference and reason are retained as `base.json` in the JavaScript coverage artifact alongside the coverage report. The reporting tool's coverage threshold is unchanged.

Run the comparison and reporting regressions with `python3 -m unittest discover -s tools/JavaScriptCoverage/tests -p 'test_*.py'`. They include a Git history with uncovered JavaScript followed by a documentation-only RC commit, proving that the full-release check catches the gap that a latest-commit comparison misses.
