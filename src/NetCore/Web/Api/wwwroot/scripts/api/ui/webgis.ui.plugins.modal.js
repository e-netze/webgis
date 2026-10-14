// WebGIS UI Plugins (modal)
// webgis_modalDialog
// Modal dialog of the viewer and the Portal: blocker element (.webgis-modal, position fixed) with the
// dialog frame (.webgis-modal-body), title (.webgis-modal-title) and content (#webgis-modal-content).
// The plugin lives on the .webgis-modal element: one element = one dialog, closing removes the element
// ($.fn.remove destroys the instance). Dialogs are created and addressed by id (default 'modaldialog')
// only through the flat functions below - no element needed:
//   webgis.ui.showModal(options) -> $content (options.container: jQuery/element/selector, default 'body';
//     an open dialog with the same id is replaced, page-wide)
//   webgis.ui.closeModal(id) -> bool, webgis.ui.getModalContent(id), webgis.ui.getModalTitle(id),
//   webgis.ui.hideModal(id), webgis.ui.unhideModal(id), webgis.ui.fitModal(id, { dock }),
//   webgis.ui.toggleModalFullscreen(id), webgis.ui.toggleModalMaximize(id)
// Progress dialog of webgis.showProgress/hideProgress: webgis.ui.showProgressModal/_hideProgressModal.
// Legacy facades (custom code only, don't use them in new code): $.fn.webgis_modal, $.fn.webgis_modalprogress.

