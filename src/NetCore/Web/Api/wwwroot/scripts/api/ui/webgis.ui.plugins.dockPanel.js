// WebGIS UI Plugins (dock panel)
// webgis_dockPanel
// Creates (or reuses) a docked panel (left/right/top/bottom) inside the element the plugin is called on (e.g. the webgis container).
// Panel helpers (content, close, remove, ...) are static methods; callers outside this file use
// webgis.ui.closeDockPanel/removeDockPanel/removeAllDockPanels/setDockPanelTitle/getDockPanelId (end of file).

webgis.ui.definePlugin('webgis_dockPanel', {
    // called repeatedly on the same container (one call per panel to show) => rebuild each time
    reinit: true,
    defaults: {
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
    },
    init: function () {
        const $ = this.$;
        const Plugin = this.constructor;
        const options = this.options;
        const $parent = this.$el;

        var sizeProperty = options.dock === 'top' || options.dock === 'bottom' ? "height" : "width";
        var adverseSizeProperty = options.dock === 'top' || options.dock === 'bottom' ? "width" : "height";

        //
        // bestehendes Panel wiederverwenden (ausser bei 'useIdSelector')
        // Wichtig, falls man die Back-Button History (Handy) benutzt
        // Historyeintrag und "close" Button sollten dabei bestehen bleiben
        // Nicht mit webgis_dockPanel('remove') entfernen und neu laden, weil da die History durch die Timer darin durcheinander kommt
        //
        var $panel = options.useIdSelector === true ? $parent.find('#' + Plugin.dialogId(options)) :
                                                      $parent.find('.webgis-dockpanel-' + options.dock);
        if ($panel === null || $panel.length === 0) {
            $panel = null;
        }

        if ($panel === null) {
            $panel = $("<div>")
                .addClass('webgis-dockpanel webgis-dockpanel-' + options.dock)
                .css({ position: 'absolute' })
                .appendTo($parent);
        }

        if (options.id || $panel.attr('id')) {
            $panel.attr('id', Plugin.dialogId(options));
        }

        $panel
            .data('options', options)
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

        Plugin.updateSize($panel);

        var $title = $panel.children('.webgis-dockpanel-title'),
            $close = null;
        if ($title.length === 0) {
            $title = $("<div class='webgis-dockpanel-title'><div class='text'></div></div>").appendTo($panel)
                .on('click.webgis_dockPanel', function (e) {
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
                        $panel.css(sizeProperty, $panel.attr('data-maximized'));
                    } else {
                        $panel.css(sizeProperty, $panel.attr('data-size'));
                    }

                    Plugin.updateSize($panel);
                });

            if (options.canClose) {
                $("<div class='webgis-dockpanel-resize'></div>").appendTo($title);
                $close = $("<div class='webgis-dockpanel-close'></div>").css({}).appendTo($title)
                    .on('click.webgis_dockPanel', function (e) {
                        e.stopPropagation();
                        var $panel = $(this).closest('.webgis-dockpanel');
                        var options = $panel.data('options');
                        if (options.onclose)
                            options.onclose($panel);
                        $panel.remove();
                    });
            } else {
                $("<div class='webgis-dockpanel-minimize'></div>").css({}).appendTo($title)
                    .on('click.webgis_dockPanel', function (e) {
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
            if (resize === false || sizeProperty === 'width')
                $panel.css(sizeProperty, $panel.attr('data-size'));

            webgis.delayed(function () {
                if (options.onload) {
                    options.onload($content, $panel);
                    if (resize === true && sizeProperty === 'height') {
                        let height = 85; // 66;
                        $content.children().each(function (i, c) {
                            height += $(c).outerHeight();
                        });
                        Plugin.resize($panel, { size: height });

                        if (options.autoResizeBoth) {
                            $panel.css({ width: 'unset', right: '0px' });

                            let width = 0;
                            let bordContent = $content.outerWidth() - $content.innerWidth();
                            let paddContent = $content.innerWidth() - $content.width();
                            let margContent = $content.outerWidth(true) - $content.outerWidth();
                            let addWidth = bordContent + paddContent + margContent;

                            $content.children().addClass('min-width');
                            $content.children().each(function (i, c) {
                                width = Math.max(width, $(c).outerWidth());
                            });
                            $content.children().removeClass('min-width');
                            $panel.css('width', Math.min(width + addWidth, parseInt($(window).width() - $panel.offset().left)));

                            $panel.css('right', '');
                        }
                    }
                    if ($close) {
                        webgis.setHistoryItem($close, $content);
                    }
                }
            }, 350);
        }, 1);

        // map events are subscribed once per map (for all dock panels of the map) and live as long as the map
        if (options.map && !options.map._webgis_dockPanel_eventsInitialized) {
            options.map._webgis_dockPanel_eventsInitialized = true;

            options.map.events.on('begin-resize', function (channel, sender) {
                Plugin.hideAll(sender);
            });
            options.map.events.on('resize', function (channel, sender) {
                Plugin.updateAllSizes(sender);
            });
        }
    },
    destroy: function () {
        // nothing to release: the created panel is intentionally kept (it is reused by the next call),
        // its child handlers are removed with the panel, map events are per map (see init)
    },
    methods: {
        content: function (options) {
            return this.constructor.content(options);
        },
        exists: function (options) {
            return this.constructor.exists(options);
        },
        close: function (options) {
            return this.constructor.close(options);
        },
        removeAll: function () {
            this.constructor.removeAll(this.$el);
        },
        set_title: function (options) {
            this.constructor.set_title(options);
        },
        panelId: function (options) {
            return this.constructor.dialogId(options);
        }
    },
    staticMethods: {
        content: function (options) {
            const $ = webgis.$ || jQuery;
            var settings = $.extend({}, options);
            return $(this.dialogSelector(settings)).find('.webgis-dockpanel-content');
        },
        exists: function (options) {
            return this.content(options).length === 1;
        },
        close: function (options) {
            const $ = webgis.$ || jQuery;
            options = $.extend({}, options);
            var $panel = $(this.dialogSelector(options));
            if ($panel.length > 0) {
                var panelOptions = $panel.data('options');
                if (panelOptions.onclose)
                    panelOptions.onclose($panel);
                this.remove($panel);
                return true;
            }
            return false;
        },
        remove: function ($panel) {
            const $ = webgis.$ || jQuery;
            $panel = $($panel);
            if ($panel.hasClass('webgis-dockpanel')) {
                webgis.removeHistoryItem($panel.find('.webgis-dockpanel-close'));
                $panel.remove();
            }
        },
        removeAll: function ($container) {
            const $ = webgis.$ || jQuery;
            this.remove($($container).find('.webgis-dockpanel'));
        },
        resize: function ($panel, options) {
            const $ = webgis.$ || jQuery;
            $panel = $($panel);
            if (options.size) {
                var sizeProperty = $panel.hasClass('webgis-dockpanel-top') || $panel.hasClass('webgis-dockpanel-bottom') ? "height" : "width";
                $panel.attr('data-size', options.size);
                $panel.css(sizeProperty, options.size);
            }
        },
        set_title: function (options) {
            const $ = webgis.$ || jQuery;
            var $panel = $(this.dialogSelector(options));
            if ($panel.length === 1) {
                $panel.find('.webgis-dockpanel-title').find('.text').text(options.title);
            }
        },
        panelId: function (options) {
            return this.dialogId(options);
        },
        dialogId: function (options) {
            if (options.id)
                return 'webgis-dockpanel-' + options.id.replace(/\./g, '-').replace(/:/g, '-');
        },
        dialogSelector: function (options) {
            var id = this.dialogId(options);
            return (id === '' ? '.webgis-dockpanel' : '#' + id + '.webgis-dockpanel');
        },
        updateSize: function ($panel) {
            const $ = webgis.$ || jQuery;
            var options = $panel.data('options');
            var sizeProperty = options.dock === 'top' || options.dock === 'bottom' ? "height" : "width";

            var initalSize = $panel.hasClass('minimized') ?
                $panel.attr('data-size') :
                $panel.css(sizeProperty);

            var sizeMaximized = '100%', deltaWidth = 0, deltaHeight = 0;
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
                        width: initalSize
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
                        width: initalSize,
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
                        height: initalSize
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
                        height: initalSize
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
        },
        updateAllSizes: function (map) {
            const $ = webgis.$ || jQuery;
            const Plugin = this;
            $(map._webgisContainer).find('.webgis-dockpanel').each(function (i, panel) {
                Plugin.updateSize($(panel).css('display', 'block'));
            });
        },
        hideAll: function (map) {
            const $ = webgis.$ || jQuery;
            $(map._webgisContainer).find('.webgis-dockpanel').each(function (i, panel) {
                $(panel).css('display', 'none');
            });
        }
    }
});

// static helpers for callers outside this file (no plugin instance needed)
webgis.ui.closeDockPanel = function (options) {
    return (webgis.$ || jQuery).fn.webgis_dockPanel.plugin.close(options);
};
webgis.ui.removeDockPanel = function ($panel) {
    (webgis.$ || jQuery).fn.webgis_dockPanel.plugin.remove($panel);
};
webgis.ui.removeAllDockPanels = function ($container) {
    (webgis.$ || jQuery).fn.webgis_dockPanel.plugin.removeAll($container);
};
webgis.ui.setDockPanelTitle = function (options) {
    (webgis.$ || jQuery).fn.webgis_dockPanel.plugin.set_title(options);
};
webgis.ui.getDockPanelId = function (options) {
    return (webgis.$ || jQuery).fn.webgis_dockPanel.plugin.panelId(options);
};
