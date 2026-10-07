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

    this.refreshStatus = function (fetch, onComplete) {
        var $button = $('#cms-git-panel .cms-git-button.check-status');
        if (fetch === true) {
            $button.prop('disabled', true).text(t('please-wait'));
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
            $button.prop('disabled', false).text(t('check-status'));
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

        $('<div class="cms-git-title">').text(t('title')).appendTo($panel);

        if (!status) {
            $('<div class="cms-git-info">').text(t('please-wait')).appendTo($panel);
            return;
        }

        $('<div class="cms-git-branch">')
            .text(t('branch') + ': ' + (status.branch || '?'))
            .toggleClass('default', status.is_default_branch === true)
            .appendTo($panel);

        var changes = status.changes ? status.changes.length : 0;
        $('<div class="cms-git-info">')
            .toggleClass('warning', changes > 0)
            .text(changes > 0 ? t('changes', changes) : t('no-changes'))
            .appendTo($panel);

        if (!status.has_upstream) {
            $('<div class="cms-git-info warning">').text(t('no-upstream')).appendTo($panel);
        } else {
            if (status.ahead > 0) {
                $('<div class="cms-git-info warning">').text('↑ ' + t('ahead', status.ahead)).appendTo($panel);
            }
            if (status.behind > 0) {
                $('<div class="cms-git-info warning">').text('↓ ' + t('behind', status.behind)).appendTo($panel);
            }
            if (!status.ahead && !status.behind && !status.stale) {
                $('<div class="cms-git-info ok">').text(t('up-to-date')).appendTo($panel);
            }
        }

        if (status.stale) {
            $('<div class="cms-git-info stale">')
                .text(t('stale'))
                .attr('title', status.fetch_error || '')
                .appendTo($panel);
        }

        if (_lastChecked) {
            $('<div class="cms-git-info last-checked">')
                .text(t('last-checked', _lastChecked.toLocaleTimeString()))
                .appendTo($panel);
        }

        var $buttons = $('<div class="cms-git-buttons">').appendTo($panel);
        var addButton = function (text, onclick, enabled, primary) {
            return $('<button class="cms-git-button">')
                .text(text)
                .toggleClass('primary', primary === true)
                .prop('disabled', enabled === false)
                .click(function (e) { e.preventDefault(); onclick(); })
                .appendTo($buttons);
        };

        if (status.is_merging) {
            addButton(t('resolve-conflicts'), _self.showConflictsDialog, true, status.conflict_count > 0);
            addButton(t('merge-complete'), _self.completeMerge, !status.conflict_count, !status.conflict_count);
            addButton(t('merge-abort'), _self.abortMerge).addClass('danger');
            addButton(t('check-status'), function () { _self.refreshStatus(true); }).addClass('check-status');
            return;
        }

        addButton(t('commit'), _self.showCommitDialog, changes > 0, changes > 0);
        addButton(t('push'), _self.push, changes === 0 && (status.ahead > 0 || !status.has_upstream), changes === 0 && status.ahead > 0);
        addButton(t('pull'), _self.pull, changes === 0);
        if (!status.is_default_branch) {
            addButton(t('merge-from-default'), _self.mergeFromDefault, changes === 0);
            addButton(t('merge-into-default'), _self.showMergeIntoDefaultDialog, changes === 0);
        }
        addButton(t('branches'), _self.showBranchesDialog);
        if (changes > 0) {
            addButton(t('discard-all'), function () { _self.discard(''); }).addClass('danger');
        }
        addButton(t('check-status'), function () { _self.refreshStatus(true); }).addClass('check-status');
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
            $('<button class="cms-git-button primary">').text(t('resolve-conflicts')).appendTo($buttons)
                .click(function (e) { e.preventDefault(); _self.showConflictsDialog(); });
        } else {
            $('<button class="cms-git-button primary">').text(t('merge-complete')).appendTo($buttons)
                .click(function (e) { e.preventDefault(); _self.completeMerge(); });
        }
        $('<button class="cms-git-button danger">').text(t('merge-abort')).appendTo($buttons)
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
            $node.find('.node-git-discard').remove();

            if ($node.hasClass('up') || $node.hasClass('current')) {
                $node.removeClass('cms-git-changed');
                return;
            }

            var path = $node.attr('data-path'), changed = isNodeChanged(path, changedFiles);
            $node.toggleClass('cms-git-changed', changed).attr('title', changed ? t('node-changed') : null);

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

    this.discard = function (nodePath, displayName) {
        var text = nodePath ? t('discard-confirm', displayName || nodePath) : t('discard-all-confirm');
        CMS.confirm(esc(text), function () {
            run('discard', { node: nodePath || '' }, function () {
                reloadContent();
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
                .text(t('merge-into-default-button'))
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
                    .text(t('merge-complete'))
                    .appendTo($('<div class="cms-git-dialog-buttons">').appendTo($dialog))
                    .click(function () {
                        CMS.closeModal($content);
                        _self.completeMerge();
                    });
                return;
            }

            $('<p class="cms-git-conflicts-intro">').text(t('conflicts-intro', theirsName, mineName)).appendTo($dialog);

            var $all = $('<div class="cms-git-dialog-buttons cms-git-conflicts-all">').appendTo($dialog);
            $('<button class="cms-git-button mine">').text(t('conflict-all-mine', mineName)).appendTo($all)
                .click(function () { resolve('*', 'mine'); });
            $('<button class="cms-git-button theirs">').text(t('conflict-all-theirs', theirsName)).appendTo($all)
                .click(function () { resolve('*', 'theirs'); });

            var $list = $('<ul class="cms-git-conflicts">').appendTo($dialog);
            $.each(conflicts, function (i, conflict) {
                var $li = $('<li>').appendTo($list);

                var $header = $('<div class="header">').appendTo($li);
                $('<span class="name">').text(conflict.node || '/').appendTo($header);
                $('<span class="kind">').text(' (' + t('conflict-kind-' + conflict.kind, mineName, theirsName) + ')').appendTo($header);

                var $actions = $('<div class="actions">').appendTo($li);
                $('<button class="cms-git-button mine">').text(t('conflict-use-mine', mineName)).appendTo($actions)
                    .click(function () { resolve(conflict.node, 'mine'); });
                $('<button class="cms-git-button theirs">').text(t('conflict-use-theirs', theirsName)).appendTo($actions)
                    .click(function () { resolve(conflict.node, 'theirs'); });

                $.each(conflict.files || [], function (j, file) {
                    $('<div class="file">').text(file.path).appendTo($li);
                    renderDiff(file.mine, file.theirs, mineName, theirsName).appendTo($li);
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

    var renderDiff = function (mine, theirs, mineName, theirsName) {
        var context = 3;
        var rows = diffLines(splitLines(mine), splitLines(theirs));

        var $table = $('<table class="cms-git-diff">');
        $('<colgroup><col class="ln"><col><col class="ln"><col></colgroup>').appendTo($table);
        var $head = $('<tr>').appendTo($('<thead>').appendTo($table));
        var addHead = function (cls, name, content) {
            $('<th colspan="2">').addClass(cls)
                .text(t('conflict-version', name) + (content === null || content === undefined ? ' ' + t('conflict-deleted') : ''))
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
            var $form = $('<div class="cms-git-dialog">').appendTo($content);

            var $list = $('<ul class="cms-git-changes">').appendTo($form);
            $.each((_status && _status.changes) || [], function (i, change) {
                $('<li>')
                    .addClass(change.state)
                    .html('<span class="state">' + esc(t('change-' + change.state)) + '</span> ' + esc(change.path))
                    .appendTo($list);
            });

            $('<label>').text(t('commit-message')).appendTo($form);
            var $message = $('<textarea rows="4" class="cms-git-message">')
                .attr('placeholder', t('commit-message-placeholder'))
                .appendTo($form);

            $('<button class="cms-git-button primary">')
                .text(t('commit-button'))
                .appendTo($('<div class="cms-git-dialog-buttons">').appendTo($form))
                .click(function () {
                    var message = $.trim($message.val());
                    if (!message) {
                        CMS.alert(_self.l10n['error-message-required'] || t('commit-message'));
                        return;
                    }
                    run('commit', { message: message }, function () {
                        CMS.closeModal($content);
                    });
                });

            setTimeout(function () { $message.focus(); }, 100);
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
                .text(t('branch-create'))
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
                        .text(t('branch-switch'))
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
                            .text(t('branch-delete'))
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

    var reloadContent = function () {
        if (typeof window.updateContent === 'function' && document.currentPath !== undefined) {
            window.updateContent(document.currentPath);
        } else {
            reloadTree();
        }
    };
};