webgis.ui.definePlugin('webgis_modalDialog', {
    defaults: {
        id: 'modaldialog',
        meta_left_frame: false,
        width: '50%',
        height: '80%',
        minWidth: '330px',
        slide: true,
        closebutton: true,
        blockerclickclose: true,
        blocker_alpha: 0.5,
        dock: 'center',
        mobile_fullscreen: true,
        animate: true,
        hasBlocker: true,
        titleButtons: null  // [{ img: 'url....', onClick: function (titleButton) {...} }]
    },
    init: function (rawOptions) {
        const $ = this.$, self = this, Ctor = this.constructor;
        // shallow (like the legacy plugin): callbacks, content, ... keep their references
        const options = this.options = $.extend({}, this.options, rawOptions);
        const dialogId = Ctor.dialogId(options.id);

        var useMobile = $(window).width() < 1024 && options.mobile_fullscreen === true;
        var useMobileFullscreenDockPanels = useMobile && screen.width < 800;
        let isEnlargeable = useMobileFullscreenDockPanels === false && (options.dock === 'left' || options.dock === 'right');

        options.width = Ctor.toModalSize(options.width);
        options.height = Ctor.toModalSize(options.height);

        var framePos = options.framepos ?
            options.framepos :
            (((useMobile && options.dock === 'center') || (useMobileFullscreenDockPanels && options.dock !== 'center')) ?
                "left:4px;right:4px;top:4px;bottom:4px;" :
                "width:" + options.width + ";height:" + options.height + ";min-width:" + options.minWidth + ";max-height:" + options.maxHeight + ";max-width:100%;");

        // always fixed => also works if the API is embedded in (scrolling) third party pages
        var $blocker = this.$el
            .attr('id', dialogId)
            .attr('style', 'z-index:9999;position:fixed;left:0px;right:0px;top:0px;bottom:0px;background:rgba(0,0,0,' + options.blocker_alpha + ');')
            .addClass('webgis-modal');

        var $frame = $("<div id='" + (options.hasBlocker === true ? '' : dialogId) + "' style='z-index:1000;position:absolute;" + framePos + "background:var(--webgis-ui-surface);opacity:0;" + (useMobile === true ? "" : "display:none;") + "' class='webgis-modal-body " + options.dock + "'></div>").appendTo($blocker);
        var pPos = "left:0px;top:calc(43px + var(--webgis-ui-border-width-item-selected));right:0px;bottom:0px";
        var $content = $("<div id='webgis-modal-content' class='webgis-modal-content' style='z-index:1;position:absolute;overflow:auto;" + pPos + "'></div>");
        if (options.content)
            $content.html(options.content);

        $content.appendTo($frame);
        $frame.on('click.webgis_modalDialog', function (e) {
            e.stopPropagation();
        });
        if (options.blockerclickclose) {
            // Chrome fires a click on the blocker when dragging (e.g. selecting text in the dialog) ends
            // over the blocker -> only close, if the mouse didn't move between mousedown and click
            $blocker.on('mousedown.webgis_modalDialog', function (e) {
                $(this).data('mousedown_x', e.originalEvent.offsetX);
                $(this).data('mousedown_y', e.originalEvent.offsetY);
            });
            $blocker.on('click.webgis_modalDialog', function (e) {
                var dx = e.originalEvent.offsetX - $(this).data('mousedown_x');
                var dy = e.originalEvent.offsetY - $(this).data('mousedown_y');

                if (Math.abs(dx) < 5 && Math.abs(dy) < 5) {
                    if ($(this).find('.webgis-modal-close').length > 0)
                        $(this).find('.webgis-modal-close').trigger('click');
                    else if ($(this).find('.webgis-modal-close-element').length > 0)
                        $(this).find('.webgis-modal-close-element').trigger('click');
                }
            });
        }

        if (useMobileFullscreenDockPanels === false && options.dock === 'left') {
            $frame.css({
                left: 0,
                bottom: 0,
                top: 0,
                height: '',
                maxHeight: '',
                width: options.width
            });
            if (options.hasBlocker === false) {
                $blocker.css({
                    right: '', width: options.width
                });
            }
        }
        else if (useMobileFullscreenDockPanels === false && options.dock === 'right') {
            $frame.css({
                right: 0,
                bottom: 0,
                top: 0,
                height: '',
                maxHeight: '',
                width: options.width
            });
            if (options.hasBlocker === false) {
                $blocker.css({
                    left: '', width: options.width
                });
            }
        }
        else if (options.dock === 'center') {
            let frameTop = Math.max(0, ($blocker.height() / 2 - $frame.height() / 2) / 2);
            $frame.css({
                left: $blocker.width() / 2 - $frame.width() / 2,
                top: frameTop,
                maxHeight: 'calc(100% - ' + 2 * frameTop + 'px)'
            });
        }

        var $title = $("<div style='position:absolute;left:0px;right:0px;top:0px;height:35px'><div class='webgis-modal-title'></div></div>"),
            $close = null;
        $title.children('.webgis-modal-title').html(options.title ? options.title : '');
        $title.appendTo($frame);

        let buttonsCount = 0;
        if (options.closebutton) {
            buttonsCount++;

            $title.children('.webgis-modal-title')
                .addClass('has-closebutton');

            if (!isEnlargeable) {
                buttonsCount++;
                $("<div class='webgis-modal-resize'></div>")
                    .appendTo($title);
            }
            $close = $("<div class='webgis-modal-close'></div>");
            $close.appendTo($title);
            $close.on('click.webgis_modalDialog', function (e) {
                e.stopPropagation();
                if (options.onclose)
                    options.onclose();
                self.close();
            });
        }

        if (options.titleButtons && options.titleButtons.length > 0) {
            for (let i in options.titleButtons) {
                let titleButton = options.titleButtons[i];

                $("<div>")
                    .addClass('webgis-modal-title-button')
                    .css({ right: buttonsCount * 44, backgroundImage: 'url(' + titleButton.img + ')' })
                    .data('tileButton', titleButton)
                    .appendTo($title)
                    .on('click.webgis_modalDialog', function (e) {
                        e.stopPropagation();

                        let titleButton = $(this).data('tileButton');
                        if (titleButton.onClick) {
                            titleButton.onClick(titleButton);
                        }
                    });

                buttonsCount++;
            }
        }

        if (isEnlargeable) {
            $frame.addClass('enlargeable');
            $title.on('click.webgis_modalDialog', function () {
                if ($frame.hasClass('modal-large')) {
                    $frame.css('width', 330);
                    if (options.hasBlocker === false) {
                        $blocker.css('width', Math.min(screen.width, 330));
                    }
                } else {
                    $frame.css('width', 640);
                    if (options.hasBlocker === false) {
                        $blocker.css('width', Math.min(screen.width, 640));
                    }
                }
                $frame.toggleClass('modal-large');
            });
        }
        else if (options.dock === 'center') {
            $title
                .css('cursor', 'pointer')
                .on('click.webgis_modalDialog', function () {
                    if ($frame.hasClass('maximized')) {
                        $frame.css({
                            width: $frame.attr('data-normal-width'),
                            height: $frame.attr('data-normal-height'),
                            left: $frame.attr('data-normal-left'),
                            top: $frame.attr('data-normal-top'),
                            right: '', bottom: ''
                        }).removeClass('maximized');
                    } else {
                        $frame
                            .attr('data-normal-width', $frame.css('width'))
                            .attr('data-normal-height', $frame.css('height'))
                            .attr('data-normal-left', $frame.css('left'))
                            .attr('data-normal-top', $frame.css('top'));

                        $frame.css({
                            left: '0px',
                            top: '0px',
                            right: '0px',
                            bottom: '0px',
                            width: 'auto',
                            height: 'auto'
                        }).addClass('maximized');
                    }

                    if (options.onresize) {
                        webgis.delayed(function () {
                            options.onresize();
                        }, 500);
                    }
                });
        }
        if (useMobile === false && (options.onmaximize || options.allowfullscreen === true)) {
            var $max = $("<table style='cursor:pointer;color:black;font-size:14px;font-weight:bold;position:absolute;top:2px;right:72px;margin:4px'><tr><td>Fullscreen</td><td><div class='i8-button-26 i8-maximize-26-w'></div></td></tr></table>");
            $max.appendTo($title);
            $max.on('click.webgis_modalDialog', function () {
                if (options.onmaximize)
                    options.onmaximize();
                else
                    self.toggleFullscreen();
            });
        }

        this._$frame = $frame;
        this._$content = $content;
        this._$title = $title.children('.webgis-modal-title');

        $frame.css('display', 'block');
        if (options.onload) {
            options.onload($content);
        }
        if ($close) {
            webgis.setHistoryItem($close, $content);
        }
        $frame.removeClass('animate');
        webgis.delayed(function ($frame) {
            if (options.animate && options.show !== false) {
                var originHeight = $frame.css('height'), originWidth = $frame.css('width'), originLeft = $frame.position().left, originTop = $frame.position().top;
                var originMinHeight = $frame.css('min-height'), originMinWidth = $frame.css('min-width');

                $frame.css(
                    options.dock == 'left'
                        ? {
                            opacity: 1, minWidth: '0px',
                            width: '0px', height: originHeight,
                            left: 0, top: 0
                        }
                        : {
                            width: '0px', height: '0px', minWidth: '0px', minHeight: '0px',
                            left: $(document).width() / 5, top: $(document).height() / 2
                        }
                );
                webgis.delayed(function ($frame) {
                    $frame.addClass('animate')
                        .css({
                            opacity: 1,
                            minWidth: originMinWidth,
                            minHeight: originMinHeight,
                            width: originWidth,
                            height: originHeight,
                            left: originLeft,
                            top: originTop
                        });
                }, 10, $frame);
            }
            else {
                $frame.css('opacity', 1);
            }
        }, 10, $frame);
    },
    destroy: function () {
        // the handlers live on the dialog elements and are released by jQuery when the element is removed
        this._$frame = this._$content = this._$title = null;
    },
    methods: {
        content: function () {
            return this._$content;
        },
        title: function () {
            return this._$title;
        },
        close: function () {
            this.$el.remove();
        },
        hide: function () {
            this.$el.css('display', 'none');
        },
        unhide: function () {
            this.$el.css('display', '');
        },
        // options.dock: 'left'/'right' only adjusts the width (default: 'center', independent of the dialog's dock)
        fit: function (options) {
            const $ = this.$;
            options = $.extend({ dock: 'center' }, options);

            if ($(window).width() < 1024)
                return;

            var $modalBody = this._$frame, $content = this._$content;
            var contentWidth = 0, contentHeight = 0;
            $content.children().each(function (i, e) {
                contentWidth = Math.max($(e).outerWidth() + parseInt(parseInt($(e).css('marginLeft')) + parseInt($(e).css('marginRight'))), contentWidth);
                contentHeight += $(e).outerHeight() + parseInt(parseInt($(e).css('marginTop')) + parseInt($(e).css('marginBottom')));
            });

            contentHeight += parseInt($content.css('paddingTop')) + parseInt($content.css('paddingBottom'));
            contentWidth += parseInt($content.css('paddingLeft')) + parseInt($content.css('paddingRight'));

            if (options.dock === 'left' || options.dock === 'right') {
                $modalBody.css({
                    width: contentWidth + parseInt(parseInt($content.css('paddingLeft')) + parseInt($content.css('paddingRight')))
                });
            }
            else {
                const position = $modalBody.position();
                if (position) {
                    $modalBody.css({
                        width: contentWidth,
                        height: contentHeight + 60,
                        maxHeight: 'calc(100% - ' + position.top + 'px)'
                    });
                }
            }
        },
        toggleFullscreen: function () {
            const $ = this.$;
            var elem = this._$frame.get(0);
            if ($.fullScreenEnabled()) {
                elem.style.left = elem.style.top = elem.style.right = elem.style.bottom = '10px';
                elem.style.border = '1px solid #888888';
                $.exitFullScreen();
            }
            else {
                elem.style.left = elem.style.top = elem.style.right = elem.style.bottom = '0px';
                elem.style.border = '';
                $.fullScreen(elem);
            }
        },
        toggleMaximize: function () {
            var elem = this._$frame.get(0);
            elem.style.width = elem.style.height = '';
            if (elem.style.left !== '0px') {
                elem.style.left = elem.style.top = elem.style.right = elem.style.bottom = '0px';
            }
            else {
                elem.style.left = elem.style.top = elem.style.right = elem.style.bottom = '10px';
                elem.style.border = '1px solid #888888';
            }
        }
    },
    staticMethods: {
        dialogId: function (id) {
            return 'webgis-modal-' + String(id || 'modaldialog').replace(/\./g, '-').replace(/:/g, '-');
        },
        // the dialog element(s) with this id, page-wide
        find: function (id) {
            const $ = webgis.$ || jQuery;
            return $('#' + this.dialogId(id) + '.webgis-modal');
        },
        // numbers -> px
        toModalSize: function (val) {
            if (val === undefined || val === null) return val;
            var s = String(val).trim();
            return /^\d+(\.\d+)?$/.test(s) ? s + 'px' : s;
        }
    }
});

