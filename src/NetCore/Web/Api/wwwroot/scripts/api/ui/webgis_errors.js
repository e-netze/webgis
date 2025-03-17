(function ($) {
    "use strict";
    $.fn.webgis_errors = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_errors');
        }
    };
    var defaults = {
        map: null,
        tab_element_selector: null,
        allow_remove: true,
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        add: function (options) {
            addError($(this), {
                requestcommand: options.title,
                exception: options.message,
                map: options.map
            }, options.suppressShowTab !== true);
        },
        hasErrors: function (options) {
            var $holder = $(this).length > 0 ? $(this) : $('.webgis-errors-holder');

            return $holder.find('.webgis-error-item').length > 0;
        }
    };
    var initUI = function (elem, options) {
        if (!options.map)
            return;

        var $elem = $(elem).addClass('webgis-errors-holder');
        $elem.data('options', options);

        var map = options.map;

        if (options.allow_remove) {
            var $button = $("<div>")
                .addClass('uibutton uibutton-cancel')
                .appendTo($elem);
            $("<input type='checkbox' checked=checked id='webgis-error-control-auto-show-errors' />")
                .appendTo($button);
            $("<label for='webgis-error-control-auto-show-errors'>")
                .text('Warnungen/Fehler automatisch anzeigen')
                .appendTo($button);

            $("<button>")
                .addClass('webgis-button uibutton')
                .text('Alle entfernen')
                .appendTo($elem)
                .click(function () {
                    $(this).parent().find('.webgis-error-list').empty();
                    refreshErrors($elem);
                });
        }

        $("<ul>")
            .addClass('webgis-error-list')
            .appendTo($elem);

        map.events.on('onserviceerror', function (channel, sender, args) {
            //console.log('map-service-error', args);
            addError($elem, args, args.map == null/*true*/);  // if map => no click => ToastMenu
        });

        map.events.on('requestidchanged', function (channel, sender, args) {
            removeGetMapRequests($elem)
        });
    };

    var addError = function ($elem, args, triggerTabClick) {
        var $list = $elem.children('.webgis-error-list');
        var options = $elem.data('options');

        if (args.requestid && options.map && args.requestid != options.map.currentRequestId()) {

        }

        var title = '';
        if (args.requestcommand) {
            title += args.requestcommand + ': ';
        }
        if (args.service && args.service.serviceInfo)
            title += args.service.serviceInfo.name;

        var currentdate = new Date();
        var timestamp = currentdate.getDate() + "."
            + (currentdate.getMonth() + 1) + "."
            + currentdate.getFullYear() + " @ "
            + currentdate.getHours().toString().padStart(2,'0') + ":"
            + currentdate.getMinutes().toString().padStart(2, '0') + ":"
            + currentdate.getSeconds().toString().padStart(2, '0');

        var message = args.exception;

        var $item = $("<li>")
            .addClass('webgis-error-item')
            .prependTo($list);

        $("<div>")
            .addClass('title')
            .text(title)
            .appendTo($item);

        $("<div>")
            .addClass('timestamp')
            .text(timestamp)
            .appendTo($item);

        $.each(message.split('\n'), function (i, message) {
            $("<p>")
                .addClass('message')
                .text(message)
                .appendTo($item);
        });
       

        if (args.requestcommand) {
            $item.addClass(args.requestcommand.toString().toLowerCase());
        }
        if (args.requestid) {
            $item.addClass('hasrequestid').attr('data-requestid', args.requestid);
        }

        webgis.effects.popup($item);

        refreshErrors($elem, triggerTabClick);

        if (args.map) {
            // ToastMenu
            webgis.toastMessage("Warnung:", "Es sind Fehler aufgetreten: " + message.substr(0, Math.min(50, message.length)),
                function () {
                    $(options.tab_element_selector).trigger('click');
                }, 'warning');
        }
    };

    var removeGetMapRequests = function ($elem) {
        var $list = $elem.children('.webgis-error-list');

        $list.children('li.getmap.hasrequestid').remove();
        refreshErrors($elem);
    };

    var refreshErrors = function ($elem, triggerTabClick) {
        var options = $elem.data('options');

        var $list = $elem.children('.webgis-error-list');
        var count = $list.children('.webgis-error-item').length;

        if (options.tab_element_selector) {
            var $tab = $(options.tab_element_selector);
            var $counter = $tab.find('.webgis-error-counter');

            $tab.css('display', count > 0 ? '' : 'none');
            $counter.css('display', count > 0 ? '' : 'none').text(count);

            if ($tab.hasClass('webgis-tabs-tab') && $tab.parent().hasClass('webgis-tabs-holder') && $.fn.webgis_tabs) {
                $tab.parent().webgis_tabs('resize');

                if (count === 0 && $tab.hasClass('webgis-tabs-tab-selected')) {
                    $tab.parent().find('.webgis-tabs-tab').first().trigger('click');
                }
            }

            if (count > 0 && triggerTabClick === true && $elem.find('#webgis-error-control-auto-show-errors').is(':checked')) {
                $tab.trigger('click');
            }
        }
    };
})(webgis.$ || jQuery);
