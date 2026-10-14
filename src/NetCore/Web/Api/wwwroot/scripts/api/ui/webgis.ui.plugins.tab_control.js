// WebGIS UI Plugins (tab_control)
// webgis_tab_control
// Tab control for the query results (desktop layout, .query-results-tab-control-container):
// tab bar with closeable/pinable/editable tabs, one content element per tab and an "all tabs" dialog.
// Created in webgis.map.ui (once per map); calling webgis_tab_control({ map }) again rebuilds it (reinit).
// The tab helpers (addTab, removeTab, selectTab, ...) are static methods working on a $control element.

webgis.ui.definePlugin('webgis_tab_control', {
    reinit: true,
    defaults: {
    },
    init: function () {
        const $ = this.$, options = this.options, Ctor = this.constructor;

        const $control = this.$el
            .addClass('webgis-tab-control')
            .data('options', options)
            .empty();

        const $tabBar = $("<div>")
            .addClass('webgis-tab-control-tabbar')
            .appendTo($control);

        $("<div>")
            .addClass('webgis-tab-control-tabbar-selector')
            .appendTo($tabBar)
            .on('click.webgis_tab_control', function () {
                var tabs = $control.webgis_tab_control('getTabsOptions');

                webgis.ui.showModal({
                    id: 'webgis-tab-control-all-tabs-modal',
                    title: webgis.l10n.get('active-queryresults'),
                    width: '330px',
                    height: (50 + (tabs.length + 1) * 40 + 20) + 'px',
                    onload: function ($content) {
                        Ctor.renderOpenTabsDialog($control, $content, tabs);
                    }
                });
            });

        if (options.tabs) {
            for (var t in options.tabs) {
                var tab = options.tabs[t];

                Ctor.addTab($control, tab);
            }
        }

        if (options.map) {
            this._onResizeLive = function () {
                var $tabBar = this
                    .children('.webgis-tab-control-tabbar');

                Ctor.checkSize($tabBar);
            };
            options.map.events.on('resize-live', this._onResizeLive, $control);
        }
    },
    destroy: function () {
        if (this.options.map && this._onResizeLive) {
            this.options.map.events.off('resize-live', this._onResizeLive);
        }
        this._onResizeLive = null;

        const $tabBar = this.$el.children('.webgis-tab-control-tabbar');
        $tabBar.find('*').addBack()
            .add(this.$el.children('.webgis-tab-control-tab-content'))
            .off('.webgis_tab_control');
    },
    methods: {
        add: function (options) {
            return this.constructor.addTab(this.$el, options);
        },
        remove: function (options) {
            return this.constructor.removeTab(this.$el, options.id);
        },
        removeAll: function (options) {
            const $ = this.$;
            options = $.extend({}, options);

            var $this = this.$el, $tabs = $this
                .children('.webgis-tab-control-tabbar')
                .children('.webgis-tab-control-tab[tab-id]');

            $tabs.each(function (i, tab) {
                var $tab = $(tab);
                if ($tab.hasClass('static') && !options.includeStatic) {
                    return;
                }
                $this.webgis_tab_control('remove', { id: $tab.attr('tab-id') });
            });

            return $this;
        },
        select: function (options) {
            return this.constructor.selectTab(this.$el, options);
        },
        refresh: function (options) {
            const $ = this.$, Ctor = this.constructor;
            var $control = this.$el,
                $tabBar = $control
                    .children('.webgis-tab-control-tabbar');

            $tabBar.children(".webgis-tab-control-tab").each(function (i, tab) {
                var tabOptions = Ctor.tab2tabOptions($(tab));
                if (tabOptions.selected) { // reselect current tab...
                    Ctor.selectTab($control, tabOptions);
                }
            });

            return $control;
        },
        getTab: function (options) {
            var $tabBar = this.$el
                .children('.webgis-tab-control-tabbar');

            var $tab = $tabBar
                .children(".webgis-tab-control-tab[tab-id='" + options.id + "']");

            return $tab.length === 1 ? $tab : null;
        },
        getContent: function (options) {
            var $content = this.$el
                .children(".webgis-tab-control-tab-content[tab-id='" + options.id + "']");

            return $content.length === 1 ? $content : null;
        },
        getTabsOptions: function (options) {
            const $ = this.$, Ctor = this.constructor;
            var $tabBar = this.$el
                .children('.webgis-tab-control-tabbar');

            var tabs = [];
            $tabBar.children(".webgis-tab-control-tab").each(function (i, tab) {
                tabs.push(Ctor.tab2tabOptions($(tab)));
            });

            return tabs;
        },
        getSelectedTabOptions: function (options) {
            var $tabBar = this.$el
                .children('.webgis-tab-control-tabbar');

            var $tab = $tabBar
                .children(".webgis-tab-control-tab.selected");

            return this.constructor.tab2tabOptions($tab);
        },
        setTabPayload: function (options) {
            var $tabBar = this.$el
                .children('.webgis-tab-control-tabbar');

            var $tab = $tabBar
                .children(".webgis-tab-control-tab[tab-id='" + options.id + "']");

            if ($tab.length === 1) {
                $tab.data('tab-payload', options.payload);
            }

            return this.$el;
        },
        setTabCounter: function (options) {
            var $tab = this.getTab(options);

            if ($tab) {
                this.constructor.setOrAddCounter($tab, options);
            }

            return this.$el;
        },
        currentContent: function (options) {
            var $control = this.$el;
            var $tabBar = $control
                .children('.webgis-tab-control-tabbar');
            var $tab = $tabBar
                .children(".webgis-tab-control-tab.selected");

            return $control.children(".webgis-tab-control-tab-content[tab-id='" + $tab.attr('tab-id') + "']");
        },
        changeTabId: function (options) {
            var $tab = this.getTab({ id: options.fromId });
            if ($tab) {
                $tab.attr('tab-id', options.toId);
            }

            var $content = this.getContent({ id: options.fromId });
            if ($content) {
                $content.attr('tab-id', options.toId);
            }
        },
        editTitle: function (options) {
            const $ = this.$;

            var $tab = this.getTab({ id: options.id });
            if ($tab) {
                var $title = $tab.children('.title');
                var title = $title.text();

                var isTitleEditable = $tab.hasClass('title-editable') || options.setTitleEditable;
                $tab.removeClass('title-editable');  // hide edit-button

                $("<input type='text'>")
                    .val(title)
                    .on('click.webgis_tab_control', function (e) {
                        e.stopPropagation();
                    })
                    .on('keydown.webgis_tab_control', function (e) {
                        if (e.keyCode === 13) {
                            $(this).blur();
                        }
                    })
                    .on('blur.webgis_tab_control', function () {
                        var val = $(this).val();
                        $(this).remove();
                        $title.text(val);

                        if (isTitleEditable === true) {
                            $tab.addClass('title-editable')
                        }
                    })
                    .appendTo($title.empty())
                    .focus();
            }
        },
        setTitleEditable: function (options) {
            var $tab = this.getTab({ id: options.id });
            if ($tab) {
                if (options.editable === false) {
                    $tab.removeClass('title-editable');
                } else {
                    $tab.addClass('title-editable');
                }
            }
        },
        resize: function (options) {
            var $tabBar = this.$el
                .children('.webgis-tab-control-tabbar');

            this.constructor.checkSize($tabBar);
        }
    },
    staticMethods: {
        addTab: function ($control, tab) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_tab_control.plugin;

            if (!tab.id)
                return;

            var $tabBar = $control
                .children('.webgis-tab-control-tabbar');

            var $tab = $tabBar
                .children(".webgis-tab-control-tab[tab-id='" + tab.id + "']");

            if ($tab.length === 0) {
                $tab = $("<div>")
                    .addClass('webgis-tab-control-tab')
                    .attr('tab-id', tab.id)
                    .data('tab-payload', tab.payload || {})
                    .appendTo($tabBar)
                    .on('click.webgis_tab_control', function (e) {
                        e.stopPropagation();

                        Ctor.selectTab($(this).closest('.webgis-tab-control'), { id: $(this).attr('tab-id') });
                    });

                $("<div>")
                    .addClass('title')
                    .text(tab.title || tab.id)
                    .appendTo($tab);
            }

            var $tabContent = $control
                .children(".webgis-tab-control-tab-content[tab-id='" + tab.id + "']");

            if ($tabContent.length === 0) {
                $tabContent = $("<div>")
                    .addClass('webgis-tab-control-tab-content')
                    .attr('tab-id', tab.id)
                    .css('display', 'none')
                    .on('scroll.webgis_tab_control', function () {   // make sure scroll position is saved
                        const $this = $(this);
                        $this.data("_scrollTop", $this.scrollTop());
                    })
                    .appendTo($control);
            }

            if (tab.titleEditable) {
                $tab.addClass('title-editable');
            }

            $("<div>")  // always add edit-title button (invisible if tab don't has class 'title-editable')
                .addClass('edit-title')
                .appendTo($tab)
                .on('click.webgis_tab_control', function (e) {
                    e.stopPropagation();

                    var $tab = $(this).parent();

                    $control.webgis_tab_control('editTitle', { id: $tab.attr('tab-id') });
                });

            if (tab.pinable) {
                $tab.addClass('pinable');
                $("<div>")
                    .addClass('pin')
                    .appendTo($tab)
                    .on('click.webgis_tab_control', function (e) {
                        e.stopPropagation();

                        var $tab = $(this).parent();
                        $tab.toggleClass('pinned');

                        if ($tab.hasClass('pinned') && $tab.data('onPinned')) {
                            var tabOptions = Ctor.tab2tabOptions($tab);
                            tabOptions.clickEvent = e;

                            $tab.data('onPinned')($control, $tab, tabOptions);
                        }
                    });
                if (tab.onPinned) {
                    $tab.data('onPinned', tab.onPinned);
                }
            }

            Ctor.setOrAddCounter($tab, tab);

            if (tab.static) {
                $tab.addClass('static');
            } else {
                $("<div>")
                    .addClass('close-button')
                    .appendTo($tab)
                    .on('click.webgis_tab_control', function (e) {
                        e.stopPropagation();

                        var $tab = $(this).closest('.webgis-tab-control-tab');

                        $(this).closest('.webgis-tab-control').webgis_tab_control('remove', { id: $tab.attr('tab-id') });
                    });

                if (tab.onRemoved) {
                    $tab.data('onRemoved', tab.onRemoved);
                }
            }

            if (tab.onSelected) {
                $tab.data('onSelected', tab.onSelected);
            }

            if (tab.onCreated) {
                tab.onCreated($control, $tab, $tabContent);
            }

            if (tab.select === true) {
                Ctor.selectTab($control, { id: tab.id });
            }

            if (tab.customData) {
                $tab.data('data-customdata', tab.customData);
            }

            if (tab.icon) {
                $tab.css('background-image', 'url(' + tab.icon + ')');
            }

            Ctor.checkSize($tabBar);

            if (tab.pinned && $tab.data('onPinned')) {
                $tab.addClass('pinned').data('onPinned')($control, $tab, Ctor.tab2tabOptions($tab));
            }

            return $tabContent;
        },

        removeTab: function ($control, tabId) {
            const Ctor = (webgis.$ || jQuery).fn.webgis_tab_control.plugin;

            var $tabBar = $control
                .children('.webgis-tab-control-tabbar');
            var $tab = $tabBar
                .children(".webgis-tab-control-tab[tab-id='" + tabId + "']");

            var onRemoved = $tab.data('onRemoved'), options = Ctor.tab2tabOptions($tab);

            if ($tab.length !== 0) {
                $tab.remove();
            }

            var $tabContent = $control
                .children(".webgis-tab-control-tab-content[tab-id='" + tabId + "']");

            if ($tabContent.length !== 0) {
                $tabContent.remove();
            }

            var controlOptions = $control.data('options');
            if (controlOptions && controlOptions.map) {
                var activeTool = controlOptions.map.getActiveTool();
                if (activeTool && activeTool.hasui !== true) {  // eg. if tool is "add to selection" clear this tool because it is invisible after deleting all results
                    controlOptions.map.setDefaultTool();
                }
            }

            if (onRemoved) {
                onRemoved($control, options);
            }

            Ctor.checkSize($tabBar);
        },

        selectTab: function ($control, options) {
            const Ctor = (webgis.$ || jQuery).fn.webgis_tab_control.plugin;

            const $tabBar = $control
                .children('.webgis-tab-control-tabbar');

            // unselect current
            if (options.id == null) {
                $control.children('.webgis-tab-control-tab-content').css('display', 'none');
                $tabBar.children(".webgis-tab-control-tab").removeClass('selected');
                return;
            }

            const $tab = $tabBar
                .children(".webgis-tab-control-tab[tab-id='" + options.id + "']");

            if ($tab.length === 0)
                return null;

            $tabBar
                .children('.webgis-tab-control-tab')
                .removeClass('selected');
            $tab.addClass('selected');

            if (options.title) {
                $tab.children('.title').text(options.title);
            }

            Ctor.setOrAddCounter($tab, options);

            const $tabContent = $control
                .children(".webgis-tab-control-tab-content[tab-id='" + options.id + "']");

            if ($tabContent.length === 0)
                return null;

            $tab.addClass('loading');

            webgis.delayed(function () {  // run this in a new "thread" to let ui thread show loading icon
                $control.children(".webgis-tab-control-tab-content").css("display", "none");

                if ($tab.data("onSelected")) {
                    $tab.data("onSelected")($control, $tab, Ctor.tab2tabOptions($tab));
                }

                Ctor.checkSize($tabBar);

                $tabContent
                    .css("display", "block")
                    .data("$tab", $tab)
                    .scrollTop($tabContent.data("_scrollTop") || 0);  // and restore scroll position after reopening tab

                $tab.removeClass('loading');
            }, 1);

            return $tabContent;
        },

        tab2tabOptions: function ($tab) {
            if (!$tab || $tab.length === 0) {
                return null;
            }

            return {
                id: $tab.attr('tab-id'),
                title: $tab.children('.title').text(),
                payload: $tab.data('tab-payload'),
                customData: $tab.data('data-customdata'),
                selected: $tab.hasClass('selected'),
                pinned: $tab.hasClass('pinned'),
                counter: $tab.attr('tab-counter') || null
            };
        },

        setOrAddCounter: function ($tab, tabOptions) {
            const $ = webgis.$ || jQuery;

            if (tabOptions.counter) {
                $tab
                    .attr('tab-counter', tabOptions.counter)
                    .addClass('has-counter');

                var $counter = $tab.children('.counter');
                if ($counter.length === 0) {
                    $counter = $("<div>")
                        .addClass('counter')
                        .appendTo($tab)
                }

                var counter = tabOptions.counter, text;
                // Exakten Wert anzeigen (mit Tausenderpunkt) statt gerundeter "K"-Abkuerzung -
                // die K-Abkuerzung war ok, solange 1000 das AGS-Maximum war, ist aber seit dem
                // Ids-Workaround (mehr als 1000 Ergebnisse moeglich) zu ungenau.
                text = counter.toLocaleString('de-DE');

                $counter.text(text).attr('title', text);
            }
        },

        checkSize: function ($tabBar) {
            const $ = webgis.$ || jQuery;

            function check(skip) {
                var pos = 0, tabsWidth = $tabBar.width();

                $tabBar.children('.webgis-tab-control-tab').each(function (i, tab) {
                    var $tab = $(tab).css('display', ''), tabWidth = $tab.outerWidth();

                    if (i < skip) {
                        $tab.css('display', 'none');
                    } else {
                        if (pos + tabWidth >= tabsWidth - 30) {
                            $tab.css('display', 'none');
                        } else {
                            pos += tabWidth;
                        }
                    }
                });
            };

            var numTabs = $tabBar.children('.webgis-tab-control-tab').length, skip = 0;
            var $selectedTab = $tabBar.children('.webgis-tab-control-tab.selected');

            while (skip < numTabs) {
                check(skip);

                if ($selectedTab.length === 0 || $selectedTab.css('display') !== 'none') {
                    break;
                }

                skip++;
            }
        },

        renderOpenTabsDialog: function ($control, $parent, tabs) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_tab_control.plugin;

            var closeDialog = function () {
                webgis.ui.closeModal('webgis-tab-control-all-tabs-modal');
            }

            var options = $control.data('options');

            var $list = $("<ul>")
                .addClass('webgis-list checkable clickable')
                .appendTo($parent);

            var $item = $("<li>")
                .addClass('webgis-list-item')
                .css({ paddingTop: 0, paddingBottom: 4 })
                .appendTo($list);

            $("<button>")
                .addClass('webgis-button uibutton-cancel uibutton')
                .text(webgis.l10n.get('close-selected-tabs'))
                .appendTo($item)
                .on('click.webgis_tab_control', function (e) {
                    e.stopPropagation();

                    $list.children('.webgis-list-item').each(function (i, item) {
                        var $item = $(item), tab = $item.data('tab');
                        if (tab && $item.children('.checkbox.checked').length === 1) {
                            Ctor.removeTab($control, tab.id);
                        }
                    });

                    closeDialog();
                });

            $("<div>")
                .addClass('checkbox')
                .appendTo($item)
                .on('click.webgis_tab_control', function (e) {
                    e.stopPropagation();

                    var $this = $(this).toggleClass('checked')
                    var checked = $this.hasClass('checked');

                    $list.find('.checkbox').each(function (i, checkbox) {
                        var $checkbox = $(checkbox);

                        if (checked) {
                            if (!$checkbox.hasClass('pinned')) {
                                $checkbox.addClass('checked');
                            }
                        } else {
                            $checkbox.removeClass('checked');
                        }
                    });
                });

            $.each(tabs, function (i, tab) {
                $item = $("<li>")
                    .addClass('webgis-list-item' + (tab.selected ? ' selected' : ''))
                    .text(tab.title)
                    .data('tab', tab)
                    .appendTo($list)
                    .on('click.webgis_tab_control', function (e) {
                        e.stopPropagation();

                        var tab = $(this).data('tab');

                        closeDialog();
                        Ctor.selectTab($control, tab);
                    });

                if (tab.customData && tab.customData.dynamicContent) {
                    if (options && options.map) {
                        $("<div>")
                            .addClass('checkbox')
                            .css('background-image', 'url(' + options.map.ui.dynamicContentIcon(tab.customData.dynamicContent) + ')')
                            .appendTo($item);
                    }
                } else {
                    $("<div>")
                        .addClass('checkbox' + (tab.pinned ? ' pinned' : ''))
                        .appendTo($item)
                        .on('click.webgis_tab_control', function (e) {
                            e.stopPropagation();

                            $(this).toggleClass('checked');
                        });
                }

                Ctor.setOrAddCounter($item, tab);
            });
        }
    }
});
