// cms branch selection for portal pages and the map viewer
// the selected (encoded) branch is stored in localStorage 'currentBranch' and sent with every api request (hmac_br)
(function () {
    "use strict";

    var storageKey = 'currentBranch';

    var l10n = function (key) {
        return window.webgis && webgis.l10n ? webgis.l10n.get(key) : key;
    };

    var esc = function (s) {
        return $('<div>').text(s == null ? '' : String(s)).html();
    };

    var self = window.webgisPortalBranches = {
        current: function () {
            return webgis.localStorage.get(storageKey) || '';
        },
        set: function (encoded) {
            webgis.localStorage.set(storageKey, encoded || '');
        },

        // same as CmsBranches.Encode (C#): a-z, 0-9, '-' are kept, everything else => _xx (utf-8 hex)
        encode: function (name) {
            if (!name) return '';
            var bytes = new TextEncoder().encode(name), result = '';
            for (var i = 0; i < bytes.length; i++) {
                var c = String.fromCharCode(bytes[i]);
                result += /[a-z0-9-]/.test(c) ? c : '_' + ('0' + bytes[i].toString(16)).slice(-2);
            }
            return result;
        },
        decode: function (encoded) {
            if (!encoded) return '';
            try {
                return decodeURIComponent(encoded.replace(/_([0-9a-f]{2})/g, '%$1'));
            } catch (e) {
                return encoded;
            }
        },
        isValidEncoded: function (encoded) {
            return !!encoded && /^([a-z0-9-]|_[0-9a-f]{2})+$/.test(encoded);
        },

        find: function (branches, encoded) {
            encoded = encoded || '';
            return $.grep(branches || [], function (b) { return (b.encoded || '') === encoded; })[0] || null;
        },
        displayName: function (branch) {
            if (!branch || !branch.encoded) return l10n('branch-main');
            return branch.name || self.decode(branch.encoded);
        },
        details: function (branch) {
            if (!branch || !branch.encoded) return '';
            var parts = [];
            if (branch.user) parts.push(branch.user);
            if (branch.date) {
                var d = new Date(branch.date);
                if (!isNaN(d.getTime())) parts.push(d.toLocaleString());
            }
            if (branch.commit) parts.push(branch.commit.substring(0, 7));
            if (branch.cms_count) parts.push(branch.cms_count + ' ' + l10n('branch-cms-count'));
            return parts.join(' · ');
        },

        // must run synchronously before the first api request (webgis.init)
        // returns false, if the stored branch was invalid and has been reset to main
        prepare: function (canSelect, branches) {
            if (!canSelect) {
                self.set('');
                return true;
            }

            var param = new URLSearchParams(window.location.search).get('branch');
            if (param !== null) {
                param = param.trim();
                if (!param || param === 'main') {
                    self.set('');
                } else if (self.find(branches, param)) {
                    self.set(param);
                } else {
                    // branch name in clear text
                    self.set(self.encode(param));
                }
            }

            var current = self.current();
            if (current && branches && !self.find(branches, current)) {
                self.set('');
                return false;
            }

            return true;
        },

        load: function (url, callback) {
            $.ajax({
                url: url,
                type: 'get',
                dataType: 'json',
                cache: false,
                success: function (result) {
                    callback($.isArray(result) ? result : null);
                },
                error: function () {
                    callback(null);
                }
            });
        },

        select: function (encoded) {
            if ((encoded || '') === self.current()) return;
            self.set(encoded);

            // remove a ?branch= parameter, otherwise it would overrule the selection on reload
            var url = new URL(window.location.href);
            if (url.searchParams.has('branch')) {
                url.searchParams.delete('branch');
                window.location.href = url.toString();
            } else {
                window.location.reload();
            }
        },

        showSelectDialog: function (branches, url) {
            var render = function ($content, list) {
                $content.empty();
                $('<p>').addClass('webgis-branch-select-info').text(l10n('branch-select-info')).appendTo($content);

                var $list = $('<ul>').addClass('webgis-branch-select-list').appendTo($content);
                var current = self.current();

                $.each(list || [], function (i, branch) {
                    var encoded = branch.encoded || '';
                    var $li = $('<li>')
                        .addClass('webgis-branch-select-item' + (encoded === current ? ' selected' : '') + (encoded ? '' : ' main'))
                        .appendTo($list)
                        .click(function () {
                            self.select(encoded);
                        });
                    $('<div>').addClass('name').text(self.displayName(branch)).appendTo($li);
                    var details = self.details(branch);
                    if (details) {
                        $('<div>').addClass('details').text(details).appendTo($li);
                    }
                });
            };

            $('body').webgis_modal({
                id: 'webgis-branch-select-dialog',
                title: l10n('branch-select'),
                width: '480px',
                height: '520px',
                onload: function ($content) {
                    $content.addClass('webgis-branch-select');
                    render($content, branches);
                    if (url) {
                        // refresh with the current list (new deploys since page load)
                        self.load(url, function (list) {
                            if (list) render($content, list);
                        });
                    }
                }
            });
        },

        // badge (top center of the map): active branch or a discreet entry point on main; shows a hint, if the stored branch has been reset
        initViewer: function (options) {
            var branches = options.branches || [];
            var current = self.current();

            if (options.wasReset === true) {
                webgis.alert(l10n('branch-not-found'), 'info');
            }

            var branch = current ? (self.find(branches, current) || { encoded: current }) : null;
            var details = self.details(branch);

            // on main the badge is only a discreet entry point for the selection dialog
            var $badge = $('<div>')
                .addClass('webgis-branch-badge' + (current ? '' : ' main'))
                .attr('title', current ? l10n('branch-active-badge-title') + (details ? '\n' + details : '') : l10n('branch-select'))
                .html(esc(l10n('branch')) + ': <strong>' + esc(current ? self.displayName(branch) : 'main') + '</strong>')
                .appendTo($(options.container || 'body'))
                .click(function () {
                    self.showSelectDialog(branches, options.url);
                });

            if (current) {
                $('<span>')
                    .addClass('webgis-branch-badge-main')
                    .attr('title', l10n('branch-back-to-main'))
                    .text('×')
                    .appendTo($badge)
                    .click(function (e) {
                        e.stopPropagation();
                        self.select('');
                    });
            }
        }
    };
})();
