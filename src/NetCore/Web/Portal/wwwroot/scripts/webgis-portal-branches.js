// cms branch selection for portal pages and the map viewer
// localStorage 'currentBranch': encrypted branch token (enc:...), sent with every api request (hmac_br)
// localStorage 'currentBranchName': encoded branch name (CmsBranches.Encode), only for display and to find the branch in the list
// the api only accepts branch tokens: map authors get tokens without expiration (branch list), other users only via a temporary branch link (?branch=enc:...)
(function () {
    "use strict";

    var storageKey = 'currentBranch', storageNameKey = 'currentBranchName';

    var l10n = function (key) {
        return window.webgis && webgis.l10n ? webgis.l10n.get(key) : key;
    };

    var esc = function (s) {
        return $('<div>').text(s == null ? '' : String(s)).html();
    };

    var alertMessage = function (message, type) {
        if (window.webgis && webgis.alert && $.fn.webgis_modal) {
            webgis.alert(message, type || 'info');
        } else {
            window.alert(message);
        }
    };

    var self = window.webgisPortalBranches = {
        current: function () {
            return webgis.localStorage.get(storageKey) || '';
        },
        currentEncoded: function () {
            return self.current() ? (webgis.localStorage.get(storageNameKey) || '') : '';
        },
        set: function (token, encoded) {
            webgis.localStorage.set(storageKey, token || '');
            webgis.localStorage.set(storageNameKey, token ? (encoded || '') : '');
        },
        isToken: function (value) {
            return !!value && value.indexOf('enc:') === 0;
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
        // link: branch link (?branch=enc:...), validated by the server; null otherwise
        // returns false, if the stored branch was invalid and has been reset to main
        prepare: function (canSelect, branches, link) {
            if (link && link.token) {
                self.set(link.token, link.encoded);
                return true;
            }

            if (!canSelect) {
                // users without author rights may only use a branch with a branch link
                self.set('');
                return true;
            }

            var param = new URLSearchParams(window.location.search).get('branch');
            if (param !== null && !self.isToken(param)) {
                param = param.trim();
                if (!param || param === 'main') {
                    self.set('');
                } else {
                    // encoded or clear branch name
                    var branch = self.find(branches, param) || self.find(branches, self.encode(param));
                    if (!branch || !branch.token) {
                        self.set('');
                        return false;
                    }
                    self.set(branch.token, branch.encoded);
                }
            }

            if (self.current()) {
                // always use the current token from the branch list (no expiration); older versions stored the encoded name instead of a token
                var current = self.find(branches, self.isToken(self.current()) ? self.currentEncoded() : self.current());
                if (!current || !current.encoded || !current.token) {
                    self.set('');
                    return false;
                }
                self.set(current.token, current.encoded);
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

        // branch: item of the branch list (null or main => main)
        select: function (branch) {
            var encoded = branch ? branch.encoded || '' : '';
            if (encoded && !branch.token) return;

            self.set(encoded ? branch.token : '', encoded);

            // remove a ?branch= parameter, otherwise it would overrule the selection on reload
            var url = new URL(window.location.href);
            if (url.searchParams.has('branch')) {
                url.searchParams.delete('branch');
                window.location.href = url.toString();
            } else {
                window.location.reload();
            }
        },

        // temporary link for users without author rights: map url + ?branch=enc:...
        copyLink: function (linkUrl, branch, hours) {
            $.ajax({
                url: linkUrl,
                type: 'get',
                dataType: 'json',
                cache: false,
                data: { branch: branch.encoded, hours: hours },
                success: function (result) {
                    if (!result || !result.success || !result.token) {
                        alertMessage(l10n('branch-link-error') + (result && result.exception ? ': ' + result.exception : ''), 'error');
                        return;
                    }

                    var url = new URL(window.location.href);
                    url.searchParams.delete('branch');
                    url.searchParams.set('branch', result.token);
                    var link = url.toString();

                    var expires = new Date(result.expires);
                    var message = l10n('branch-link-copied') + ' ' + self.displayName(branch) +
                        (isNaN(expires.getTime()) ? '' : ' (' + l10n('branch-link-expires') + ' ' + expires.toLocaleString() + ')');

                    var fallback = function () {
                        window.prompt(message, link);
                    };

                    if (navigator.clipboard && navigator.clipboard.writeText) {
                        navigator.clipboard.writeText(link).then(function () {
                            alertMessage(message + '\n\n' + link, 'info');
                        }, fallback);
                    } else {
                        fallback();
                    }
                },
                error: function () {
                    alertMessage(l10n('branch-link-error'), 'error');
                }
            });
        },

        showSelectDialog: function (branches, url) {
            var render = function ($content, list) {
                $content.empty();
                $('<p>').addClass('webgis-branch-select-info').text(l10n('branch-select-info')).appendTo($content);

                var $list = $('<ul>').addClass('webgis-branch-select-list').appendTo($content);
                var current = self.currentEncoded();

                $.each(list || [], function (i, branch) {
                    var encoded = branch.encoded || '';
                    var $li = $('<li>')
                        .addClass('webgis-branch-select-item' + (encoded === current ? ' selected' : '') + (encoded ? '' : ' main'))
                        .appendTo($list)
                        .click(function () {
                            if (encoded !== current) {
                                self.select(branch);
                            }
                        });

                    if (encoded && url) {
                        var $links = $('<div>').addClass('links').attr('title', l10n('branch-link-title')).appendTo($li);
                        $.each([{ hours: 1, key: 'branch-link-1h' }, { hours: 24, key: 'branch-link-24h' }], function (j, option) {
                            $('<button>')
                                .attr('type', 'button')
                                .text(l10n(option.key))
                                .appendTo($links)
                                .click(function (e) {
                                    e.stopPropagation();
                                    self.copyLink(url + '/link', branch, option.hours);
                                });
                        });
                    }

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

        // badge (top center of the map)
        // authors: active branch or a discreet entry point on main => selection dialog
        // other users (branch link): active branch only, no selection dialog
        initViewer: function (options) {
            var branches = options.branches || [];
            var canSelect = options.canSelect !== false;
            var link = options.link;
            var current = self.currentEncoded();

            if (options.wasReset === true) {
                alertMessage(l10n('branch-not-found'), 'info');
            }

            if (!canSelect && !current) {
                return;
            }

            var branch = current
                ? (self.find(branches, current) || (link && link.encoded === current ? { encoded: current, name: link.name } : { encoded: current }))
                : null;

            var title = l10n('branch-select');
            if (current) {
                var details = self.details(branch);
                title = (canSelect ? l10n('branch-active-badge-title') : l10n('branch-active')) + (details ? '\n' + details : '');
                if (link && link.expires) {
                    var expires = new Date(link.expires);
                    if (!isNaN(expires.getTime())) {
                        title += '\n' + l10n('branch-link-expires') + ' ' + expires.toLocaleString();
                    }
                }
            }

            var $badge = $('<div>')
                .addClass('webgis-branch-badge' + (current ? '' : ' main') + (canSelect ? '' : ' readonly'))
                .attr('title', title)
                .html(esc(l10n('branch')) + ': <strong>' + esc(current ? self.displayName(branch) : 'main') + '</strong>')
                .appendTo($(options.container || 'body'));

            if (canSelect) {
                $badge.click(function () {
                    self.showSelectDialog(branches, options.url);
                });
            }

            if (current) {
                $('<span>')
                    .addClass('webgis-branch-badge-main')
                    .attr('title', l10n('branch-back-to-main'))
                    .text('×')
                    .appendTo($badge)
                    .click(function (e) {
                        e.stopPropagation();
                        self.select(null);
                    });
            }
        }
    };
})();