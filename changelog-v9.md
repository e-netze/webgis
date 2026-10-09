# Change Log - WebGIS V9

All notable changes to WebGIS V9 are documented in this file.
A compact, admin-oriented summary is available in [release-notes-v9.md](release-notes-v9.md).
Changes of the previous major version: [changelog-v8.md](changelog-v8.md).

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## Unreleased

### Added

- CMS git support (optional, per cms-item): with a ``git`` section in ``cms.config``
  (``remote-url``, ``username``, ``token``, ``default-branch``, ``workspace-root``,
  ``author-email-domain``, ``committer-name``, ``committer-email``) the CMS tree is versioned in
  an external (private) git repository via LibGit2Sharp. Without git the CMS behaves as before.
  * Each CMS user works in an own working copy (workspace) of the tree; global default
    ``git-workspace-root`` in ``cms.config``, overridable per cms-item.
  * Branches, commit, pull/push and merge into the default branch including conflict resolution
    in the CMS; direct commits to ``main`` are allowed.
  * History dialog with commit graph, branch/HEAD/deploy markers, commit details and per-file diffs;
    working-copy diffs with property table view and a "My changes" dialog with suggested commit messages.
  * Node history with restore and "compare with main" (with take-over from main).
  * Deployments always use the latest state of the remote default branch (separate deploy clone,
    one deployment per cms-item at a time); the deploy page shows the deployed commit.
  * Workspace admin dialog; invalid git configurations are reported on startup and disable git
    for the cms-item. Docker (Alpine) images contain the musl build of libgit2.

  [Architecture](docs/architecture/cms-git.md)

- CMS branch deploys: with ``allowBranchDeploy`` on a deployment, a user can deploy the current
  state of the own workspace (including uncommitted changes, flagged in the deploy info) as a branch.
  * File targets: ``{target-dir}/branches/{encoded-branch}/...``; url targets: upload with a branch
    parameter. ``{branch}`` placeholder in ``postEvents`` (empty for the main deploy).
  * The WebGIS API discovers deployed branches automatically if ``allow-branches`` is set in
    ``api.config``.
  * Deployed branches are listed and can be removed on the deploy page; stale branches are removed
    automatically. Deploy tiles show a "main only" / "main + branches" badge.
  * Link warnings of a branch deploy are written to a per-user warnings file, so an aborted branch
    deploy offers its own "solve warnings" tile without touching the main deploy.

  [Architecture](docs/architecture/cms-deployment.md)

- Branch selection in portal and viewer: CMS branches can be selected per map (select, badge,
  dialog, ``?branch=`` url parameter, secured branch links with tokens). Stale branches are reset,
  and warnings are shown when publishing/saving maps with a selected branch.

- Fast branch deploy: branch deploys keep a per-user snapshot of the read CMS files
  (``{workspace-root}/{cms-id}/cache/{user}.snapshot``). Subsequent branch deploys only re-read
  the files changed since the snapshot (git diff + uncommitted changes); the result is identical
  to a full read. The deploy dialog shows the snapshot date/commit and offers
  "Read everything again". Main deploys are unchanged.
  * Git status no longer detects renames: renamed files are listed as deleted + added.

  [Architecture](docs/architecture/cms-deployment.md)

- CMS deploy performance: single-pass export with link warnings collected during the export
  (no separate warnings scan), cached directory listings and per-phase timings in the deploy console.

  [Architecture](docs/architecture/cms-deployment.md)

- Modernized CMS look: design tokens (``--webgis-ui-*``), breadcrumb chevrons, resizable tree,
  refreshed tiles, dialogs and forms. In the deploy dialog the main deploy is now labeled
  "Published state (main)" instead of "Production".

### Fixed

- CMS export: ``root.acl`` was read from a wrong path, so a root-level ACL was never applied.
