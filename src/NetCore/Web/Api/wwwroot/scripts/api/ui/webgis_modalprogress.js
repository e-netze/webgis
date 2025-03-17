(function ($) {
    "use strict";
    $.fn.webgis_modalprogress = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_modalprogress');
        }
    };
    var defaults = {
        title: 'webGIS',
        message: '...',
        width: '200px',
        height: '120px',
        slide: false,
        closebutton: false,
        cancelable: false,
        oncancel: function () { },
        blocker_alpha: 0.0,
        id: 'webgis-modalprogress'
    };
    var methods = {
        init: function (options) {
            var settings = $.extend({}, defaults, options);
            return this.each(function () {
                var $content = $(this).webgis_modal('content', settings);
                if ($content.length > 0) {
                    var exits = null;
                    $content.find('.webgis-progress-message').each(function (i, e) {
                        if ($(e).data('message') === settings.message)
                            exits = $(e);
                    });
                    if (!exits) {
                        $("<div class='webgis-progress-message webgis-progress-message-running'><img class='webgis-progress-message-img' src='" + webgis.baseUrl + "/content/api/img/hourglass/loader1.gif' style='width:16px;height:16px' />&nbsp;" + settings.message + "</div>").data('message', settings.message).appendTo($content);
                    }
                }
                else {
                    settings.onload = function ($content) {
                        $content.css({ padding: '10px', background: 'white', color: 'black'/*, borderRadius: '0 0 10px 10px'*/ });
                        $content.parent().css({ /*borderRadius: '10px',*/ background: '' })
                            .addClass('webgis-tabs-tab-header')
                            .css('font-size', '1em')
                            .css('font-weight', 'normal');
                            //.find('.webgis-modal-title')
                            //.css({ backgroundColor: 'transparent', fontSize: '2em' });

                        //$("<img src='" + webgis.baseUrl + "/content/api/img/hourglass/progress-loader.gif' style='float:left' />").appendTo($content);
                        $("<div class='webgis-progress-message webgis-progress-message-running'><img class='webgis-progress-message-img' src='" + webgis.baseUrl + "/content/api/img/hourglass/loader1.gif' style='width:16px;height:16px' />&nbsp;" + settings.message + "</div>").data('message', settings.message).appendTo($content);
                        if (settings.cancelable) {
                            var $buttonGroup = $("<div></div>").css({
                                'text-align': 'right',
                                'margin-top': 10
                            }).appendTo($content);
                            $("<button class='webgis-button'>Abbrechen</button>").appendTo($buttonGroup).data('options', options)
                                .click(function () {
                                var options = $(this).data('options');
                                $(null).webgis_modal('close', { id: 'webgis-modalprogress' });
                                if (options.oncancel)
                                    options.oncancel();
                            });
                        }
                    };
                    settings.mobile_fullscreen = false;
                    $(this).webgis_modal(settings);
                }
            });
        },
        close: function (options) {
            var $this = $(this);
            var settings = $.extend({}, defaults, options);
            var $content = $this.webgis_modal('content', settings);
            if ($content.length > 0) {
                $content.find('.webgis-progress-message').each(function (i, e) {
                    if ($(e).data('message') === settings.message) {
                        $(e).removeClass('webgis-progress-message-running').fadeOut();
                    }
                });
                if ($content.find('.webgis-progress-message-running').length === 0) {
                    webgis.delayed(function () {
                        $content.find('.webgis-progress-message-running').length === 0;
                        $this.webgis_modal('close', settings);
                    }, 300, settings);
                }
            }
        }
    };
})(webgis.$ || jQuery);
