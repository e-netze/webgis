#title: Versioning (Git)

#please-wait: Please wait...

#workspace-missing-title: No workspace available

#workspace-missing-text: This CMS is versioned with Git. Every editor works in a personal workspace. Get the current state to start editing.

#init-workspace: Get workspace

#branch: Branch

#no-changes: No unsaved changes

#changes: {0} unsaved change(s)

#ahead: {0} saved change(s) not published yet

#behind: {0} new change(s) on the server

#up-to-date: Up to date

#no-upstream: Not published yet

#stale: Server not reachable - the displayed state may be outdated

#check-status: Check status

#last-checked: Last checked: {0}

#pull: Update

#commit: Save changes...

#push: Publish

#branches: Branches...

#pull-success: The workspace has been updated.

#push-success: The changes have been published.

#commit-success: The changes have been saved.

#commit-title: Save changes

#commit-message: Description of the changes

#commit-message-placeholder: What has been changed?

#commit-button: Save

#change-added: new

#change-modified: modified

#change-deleted: deleted

#change-conflicted: conflict

#branches-title: Branches

#branch-new: New branch

#branch-new-placeholder: Name of the branch

#branch-create: Create

#branch-switch: Switch

#branch-delete: Delete

#branch-delete-confirm: Delete branch '{0}' (on the server too)?

#branch-current: current

#branch-default: main branch

#branch-remote-only: server only

#branch-created-offline: The branch has been created locally. The server is not reachable, it will be transferred when publishing.

#close: Close

#initial-commit-message: Initial state of the CMS

#error-no-workspace: There is no workspace yet.

#error-busy: Another git action is running in this workspace. Please try again later.

#error-commit-first: There are unsaved changes. Please save the changes first.

#error-nothing-to-commit: There are no changes to save.

#error-message-required: Please enter a description of the changes.

#error-invalid-branch-name: Invalid branch name.

#error-branch-exists: A branch with this name already exists.

#error-branch-not-found: The branch was not found.

#error-cannot-delete-current-branch: The current branch cannot be deleted.

#error-cannot-delete-default-branch: The main branch cannot be deleted.

#error-merge-conflicts: The changes conflict with other changes. Please resolve the conflicts or abort the merge.

#error-merge-in-progress: A merge is in progress. Please finish it first.

#error-push-rejected: The changes could not be published.

#error-remote-not-reachable: The git server is not reachable.

#error-no-initial-tree: There is no CMS tree for the initial state.

#error-not-merging: No merge is in progress.

#error-conflicts-remaining: Not all conflicts have been resolved yet.

#error-conflict-not-found: The conflict was not found.

#error-invalid-conflict-choice: Invalid choice for resolving the conflict.

#error-already-on-default-branch: This action is not possible on the main branch.

#error-invalid-path: Invalid path.

#merge-from-default: Take over main branch

#merge-from-default-success: The current state of the main branch has been taken over.

#merge-into-default: Merge into main branch...

#merge-into-default-title: Merge into main branch

#merge-into-default-text: The changes of the working branch '{0}' will be merged into the main branch '{1}' and published. Afterwards you continue working on the main branch.

#merge-delete-branch: Delete working branch afterwards

#merge-into-default-button: Merge

#merge-into-default-success: The changes have been merged into the main branch and published.

#merging-banner: Merge with '{0}' in progress - {1} conflict(s) open. Editing is locked until the merge is finished.

#merging-banner-resolved: Merge with '{0}' in progress - all conflicts resolved. Please finish the merge.

#resolve-conflicts: Resolve conflicts...

#merge-complete: Finish merge

#merge-complete-success: The merge has been finished. The changes can now be published.

#merge-abort: Abort merge

#merge-abort-confirm: Abort the merge? The state before the merge will be restored.

#conflicts-title: Resolve conflicts

#conflicts-none: All conflicts are resolved.

#conflict-mine: Version '{0}'

#conflict-theirs: Version '{0}'

#conflict-use-mine: Use '{0}'

#conflict-use-theirs: Use '{0}'

#conflict-all-mine: Use '{0}' everywhere

#conflict-all-theirs: Use '{0}' everywhere

#conflict-deleted: (deleted)

#conflict-kind-modified: changed on both sides

#conflict-kind-added: added on both sides

#conflict-kind-deleted-by-me: deleted in '{0}', changed in '{1}'

#conflict-kind-deleted-by-them: changed in '{0}', deleted in '{1}'

#discard: Discard change

#discard-confirm: Discard unsaved changes to '{0}'?

#discard-all: Discard all changes

#discard-all-confirm: Discard all unsaved changes?

#node-changed: unsaved change

#conflict-version: Version '{0}'

#conflicts-intro: '{0}' is being merged into the current working branch '{1}'. The following entries have been changed differently on both sides. Please choose which version to keep for each entry. Changed lines are highlighted.

#diff-unchanged-lines: ... show {0} unchanged line(s)

#error-deploy-running: A deployment is already running for this CMS. Please wait until it has finished.

#deploy-git-title: Versioning (Git)

#deploy-git-source: The current state of '{0}' on the server will be deployed:

#deploy-git-no-remote-head: The working branch '{0}' does not exist on the server yet. Deploying is possible as soon as a state has been published.

#deploy-git-last-deployed: Last deployed: {0}

#deploy-git-never-deployed: Nothing has been deployed yet.

#deploy-git-already-deployed: This state has already been deployed.

#deploy-git-unpublished: Attention: The following changes in your workspace are not part of the deployment:

#deploy-git-unpublished-branch: You are working on branch '{0}'. Only changes merged into '{1}' and published will be deployed.

#deploy-git-unpublished-merging: A merge has not been completed yet.

#deploy-git-no-workspace: You do not have a workspace yet.

#deploy-git-running: A deployment is currently running (started by {0}).

#deploy-git-remote-error: The server is not reachable - the displayed state may be outdated:

#workspaces: Manage workspaces...

#workspaces-title: Manage workspaces

#workspaces-intro: Server-side workspaces of all editors of this CMS. Deleting a workspace discards all unsaved and unpublished changes.

#workspaces-none: There are no workspaces.

#workspace-user: Editor

#workspace-branch: Working branch

#workspace-state: State

#workspace-last-modified: Last modified

#workspace-current-user: (you)

#workspace-clean: no pending changes

#workspace-merging: merge in progress

#workspace-delete: Delete

#workspace-delete-confirm: Really delete the workspace of '{0}'? Unsaved and unpublished changes will be lost.

#workspace-delete-own-confirm: Really delete your own workspace? Unsaved and unpublished changes will be lost. You will have to fetch a new workspace afterwards.

#workspace-deleted: The workspace has been deleted.

#deploy-workspace-reset: Reset deploy clone

#deploy-workspace-reset-confirm: Delete the deploy clone on the server? It will be fetched again automatically with the next deployment.

#deploy-workspace-reset-success: The deploy clone has been deleted.

#history: History...

#history-title: History (branches and changes)

#history-all-branches: Show all branches

#history-load-more: Load more

#history-none: No saved changes yet

#history-head: current state

#history-deployed: deployed

#history-unpushed: not published yet

#history-select-commit: Click a change in the history to show its details.

#history-author: Author

#history-date: Date

#history-commit: Commit

#history-parents: Parents

#history-merge: Merge - changes compared to the first parent ({0}) are shown

#history-changes: Changed files

#history-no-changes: No file changes

#history-before: before ({0})

#history-after: after ({0})

#error-commit-not-found: The change (commit) was not found.