// returns the content element of the new dialog (empty, if the container doesn't exist)
webgis.ui.showModal = function (options) {
    const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_modalDialog.plugin;
    options = $.extend({}, options);
    options.id = options.id || 'modaldialog';

    Ctor.find(options.id).remove();

    const $container = $(options.container || 'body').first();
    if ($container.length === 0)
        return $();

    return $("<div>")
        .appendTo($container)
        .webgis_modalDialog(options)
        .webgis_modalDialog('content');
};
webgis.ui.closeModal = function (id) {
    const $modal = (webgis.$ || jQuery).fn.webgis_modalDialog.plugin.find(id);
    if ($modal.length === 0)
        return false;
    $modal.webgis_modalDialog('close');
    return true;
};
// empty jQuery, if the dialog doesn't exist
webgis.ui.getModalContent = function (id) {
    return (webgis.$ || jQuery).fn.webgis_modalDialog.plugin.find(id).webgis_modalDialog('content');
};
webgis.ui.getModalTitle = function (id) {
    return (webgis.$ || jQuery).fn.webgis_modalDialog.plugin.find(id).webgis_modalDialog('title');
};
webgis.ui.hideModal = function (id) {
    (webgis.$ || jQuery).fn.webgis_modalDialog.plugin.find(id).webgis_modalDialog('hide');
};
webgis.ui.unhideModal = function (id) {
    (webgis.$ || jQuery).fn.webgis_modalDialog.plugin.find(id).webgis_modalDialog('unhide');
};
webgis.ui.fitModal = function (id, options) {
    (webgis.$ || jQuery).fn.webgis_modalDialog.plugin.find(id).webgis_modalDialog('fit', options);
};
webgis.ui.toggleModalFullscreen = function (id) {
    (webgis.$ || jQuery).fn.webgis_modalDialog.plugin.find(id).webgis_modalDialog('toggleFullscreen');
};
webgis.ui.toggleModalMaximize = function (id) {
    (webgis.$ || jQuery).fn.webgis_modalDialog.plugin.find(id).webgis_modalDialog('toggleMaximize');
};

