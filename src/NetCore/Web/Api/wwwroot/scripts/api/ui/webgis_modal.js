(function ($) {
    "use strict";
    $.fn.webgis_modal = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_modal');
        }
    };
    var defaults = {
        meta_left_frame: false,
        width: '50%',
        height: '80%',
        minWidth: '330px',
        //maxHeight: '80%',
        slide: true,
        closebutton: true,
        blockerclickclose: true,
        id: 'modaldialog',
        blocker_alpha: 0.5,
        dock: 'center',
        mobile_fullscreen: true,
        animate: true,
        hasBlocker: true,
        titleButtons: null  // [{ img: 'url....', onclick:function(...) }]
    };
    var methods = {
        init: function (options) {
            var settings = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, settings);
            });
        },
        content: function (options) {
            var settings = $.extend({}, defaults, options);
            return $(dialogSelector(settings)).find('#webgis-modal-content');
        },
        title: function (options) {
            var settings = $.extend({}, defaults, options);
            return $(dialogSelector(settings)).find('.webgis-modal-title');
        },
        close: function (options) {
            options = $.extend({}, defaults, options);
            /*
            var framebody = $('.webgis-modal-body');
            if (framebody.length > 0) {
                $(dialogSelector(options)).remove();
                return true;
            } else if ($('.webgis-modal').length > 0) {
                $(dialogSelector(options)).remove();
                return true;
            }
            */
            //alert(dialogSelector(options) + " " + $(dialogSelector(options)).length);

            if ($(this).hasClass('modal-dialog')) {
                $(this).remove();
            } else {
                if ($(dialogSelector(options)).length > 0) {
                    $(dialogSelector(options)).remove();
                    return true;
                }
            }
            return false;
        },
        hide: function (options) {
            options = $.extend({}, defaults, options);
            $(dialogSelector(options)).css('display', 'none');
        },
        show: function (options) {
            options = $.extend({}, defaults, options);
            $(dialogSelector(options)).css('display', '');
        },
        toggle_fullscreen: function (options) {
            options = $.extend({}, defaults, options);
            var elem = $(dialogSelector(options)).find('.webgis-modal-body').get(0);
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
        toggle_maximize: function (options) {
            options = $.extend({}, defaults, options);
            var elem = $(dialogSelector(options)).find('.webgis-modal-body').get(0);
            elem.style.width = elem.style.height = '';
            if (elem.style.left !== '0px') {
                elem.style.left = elem.style.top = elem.style.right = elem.style.bottom = '0px';
            }
            else {
                elem.style.left = elem.style.top = elem.style.right = elem.style.bottom = '10px';
                elem.style.border = '1px solid #888888';
            }
        },
        fit: function (options) {
            options = $.extend({}, defaults, options);

            var useMobile = $(window).width() < 1024;
            if (useMobile)
                return;
            var $modalBody = $(dialogSelector(options)).find('.webgis-modal-body');
            var $content = $modalBody.find('#webgis-modal-content');
            var contentWidth = 0, contentHeight = 0;
            $content.children().each(function (i, e) {
                contentWidth = Math.max($(e).outerWidth() + parseInt(parseInt($(e).css('marginLeft')) + parseInt($(e).css('marginRight'))), contentWidth);
                contentHeight += $(e).outerHeight() + parseInt(parseInt($(e).css('marginTop')) + parseInt($(e).css('marginBottom')));

                //console.log('marginWidth :' + parseInt(parseInt($(e).css('marginLeft')) + parseInt($(e).css('marginRight'))));
                //console.log('marginHeight:' + parseInt(parseInt($(e).css('marginTop')) + parseInt($(e).css('marginBottom'))));
            });

            contentHeight += parseInt($content.css('paddingTop')) + parseInt($content.css('paddingBottom'));
            contentWidth += parseInt($content.css('paddingLeft')) + parseInt($content.css('paddingRight'));

            //console.log('contentWidth', contentWidth);
            //console.log('contentHeight', contentHeight);

            //console.log('contentPaddingWidth: ' + parseInt(parseInt($content.css('paddingLeft')) + parseInt($content.css('paddingRight'))));
            if (options.dock === 'left' || options.dock === 'right') {
                if ($modalBody)
                    $modalBody.css({
                        width: contentWidth + parseInt(parseInt($content.css('paddingLeft')) + parseInt($content.css('paddingRight')))
                    });
            }
            else {
                const position = $modalBody.position();
                if (position) {  // if model is closed, position is null/undefined
                    $modalBody.css({
                        width: contentWidth,
                        height: contentHeight + 60,
                        maxHeight: 'calc(100% - ' + $modalBody.position().top + 'px)'
                    });
                }
            }
        }
    };
    var initUI = function (parent, options) {
        var wWidth = $(window).width();
        var wHeight = $(window).height();
        var $parent = $(parent);
        $parent.find(dialogSelector(options)).each(function (i, e) {
            e.parentNode.removeChild(e);
        });
        var useMobile = $(window).width() < 1024 && options.mobile_fullscreen === true;
        var useMobileFullscreenDockPanels = useMobile && screen.width < 800;

        let isEnlargeable = useMobileFullscreenDockPanels === false && (options.dock === 'left' || options.dock === 'right');
        let isCollapssable = isEnlargeable && !options.closebutton;

        var framePos = options.framepos ?
            options.framepos :
            (((useMobile && options.dock === 'center') || (useMobileFullscreenDockPanels && options.dock !== 'center')) ?
                "left:4px;right:4px;top:4px;bottom:4px;" :
                "width:" + options.width + ";height:" + options.height + ";min-width:" + options.minWidth + ";max-height:" + options.maxHeight + ";max-width:100%;");

        // Absolute wenn sich das ganze innerhalb eines webgis-container abspielen soll
        //var blockerPosition = $(parent).css('position') === "absolute" || $(parent).css('position') === "relative" ? "absolute" : "fixed";
       
        // Immer fixed => funktionerrt dann auch für Dialog, wenn API auf Drittseiten eingebunden ist, auf denen gescrollt werden muss
        var blockerPosition = 'fixed';

        var $blocker = $("<div id='" + dialogId(options) + "' style='z-index:9999;position:" + blockerPosition + ";left:0px;right:0px;top:0px;bottom:0px;background:rgba(0,0,0," + options.blocker_alpha + ");' class='webgis-modal'></div>");

        var $frame = $("<div id='" + (options.hasBlocker === true ? '' : dialogId(options)) + "' style='z-index:1000;position:absolute;" + framePos + "background:white;opacity:0;" + (useMobile === true ? "" : "display:none;") + "' class='webgis-modal-body " + options.dock + "'></div>").appendTo($blocker);
        var pPos, mPos;
        if (useMobile === true) {
            pPos = "left:0px;top:44px;right:0px;bottom:0px";
        }
        else {
            pPos = "left:0px;top:44px;right:0px;bottom:0px";
        }
        var $content = $("<div id='webgis-modal-content' class='webgis-modal-content' style='z-index:1;position:absolute;overflow:auto;" + pPos + "'></div>");
        if (options.content)
            $content.html(options.content);

        $blocker.appendTo($(parent));

        $content.appendTo($frame);
        $frame.click(function (e) {
            e.stopPropagation();
        });
        if (options.blockerclickclose) {

            // Im Chrome kann es durch ziehen zum schließen kommen
            // Das passiert beispeilsweise, wenn ein Wert aus dem Dialog kopiert wird und beim ziehen dann 
            // die Mause über dem grauen Bereich (Blocker) losgelasssen wird (chrome wirft click-event!!)
            // -> Darum Koordinaten merken, wann Mouse gedrückt wird.

            $blocker.on('mousedown', function (e) {
                $(this).data('mousedown_x', e.originalEvent.offsetX);
                $(this).data('mousedown_y', e.originalEvent.offsetY);
            });
            $blocker.click(function (e) {

                var dx = e.originalEvent.offsetX - $(this).data('mousedown_x');
                var dy = e.originalEvent.offsetY - $(this).data('mousedown_y');

                //console.log(dx + " " + dy);
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
        

        //console.log("width: " + $frame.width());

        var $title = $("<div style='position:absolute;left:0px;right:0px;top:0px;height:35px'><div class='webgis-modal-title'>" + (options.title ? options.title : '') + "</div></div>"),
            $close = null;
        $title.appendTo($frame);

        let buttonsCount = 0;
        if (options.closebutton) {
            buttonsCount++;

            $title.children('.webgis-modal-title')
                .addClass('has-closebutton');

            if (!isEnlargeable) {
                buttonsCount++;
                $("<div class='webgis-modal-resize'></div>")
                    .appendTo($title)
            }
            $close = $("<div class='webgis-modal-close'></div>");
            $close.appendTo($title).get(0).options = options;
            $close.click(function (e) {
                e.stopPropagation();
                if (options.onclose)
                    options.onclose();
                $(null).webgis_modal('close', this.options);
            });
        }
        //if (isCollapssable) {
        //    $("<div>")
        //        .css({ position: 'absolute', right: 60, top: 0, width: 30, height: 30, background: 'red', cursor: 'pointer' })
        //        .appendTo($title)
        //        .click(function (e) {
        //            e.stopPropagation();
        //            let $blocker = $(this).closest('.webgis-modal');
        //            $blocker.toggleClass('collapsed');
        //            if ($blocker.hasClass('collapsed')) {
        //                $blocker.css({ overflow: 'hidden', height: 42 });
        //            } else {
        //                $blocker.css({ overflow: '', height: '' });
        //            }
        //        });
        //    $("#tab-presentations").trigger('click');
        //}

        if (options.titleButtons && options.titleButtons.length > 0) {
            for (let i in options.titleButtons) {
                let titleButton = options.titleButtons[i];

                $("<div>")
                    .addClass('webgis-modal-title-button')
                    .css({ right: buttonsCount * 44, backgroundImage: 'url(' + titleButton.img + ')' })
                    .data('tileButton', titleButton)
                    .appendTo($title)
                    .click(function (e) {
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
            $title.click(function () {
                var $modal = $(this).closest('.webgis-modal-body');
                var $blocker = $(this).closest('.webgis-modal');

                if ($modal.hasClass('modal-large')) {
                    $modal.css('width', 330);
                    if (options.hasBlocker === false) {
                        $blocker.css('width', Math.min(screen.width, 330));
                    }
                } else {
                    $modal.css('width', 640);
                    if (options.hasBlocker === false) {
                        $blocker.css('width', Math.min(screen.width, 640));
                    }
                }
                $modal.toggleClass('modal-large');
            });
        }
        else if (options.dock === 'center') {
            $title
                .css('cursor', 'pointer')
                .click(function () {
                    var $modal = $(this).closest('.webgis-modal-body');
                    if ($modal.hasClass('maximized')) {
                        $modal.css({
                            width: $modal.attr('data-normal-width'),
                            height: $modal.attr('data-normal-height'),
                            left: $modal.attr('data-normal-left'),
                            top: $modal.attr('data-normal-top'),
                            right: '', bottom: ''
                        }).removeClass('maximized');
                    } else {
                        $modal
                            .attr('data-normal-width', $modal.css('width'))
                            .attr('data-normal-height', $modal.css('height'))
                            .attr('data-normal-left', $modal.css('left'))
                            .attr('data-normal-top', $modal.css('top'));

                        $modal.css({
                            left: '0px',
                            top: '0px',
                            right: '0px',
                            bottom: '0px',
                            width: 'auto',
                            height: 'auto'
                        }).addClass('maximized');
                    }

                    if (options && options.onresize) {
                        webgis.delayed(function () {
                            options.onresize();
                        }, 500);
                    }
                });
        }
        if (useMobile === false && (options.onmaximize || options.allowfullscreen === true)) {
            var $max = $("<table style='cursor:pointer;color:black;font-size:14px;font-weight:bold;position:absolute;top:2px;right:72px;margin:4px'><tr><td>Fullscreen</td><td><div class='i8-button-26 i8-maximize-26-w'></div></td></tr></table>");
            $max.appendTo($title);
            if (options.onmaximize) {
                $max.get(0).onmaximize = options.onmaximize;
                $max.click(function () { this.onmaximize(); });
            }
            else {
                $max.click(function () { $(null).webgis_modal('toggle_fullscreen'); });
            }
        }
        $frame.css('display', 'block');
        if (options.onload) {
            options.onload($content);
        } 
        if ($close) {
            webgis.setHistoryItem($close, $content);
        }
        $frame.removeClass('animate');
        webgis.delayed(function ($frame) {
            //console.log('options.animate', options.animate, options.animate && options.show !== false);
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
                //webgis.delayed(function ($frame) {
                $frame.css('opacity', 1);
                //}, 10, $frame);
            }
        }, 10, $frame);
    };
    var dialogId = function (options) {
        if (options.id)
            return 'webgis-modal-' + options.id.replace(/\./g, '-').replace(/:/g, '-');
        ;
    };
    var dialogSelector = function (options) {
        var id = dialogId(options);
        return (id === '' ? '.webgis-modal' : '#' + id + '.webgis-modal');
    };
})(webgis.$ || jQuery);
