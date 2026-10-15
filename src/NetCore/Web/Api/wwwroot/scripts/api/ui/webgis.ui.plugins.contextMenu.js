// WebGIS UI Plugins (contextMenu)
// webgis_contextMenu
// Popup menu at the mouse position (e.g. right click on a query results table row): blocker element
// (.webgis-contextmenu-blocker, optionally shading everything except options.sender) with the menu (ul.webgis-contextmenu).
// The plugin lives on the blocker: one element = one menu, closing (click/right click on the blocker, item click,
// Escape) removes the element. Items can be triggered by their shortcut key.
// Items: { text, icon, shortcut, data, checked, items } - 'items' renders a submenu (opens on hover or click, the
// parent item itself never triggers the callback), 'checked' shows a check mark; { separator: true } renders a line.
// Created only through: webgis.ui.showContextMenu({ clickEvent, items, callback(data), sender, container })

webgis.ui.definePlugin('webgis_contextMenu', {
    defaults: {
        clickEvent: null,
        callback: null,
        sender: null,
        items: []
    },
    init: function (rawOptions) {
        const $ = this.$, self = this;
        // shallow: item data (jQuery elements) and callback keep their references
        const options = this.options = $.extend({}, this.options, rawOptions);
        // the menu may live in another window (webgis.windowManager)
        const $win = this._$win = $(this.$el[0].ownerDocument.defaultView || window);

        const $blocker = this.$el
            .addClass('webgis-contextmenu-blocker')
            .data('time-stamp', new Date().getTime())
            .on('contextmenu.webgis_contextMenu', function (e) {
                self._close(e);
                return false;
            })
            .on('click.webgis_contextMenu', function (e) {
                self._close(e);
            });

        if (options.sender) {
            const $sender = $(options.sender);
            const offset = $sender.offset();

            $("<div>")
                .addClass('shader')
                .css({ top: 0, left: 0, right: 0, height: offset.top })
                .appendTo($blocker);

            $("<div>")
                .addClass('shader')
                .css({ top: (offset.top + $sender.height()), left: 0, right: 0, bottom: 0 })
                .appendTo($blocker);

            $("<div>")
                .addClass('shader')
                .css({ top: offset.top, left: 0, width: offset.left, height: $sender.height() })
                .appendTo($blocker);

            $("<div>")
                .addClass('shader')
                .css({ top: offset.top, right: 0, width: $win.width() - offset.left - $sender.width(), height: $sender.height() })
                .appendTo($blocker);
        }

        const $menu = this._$menu = $("<ul>")
            .addClass('webgis-contextmenu')
            .appendTo($blocker);

        this._renderItems($menu, options.items);

        $menu.css({
            left: Math.max(0, Math.min($win.width() - $menu.width() - 10, options.clickEvent.originalEvent.clientX)),
            top: Math.max(0, Math.min($win.height() - $menu.height() - 10, options.clickEvent.originalEvent.clientY))
        });

        this._onKeyUp = function (e) {
            if (e.key === "Escape") {
                self._close(e);
                return false;
            }

            const $item = $menu.children("li[shortcut='" + e.key + "']");
            if ($item.length === 1) {
                $item.trigger('click');
            }

            return false;
        };
        $win.on('keyup', this._onKeyUp);
    },
    destroy: function () {
        if (this._onKeyUp) {
            this._$win.off('keyup', this._onKeyUp);
            this._onKeyUp = null;
        }
        this._$menu = this._$win = null;
    },
    methods: {
        _renderItems: function ($ul, items) {
            const $ = this.$, self = this, options = this.options;

            for (let item of items || []) {
                if (!item) continue;

                if (item.separator === true) {
                    $("<li>")
                        .addClass('webgis-contextmenu-separator')
                        .appendTo($ul);
                    continue;
                }

                const hasSubItems = Array.isArray(item.items) && item.items.length > 0;

                let $item = $("<li>")
                    .addClass('webgis-contextmenu-item')
                    .toggleClass('has-submenu', hasSubItems)
                    .toggleClass('item-checked', item.checked === true)
                    .attr('shortcut', item.shortcut)
                    .data('item-data', item.data)
                    .appendTo($ul);

                $("<div>")
                    .text(item.text)
                    .appendTo($item);

                if (item.shortcut) {
                    $("<div>")
                        .addClass('shortcut-text')
                        .text(webgis.l10n.get('shortcut-key') + ': ' + item.shortcut.toString().toUpperCase())
                        .appendTo($item);
                }

                if (item.icon) {
                    $item.css('backgroundImage', item.icon.indexOf('url(') === 0 ? item.icon : 'url(' + item.icon + ')');
                }

                if (hasSubItems) {
                    const $submenu = $("<ul>")
                        .addClass('webgis-contextmenu webgis-contextmenu-submenu')
                        .appendTo($item);
                    this._renderItems($submenu, item.items);

                    // parent items only open the submenu (hover or click), they never trigger the callback
                    $item
                        .on('mouseenter.webgis_contextMenu', function () {
                            self._openSubmenu($(this));
                        })
                        .on('mouseleave.webgis_contextMenu', function () {
                            $(this).removeClass('open');
                        })
                        .on('click.webgis_contextMenu', function (e) {
                            e.stopPropagation();
                            if ($(e.target).closest('ul.webgis-contextmenu-submenu', this).length > 0)
                                return;   // click inside the submenu (separator, padding)
                            if ($(this).hasClass('open')) {
                                $(this).removeClass('open');
                            } else {
                                self._openSubmenu($(this));
                            }
                        });
                } else {
                    $item.on('click.webgis_contextMenu', function (e) {
                        e.stopPropagation();

                        if (options.callback) {
                            options.callback($(this).data('item-data'));
                        }

                        self._close(e);
                    });
                }
            }
        },
        _openSubmenu: function ($item) {
            const $win = this._$win;

            $item.siblings('.open').removeClass('open');
            const $submenu = $item.children('ul.webgis-contextmenu-submenu')
                .removeClass('flip-left')
                .css('top', '');
            $item.addClass('open');

            // keep the submenu inside the window: open to the left / move up if necessary
            const rect = $submenu.get(0).getBoundingClientRect();
            if (rect.right > $win.width() - 4) {
                $submenu.addClass('flip-left');
            }
            const overflowBottom = rect.bottom - ($win.height() - 4);
            if (overflowBottom > 0) {
                $submenu.css('top', -Math.min(overflowBottom, Math.max(0, rect.top)) + 'px');
            }
        },
        close: function () {
            this.$el.remove();
        },
        _close: function (e) {
            e.stopPropagation();

            // the click/contextmenu event that opened the menu may also reach the new blocker
            if (new Date().getTime() - this.$el.data('time-stamp') < 500)
                return;

            this.close();
        }
    }
});

// nothing happens without clickEvent or items; returns the menu element (blocker)
webgis.ui.showContextMenu = function (options) {
    const $ = webgis.$ || jQuery;
    if (!options || !options.clickEvent || !options.items || !options.items.length)
        return $();

    let $container = $(options.container || 'body').first();
    if ($container.length === 0)
        return $();

    if (webgis.windowManager) {   // menu in the detached window, if it was opened there
        $container = webgis.windowManager.overlayContainer($container, {
            element: (options.clickEvent && options.clickEvent.target) || options.sender
        });
    }

    return $("<div>")
        .appendTo($container)
        .webgis_contextMenu(options);
};
