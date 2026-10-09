# Release Notes - WebGIS V9

Compact, admin-oriented summary of the changes in WebGIS V9: upgrade notes, new features and
configuration options, and fixed issues. For the full technical details see
[changelog-v9.md](changelog-v9.md). Older versions: [release-notes-v8.md](release-notes-v8.md).

The version number encodes the release date: `Major.{Year-2000}.{CalendarWeek}{Build}`
(e.g. `9.26.4102` = 2026, week 41).

## Unreleased

### Upgrade notes
### New

- Optional git versioning for CMS trees: configure a `git` section per cms-item in `cms.config` (`remote-url`, `username`, `token`, `default-branch`, ...); each user then works in an own workspace with branches, commit, push, merge, history and diffs.
- Branch deploys: with `allowBranchDeploy` on a deployment, users can deploy their workspace as a branch for testing. The WebGIS API needs `allow-branches` in `api.config`.
- Portal and viewer can show maps with a selected CMS branch (also via the `?branch=` url parameter).
- Fast branch deploy: repeated branch deploys only re-read changed CMS files; a full read can be forced in the deploy dialog.
- Faster CMS deploys: link warnings are now collected during the export instead of a separate scan.
- Modernized CMS look; the main deploy is now labeled "Published state (main)" instead of "Production".

### Fixed