// progress dialog, used by webgis.showProgress/hideProgress: one dialog, one line per running message
webgis.ui.__progressModalDefaults = {
    id: 'webgis-modalprogress',
    title: 'webGIS',
    message: '...',
    width: '200px',
    height: '120px',
    slide: false,
    closebutton: false,
    cancelable: false,
    oncancel: function () { },
    blocker_alpha: 0.0,
    mobile_fullscreen: false
};
webgis.ui.showProgressModal = function (options) {
    const $ = webgis.$ || jQuery;
    const settings = $.extend({}, webgis.ui.__progressModalDefaults, options);

    const appendMessage = function ($content) {
        $("<div class='webgis-progress-message webgis-progress-message-running'><img class='webgis-progress-message-img' src='" + webgis.baseUrl + "/content/api/img/hourglass/loader1.gif' style='width:16px;height:16px' />&nbsp;" + settings.message + "</div>")
            .data('message', settings.message)
            .appendTo($content);
    };

    const $content = webgis.ui.getModalContent(settings.id);
    if ($content.length > 0) {
        let exists = false;
        $content.find('.webgis-progress-message').each(function (i, e) {
            if ($(e).data('message') === settings.message)
                exists = true;
        });
        if (!exists)
            appendMessage($content);
        return;
    }

    settings.onload = function ($content) {
        $content.css({ padding: '10px', background: 'var(--webgis-ui-surface)', color: 'var(--webgis-ui-text)' });
        $content.parent().css({ background: '' })
            .addClass('webgis-tabs-tab-header')
            .css('font-size', '1em')
            .css('font-weight', 'normal');

        appendMessage($content);
        if (settings.cancelable) {
            var $buttonGroup = $("<div></div>").css({
                'text-align': 'right',
                'margin-top': 10
            }).appendTo($content);
            $("<button class='webgis-button'>")
                .text(webgis.l10n.get('cancel'))
                .appendTo($buttonGroup)
                .on('click', function () {
                    webgis.ui.closeModal(settings.id);
                    if (settings.oncancel)
                        settings.oncancel();
                });
        }
    };
    webgis.ui.showModal(settings);
};
webgis.ui.hideProgressModal = function (options) {
    const $ = webgis.$ || jQuery;
    const settings = $.extend({}, webgis.ui.__progressModalDefaults, options);
    const $content = webgis.ui.getModalContent(settings.id);
    if ($content.length === 0)
        return;

    $content.find('.webgis-progress-message').each(function (i, e) {
        if ($(e).data('message') === settings.message) {
            $(e).removeClass('webgis-progress-message-running').fadeOut();
        }
    });
    if ($content.find('.webgis-progress-message-running').length === 0) {
        webgis.delayed(function () {
            // a new progress message may have been started in the meantime
            if ($content.find('.webgis-progress-message-running').length === 0)
                webgis.ui.closeModal(settings.id);
        }, 300);
    }
};

