(function ($) {
    "use strict";
    $.fn.webgis_tab_control = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_tab_control');
        }
    };

    var defaults = {

    };

    var methods = {
        init: function (options) {
            options = $.extend({}, defaults, options);

            return this.each(function () {
                new initUI(this, options);
            });
        },
        add: function (options) {
            return addTab($(this), options);
        },
        remove: function (options) {
            return removeTab($(this), options.id);
        },
        removeAll: function (options) {
            options = $.extend({}, defaults, options);

            var $this = $(this), $tabs = $this
                .children('.webgis-tab-control-tabbar')
                .children('.webgis-tab-control-tab[tab-id]');

            $tabs.each(function (i, tab) {
                var $tab = $(tab);
                if ($tab.hasClass('static') && !options.includeStatic) {
                    return;
                }
                $this.webgis_tab_control('remove', { id: $tab.attr('tab-id') });
            });

            return $(this);
        },
        select: function(options) {
            return selectTab($(this), options);
        },
        refresh: function (options) {
            var $control = $(this),
                $tabBar = $control
                    .children('.webgis-tab-control-tabbar');

            $tabBar.children(".webgis-tab-control-tab").each(function (i, tab) {
                var tabOptions = $tab2tabOptions($(tab));
                if (tabOptions.selected) { // reselect current tab...
                    selectTab($control, tabOptions);
                }
            });

            return $(this);
        },
        getTab: function (options) {
            var $tabBar = $(this)
                .children('.webgis-tab-control-tabbar');

            var $tab = $tabBar
                .children(".webgis-tab-control-tab[tab-id='" + options.id + "']");

            return $tab.length === 1 ? $tab : null;
        },
        getContent: function (options) {
            var $control = $(this);

            var $content = $control
                .children(".webgis-tab-control-tab-content[tab-id='" + options.id + "']");

            return $content.length === 1 ? $content : null;
        },
        getTabsOptions: function (options) {
            var $tabBar = $(this)
                .children('.webgis-tab-control-tabbar');

            var tabs = [];
            $tabBar.children(".webgis-tab-control-tab").each(function (i, tab) {
                tabs.push($tab2tabOptions($(tab)));
            });

            return tabs;
        },
        getSelectedTabOptions: function (options) {
            var $tabBar = $(this)
                .children('.webgis-tab-control-tabbar');

            var $tab = $tabBar
                .children(".webgis-tab-control-tab.selected");

            return $tab2tabOptions($tab);
        },
        setTabPayload: function (options) {
            var $this = $(this);

            var $tabBar = $(this)
                .children('.webgis-tab-control-tabbar');

            var $tab = $tabBar
                .children(".webgis-tab-control-tab[tab-id='" + options.id + "']");

            if ($tab.length === 1) {
                $tab.data('tab-payload', options.payload);
            }

            return $this;
        },
        setTabCounter: function (options) {
            var $tab = $(this).webgis_tab_control('getTab', options);

            if ($tab) {
                _setOrAddCounter($tab, options);
            }

            return $(this);
        },
        currentContent: function (options) {
            var $control = $(this);
            var $tabBar = $control
                .children('.webgis-tab-control-tabbar');
            var $tab = $tabBar
                .children(".webgis-tab-control-tab.selected");

            return $control.children(".webgis-tab-control-tab-content[tab-id='" + $tab.attr('tab-id') + "']");
        },
        changeTabId: function (options) {
            var $control = $(this);

            var $tab = $control.webgis_tab_control('getTab', { id: options.fromId });
            if ($tab) {
                $tab.attr('tab-id', options.toId);
            }

            var $content = $control.webgis_tab_control('getContent', { id: options.fromId });
            if ($content) {
                $content.attr('tab-id', options.toId);
            }
        },
        editTitle: function (options) {
            var $control = $(this);

            var $tab = $control.webgis_tab_control('getTab', { id: options.id });
            if ($tab) {
                var $title = $tab.children('.title');
                var title = $title.text();

                var isTitleEditable = $tab.hasClass('title-editable') || options.setTitleEditable;
                $tab.removeClass('title-editable');  // hide edit-button

                $("<input type='text'>")
                    .val(title)
                    .click(function (e) {
                        e.stopPropagation();
                    })
                    .keydown(function (e) {
                        if (e.keyCode === 13) {
                            $(this).blur();
                        }
                    })
                    .blur(function () {
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
            var $control = $(this);

            var $tab = $control.webgis_tab_control('getTab', { id: options.id });
            if ($tab) {
                if (options.editable === false) {
                    $tab.removeClass('title-editable');
                } else {
                    $tab.addClass('title-editable');
                }
            }
        },
        resize: function (options) {
            var $tabBar = $(this)
                .children('.webgis-tab-control-tabbar');

            _checkSize($tabBar);
        }
    };

    var initUI = function (parent, options) {
        //var $holder = $("<div>")
        //    .addClass('webgis-tab-control')
        //    .appendTo($(parent));
        var $control = $(parent)
            .addClass('webgis-tab-control')
            .data('options', options)
            .empty();

        var $tabBar = $("<div>")
            .addClass('webgis-tab-control-tabbar')
            .appendTo($control);

        $("<div>")
            .addClass('webgis-tab-control-tabbar-selector')
            .appendTo($tabBar)
            .click(function () {
                var tabs = $control.webgis_tab_control('getTabsOptions');

                $('body').webgis_modal({
                    id: 'webgis-tab-control-all-tabs-modal',
                    title: webgis.l10n.get('active-queryresults'),
                    width: '330px',
                    height: (50 + (tabs.length + 1) * 40 + 20) + 'px',
                    onload: function ($content) {

                        _renderOpenTabsDialog($control, $content, tabs);
                    }
                });
            });

        if (options.tabs) {
            for (var t in options.tabs) {
                var tab = options.tabs[t];

                addTab($control, tab);
            }
        }

        if (options.map) {
            options.map.events.on('resize-live', function () {
                var $tabBar = this
                    .children('.webgis-tab-control-tabbar');

                _checkSize($tabBar);
            }, $control);
        }
    };

    var addTab = function ($control, tab) {
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
                .click(function (e) {
                    e.stopPropagation();

                    selectTab($(this).closest('.webgis-tab-control'), { id: $(this).attr('tab-id') });
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
                .appendTo($control);
        }

        if (tab.titleEditable) {
            $tab.addClass('title-editable');
        }

        $("<div>")  // always add edit-title button (invisible if tab don't has class 'title-editable')
            .addClass('edit-title')
            .appendTo($tab)
            .click(function (e) {
                e.stopPropagation();

                var $tab = $(this).parent();

                $control.webgis_tab_control('editTitle', { id: $tab.attr('tab-id') });
            });

        if (tab.pinable) {
            $tab.addClass('pinable');
            $("<div>")
                .addClass('pin')
                .appendTo($tab)
                .click(function (e) {
                    e.stopPropagation();

                    var $tab = $(this).parent();
                    $tab.toggleClass('pinned');

                    if ($tab.hasClass('pinned') && $tab.data('onPinned')) {
                        var tabOptions = $tab2tabOptions($tab);
                        tabOptions.clickEvent = e;

                        $tab.data('onPinned')($control, $tab, tabOptions);
                    }
                });
            if (tab.onPinned) {
                $tab.data('onPinned', tab.onPinned);
            }
        }

        _setOrAddCounter($tab, tab);

        if (tab.static) {
            $tab.addClass('static');
        } else {
            $("<div>")
                .addClass('close-button')
                .appendTo($tab)
                .click(function (e) {
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
            selectTab($control, { id: tab.id });
        }

        if (tab.customData) {
            $tab.data('data-customdata', tab.customData);
        }

        if (tab.icon) {
            $tab.css('background-image', 'url(' + tab.icon + ')');
        }

        _checkSize($tabBar);

        if (tab.pinned && $tab.data('onPinned')) {
            $tab.addClass('pinned').data('onPinned')($control, $tab, $tab2tabOptions($tab));
        }

        return $tabContent;
    };

    var removeTab = function ($control, tabId) {
        //console.log('removeTab', tabId);

        var $tabBar = $control
            .children('.webgis-tab-control-tabbar');
        var $tab = $tabBar
            .children(".webgis-tab-control-tab[tab-id='" + tabId + "']");

        var onRemoved = $tab.data('onRemoved'), options = $tab2tabOptions($tab);

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

        _checkSize($tabBar);
    }

    var selectTab = function ($control, options) {
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

        _setOrAddCounter($tab, options);

        const $tabContent = $control
            .children(".webgis-tab-control-tab-content[tab-id='" + options.id + "']");

        if ($tabContent.length === 0)
            return null;

        $control.children(".webgis-tab-control-tab-content").css("display", "none");

        if ($tab.data("onSelected")) {
            $tab.data("onSelected")($control, $tab, $tab2tabOptions($tab));
        }

        _checkSize($tabBar);

        return $tabContent
            .css("display", "block")
            .data("$tab", $tab)
            .on('scroll', function () {   // make sure scroll position is saved
                const $this = $(this);
                $this.data("_scrollTop", $this.scrollTop());
            })
            .scrollTop($tabContent.data("_scrollTop") || 0);  // and restore scroll position after reopening tab
    };

    var $tab2tabOptions = function ($tab) {
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
    }

    var _setOrAddCounter = function ($tab, tabOptions) {
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
            if (tabOptions.counter >= 1000) {
                var k = Math.round(counter / 1000);
                text = k + "K" + (k * 1000 < counter ? '+' : '');
            } else {
                text = counter.toString();
            }

            $counter.text(text);
        }
    }

    var _checkSize = function ($tabBar) {
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
    };

    var _renderOpenTabsDialog = function ($control, $parent, tabs) {

        var closeDialog = function () {
            $(null).webgis_modal('close', { id: 'webgis-tab-control-all-tabs-modal' });
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
            .click(function (e) {
                e.stopPropagation();

                $list.children('.webgis-list-item').each(function (i, item) {
                    var $item = $(item), tab = $item.data('tab');
                    if (tab && $item.children('.checkbox.checked').length === 1) {
                        removeTab($control, tab.id);
                    }
                });

                closeDialog();
            });

        $("<div>")
            .addClass('checkbox')
            .appendTo($item)
            .click(function (e) {
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
                .click(function (e) {
                    e.stopPropagation();

                    var tab = $(this).data('tab');

                    closeDialog();
                    selectTab($control, tab);
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
                    .click(function (e) {
                        e.stopPropagation();

                        $(this).toggleClass('checked');
                    });
            }

            _setOrAddCounter($item, tab);
        });
    }

})(webgis.$ || jQuery);