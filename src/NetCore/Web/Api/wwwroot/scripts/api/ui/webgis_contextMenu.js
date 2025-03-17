(function ($) {
    "use strict";
    $.fn.webgis_contextMenu = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_contextMenu');
        }
    };
    var defaults = {
        clickEvent: null,
        callback: null,
        items: []
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
    };

    var initUI = function (parent, options) {
        if (!options.clickEvent || !options.items.length)
            return;

        let $parent = $(parent);

        let close = function (sender, e) {
            e.stopPropagation();
            
            var $sender = $(sender);
            if (!$sender.hasClass('.webgis-contextmenu-blocker')) {
                $sender = $sender.closest('.webgis-contextmenu-blocker');
            }

            if(new Date().getTime() - $sender.data('time-stamp') < 500) {
                //console.log('prevent closing context menu soonly');
                return;
            }
            
            $sender.remove();
            $(window).off('keyup', keypress);
        }

        let $blocker = $("<div>")
            .addClass('webgis-contextmenu-blocker')
            .appendTo($parent)
            .data('time-stamp', new Date().getTime())
            .contextmenu(function (e) {
                close(this, e);
                return false;
            })
            .click(function (e) {
                close(this, e);
            });

        if (options.sender) {
            let $sender = $(options.sender);
            let offset = $sender.offset();

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

            //console.log(offset);
        }
        let $menu = $("<ul>")
            .addClass('webgis-contextmenu')
            .appendTo($blocker);

        for (let item of options.items) {
            let $item = $("<li>")
                .addClass('webgis-contextmenu-item')
                .attr('shortcut', item.shortcut)
                .data('item-data', item.data)
                .appendTo($menu)
                .click(function (e) {
                    e.stopPropagation();

                    if (options.callback) {
                        options.callback($(this).data('item-data'));
                    }

                    close(this, e);
                });

            $("<div>")
                .text(item.text)
                .appendTo($item);

            if (item.shortcut) {
                $item.attr('shortcut', item.shortcut);
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

        let keypress = function (e) {
            //console.log(e);
            //console.log('key', e.key);
            //console.log('wich', e.wich);

            if (e.key === "Escape") {
                close($blocker, e);
                return false;
            }

            const $item = $menu.children("li[shortcut='" + e.key + "']");
            if ($item.length === 1) {
                $item.trigger('click');
            }

            return false;
        };

        $(window).on('keyup', keypress);
    }
})(webgis.$ || jQuery);