// legacy facades for custom code: $(container).webgis_modal(options) / $(...).webgis_modal('close', { id }), ...
// $(container).webgis_modalprogress(options) / $(...).webgis_modalprogress('close', { message })
(function ($) {
    "use strict";

    $.fn.webgis_modal = function (method, options) {
        if (typeof method === 'object' || !method) {
            if (this.length > 0)
                webgis.ui.showModal($.extend({}, method, { container: this.first() }));
            return this;
        }

        var id = options && options.id;
        switch (method) {
            case 'content':
                return webgis.ui.getModalContent(id);
            case 'title':
                return webgis.ui.getModalTitle(id);
            case 'close':
                if (this.hasClass('modal-dialog')) {
                    this.remove();
                    return false;
                }
                return webgis.ui.closeModal(id);
            case 'hide':
                webgis.ui.hideModal(id);
                return this;
            case 'show':
                webgis.ui.unhideModal(id);
                return this;
            case 'fit':
                webgis.ui.fitModal(id, options);
                return this;
            case 'toggle_fullscreen':
                webgis.ui.toggleModalFullscreen(id);
                return this;
            case 'toggle_maximize':
                webgis.ui.toggleModalMaximize(id);
                return this;
        }
        $.error('Method ' + method + ' does not exist on webgis.$.webgis_modal');
    };

    $.fn.webgis_modalprogress = function (method, options) {
        if (typeof method === 'object' || !method) {
            if (this.length > 0)
                webgis.ui.showProgressModal($.extend({}, method, { container: this.first() }));
            return this;
        }
        if (method === 'close') {
            webgis.ui.hideProgressModal(options);
            return this;
        }
        $.error('Method ' + method + ' does not exist on webgis.$.webgis_modalprogress');
    };
})(webgis.$ || jQuery);
