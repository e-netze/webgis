(function ($) {
    "use strict";
    $.fn.webgis_dockPanel = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_dockPanel');
        }
    };
    var defaults = {
        dock: 'left',
        size: 300,
        maxSize: null,
        adverseSize: null,
        maxAdverseSize: null,
        maximizeAdverse: false,
        autoResize: false,
        autoResizeBoth: false,
        title: '',
        titleImg: '',
        slide: true,
        usePadding: true,
        refElement: null,
        canClose: true,
        useIdSelector: false,
        map: null
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
            return $(dialogSelector(settings)).find('.webgis-dockpanel-content');
        },
        exists: function (options) {
            return $(this).webgis_dockPanel('content', options).length === 1;
        },
        close: function (options) {
            options = $.extend({}, defaults, options);
            var $panel = $(dialogSelector(options));
            if ($panel.length > 0) {
                var options = $panel.data('options');
                if (options.onclose)
                    options.onclose($panel);
                $panel.webgis_dockPanel('remove');
                return true;
            }
            return false;
        },
        remove: function () {
            var $this = $(this);
            if ($this.hasClass('webgis-dockpanel')) {
                webgis.removeHistoryItem($this.find('.webgis-dockpanel-close'));
                $this.remove();
            }
        },
        removeAll: function () {
            $(this).find('.webgis-dockpanel').webgis_dockPanel('remove');
        },
        resize: function (options) {
            var $this = $(this);
            if (options.size) {
                var sizeProperty = $this.hasClass('webgis-dockpanel-top') || $this.hasClass('webgis-dockpanel-bottom') ? "height" : "width";
                $this.attr('data-size', options.size);
                $this.css(sizeProperty, options.size);
            }
        },
        set_title: function (options) {
            var $panel = $(dialogSelector(options));
            if ($panel.length === 1) {
                $panel.find('.webgis-dockpanel-title').find('.text').text(options.title);
            }
        },
        panelId: function (options) {
            return dialogId(options);
        }
    };
    var initUI = function (parent, options) {
        var $parent = parent ? $(parent) : $('body');

        var sizeProperty = options.dock === 'top' || options.dock === 'bottom' ? "height" : "width";
        var adverseSizeProperty = options.dock === 'top' || options.dock === 'bottom' ? "width" : "height";

        //
        // bestehendes Panel wiederverwenden (ausser bei 'useIdSelector')
        // Wichtig, falls man die Back-Button History (Handy) benutzt
        // Historyeintrag und "close" Button sollten dabei bestehen bleiben
        // Nicht mit webgis_dockPanel('remove') entfernen und neu laden, weil da die History durch die Timer darin durcheinander kommt
        //
        var $panel = options.useIdSelector === true ? $parent.find('#' + dialogId(options)) :
                                                      $parent.find('.webgis-dockpanel-' + options.dock);
        if ($panel !== null && $panel.length > 0) {
            
        } else {
            $panel = null;
        }

        if ($panel === null) {
            $panel = $("<div>")
                .addClass('webgis-dockpanel webgis-dockpanel-' + options.dock)
                .css({ position: 'absolute' })
                .appendTo($parent);
        }

        if (options.id || $panel.attr('id')) {
            $panel.attr('id', dialogId(options));
        }

        $panel
            .data('options',options)
            .attr('data-size', options.size)
            .attr('data-maxsize', options.maxSize !== null ? options.maxSize : 'auto')
            .css('max-' + sizeProperty, options.maxSize !== null ? options.maxSize : 'auto');

        if (options.adverseSize || options.maxAdverseSize) {
            $panel.attr('data-adverse-size', options.adverseSize)
                .attr('data-adverse-maxsize', options.maxAdverseSize !== null ? options.maxAdverseSize : 'auto')
                .css('max-' + adverseSizeProperty, options.maxAdverseSize !== null ? options.maxAdverseSize : 'auto');
        }

        if (options.maximizeAdverse === true) {
            $panel.addClass('webgis-dockpanel-maximize-adverse');
        }

        updateSize($panel, options);

        var $title = $panel.children('.webgis-dockpanel-title'),
            $close = null;
        if ($title.length === 0) {
            $title = $("<div class='webgis-dockpanel-title'><div class='text'></div></div>").appendTo($panel)
                .click(function (e) {
                    e.stopPropagation();
                    var $panel = $(this).closest('.webgis-dockpanel');

                    if ($panel.hasClass('minimized')) {
                        $panel.removeClass('minimized');
                        return;
                    }

                    $panel.toggleClass('maximized');
                    var maximizeAdverse = $(this).closest('.webgis-dockpanel').hasClass('webgis-dockpanel-maximize-adverse');

                    var sizeProperty = maximizeAdverse ?
                        ($panel.hasClass('webgis-dockpanel-top') || $panel.hasClass('webgis-dockpanel-bottom') ? "width" : "height") :
                        ($panel.hasClass('webgis-dockpanel-top') || $panel.hasClass('webgis-dockpanel-bottom') ? "height" : "width");

                    if ($panel.hasClass('maximized')) {
                        $panel.css(sizeProperty, $panel.attr('data-maximized')); //.css('max-' + sizeProperty, '100%');
                    } else {
                        $panel.css(sizeProperty, $panel.attr('data-size')); //.css('max-' + sizeProperty, $panel.attr('data-maxsize'));
                    }

                    updateSize($panel);
                });

            if (options.canClose) {
                $("<div class='webgis-dockpanel-resize'></div>").appendTo($title)
                $close = $("<div class='webgis-dockpanel-close'></div>").css({}).appendTo($title)
                    .click(function (e) {
                        e.stopPropagation();
                        var $panel = $(this).closest('.webgis-dockpanel');
                        var options = $panel.data('options');
                        if (options.onclose)
                            options.onclose($panel);
                        $panel.remove();
                    });
            } else {
                $("<div class='webgis-dockpanel-minimize'></div>").css({}).appendTo($title)
                    .click(function (e) {
                        e.stopPropagation();
                        $(this).closest('.webgis-dockpanel').toggleClass('minimized');
                    });
            }
        }
        if (options.titleImg) {
            $title.addClass('image').css({
                backgroundImage: "url(" + options.titleImg + ")"
            });
        }
        $title.children('.text').html(options.title);
       
        var $content = $panel.children('.webgis-dockpanel-content');
        if ($content.length === 0) {
            $content = $("<div class='webgis-dockpanel-content'>")
                .appendTo($panel);
        } else {
            $content.empty();
        }

        webgis.delayed(function () {
            var resize = options.autoResize || options.autoResizeBoth;
            if (resize === false || sizeProperty==='width')
                $panel.css(sizeProperty, $panel.attr('data-size'));

            webgis.delayed(function () {
                if (options.onload) {
                    options.onload($content, $panel);
                    if (resize === true && sizeProperty === 'height') {
                        var height = 85; // 66;
                        $content.children().each(function (i, c) {
                            height += $(c).outerHeight();
                        });
                        $panel.webgis_dockPanel('resize', { size: height });

                        if (options.autoResizeBoth) {
                            var width = 0;
                            $content.children().addClass('min-width');
                            $content.children().each(function (i, c) {
                                width = Math.max(width, $(c).outerWidth());

                                //console.log('outerWidth', $(c).outerWidth());
                            });
                            $content.children().removeClass('min-width');
                            $panel.css('width', Math.min(width + 40, parseInt($(window).width() - $panel.offset().left)));

                            //console.log('max-width: ' + parseInt($(window).width() - $panel.offset().left));

                            $panel.css('right', '');
                        };
                    }
                    if ($close) {
                        webgis.setHistoryItem($close, $content);
                    }
                }
            }, 350);
        }, 1);

        if (options.map && !options.map._webgis_dockPanel_eventsInitialized) {
            options.map._webgis_dockPanel_eventsInitialized = true;

            options.map.events.on('begin-resize', function (channel, sender) {
                hideAll(sender);
            });
            options.map.events.on('resize', function (channel, sender) {
                updateAllSizes(sender);
            });
        }
    };
    var dialogId = function (options) {
        if (options.id)
            return 'webgis-dockpanel-' + options.id.replace(/\./g, '-').replace(/:/g, '-');
    };
    var dialogSelector = function (options) {
        var id = dialogId(options);
        return (id === '' ? '.webgis-dockpanel' : '#' + id + '.webgis-dockpanel');
    };

    var updateSize = function ($panel) {
        var options = $panel.data('options');
        var sizeProperty = options.dock === 'top' || options.dock === 'bottom' ? "height" : "width";

        var initalSize = $panel.hasClass('minimized') ?
            $panel.attr('data-size') :
            $panel.css(sizeProperty);

        var sizeMaximized = '100%', deltaWidth=0, deltaHeight=0;
        var dockRef = null;
        var $parent = $panel.parent();

        if (options.refElement) {
            var parentOffset = $parent.offset(), panelOffset = $(options.refElement).offset();
            var pos = { left: panelOffset.left - parentOffset.left, top: panelOffset.top - parentOffset.top };
            
            if (pos) {
                dockRef = {
                    left: pos.left,
                    top: pos.top,
                    right: parseInt($parent.width() - pos.left - $(options.refElement).width()),
                    bottom: parseInt($parent.height() - pos.top - $(options.refElement).height())
                };
                //alert(JSON.stringify(dockRef));
            }

            deltaWidth = $parent.width() - $(options.refElement).width();
            deltaHeight = $parent.height() - $(options.refElement).height();
        }

        switch (options.dock) {
            case 'left':
                $panel.css({
                    left: (dockRef && dockRef.left ? dockRef.left : 0) + (options.usePadding ? webgis.usability.dockPanelPadding.left : 0),
                    top: (dockRef && dockRef.top ? dockRef.top : 0) + (options.usePadding ? webgis.usability.dockPanelPadding.top : 0),
                    bottom: (dockRef && dockRef.bottom ? dockRef.bottom : 0) + + (options.usePadding ? webgis.usability.dockPanelPadding.bottom : 0),
                    width: initalSize//options.size
                });
                if (options.usePadding) {
                    if (options.maximizeAdverse) {
                        sizeMaximized = 'calc(100% - ' + (webgis.usability.dockPanelPadding.top + webgis.usability.dockPanelPadding.bottom) + 'px)';
                    } else {
                        sizeMaximized = 'calc(100% - ' + (webgis.usability.dockPanelPadding.left + webgis.usability.dockPanelPadding.right + deltaWidth) + 'px)';
                    }
                }
                break;
            case 'right':
                $panel.css({
                    right: (dockRef ? dockRef.right : 0) + (options.usePadding ? webgis.usability.dockPanelPadding.right : 0),
                    top: options.adverseSize ? '' : (dockRef && dockRef.top ? dockRef.top : 0) + (options.usePadding ? webgis.usability.dockPanelPadding.top : 0),
                    bottom: (dockRef && dockRef.bottom ? dockRef.bottom : 0) + (options.usePadding ? webgis.usability.dockPanelPadding.bottom : 0),
                    width: initalSize, //options.size
                    height: options.adverseSize ? options.adverseSize : ''
                });
                if (options.usePadding) {
                    if (options.maximizeAdverse) {
                        sizeMaximized = 'calc(100% - ' + (webgis.usability.dockPanelPadding.top + webgis.usability.dockPanelPadding.bottom) + 'px)';
                    } else {
                        sizeMaximized = 'calc(100% - ' + (webgis.usability.dockPanelPadding.left + webgis.usability.dockPanelPadding.right + deltaWidth) + 'px)';
                    }
                }
                break;
            case 'top':
                $panel.css({
                    left: (dockRef && dockRef.left ? dockRef.left : 0) + (options.usePadding ? webgis.usability.dockPanelPadding.left : 0),
                    top: (dockRef && dockRef.top ? dockRef.top : 0) + (options.usePadding ? webgis.usability.dockPanelPadding.top : 0),
                    right: (dockRef && dockRef.right ? dockRef.right : 0) + (options.usePadding ? webgis.usability.dockPanelPadding.right : 0),
                    height: initalSize //options.size
                });
                if (options.usePadding) {
                    sizeMaximized = 'calc(100% - ' + (webgis.usability.dockPanelPadding.top + webgis.usability.dockPanelPadding.bottom + deltaHeight) + 'px)';
                }
                break;
            case 'bottom':
                $panel.css({
                    left: (dockRef && dockRef.left ? dockRef.left : 0) + (options.usePadding ? webgis.usability.dockPanelPadding.left : 0),
                    right: (dockRef && dockRef.right ? dockRef.right : 0) + (options.usePadding ? webgis.usability.dockPanelPadding.right : 0),
                    bottom: (dockRef && dockRef.bottom ? dockRef.bottom : 0) + (options.usePadding ? webgis.usability.dockPanelPadding.bottom : 0),
                    height: initalSize, // options.size
                });
                if (options.autoResizeBoth !== true) {
                    $panel.css({
                        width: 'inherit'
                    });
                }
                if (options.usePadding) {
                    sizeMaximized = 'calc(100% - ' + (webgis.usability.dockPanelPadding.top + webgis.usability.dockPanelPadding.bottom + deltaHeight) + 'px)';
                }
                break;
        }

        $panel.attr('data-maximized', sizeMaximized);
        if ($panel.hasClass('maximized')) {
            $panel.css('max-' + sizeProperty, sizeMaximized);
        } else {
            $panel.css('max-' + sizeProperty, $panel.attr('data-maxsize'));
        }

        if ($panel.hasClass('maximized')) {
            $panel.css(sizeProperty, $panel.attr('data-maximized'));
        }
    };

    var updateAllSizes = function (map) {
        $(map._webgisContainer).find('.webgis-dockpanel').each(function (i, panel) {
            var $panel = $(panel);

            updateSize($panel.css('display','block'));
        });
    };

    var hideAll = function (map) {
        $(map._webgisContainer).find('.webgis-dockpanel').each(function (i, panel) {
            var $panel = $(panel);

            $panel.css('display', 'none');
        });
    }
})(webgis.$ || jQuery);
