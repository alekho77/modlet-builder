# Release Process

## Overview

Releases are created manually via the **Release** workflow (`release.yml`).
The workflow validates the requested tag, builds self-contained executables for
win-x64 and linux-x64, and publishes a GitHub Release with a changelog body
generated automatically from git history between the previous and current tag.

## Step-by-step

1. Merge your changes to `main`.
   The **Tag Version** workflow runs automatically and creates an annotated
   `vMAJOR.MINOR.PATCH` tag if the version changed.

2. Go to **Actions → Release → Run workflow**.
   Enter the tag produced in step 1 (e.g. `v0.6.0`).

3. The workflow:
   - validates the tag
   - runs the full build and test suite
   - publishes self-contained binaries
   - generates a changelog from commits since the previous tag
   - creates the GitHub Release with the changelog as its body

## Version bumping

Version is controlled by commit message keywords (see `GitVersion.yml`):

| Keyword in commit message | Version bump |
| --- | --- |
| `+semver: breaking` or `+semver: major` | Major (`1.x.x → 2.0.0`) |
| `+semver: feature` or `+semver: minor` | Minor (`x.1.x → x.2.0`) |
| `+semver: fix` or `+semver: patch` | Patch (`x.x.1 → x.x.2`) |

If no keyword appears in any commit since the last tag, the version does
not change and no new tag is created.

## Writing commit messages for a good changelog

The changelog generator classifies each commit into a section based on its
subject line.

| Section | Matched patterns |
| ------- | ---------------- |
| **Breaking Changes** | `+semver: breaking`, `+semver: major`, `BREAKING CHANGE` |
| **Features** | `+semver: feature`, `+semver: minor`, `feat:`, `feat(…):` |
| **Fixes** | `+semver: fix`, `+semver: patch`, `fix:`, `fix(…):` |
| **Documentation** | `docs:`, `docs(…):` |
| **Tests** | `test:`, `tests:`, `test(…):` |
| **Build and CI** | `ci:`, `build:`, `ci(…):`, `build(…):` |
| **Maintenance** | `chore:`, `refactor:`, `style:`, `perf:` and scoped variants |
| **Other Changes** | everything else |

Commits that do not match any prefix are not dropped — they appear under
**Other Changes**. Using recognisable prefixes produces a cleaner,
better-organised release body, but is not required.

### Examples

```text
feat: add --clean flag to remove stale output files before build  (+semver: minor)
fix: preserve XML declaration encoding when writing output files   (+semver: fix)
docs: document known target values in README
ci: pin actions/checkout to SHA for supply-chain safety
chore: remove unused using directives in FragmentParser
refactor: extract TopologicalSorter into its own class
BREAKING CHANGE: rename --output to --out in build command         (+semver: breaking)
```

## Notes

- The release workflow is **manually triggered** — pushing to `main` creates
  a tag automatically but does not publish a release.
- If the same tag already exists, the Tag Version workflow skips tag creation
  silently.
- The generated changelog is stored only in the GitHub Release body.
  No `CHANGELOG.md` file is maintained in the repository.
