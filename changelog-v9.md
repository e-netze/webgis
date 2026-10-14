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

  [Docs](https://docs.webgiscloud.com/en/webgis/apps/cms/git.html),
  [Docs config](https://docs.webgiscloud.com/en/webgis/config/cms/index.html#cms-config-git),
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

  [Docs](https://docs.webgiscloud.com/en/webgis/apps/cms/deploy.html#cms-deploy-branch),
  [Docs api.config](https://docs.webgiscloud.com/en/webgis/config/api/index.html),
  [Architecture](docs/architecture/cms-deployment.md)

- Branch selection in portal and viewer: CMS branches can be selected per map (select, badge,
  dialog, ``?branch=`` url parameter, secured branch links with tokens). Stale branches are reset,
  and warnings are shown when publishing/saving maps with a selected branch.

  [Docs](https://docs.webgiscloud.com/en/webgis/apps/portal/branches.html)

- Fast branch deploy: branch deploys keep a per-user snapshot of the read CMS files
  (``{workspace-root}/{cms-id}/cache/{user}.snapshot``). Subsequent branch deploys only re-read
  the files changed since the snapshot (git diff + uncommitted changes); the result is identical
  to a full read. The deploy dialog shows the snapshot date/commit and offers
  "Read everything again". Main deploys are unchanged.
  * Git status no longer detects renames: renamed files are listed as deleted + added.

  [Docs](https://docs.webgiscloud.com/en/webgis/apps/cms/deploy.html#cms-deploy-fast),
  [Architecture](docs/architecture/cms-deployment.md)

- CMS deploy performance: single-pass export with link warnings collected during the export
  (no separate warnings scan), cached directory listings and per-phase timings in the deploy console.

  [Architecture](docs/architecture/cms-deployment.md)

- Modernized CMS look: design tokens (``--webgis-ui-*``), breadcrumb chevrons, resizable tree,
  refreshed tiles, dialogs and forms. In the deploy dialog the main deploy is now labeled
  "Published state (main)" instead of "Production".

- Debug UI tools (only in ``DEBUG`` builds, toolbox container "Debug"): a set of viewer tools in
  ``E.Standard.WebGIS.Tools/Debug/UI`` that render all server-side UI elements, targets/dialogs,
  setters and UI plugins reachable via UI elements, to check their rendering and callbacks manually
  (echo of all received tool parameters). New skill ``.github/skills/create-webgis-tool``.

### Fixed

- CMS export: ``root.acl`` was read from a wrong path, so a root-level ACL was never applied.
- UI builder: ``UIConditionDiv`` (element value) with ``ConditionResult = false`` was not hidden
  when the value matched one of the condition arguments.
- Validation messages of tool parameters (incl. edit masks) were shown at the bottom of the dialog
  instead of directly below the input field (new ``.webgis-validation-holder`` wrapper).
- Edit theme combo/tree: the ``db_rights`` property was read as ``dbrights`` and therefore ignored,
  so e.g. the mobile insert/select tools listed edit themes without the required rights.
- UI setters: ``UISelectOptionsSetter`` crashed without a map in the options, the option container
  ``UICssSetter`` did not set the container value, and value setters on ``textarea`` only changed
  the default value.
- ``UIEmpty`` (close target) did not close dialogs opened with the targets ``modaldialog_hidden``,
  ``tool_modaldialog_hidden``, ``tool_modaldialog_noblocking`` and
  ``tool_modaldialog_noblocking_closable``; only their content was removed.
- Tool parameters with non-string values (e.g. ``UILineWieghtSelector``, ``UISymbolSelector`` used
  as tool parameters) caused ``val.replace is not a function``; they are now sent as string/JSON.
