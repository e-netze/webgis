// WebGIS UI Plugins (contextMenu)
// webgis_contextMenu
// Popup menu at the mouse position (e.g. right click on a query results table row): blocker element
// (.webgis-contextmenu-blocker, optionally shading everything except options.sender) with the menu (ul.webgis-contextmenu).
// The plugin lives on the blocker: one element = one menu, closing (click/right click on the blocker, item click,
// Escape) removes the element. Items can be triggered by their shortcut key.
// Created only through: webgis.ui.showContextMenu({ clickEvent, items: [{ text, icon, shortcut, data }], callback(data), sender, container })

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
                .css({ top: offset.top, right: 0, width: $(window).width() - offset.left - $sender.width(), height: $sender.height() })
                .appendTo($blocker);
        }

        const $menu = this._$menu = $("<ul>")
            .addClass('webgis-contextmenu')
            .appendTo($blocker);

        for (let item of options.items) {
            let $item = $("<li>")
                .addClass('webgis-contextmenu-item')
                .attr('shortcut', item.shortcut)
                .data('item-data', item.data)
                .appendTo($menu)
                .on('click.webgis_contextMenu', function (e) {
                    e.stopPropagation();

                    if (options.callback) {
                        options.callback($(this).data('item-data'));
                    }

                    self._close(e);
                });

            $("<div>")
                .text(item.text)
                .appendTo($item);

            if (item.shortcut) {
                $("<div>")
                    .addClass('shortcut-text')
                    .text('Shortcut-Key: ' + item.shortcut.toString().toUpperCase())
                    .appendTo($item);
            }

            if (item.icon) {
                $item.css('backgroundImage', item.icon.indexOf('url(') === 0 ? item.icon : 'url(' + item.icon + ')');
            }
        }

        $menu.css({
            left: Math.max(0, Math.min($(window).width() - $menu.width() - 10, options.clickEvent.originalEvent.clientX)),
            top: Math.max(0, Math.min($(window).height() - $menu.height() - 10, options.clickEvent.originalEvent.clientY))
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
        $(window).on('keyup', this._onKeyUp);
    },
    destroy: function () {
        if (this._onKeyUp) {
            this.$(window).off('keyup', this._onKeyUp);
            this._onKeyUp = null;
        }
        this._$menu = null;
    },
    methods: {
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

    const $container = $(options.container || 'body').first();
    if ($container.length === 0)
        return $();

    return $("<div>")
        .appendTo($container)
        .webgis_contextMenu(options);
};
