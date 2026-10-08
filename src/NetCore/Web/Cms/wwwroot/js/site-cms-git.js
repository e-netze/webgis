// Git support for the CMS (only loaded if git is configured for the cms-item)
var CMSGit = new function () {
    var _self = this;
    var _status = null;

    this.l10n = {};

    var t = function (key) {
        var args = Array.prototype.slice.call(arguments, 1);
        var text = _self.l10n[key] || key;
        for (var i = 0; i < args.length; i++) {
            text = text.replace('{' + i + '}', args[i]);
        }
        return text;
    };

    var esc = function (text) {
        return $('<div>').text(text === null || text === undefined ? '' : String(text)).html();
    };

    // simple line icons (viewBox 24x24, stroke = currentColor)
    var ICONS = {
        'commit': '<circle cx="12" cy="12" r="4"/><path d="M2 12h6M16 12h6"/>',
        'push': '<path d="M12 20V5M5 11l7-7 7 7"/>',
        'pull': '<path d="M12 4v15M19 13l-7 7-7-7"/>',
        'merge': '<circle cx="6" cy="5" r="2.5"/><circle cx="6" cy="19" r="2.5"/><circle cx="18" cy="9" r="2.5"/><path d="M6 7.5v9M18 11.5c0 4-5 5-11.5 5.5"/>',
        'merge-into': '<path d="M3 12h12M10 6l6 6-6 6M21 4v16"/>',
        'discard': '<path d="M9 14L4 9l5-5"/><path d="M4 9h11a5 5 0 0 1 0 10h-4"/>',
        'branch': '<circle cx="6" cy="5" r="2.5"/><circle cx="6" cy="19" r="2.5"/><circle cx="18" cy="5" r="2.5"/><path d="M6 7.5v9M18 7.5c0 6-11 4-11.5 9"/>',
        'history': '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 3"/>',
        'workspaces': '<circle cx="9" cy="8" r="3.5"/><path d="M2 20c0-4 3-6.5 7-6.5s7 2.5 7 6.5"/><path d="M16 4.5a3.5 3.5 0 0 1 0 7M18.5 14c2.2.7 3.5 2.8 3.5 6"/>',
        'refresh': '<path d="M20 12a8 8 0 1 1-2.4-5.7"/><path d="M20 4v5h-5"/>',
        'conflicts': '<path d="M12 3L2 21h20z"/><path d="M12 10v5M12 18v.5"/>',
        'check': '<path d="M5 12.5l4.5 4.5L19 7"/>',
        'close': '<path d="M6 6l12 12M18 6L6 18"/>',
        'trash': '<path d="M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3"/>',
        'switch': '<path d="M4 8h15l-4-4M20 16H5l4 4"/>',
        'plus': '<path d="M12 5v14M5 12h14"/>',
        'edit': '<path d="M4 20h4L19 9l-4-4L4 16z"/><path d="M13 7l4 4"/>',
        'more': '<path d="M6 9l6 6 6-6"/>',
        'diff': '<path d="M8 3v8M4 7h8M4 17h8"/><path d="M16 3v18M13 18l3 3 3-3"/>',
        'goto': '<path d="M4 12h15M13 6l6 6-6 6"/>'
    };

    var iconHtml = function (name) {
        return '<svg class="cms-git-icon" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' + (ICONS[name] || '') + '</svg>';
    };

    // icon + label, used for (dialog) buttons
    var labelHtml = function (iconName, text) {
        return iconHtml(iconName) + '<span class="cms-git-label">' + esc(text) + '</span>';
    };

    var api = function (action, data, onsuccess, onerror) {
        var formData = new FormData();
        data = data || {};
        for (var key in data) {
            if (data.hasOwnProperty(key) && data[key] !== null && data[key] !== undefined) {
                formData.append(key, data[key]);
            }
        }

        fetch(CMS.appRootUrl + '/' + encodeURIComponent(CMS.id) + '/git/' + action + '?_ul=' + encodeURIComponent(CMS.language || ''), {
            method: 'POST',
            body: formData
        })
            .then(function (response) { return response.json(); })
            .then(function (result) {
                if (result.success === false) {
                    if (onerror) onerror(result);
                    if (result.error_key === 'error-merge-conflicts') {
                        // the merge state is kept => let the user resolve the conflicts
                        messageThen(esc(result.exception), function () {
                            _self.refreshStatus(false, _self.showConflictsDialog);
                        });
                    } else {
                        CMS.alert(result.exception);
                    }
                } else {
                    onsuccess(result);
                }
            })
            .catch(function (error) {
                if (onerror) onerror(error);
                CMS.alert(String(error));
            });
    };

    // runs a modifying action with hourglass
    var run = function (action, data, onsuccess) {
        CMS.showHourglass(t('please-wait'));
        api(action, data, function (result) {
            CMS.hideHourglass();
            if (result.status) {
                _self.renderStatus(result.status);
            }
            onsuccess(result);
        }, function () {
            CMS.hideHourglass();
            _self.refreshStatus(false);
        });
    };

    this.init = function (hasWorkspace) {
        if (hasWorkspace) {
            $(function () {
                _self.renderStatus(null);
                _self.refreshStatus(true);
            });

            // keep the change counter up to date after editing the tree
            var cmsApi = CMS.api, timer = null;
            CMS.api = function (action, data, onsuccess, onerror) {
                return cmsApi.call(CMS, action, data, function (result) {
                    if (timer) clearTimeout(timer);
                    timer = setTimeout(function () { _self.refreshStatus(false); }, 1000);
                    onsuccess(result);
                }, onerror);
            };

            // mark changed nodes, every time the content is rendered
            if (typeof window.updateContent === 'function') {
                var cmsUpdateContent = window.updateContent;
                window.updateContent = function (path, onComplete) {
                    return cmsUpdateContent(path, function (result) {
                        markNodes();
                        if (typeof onComplete === 'function') {
                            onComplete(result);
                        }
                    });
                };
            }
        }
    };

    this.initWorkspace = function () {
        run('initworkspace', {}, function () {
            document.location.reload();
        });
    };

    var _lastChecked = null;
    var CollapsedStorageKey = 'cms-git-panel-collapsed';

    this.refreshStatus = function (fetch, onComplete) {
        var $button = $('#cms-git-panel .check-status');
        if (fetch === true) {
            $button.prop('disabled', true).addClass('spinning');
        }

        api('status', { fetch: fetch === true }, function (result) {
            if (fetch === true) {
                _lastChecked = new Date();
            }
            if (result.enabled !== false) {
                _self.renderStatus(result.status);
            }
            if (typeof onComplete === 'function') {
                onComplete();
            }
        }, function () {
            $button.prop('disabled', false).removeClass('spinning');
        });
    };

    this.renderStatus = function (status) {
        _status = status;

        renderMergeBanner();
        markNodes();

        var $panel = $('#cms-git-panel').empty();
        if ($panel.length === 0) {
            return;
        }

        var collapsed = false;
        try { collapsed = window.localStorage.getItem(CollapsedStorageKey) === '1'; } catch (e) { }
        $panel.toggleClass('cms-git-collapsed', collapsed);

        // title row: title, refresh (git fetch), collapse
        var $title = $('<div class="cms-git-title">').appendTo($panel);
        $('<span class="text">').text(t('title')).appendTo($title);
        $('<button class="cms-git-icon-button check-status">')
            .html(iconHtml('refresh'))
            .attr('title', t('check-status') + ' (' + t('check-status-sub') + ')\n' + t('check-status-tip') +
                (_lastChecked ? '\n\n' + t('last-checked', _lastChecked.toLocaleTimeString()) : ''))
            .prop('disabled', !status)
            .click(function (e) { e.preventDefault(); _self.refreshStatus(true); })
            .appendTo($title);
        $('<button class="cms-git-icon-button toggle-panel">')
            .html(iconHtml('more'))
            .attr('title', collapsed ? t('panel-expand') : t('panel-collapse'))
            .click(function (e) {
                e.preventDefault();
                try { window.localStorage.setItem(CollapsedStorageKey, collapsed ? '0' : '1'); } catch (ex) { }
                _self.renderStatus(_status);
            })
            .appendTo($title);

        if (!status) {
            $('<div class="cms-git-info">').text(t('please-wait')).appendTo($panel);
            return;
        }

        var merging = status.is_merging === true;
        var defaultBranch = status.default_branch || 'main';
        var changes = status.changes ? status.changes.length : 0;

        // branch badge + status chips (always visible, also when collapsed)
        var $chips = $('<div class="cms-git-chips">').appendTo($panel);
        $('<button class="cms-git-branch-badge">')
            .html(labelHtml('branch', status.branch || '?'))
            .toggleClass('default', status.is_default_branch === true)
            .attr('title', t('branch') + ': ' + (status.branch || '?') + (merging ? '' : '\n' + t('branches-tip')))
            .prop('disabled', merging)
            .click(function (e) { e.preventDefault(); _self.showBranchesDialog(); })
            .appendTo($chips);

        var addChip = function (iconName, text, title, cls, onclick) {
            var $chip = $(onclick ? '<button class="cms-git-chip clickable">' : '<span class="cms-git-chip">')
                .html(labelHtml(iconName, text))
                .attr('title', title || text)
                .addClass(cls || '')
                .appendTo($chips);
            if (onclick) {
                $chip.click(function (e) { e.preventDefault(); onclick(); });
            }
            return $chip;
        };

        if (merging) {
            addChip('conflicts', status.conflict_count > 0 ? t('resolve-conflicts') : t('merge-complete'),
                status.conflict_count > 0 ? t('merging-banner', status.merge_source || '?', status.conflict_count) : t('merging-banner-resolved', status.merge_source || '?'),
                'warning', status.conflict_count > 0 ? _self.showConflictsDialog : null);
        }
        if (changes > 0) {
            addChip('edit', t('count-changes', changes), t('changes', changes) + '\n' + t('changes-tip'), 'warning', _self.showChangesDialog);
        }
        if (!status.has_upstream) {
            addChip('push', t('no-upstream'), t('no-upstream'), 'warning');
        } else {
            if (status.ahead > 0) {
                addChip('push', t('count-ahead', status.ahead), t('ahead', status.ahead), 'warning');
            }
            if (status.behind > 0) {
                addChip('pull', t('count-behind', status.behind), t('behind', status.behind), 'warning');
            }
            if (!status.ahead && !status.behind && !status.stale && changes === 0 && !merging) {
                addChip('check', t('up-to-date'), t('up-to-date'), 'ok');
            }
        }
        if (status.stale) {
            addChip('conflicts', t('stale-short'), t('stale') + (status.fetch_error ? '\n' + status.fetch_error : ''), 'stale');
        }

        if (collapsed) {
            return;
        }

        // the one recommended next step => large button, everything else => icon toolbar
        var recommended = null;
        if (merging) {
            recommended = status.conflict_count > 0 ? 'resolve-conflicts' : 'merge-complete';
        } else if (changes > 0) {
            recommended = 'commit';
        } else if (!status.has_upstream || status.ahead > 0) {
            recommended = 'push';
        } else if (status.behind > 0) {
            recommended = 'pull';
        } else if (!status.is_default_branch) {
            recommended = 'merge-into-default';
        }

        var $primary = $('<div class="cms-git-buttons">').appendTo($panel);
        var $toolbar = $('<div class="cms-git-toolbar">').appendTo($panel);
        var $secondary = $('<span class="group secondary">');

        var tooltip = function (key, disabledReason) {
            var tip = t(key, defaultBranch) + ' (' + t(key + '-sub', defaultBranch) + ')\n' + t(key + '-tip', defaultBranch);
            return disabledReason ? tip + '\n\n' + disabledReason : tip;
        };

        var addAction = function (key, iconName, onclick, disabledReason, cls) {
            if (recommended === key && !disabledReason) {
                return $('<button class="cms-git-button action primary">')
                    .html(iconHtml(iconName) + '<span class="text"><span class="cms-git-label">' + esc(t(key, defaultBranch)) + '</span><span class="sub">' + esc(t(key + '-sub', defaultBranch)) + '</span></span>')
                    .attr('title', tooltip(key))
                    .click(function (e) { e.preventDefault(); onclick(); })
                    .appendTo($primary);
            }
            return addTool($toolbar, key, iconName, onclick, disabledReason, cls);
        };

        var addTool = function ($target, key, iconName, onclick, disabledReason, cls) {
            return $('<button class="cms-git-icon-button tool">')
                .html(iconHtml(iconName))
                .attr('title', tooltip(key, disabledReason))
                .attr('aria-label', t(key, defaultBranch))
                .addClass(cls || '')
                .prop('disabled', !!disabledReason)
                .click(function (e) { e.preventDefault(); onclick(); })
                .appendTo($target);
        };

        if (merging) {
            addAction('resolve-conflicts', 'conflicts', _self.showConflictsDialog, null);
            addAction('merge-complete', 'check', _self.completeMerge, status.conflict_count > 0 ? t('disabled-conflicts-remaining') : null);
            addAction('merge-abort', 'close', _self.abortMerge, null, 'danger');
        } else {
            var commitFirst = changes > 0 ? t('disabled-commit-or-discard-first') : null;

            addAction('commit', 'commit', _self.showCommitDialog, changes === 0 ? t('disabled-no-changes') : null);
            addAction('push', 'push', _self.push,
                changes > 0 ? t('disabled-commit-first') : (status.ahead > 0 || !status.has_upstream ? null : t('disabled-nothing-to-push')));
            addAction('pull', 'pull', _self.pull, commitFirst);
            if (!status.is_default_branch) {
                addAction('merge-from-default', 'merge', _self.mergeFromDefault, commitFirst);
                addAction('merge-into-default', 'merge-into', _self.showMergeIntoDefaultDialog, commitFirst);
            }
            addAction('discard-all', 'discard', function () { _self.discard(''); }, changes === 0 ? t('disabled-no-changes') : null, 'danger');

            addTool($secondary, 'branches', 'branch', _self.showBranchesDialog);
        }
        if (changes > 0) {
            $('<button class="cms-git-icon-button tool">')
                .html(iconHtml('diff'))
                .attr('title', t('changes-title') + '\n' + t('changes-tip'))
                .attr('aria-label', t('changes-title'))
                .click(function (e) { e.preventDefault(); _self.showChangesDialog(); })
                .prependTo($secondary);
        }
        addTool($secondary, 'history', 'history', _self.showHistoryDialog);
        if (!merging) {
            addTool($secondary, 'workspaces', 'workspaces', _self.showWorkspacesDialog);
        }
        $secondary.appendTo($toolbar);

        if ($primary.children().length === 0) {
            $primary.remove();
        }
    };
    var renderMergeBanner = function () {
        var $container = $('#main-container');
        var merging = _status && _status.is_merging === true;

        $('body').toggleClass('cms-git-merging', merging);
        $container.children('.cms-git-merge-banner').remove();

        if (!merging || $container.length === 0) {
            return;
        }

        var source = _status.merge_source || '?';
        var $banner = $('<div class="cms-git-merge-banner">').prependTo($container);
        $('<span class="text">')
            .text(_status.conflict_count > 0 ? t('merging-banner', source, _status.conflict_count) : t('merging-banner-resolved', source))
            .appendTo($banner);

        var $buttons = $('<span class="buttons">').appendTo($banner);
        if (_status.conflict_count > 0) {
            $('<button class="cms-git-button primary">').html(labelHtml('conflicts', t('resolve-conflicts'))).appendTo($buttons)
                .click(function (e) { e.preventDefault(); _self.showConflictsDialog(); });
        } else {
            $('<button class="cms-git-button primary">').html(labelHtml('check', t('merge-complete'))).appendTo($buttons)
                .click(function (e) { e.preventDefault(); _self.completeMerge(); });
        }
        $('<button class="cms-git-button danger">').html(labelHtml('close', t('merge-abort'))).appendTo($buttons)
            .click(function (e) { e.preventDefault(); _self.abortMerge(); });
    };

    // node path (data-path) => normalized path relative to the cms root
    var normalizePath = function (path) {
        return String(path || '').replace(/\\/g, '/').replace(/^\/+|\/+$/g, '').toLowerCase();
    };

    // same logic as CmsGitWorkspace.BelongsToNode on the server: a/b owns a/b.* and a/b/**
    var belongsToNode = function (filePath, nodePath) {
        if (!nodePath) {
            return false;
        }
        if (filePath.indexOf(nodePath + '/') === 0) {
            return true;
        }
        return filePath.indexOf(nodePath + '.') === 0 && filePath.indexOf('/', nodePath.length) < 0;
    };

    var isNodeChanged = function (nodePath, changedFiles) {
        var path = normalizePath(nodePath);
        for (var i = 0; i < changedFiles.length; i++) {
            if (belongsToNode(changedFiles[i], path)) {
                return true;
            }
        }
        return false;
    };

    var markNodes = function () {
        var changedFiles = $.map((_status && _status.changes) || [], function (change) {
            return normalizePath(change.path);
        });
        var canDiscard = !(_status && _status.is_merging);

        $('#main-navtree .cms-treenode[data-path]').each(function (i, e) {
            var $node = $(e);
            $node.toggleClass('cms-git-changed', isNodeChanged($node.attr('data-path'), changedFiles));
        });

        $('#main-content .node[data-path]').each(function (i, e) {
            var $node = $(e);
            $node.find('.node-git-discard, .node-git-diff').remove();

            if ($node.hasClass('up') || $node.hasClass('current')) {
                $node.removeClass('cms-git-changed');
                return;
            }

            var path = $node.attr('data-path'), changed = isNodeChanged(path, changedFiles);
            $node.toggleClass('cms-git-changed', changed).attr('title', changed ? t('node-changed') : null);

            if (changed) {
                $('<div class="node-git-diff">')
                    .attr('title', t('diff-show'))
                    .html(iconHtml('diff'))
                    .appendTo($node.children('.node-tools'))
                    .click(function (evt) {
                        evt.stopPropagation();
                        $(this).parent().removeClass('expanded');
                        _self.showNodeDiffDialog(path, $node.attr('data-name') || path);
                    });
            }
            if (changed && canDiscard) {
                $('<div class="node-git-discard">')
                    .attr('title', t('discard'))
                    .text('↺')
                    .appendTo($node.children('.node-tools'))
                    .click(function (evt) {
                        evt.stopPropagation();
                        $(this).parent().removeClass('expanded');
                        _self.discard(path, $node.attr('data-name') || path);
                    });
            }
        });
    };

    this.discard = function (nodePath, displayName, onDone) {
        var text = nodePath ? t('discard-confirm', displayName || nodePath) : t('discard-all-confirm');
        CMS.confirm(esc(text), function () {
            run('discard', { node: nodePath || '' }, function () {
                reloadContent();
                if (typeof onDone === 'function') {
                    onDone();
                }
            });
        });
    };

    this.mergeFromDefault = function () {
        run('mergefromdefault', {}, function () {
            messageThen(t('merge-from-default-success'), reloadTree);
        });
    };

    this.showMergeIntoDefaultDialog = function () {
        CMS.showModal(t('merge-into-default-title'), function ($content) {
            var $dialog = $('<div class="cms-git-dialog">').appendTo($content);

            $('<p>').text(t('merge-into-default-text', (_status && _status.branch) || '?', (_status && _status.default_branch) || '?')).appendTo($dialog);

            var $label = $('<label class="cms-git-checkbox">').appendTo($dialog);
            var $deleteBranch = $('<input type="checkbox">').prop('checked', true).appendTo($label);
            $('<span>').text(' ' + t('merge-delete-branch')).appendTo($label);

            $('<button class="cms-git-button primary">')
                .html(labelHtml('merge-into', t('merge-into-default-button')))
                .appendTo($('<div class="cms-git-dialog-buttons">').appendTo($dialog))
                .click(function () {
                    var deleteBranch = $deleteBranch.prop('checked') === true;
                    CMS.closeModal($content);
                    run('mergeintodefault', { deleteBranch: deleteBranch }, function () {
                        messageThen(t('merge-into-default-success'), reloadTree);
                    });
                });
        });
    };

    this.completeMerge = function () {
        run('completemerge', {}, function () {
            messageThen(t('merge-complete-success'), reloadTree);
        });
    };

    this.abortMerge = function () {
        CMS.confirm(esc(t('merge-abort-confirm')), function () {
            run('abortmerge', {}, function () {
                reloadTree();
            });
        });
    };

    this.showConflictsDialog = function () {
        CMS.showModal(t('conflicts-title'), function ($content) {
            var $dialog = $('<div class="cms-git-dialog cms-git-conflicts-dialog">').appendTo($content);
            renderConflicts($dialog, $content);
        }, null, function () {
            // the tree on the screen may show the state before resolving
            reloadContent();
        });
    };

    var renderConflicts = function ($dialog, $content) {
        $dialog.empty().text(t('please-wait'));

        // "mine" = version of the current branch (HEAD), "theirs" = version of the branch being merged in
        var mineName = (_status && _status.branch) || '?';
        var theirsName = (_status && _status.merge_source) || '?';

        var resolve = function (node, choice) {
            run('resolveconflict', { node: node, choice: choice }, function () {
                renderConflicts($dialog, $content);
            });
        };

        api('conflicts', {}, function (result) {
            $dialog.empty();

            var conflicts = result.conflicts || [];
            if (conflicts.length === 0) {
                $('<p class="cms-git-conflicts-none">').text(t('conflicts-none')).appendTo($dialog);
                $('<button class="cms-git-button primary">')
                    .html(labelHtml('check', t('merge-complete')))
                    .appendTo($('<div class="cms-git-dialog-buttons">').appendTo($dialog))
                    .click(function () {
                        CMS.closeModal($content);
                        _self.completeMerge();
                    });
                return;
            }

            $('<p class="cms-git-conflicts-intro">').text(t('conflicts-intro', theirsName, mineName)).appendTo($dialog);

            var $all = $('<div class="cms-git-dialog-buttons cms-git-conflicts-all">').appendTo($dialog);
            $('<button class="cms-git-button mine">').html(labelHtml('check', t('conflict-all-mine', mineName))).appendTo($all)
                .click(function () { resolve('*', 'mine'); });
            $('<button class="cms-git-button theirs">').html(labelHtml('check', t('conflict-all-theirs', theirsName))).appendTo($all)
                .click(function () { resolve('*', 'theirs'); });

            var $list = $('<ul class="cms-git-conflicts">').appendTo($dialog);
            $.each(conflicts, function (i, conflict) {
                var $li = $('<li>').appendTo($list);

                var $header = $('<div class="header">').appendTo($li);
                $('<span class="name">').text(conflict.node || '/').appendTo($header);
                $('<span class="kind">').text(' (' + t('conflict-kind-' + conflict.kind, mineName, theirsName) + ')').appendTo($header);

                var $actions = $('<div class="actions">').appendTo($li);
                $('<button class="cms-git-button mine">').html(labelHtml('check', t('conflict-use-mine', mineName))).appendTo($actions)
                    .click(function () { resolve(conflict.node, 'mine'); });
                $('<button class="cms-git-button theirs">').html(labelHtml('check', t('conflict-use-theirs', theirsName))).appendTo($actions)
                    .click(function () { resolve(conflict.node, 'theirs'); });

                $.each(conflict.files || [], function (j, file) {
                    $('<div class="file">').text(file.path).appendTo($li);
                    renderDiffView(file.mine, file.theirs, mineName, theirsName, false, file.path).appendTo($li);
                });
            });
        }, function () {
            $dialog.empty();
        });
    };

    // simplified side-by-side line diff (mine | theirs), unchanged blocks are collapsed
    var diffLines = function (a, b) {
        var start = 0, endA = a.length, endB = b.length;
        while (start < endA && start < endB && a[start] === b[start]) start++;
        while (endA > start && endB > start && a[endA - 1] === b[endB - 1]) { endA--; endB--; }

        var rows = [], i, j;
        for (i = 0; i < start; i++) rows.push({ same: true, a: a[i], b: b[i] });

        var midA = a.slice(start, endA), midB = b.slice(start, endB), n = midA.length, m = midB.length;
        var ops = [];
        if (n * m > 4000000) {
            // too big for LCS => everything in the middle counts as changed
            for (i = 0; i < n; i++) ops.push({ del: midA[i] });
            for (j = 0; j < m; j++) ops.push({ ins: midB[j] });
        } else {
            var lcs = [];
            for (i = 0; i <= n; i++) lcs.push(new Uint32Array(m + 1));
            for (i = n - 1; i >= 0; i--) {
                for (j = m - 1; j >= 0; j--) {
                    lcs[i][j] = midA[i] === midB[j] ? lcs[i + 1][j + 1] + 1 : Math.max(lcs[i + 1][j], lcs[i][j + 1]);
                }
            }
            i = 0; j = 0;
            while (i < n || j < m) {
                if (i < n && j < m && midA[i] === midB[j]) { ops.push({ same: midA[i] }); i++; j++; }
                else if (i < n && (j >= m || lcs[i + 1][j] >= lcs[i][j + 1])) { ops.push({ del: midA[i] }); i++; }
                else { ops.push({ ins: midB[j] }); j++; }
            }
        }

        // pair consecutive deletions/insertions side by side
        var dels = [], inss = [];
        var flush = function () {
            for (var k = 0; k < Math.max(dels.length, inss.length); k++) {
                rows.push({ same: false, a: k < dels.length ? dels[k] : null, b: k < inss.length ? inss[k] : null });
            }
            dels = []; inss = [];
        };
        $.each(ops, function (k, op) {
            if (op.same !== undefined) { flush(); rows.push({ same: true, a: op.same, b: op.same }); }
            else if (op.del !== undefined) dels.push(op.del);
            else inss.push(op.ins);
        });
        flush();

        for (i = endA; i < a.length; i++) rows.push({ same: true, a: a[i], b: b[i - endA + endB] });
        return rows;
    };

    var splitLines = function (text) {
        return text === null || text === undefined ? [] : String(text).replace(/\r\n/g, '\n').replace(/\n$/, '').split('\n');
    };

    var diffHeadText = function (name, content, plainNames) {
        return plainNames === true ? name : t('conflict-version', name) + (content === null || content === undefined ? ' ' + t('conflict-deleted') : '');
    };

    // diff view with switch: table of properties (default) or xml text
    var DiffModeStorageKey = 'cms-git-diff-mode', DiffShowAllStorageKey = 'cms-git-diff-show-all';
    var storageGet = function (key) { try { return window.localStorage.getItem(key); } catch (e) { return null; } };
    var storageSet = function (key, value) { try { window.localStorage.setItem(key, value); } catch (e) { } };

    var renderDiffView = function (before, after, beforeName, afterName, plainNames, path) {
        var $view = $('<div class="cms-git-diff-view">');

        // not property xml (eg. .itemorder.xml, conflict markers) => text only
        var fileName = String(path || '').split('/').pop().toLowerCase();
        var textOnly = fileName.indexOf('.itemorder') === 0 ||
            parseXmlProperties(before) === null ||
            parseXmlProperties(after) === null;
        if (textOnly) {
            return $view.append(renderDiff(before, after, beforeName, afterName, plainNames));
        }

        var render = function () {
            var mode = storageGet(DiffModeStorageKey) === 'xml' ? 'xml' : 'table';
            var showAll = storageGet(DiffShowAllStorageKey) === '1';
            $view.empty();

            var $bar = $('<div class="cms-git-diff-bar">').appendTo($view);
            var $switch = $('<span class="cms-git-diff-switch">').attr('title', t('diff-view-tip')).appendTo($bar);
            $.each(['table', 'xml'], function (i, m) {
                $('<button>').text(t('diff-view-' + m)).toggleClass('selected', mode === m).appendTo($switch)
                    .click(function (e) {
                        e.preventDefault();
                        storageSet(DiffModeStorageKey, m);
                        render();
                    });
            });
            if (mode === 'table') {
                var $label = $('<label class="cms-git-checkbox">').appendTo($bar);
                $('<input type="checkbox">').prop('checked', showAll).appendTo($label)
                    .change(function () {
                        storageSet(DiffShowAllStorageKey, $(this).prop('checked') ? '1' : '0');
                        render();
                    });
                $('<span>').text(' ' + t('diff-show-all')).appendTo($label);

                $view.append(renderPropertyDiff(before, after,
                    diffHeadText(beforeName, before, plainNames), diffHeadText(afterName, after, plainNames), showAll));
            } else {
                $view.append(renderDiff(before, after, beforeName, afterName, plainNames));
            }
        };
        render();

        return $view;
    };

    var TechnicalAttributes = ['schemanode'];

    // xml => [{ key: 'a/b', value: '...' }] (element names as property names), null if not valid xml
    var parseXmlProperties = function (text) {
        if (text === null || text === undefined || $.trim(text) === '') {
            return [];
        }
        var doc;
        try {
            doc = new DOMParser().parseFromString(String(text), 'application/xml');
        } catch (e) {
            return null;
        }
        if (!doc || !doc.documentElement || doc.getElementsByTagName('parsererror').length > 0) {
            return null;
        }

        var properties = [];
        var elements = function (node) {
            return $.grep(node.childNodes, function (n) { return n.nodeType === 1; });
        };
        var addAttributes = function (element, prefix) {
            $.each(element.attributes || [], function (i, attr) {
                if ($.inArray(attr.name.toLowerCase(), TechnicalAttributes) < 0) {
                    properties.push({ key: prefix + '@' + attr.name, value: attr.value });
                }
            });
        };
        var walk = function (element, prefix) {
            var children = elements(element), counts = {}, index = {};
            $.each(children, function (i, child) {
                counts[child.nodeName] = (counts[child.nodeName] || 0) + 1;
            });
            $.each(children, function (i, child) {
                var name = child.nodeName;
                index[name] = (index[name] || 0) + 1;
                var key = prefix + name + (counts[name] > 1 ? '[' + index[name] + ']' : '');

                addAttributes(child, key + '/');
                if (elements(child).length > 0) {
                    walk(child, key + '/');
                } else {
                    properties.push({ key: key, value: child.textContent });
                }
            });
        };
        addAttributes(doc.documentElement, '');
        walk(doc.documentElement, '');

        return properties;
    };

    var MaxValueLength = 300;

    var valueCell = function (value, cls) {
        var $td = $('<td class="value">').addClass(cls || '');
        if (value === null || value === undefined) {
            return $td.addClass('empty');
        }
        value = String(value);
        if (value.length <= MaxValueLength) {
            return $td.text(value);
        }
        var $text = $('<span>').text(value.substr(0, MaxValueLength)).appendTo($td);
        $('<a href="#" class="more">').text(' ' + t('diff-more')).appendTo($td)
            .click(function (e) {
                e.preventDefault();
                $text.text(value);
                $(this).remove();
            });
        return $td;
    };

    var renderPropertyDiff = function (before, after, beforeHead, afterHead, showAll) {
        var a = parseXmlProperties(before) || [], b = parseXmlProperties(after) || [];
        var mapA = {}, mapB = {}, keys = [], seen = {};
        $.each(a, function (i, p) { mapA[p.key] = p.value; });
        $.each(b, function (i, p) { mapB[p.key] = p.value; });

        // order of the new version, removed properties after their old predecessor
        var addKey = function (key) { if (!seen[key]) { seen[key] = true; keys.push(key); } };
        var ia = 0;
        $.each(b, function (i, p) {
            if (mapA.hasOwnProperty(p.key)) {
                while (ia < a.length && a[ia].key !== p.key) {
                    if (!mapB.hasOwnProperty(a[ia].key)) addKey(a[ia].key);
                    ia++;
                }
                ia++;
            }
            addKey(p.key);
        });
        for (; ia < a.length; ia++) {
            if (!mapB.hasOwnProperty(a[ia].key)) addKey(a[ia].key);
        }
        $.each(a, function (i, p) { addKey(p.key); });

        var $table = $('<table class="cms-git-prop-diff">');
        $('<tr>')
            .append($('<th class="prop">').text(t('diff-property')))
            .append($('<th class="mine">').text(beforeHead))
            .append($('<th class="theirs">').text(afterHead))
            .appendTo($('<thead>').appendTo($table));

        var $body = $('<tbody>').appendTo($table), visible = 0;
        $.each(keys, function (i, key) {
            var inA = mapA.hasOwnProperty(key), inB = mapB.hasOwnProperty(key);
            var state = !inA ? 'added' : !inB ? 'removed' : mapA[key] !== mapB[key] ? 'changed' : 'same';
            if (state === 'same' && !showAll) {
                return;
            }
            visible++;
            $('<tr>').addClass(state)
                .append($('<td class="prop">').text(key))
                .append(valueCell(inA ? mapA[key] : null, 'mine'))
                .append(valueCell(inB ? mapB[key] : null, 'theirs'))
                .appendTo($body);
        });
        if (visible === 0) {
            $('<tr>').append($('<td colspan="3" class="none">').text(t('diff-no-property-changes'))).appendTo($body);
        }

        return $('<div class="cms-git-diff-container">').append($table);
    };

    var renderDiff = function (mine, theirs, mineName, theirsName, plainNames) {
        var context = 3;
        var rows = diffLines(splitLines(mine), splitLines(theirs));

        var $table = $('<table class="cms-git-diff">');
        $('<colgroup><col class="ln"><col><col class="ln"><col></colgroup>').appendTo($table);
        var $head = $('<tr>').appendTo($('<thead>').appendTo($table));
        var addHead = function (cls, name, content) {
            $('<th colspan="2">').addClass(cls)
                .text(diffHeadText(name, content, plainNames))
                .appendTo($head);
        };
        addHead('mine', mineName, mine);
        addHead('theirs', theirsName, theirs);

        var lineA = 0, lineB = 0;
        var addRow = function (row, $target) {
            var $tr = $('<tr>').toggleClass('changed', !row.same).appendTo($target);
            var addCell = function (cls, text, lineNo) {
                $('<td class="ln">').text(text === null ? '' : lineNo).appendTo($tr);
                $('<td class="code">').addClass(cls).toggleClass('empty', text === null).text(text === null ? '' : text).appendTo($tr);
            };
            addCell('mine', row.a, row.a !== null ? ++lineA : null);
            addCell('theirs', row.b, row.b !== null ? ++lineB : null);
        };

        // collapse long unchanged blocks (keep some lines of context around changes)
        var $body = $('<tbody>').appendTo($table);
        var i = 0, k;
        while (i < rows.length) {
            if (!rows[i].same) { addRow(rows[i], $body); i++; continue; }

            var j = i;
            while (j < rows.length && rows[j].same) j++;
            var showHead = i === 0 ? 0 : context, showTail = j === rows.length ? 0 : context;

            if (j - i <= showHead + showTail + 1) {
                for (k = i; k < j; k++) addRow(rows[k], $body);
            } else {
                for (k = i; k < i + showHead; k++) addRow(rows[k], $body);

                var $more = $('<tbody class="more">').appendTo($table);
                var $hidden = $('<tbody class="collapsed">').appendTo($table);
                for (k = i + showHead; k < j - showTail; k++) addRow(rows[k], $hidden);
                (function ($more, $hidden) {
                    $('<td colspan="4">')
                        .text(t('diff-unchanged-lines', j - i - showHead - showTail))
                        .appendTo($('<tr>').appendTo($more))
                        .click(function () { $more.remove(); $hidden.removeClass('collapsed'); });
                })($more, $hidden);

                $body = $('<tbody>').appendTo($table);
                for (k = j - showTail; k < j; k++) addRow(rows[k], $body);
            }
            i = j;
        }

        return $('<div class="cms-git-diff-container">').append($table);
    };

    this.pull = function () {
        run('pull', {}, function () {
            messageThen(t('pull-success'), reloadTree);
        });
    };

    this.push = function () {
        run('push', {}, function () {
            // a push can include a merge of remote changes => reload the tree
            messageThen(t('push-success'), reloadTree);
        });
    };

    var messageThen = function (text, callback) {
        if (window.bootbox) {
            bootbox.alert(text, callback);
        } else {
            alert(text);
            callback();
        }
    };

    this.showCommitDialog = function () {
        CMS.showModal(t('commit-title'), function ($content) {
            var $form = $('<div class="cms-git-dialog cms-git-commit-dialog">').appendTo($content);

            var $list = $('<div class="cms-git-node-list-container">').appendTo($form);

            $('<label>').text(t('commit-message')).appendTo($form);
            var $message = $('<textarea rows="4" class="cms-git-message">')
                .attr('placeholder', t('commit-message-placeholder'))
                .attr('title', t('commit-ctrl-enter'))
                .appendTo($form);

            var commit = function () {
                var message = $.trim($message.val());
                if (!message) {
                    CMS.alert(_self.l10n['error-message-required'] || t('commit-message'));
                    return;
                }
                run('commit', { message: message }, function () {
                    CMS.closeModal($content);
                });
            };

            $message.keydown(function (e) {
                if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
                    e.preventDefault();
                    commit();
                }
            });

            $('<button class="cms-git-button primary">')
                .html(labelHtml('commit', t('commit-button')))
                .attr('title', t('commit-ctrl-enter'))
                .appendTo($('<div class="cms-git-dialog-buttons">').appendTo($form))
                .click(commit);

            var suggested = null;
            loadNodeList($list, $content, null, function (nodes) {
                if (nodes.length === 0) {
                    CMS.closeModal($content);
                    return;
                }
                // replace the suggestion only, if the user has not edited it yet
                var current = $.trim($message.val());
                if (current === '' || current === suggested) {
                    suggested = suggestCommitMessage(nodes);
                    $message.val(suggested).focus().select();
                }
            });

            setTimeout(function () { $message.focus(); }, 100);
        });
    };

    // "Changed: A, B; New: C; Deleted: D"
    var suggestCommitMessage = function (nodes) {
        var MaxNames = 5;
        var groups = { modified: [], added: [], deleted: [] };
        $.each(nodes, function (i, node) {
            var list = groups[node.state] || groups.modified;
            var name = nodeName(node);
            if ($.inArray(name, list) < 0) {
                list.push(name);
            }
        });

        var parts = [];
        $.each(['modified', 'added', 'deleted'], function (i, state) {
            var names = groups[state];
            if (names.length === 0) {
                return;
            }
            var text = t('commit-message-' + state) + ': ' + names.slice(0, MaxNames).join(', ');
            if (names.length > MaxNames) {
                text += ' ' + t('commit-message-more', names.length - MaxNames);
            }
            parts.push(text);
        });
        return parts.join('; ');
    };

    var nodeName = function (node) {
        return node.name || (node.node ? node.node.split('/').pop() : '/');
    };

    // list of all changed nodes (or of one node and its subtree) with diffs, discard and "go to node"
    var loadNodeList = function ($target, $content, filterNode, onLoaded, expandAll) {
        $target.empty().text(t('please-wait'));

        api('changednodes', {}, function (result) {
            var nodes = result.nodes || [];
            if (filterNode !== null && filterNode !== undefined) {
                var filter = normalizePath(filterNode);
                nodes = $.grep(nodes, function (node) {
                    var path = normalizePath(node.node);
                    return path === filter || (filter && path.indexOf(filter + '/') === 0);
                });
            }

            $target.empty();
            if (nodes.length === 0) {
                $('<div class="cms-git-intro">').text(t('changes-none')).appendTo($target);
            } else {
                renderNodeList(nodes, $content, expandAll === true, function () {
                    loadNodeList($target, $content, filterNode, onLoaded, expandAll);
                }).appendTo($target);
            }

            if (typeof onLoaded === 'function') {
                onLoaded(nodes);
            }
        }, function () {
            $target.empty();
        });
    };

    var renderNodeList = function (nodes, $content, expandAll, reload) {
        var canDiscard = !(_status && _status.is_merging);
        var $list = $('<ul class="cms-git-node-list">');

        $.each(nodes, function (i, node) {
            var $li = $('<li>').addClass(node.state).appendTo($list);
            var $row = $('<div class="row">').appendTo($li);
            var $files = null;

            $('<span class="state">').text(t('change-' + node.state)).appendTo($row);

            var toggle = function () {
                if ($files) {
                    $files.remove();
                    $files = null;
                    $li.removeClass('expanded');
                    return;
                }
                $li.addClass('expanded');
                $files = $('<div class="files">').appendTo($li);
                $.each(node.files || [], function (j, file) {
                    renderWorkingFile(file).appendTo($files);
                });
            };

            var $name = $('<a href="#" class="name">').appendTo($row)
                .attr('title', '/' + (node.node || ''))
                .click(function (e) { e.preventDefault(); toggle(); });
            $('<span class="text">').text(nodeName(node)).appendTo($name);
            $('<span class="path">').text('/' + (node.node || '')).appendTo($name);

            var $meta = $('<span class="meta">').appendTo($row);
            if (node.order_changed) {
                $('<span class="order">').text(t('order-changed')).appendTo($meta);
            }
            $('<span class="count">').text(t('changes-files', (node.files || []).length)).appendTo($meta);

            var $actions = $('<span class="actions">').appendTo($row);
            $('<button class="cms-git-icon-button">').html(iconHtml('diff')).attr('title', t('diff-show')).appendTo($actions)
                .click(function (e) { e.preventDefault(); toggle(); });
            if (canDiscard && node.node) {
                $('<button class="cms-git-icon-button danger">').html(iconHtml('discard')).attr('title', t('discard')).appendTo($actions)
                    .click(function (e) {
                        e.preventDefault();
                        _self.discard(node.node, nodeName(node), reload);
                    });
            }
            if (node.state !== 'deleted' && node.node) {
                $('<button class="cms-git-icon-button">').html(iconHtml('goto')).attr('title', t('changes-goto')).appendTo($actions)
                    .click(function (e) {
                        e.preventDefault();
                        CMS.closeModal($content);
                        gotoNode(node.node);
                    });
            }

            if (expandAll) {
                toggle();
            }
        });

        return $list;
    };

    var renderWorkingFile = function (file) {
        var $file = $('<div class="file">').addClass(file.state);
        var $diff = null;
        var $title = $('<a href="#" class="file-title">').appendTo($file);
        $('<span class="state">').text(t('change-' + file.state)).appendTo($title);
        $('<span class="path">').text(file.path).appendTo($title);

        var load = function () {
            $diff = $('<div class="diff">').text(t('please-wait')).appendTo($file);
            api('workingfilediff', { path: file.path }, function (r) {
                if (!$diff) return;
                $diff.empty().append(renderDiffView(r.diff.before, r.diff.after, t('diff-before'), t('diff-working'), true, file.path));
            }, function () {
                if ($diff) { $diff.remove(); $diff = null; }
            });
        };
        $title.click(function (e) {
            e.preventDefault();
            if ($diff) {
                $diff.remove();
                $diff = null;
            } else {
                load();
            }
        });
        load();

        return $file;
    };

    // navigate to the parent folder and highlight the node
    // like the cms path restore (site-cms.js): walk down level by level, a direct jump does not rebuild the tree correctly
    var gotoNode = function (nodePath) {
        if (typeof window.updateContent !== 'function') {
            return;
        }
        var segments = $.grep(String(nodePath || '').split('/'), function (s) { return s !== ''; });
        var target = normalizePath(segments.join('/'));

        // the path as used by the cms (data-path of the node tile), matched case insensitive
        var findTile = function (path) {
            var $found = $();
            $('#main-content .node[data-path]').each(function (i, e) {
                var $node = $(e);
                if (!$node.hasClass('up') && !$node.hasClass('current') && normalizePath($node.attr('data-path')) === path) {
                    $found = $node;
                    return false;
                }
            });
            return $found;
        };

        var highlight = function () {
            var $node = findTile(target);
            if ($node.length === 0) {
                return;
            }
            if ($node[0].scrollIntoView) {
                $node[0].scrollIntoView({ block: 'center' });
            }
            $node.addClass('cms-git-highlight');
            setTimeout(function () { $node.removeClass('cms-git-highlight'); }, 2500);
        };

        var walk = function (path, index) {
            if (index >= segments.length - 1) {
                highlight();
                return;
            }
            var next = normalizePath(segments.slice(0, index + 1).join('/'));
            var $tile = findTile(next);
            var nextPath = $tile.length > 0 ? $tile.attr('data-path') : '/' + segments.slice(0, index + 1).join('/');
            window.updateContent(nextPath, function () {
                walk(nextPath, index + 1);
            });
        };

        window.updateContent('', function () {
            walk('', 0);
        });
    };

    this.showChangesDialog = function () {
        CMS.showModal(t('changes-title'), function ($content) {
            var $dialog = $('<div class="cms-git-dialog cms-git-changes-dialog">').appendTo($content);
            var $list = $('<div class="cms-git-node-list-container">').appendTo($dialog);
            var $buttons = $('<div class="cms-git-dialog-buttons">').appendTo($dialog);
            var merging = _status && _status.is_merging;

            loadNodeList($list, $content, null, function (nodes) {
                $buttons.empty();
                if (nodes.length > 0 && !merging) {
                    $('<button class="cms-git-button primary">')
                        .html(labelHtml('commit', t('commit')))
                        .appendTo($buttons)
                        .click(function () {
                            CMS.closeModal($content);
                            _self.showCommitDialog();
                        });
                }
            });
        });
    };

    this.showNodeDiffDialog = function (nodePath, displayName) {
        CMS.showModal(t('changes-title') + ': ' + (displayName || nodePath), function ($content) {
            var $dialog = $('<div class="cms-git-dialog cms-git-changes-dialog">').appendTo($content);
            var $list = $('<div class="cms-git-node-list-container">').appendTo($dialog);
            loadNodeList($list, $content, nodePath, null, true);
        });
    };

    this.showBranchesDialog = function () {
        CMS.showModal(t('branches-title'), function ($content) {
            var $dialog = $('<div class="cms-git-dialog">').appendTo($content);
            renderBranches($dialog, $content);
        });
    };

    var renderBranches = function ($dialog, $content) {
        $dialog.empty().text(t('please-wait'));

        api('branches', {}, function (result) {
            $dialog.empty();

            var dirty = _status && _status.changes && _status.changes.length > 0;

            // new branch
            $('<label>').text(t('branch-new')).appendTo($dialog);
            var $new = $('<div class="cms-git-new-branch">').appendTo($dialog);
            var $name = $('<input type="text" class="cms-git-branch-name">')
                .attr('placeholder', t('branch-new-placeholder'))
                .val((_status && _status.suggested_branch_prefix) || '')
                .appendTo($new);
            $('<button class="cms-git-button primary">')
                .html(labelHtml('plus', t('branch-create')))
                .appendTo($new)
                .click(function () {
                    var name = $.trim($name.val());
                    if (!name) return;
                    run('createbranch', { name: name }, function (r) {
                        if (r.status && r.status.stale) {
                            CMS.message(t('branch-created-offline'));
                        }
                        renderBranches($dialog, $content);
                    });
                });

            // existing branches
            var $list = $('<ul class="cms-git-branches">').appendTo($dialog);
            $.each(result.branches || [], function (i, branch) {
                var $li = $('<li>').toggleClass('current', branch.is_current).appendTo($list);
                $('<span class="name">').text(branch.name).appendTo($li);

                var tags = [];
                if (branch.is_current) tags.push(t('branch-current'));
                if (branch.is_default) tags.push(t('branch-default'));
                if (!branch.is_local) tags.push(t('branch-remote-only'));
                if (tags.length) {
                    $('<span class="tags">').text(' (' + tags.join(', ') + ')').appendTo($li);
                }
                if (branch.last_commit_author || branch.last_commit_date) {
                    $('<div class="meta">')
                        .text([branch.last_commit_author, branch.last_commit_date ? new Date(branch.last_commit_date).toLocaleString() : null].filter(Boolean).join(' · '))
                        .appendTo($li);
                }

                if (!branch.is_current) {
                    var $actions = $('<div class="actions">').appendTo($li);
                    $('<button class="cms-git-button">')
                        .html(labelHtml('switch', t('branch-switch')))
                        .prop('disabled', dirty)
                        .attr('title', dirty ? (_self.l10n['error-commit-first'] || '') : '')
                        .appendTo($actions)
                        .click(function () {
                            run('checkout', { name: branch.name }, function () {
                                CMS.closeModal($content);
                                reloadTree();
                            });
                        });
                    if (!branch.is_default) {
                        $('<button class="cms-git-button danger">')
                            .html(labelHtml('trash', t('branch-delete')))
                            .appendTo($actions)
                            .click(function () {
                                CMS.confirm(esc(t('branch-delete-confirm', branch.name)), function () {
                                    run('deletebranch', { name: branch.name, deleteRemote: true }, function () {
                                        renderBranches($dialog, $content);
                                    });
                                });
                            });
                    }
                }
            });
        }, function () {
            $dialog.empty();
        });
    };

    // files of the working copy changed on the server => reload the whole tree
    var reloadTree = function () {
        document.location.reload();
    };

    this.showWorkspacesDialog = function () {
        CMS.showModal(t('workspaces-title'), function ($content) {
            var $dialog = $('<div class="cms-git-dialog">').appendTo($content);
            renderWorkspaces($dialog, $content);
        });
    };

    var renderWorkspaces = function ($dialog, $content) {
        $dialog.empty().text(t('please-wait'));

        api('workspaces', {}, function (result) {
            $dialog.empty();

            $('<div class="cms-git-intro">').text(t('workspaces-intro')).appendTo($dialog);

            var deployRunning = result.deploy_running_by !== null && result.deploy_running_by !== undefined;
            if (deployRunning) {
                $('<div class="cms-git-info warning">').text(t('deploy-git-running', result.deploy_running_by)).appendTo($dialog);
            }

            var workspaces = result.workspaces || [];
            if (workspaces.length === 0) {
                $('<div class="cms-git-info">').text(t('workspaces-none')).appendTo($dialog);
            } else {
                var $table = $('<table class="cms-git-workspaces">').appendTo($dialog);
                $('<tr>')
                    .append($('<th>').text(t('workspace-user')))
                    .append($('<th>').text(t('workspace-branch')))
                    .append($('<th>').text(t('workspace-state')))
                    .append($('<th>').text(t('workspace-last-modified')))
                    .append($('<th>'))
                    .appendTo($table);

                $.each(workspaces, function (i, ws) {
                    var state = [];
                    if (ws.error) {
                        state.push(ws.error);
                    } else {
                        if (ws.is_merging) state.push(t('workspace-merging'));
                        if (ws.changes > 0) state.push(t('changes', ws.changes));
                        if (ws.ahead > 0) state.push(t('ahead', ws.ahead));
                        if (!ws.has_upstream) state.push(t('no-upstream'));
                        if (state.length === 0) state.push(t('workspace-clean'));
                    }
                    var dirty = !!ws.error || ws.is_merging || ws.changes > 0 || ws.ahead > 0 || !ws.has_upstream;

                    var $row = $('<tr>').toggleClass('current', ws.is_current_user === true).appendTo($table);
                    $('<td>').text(ws.name + (ws.is_current_user ? ' ' + t('workspace-current-user') : '')).appendTo($row);
                    $('<td>').text(ws.branch || '').appendTo($row);
                    $('<td>').toggleClass('warning', dirty).text(state.join(', ')).appendTo($row);
                    $('<td>').text(ws.last_modified ? new Date(ws.last_modified).toLocaleString() : '').appendTo($row);
                    $('<button class="cms-git-button danger">')
                        .html(labelHtml('trash', t('workspace-delete')))
                        .appendTo($('<td>').appendTo($row))
                        .click(function () {
                            var text = ws.is_current_user ? t('workspace-delete-own-confirm') : t('workspace-delete-confirm', ws.name);
                            CMS.confirm(esc(text), function () {
                                run('deleteworkspace', { name: ws.name }, function () {
                                    if (ws.is_current_user) {
                                        messageThen(t('workspace-deleted'), reloadTree);
                                    } else {
                                        renderWorkspaces($dialog, $content);
                                    }
                                });
                            });
                        });
                });
            }

            var $buttons = $('<div class="cms-git-dialog-buttons">').appendTo($dialog);
            $('<button class="cms-git-button danger">')
                .html(labelHtml('refresh', t('deploy-workspace-reset')))
                .prop('disabled', deployRunning)
                .appendTo($buttons)
                .click(function () {
                    CMS.confirm(esc(t('deploy-workspace-reset-confirm')), function () {
                        run('resetdeployworkspace', {}, function () {
                            CMS.message(t('deploy-workspace-reset-success'));
                        });
                    });
                });
        }, function () {
            $dialog.empty();
        });
    };

    // ---------- history (commit graph) ----------

    var HistoryPageSize = 100, RowHeight = 24, LaneWidth = 14, LaneOffset = 10;
    var LaneColors = ['#1f77b4', '#d62728', '#2ca02c', '#9467bd', '#ff7f0e', '#17becf', '#8c564b', '#e377c2', '#7f7f7f', '#bcbd22'];

    this.showHistoryDialog = function () {
        CMS.showModal(t('history-title'), function ($content) {
            var $dialog = $('<div class="cms-git-dialog cms-git-history-dialog">').appendTo($content);
            var state = { limit: HistoryPageSize, allBranches: true, selected: null };
            renderHistory($dialog, state, true);
        });
    };

    // lane layout: commits are ordered topologically (children before parents)
    var computeGraph = function (commits) {
        var lanes = [], rows = [], maxLanes = 1;

        $.each(commits, function (i, commit) {
            var col = lanes.indexOf(commit.sha);
            if (col < 0) {
                col = lanes.indexOf(null);
                if (col < 0) col = lanes.length;
            }

            // all lanes waiting for this commit end here
            for (var k = 0; k < lanes.length; k++) {
                if (lanes[k] === commit.sha) lanes[k] = null;
            }

            var startsAtNode = {}, extraEdges = [];
            var parents = commit.parents || [];
            if (parents.length > 0) {
                lanes[col] = parents[0];
                startsAtNode[col] = true;
            } else if (col < lanes.length) {
                lanes[col] = null;
            }

            for (var p = 1; p < parents.length; p++) {
                var existing = lanes.indexOf(parents[p]);
                if (existing >= 0) {
                    extraEdges.push(existing);
                } else {
                    var free = lanes.indexOf(null);
                    if (free < 0) free = lanes.length;
                    lanes[free] = parents[p];
                    startsAtNode[free] = true;
                }
            }

            while (lanes.length > 0 && lanes[lanes.length - 1] === null) lanes.pop();

            rows.push({ col: col, lanesOut: lanes.slice(), startsAtNode: startsAtNode, extraEdges: extraEdges });
            maxLanes = Math.max(maxLanes, lanes.length, col + 1);
        });

        return { rows: rows, maxLanes: maxLanes };
    };

    var laneX = function (lane) { return LaneOffset + lane * LaneWidth; };
    var rowY = function (row) { return row * RowHeight + RowHeight / 2; };

    var renderGraphSvg = function (commits, graph, history) {
        var ns = 'http://www.w3.org/2000/svg';
        var width = laneX(graph.maxLanes - 1) + LaneOffset;
        var height = commits.length * RowHeight;

        var svg = document.createElementNS(ns, 'svg');
        svg.setAttribute('width', width);
        svg.setAttribute('height', height);
        svg.setAttribute('class', 'cms-git-graph');

        var line = function (x1, y1, x2, y2, color) {
            var path = document.createElementNS(ns, 'path');
            var my = (y1 + y2) / 2;
            path.setAttribute('d', x1 === x2
                ? 'M' + x1 + ' ' + y1 + ' L' + x2 + ' ' + y2
                : 'M' + x1 + ' ' + y1 + ' C' + x1 + ' ' + my + ' ' + x2 + ' ' + my + ' ' + x2 + ' ' + y2);
            path.setAttribute('stroke', color);
            path.setAttribute('stroke-width', '2');
            path.setAttribute('fill', 'none');
            svg.appendChild(path);
        };

        $.each(graph.rows, function (i, row) {
            var next = commits[i + 1];
            var y1 = rowY(i), y2 = next ? rowY(i + 1) : height;
            var targetX = function (lane) {
                return next && next.sha === row.lanesOut[lane] ? laneX(graph.rows[i + 1].col) : laneX(lane);
            };

            $.each(row.lanesOut, function (lane, sha) {
                if (sha === null) return;
                var fromX = row.startsAtNode[lane] ? laneX(row.col) : laneX(lane);
                line(fromX, y1, targetX(lane), y2, LaneColors[lane % LaneColors.length]);
            });
            $.each(row.extraEdges, function (j, lane) {
                line(laneX(row.col), y1, targetX(lane), y2, LaneColors[lane % LaneColors.length]);
            });
        });

        $.each(graph.rows, function (i, row) {
            var commit = commits[i];
            var color = LaneColors[row.col % LaneColors.length];
            var isHead = commit.sha === history.head;

            var circle = document.createElementNS(ns, 'circle');
            circle.setAttribute('cx', laneX(row.col));
            circle.setAttribute('cy', rowY(i));
            circle.setAttribute('r', isHead ? 6 : 4);
            circle.setAttribute('stroke', color);
            circle.setAttribute('stroke-width', isHead ? 3 : 2);
            circle.setAttribute('fill', commit.unpushed ? '#fff' : color);
            if (commit.unpushed) {
                var title = document.createElementNS(ns, 'title');
                title.textContent = t('history-unpushed');
                circle.appendChild(title);
            }
            svg.appendChild(circle);
        });

        return svg;
    };

    var renderHistory = function ($dialog, state, fetch) {
        $dialog.empty().text(t('please-wait'));

        api('history', { fetch: fetch === true, allBranches: state.allBranches, limit: state.limit }, function (result) {
            var history = result.history || {};
            var commits = history.commits || [];
            $dialog.empty();

            var $toolbar = $('<div class="cms-git-history-toolbar">').appendTo($dialog);
            $('<label>')
                .append($('<input type="checkbox">').prop('checked', state.allBranches).change(function () {
                    state.allBranches = $(this).is(':checked');
                    renderHistory($dialog, state, false);
                }))
                .append(document.createTextNode(' ' + t('history-all-branches')))
                .appendTo($toolbar);
            $('<span class="legend">')
                .append($('<span class="node unpushed">'))
                .append(document.createTextNode(' ' + t('history-unpushed')))
                .appendTo($toolbar);

            if (history.stale) {
                $('<div class="cms-git-history-warning">').text(t('stale')).attr('title', history.fetch_error || '').appendTo($dialog);
            }

            if (commits.length === 0) {
                $('<div class="cms-git-intro">').text(t('history-none')).appendTo($dialog);
                return;
            }

            var graph = computeGraph(commits);
            var graphWidth = laneX(graph.maxLanes - 1) + LaneOffset;

            var $scroll = $('<div class="cms-git-history-scroll">').appendTo($dialog);
            var $list = $('<div class="cms-git-history">').appendTo($scroll);
            $list.append(renderGraphSvg(commits, graph, history));

            var $details = $('<div class="cms-git-history-details">');

            $.each(commits, function (i, commit) {
                var $row = $('<div class="row">')
                    .css({ height: RowHeight + 'px', paddingLeft: (graphWidth + 6) + 'px' })
                    .attr('data-sha', commit.sha)
                    .toggleClass('unpushed', commit.unpushed === true)
                    .toggleClass('selected', commit.sha === state.selected)
                    .appendTo($list)
                    .click(function () {
                        state.selected = commit.sha;
                        $list.children('.row').removeClass('selected');
                        $row.addClass('selected');
                        renderCommitDetails($details, commit.sha);
                    });

                var $labels = $('<span class="labels">').appendTo($row);
                if (commit.sha === history.head) {
                    $('<span class="ref head">').text(t('history-head')).appendTo($labels);
                }
                $.each(commit.refs || [], function (j, ref) {
                    $('<span class="ref">').addClass(ref.type)
                        .toggleClass('current', ref.type === 'local' && ref.name === history.branch)
                        .text(ref.name).appendTo($labels);
                });
                if (history.deployed && commit.sha === history.deployed) {
                    $('<span class="ref deployed">').text(t('history-deployed')).appendTo($labels);
                }

                $('<span class="message">').text(commit.message || '').attr('title', commit.message || '').appendTo($row);
                $('<span class="meta">')
                    .text((commit.author || '') + ', ' + (commit.date ? new Date(commit.date).toLocaleString() : ''))
                    .appendTo($row);
            });

            if (history.has_more) {
                $('<button class="cms-git-button">')
                    .html(labelHtml('more', t('history-load-more')))
                    .appendTo($('<div class="cms-git-history-more">').appendTo($scroll))
                    .click(function () {
                        state.limit += HistoryPageSize;
                        var scrollTop = $scroll.scrollTop();
                        renderHistory($dialog, state, false);
                        state.restoreScroll = scrollTop;
                    });
            }

            $details.appendTo($dialog);
            if (state.selected && $.grep(commits, function (c) { return c.sha === state.selected; }).length > 0) {
                renderCommitDetails($details, state.selected);
            } else {
                $details.append($('<div class="cms-git-intro">').text(t('history-select-commit')));
            }

            if (state.restoreScroll) {
                $scroll.scrollTop(state.restoreScroll);
                state.restoreScroll = null;
            }
        }, function () {
            $dialog.empty();
        });
    };

    var renderCommitDetails = function ($details, sha) {
        $details.empty().text(t('please-wait'));

        api('commitdetails', { sha: sha }, function (result) {
            var commit = result.commit;
            $details.empty();

            $('<div class="commit-message">').text(commit.message || '').appendTo($details);

            var $meta = $('<table class="commit-meta">').appendTo($details);
            var addMeta = function (label, value) {
                $('<tr>').append($('<th>').text(label)).append($('<td>').text(value)).appendTo($meta);
            };
            addMeta(t('history-author'), (commit.author || '') + (commit.author_email ? ' <' + commit.author_email + '>' : ''));
            addMeta(t('history-date'), commit.date ? new Date(commit.date).toLocaleString() : '');
            addMeta(t('history-commit'), commit.sha);
            var parents = $.map(commit.parents || [], function (p) { return p.substring(0, 8); });
            if (parents.length > 0) {
                addMeta(t('history-parents'), parents.join(', '));
            }
            if (parents.length > 1) {
                $('<div class="cms-git-intro">').text(t('history-merge', parents[0])).appendTo($details);
            }

            $('<div class="commit-changes-title">').text(t('history-changes')).appendTo($details);
            var changes = commit.changes || [];
            if (changes.length === 0) {
                $('<div class="cms-git-intro">').text(t('history-no-changes')).appendTo($details);
                return;
            }

            var beforeName = t('history-before', parents.length > 0 ? parents[0] : '-');
            var afterName = t('history-after', commit.short_sha);

            var $changes = $('<ul class="cms-git-changes cms-git-history-changes">').appendTo($details);
            $.each(changes, function (i, change) {
                var $li = $('<li>').addClass(change.state).appendTo($changes);
                var $diff = null;
                $('<span class="state">').text(t('change-' + change.state)).appendTo($li);
                $('<a href="#" class="path">').text(change.path).appendTo($li)
                    .click(function (e) {
                        e.preventDefault();
                        if ($diff) {
                            $diff.remove();
                            $diff = null;
                            return;
                        }
                        $diff = $('<div class="diff">').text(t('please-wait')).appendTo($li);
                        api('commitfilediff', { sha: commit.sha, path: change.path }, function (r) {
                            if (!$diff) return;
                            $diff.empty().append(renderDiffView(r.diff.before, r.diff.after, beforeName, afterName, true, change.path));
                        }, function () {
                            if ($diff) { $diff.remove(); $diff = null; }
                        });
                    });
            });
        }, function () {
            $details.empty();
        });
    };

    var reloadContent = function () {
        if (typeof window.updateContent === 'function' && document.currentPath !== undefined) {
            window.updateContent(document.currentPath);
        } else {
            reloadTree();
        }
    };
};
