// WebGIS UI Plugins (errors)
// webgis_errors: list of service errors/warnings (tab counter, auto show, remove all)

webgis.ui.definePlugin('webgis_errors', {
    defaults: {
        map: null,
        tab_element_selector: null,
        allow_remove: true
    },
    init: function () {
        const $ = this.$, o = this.options;
        if (!o.map)
            return;

        const $elem = this.$el.addClass('webgis-errors-holder');
        $elem.data('options', o);

        const self = this;
        if (o.allow_remove) {
            const $button = $("<div>")
                .addClass('uibutton uibutton-cancel')
                .appendTo($elem);
            $("<input type='checkbox' checked=checked id='webgis-error-control-auto-show-errors' />")
                .appendTo($button);
            $("<label for='webgis-error-control-auto-show-errors'>")
                .text(webgis.l10n.get('show-errors-automatically'))
                .appendTo($button);

            $("<button>")
                .addClass('webgis-button uibutton')
                .text(webgis.l10n.get('remove-all'))
                .appendTo($elem)
                .on('click.webgis_errors', function () {
                    $(this).parent().find('.webgis-error-list').empty();
                    self._refreshErrors();
                });
        }

        $("<ul>")
            .addClass('webgis-error-list')
            .appendTo($elem);

        this._onServiceError = function (channel, sender, args) {
            self._addError(args, args.map == null);  // if map => no click => ToastMenu
        };
        this._onRequestIdChanged = function (channel, sender, args) {
            self._removeGetMapRequests();
        };
        o.map.events.on('onserviceerror', this._onServiceError);
        o.map.events.on('requestidchanged', this._onRequestIdChanged);
    },
    destroy: function () {
        const map = this.options.map;
        if (map && map.events && this._onServiceError) {
            map.events.off('onserviceerror', this._onServiceError);
            map.events.off('requestidchanged', this._onRequestIdChanged);
        }
        this.$el.find('*').off('.webgis_errors');
    },
    methods: {
        add: function (options) {
            this._addError({
                requestcommand: options.title,
                exception: options.message,
                map: options.map
            }, options.suppressShowTab !== true);
        },
        hasErrors: function () {
            return this.$el.find('.webgis-error-item').length > 0;
        },
        _addError: function (args, triggerTabClick) {
            const $ = this.$, $elem = this.$el;
            const $list = $elem.children('.webgis-error-list');
            const options = this.options;

            let title = '';
            if (args.requestcommand) {
                title += args.requestcommand + ': ';
            }
            if (args.service && args.service.serviceInfo)
                title += args.service.serviceInfo.name;

            const currentdate = new Date();
            const timestamp = currentdate.getDate() + "."
                + (currentdate.getMonth() + 1) + "."
                + currentdate.getFullYear() + " @ "
                + currentdate.getHours().toString().padStart(2, '0') + ":"
                + currentdate.getMinutes().toString().padStart(2, '0') + ":"
                + currentdate.getSeconds().toString().padStart(2, '0');

            const message = args.exception;

            const $item = $("<li>")
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

            this._refreshErrors(triggerTabClick);

            if (args.map) {
                // ToastMenu
                webgis.toastMessage(webgis.l10n.get('warning') + ":", webgis.l10n.get('errors-occurred') + ": " + message.substr(0, Math.min(50, message.length)),
                    function () {
                        $(options.tab_element_selector).trigger('click');
                    }, 'warning');
            }
        },
        _removeGetMapRequests: function () {
            this.$el.children('.webgis-error-list').children('li.getmap.hasrequestid').remove();
            this._refreshErrors();
        },
        _refreshErrors: function (triggerTabClick) {
            const $ = this.$, $elem = this.$el, options = this.options;

            const $list = $elem.children('.webgis-error-list');
            const count = $list.children('.webgis-error-item').length;

            if (options.tab_element_selector) {
                const $tab = $(options.tab_element_selector);
                const $counter = $tab.find('.webgis-error-counter');

                $tab.css('display', count > 0 ? '' : 'none');
                $counter.css('display', count > 0 ? '' : 'none').text(count);

                if ($tab.hasClass('webgis-tabs-tab') && $tab.parent().hasClass('webgis-tabs-holder') && $.fn.webgis_tabs) {
                    webgis.ui.resizeTabs($tab.parent());

                    if (count === 0 && $tab.hasClass('webgis-tabs-tab-selected')) {
                        $tab.parent().find('.webgis-tabs-tab').first().trigger('click');
                    }
                }

                if (count > 0 && triggerTabClick === true && $elem.find('#webgis-error-control-auto-show-errors').is(':checked')) {
                    $tab.trigger('click');
                }
            }
        }
    }
});
