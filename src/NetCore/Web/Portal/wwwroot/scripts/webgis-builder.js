var _selectedEpsg = -1;

function createSelectorFromAjax(name, id, url, selectOptions, sortable, type) {
    result = webgis.ajaxSync(webgis.baseUrl + (url.indexOf('/') != 0 ? '/' : '') + url);
    if (type === 'services') {
        for (var i in result[type]) {
            result[type][i].image = "content/api/img/" + result[type][i].type + "-service.png";
        }
        result[type].sort(function (a, b) {
            if (a.type === 'tile' && b.type !== 'tile')
                return -1;
            if (b.type === 'tile' && a.type !== 'tile')
                return 1;
            return a.name.toLowerCase() > b.name.toLowerCase() ? 1 : -1;
        });
    }
    else if (type === 'extents') {
        for (var i in result[type]) {
            result[type][i].image = "content/api/img/extent.png";
        }
    }
    else if (type === 'tools') {
        for (var i in result[type]) {
            //console.log('gettoolname: ' + result[type][i].id + ' = ' + webgis.tools.getToolName(result[type][i].id));
            var toolName = webgis.tools.getToolName(result[type][i].id);
            
            result[type][i].order = webgis.tools.getToolOrder(result[type][i].id);
            result[type][i].id = toolName != null ? toolName : /*"'" +*/ result[type][i].id /*+ "'"*/;
            result[type][i].selected = true;

            if (toolName === 'webgis.tools.io.print') {
                result[type][i].options = {
                    singleselect: false,
                    items: printLayouts()
                };
            }
        }
        result[type].sort(function (a, b) {
            return (a.order - b.order);
        });

        webgis.tools._tools = result.tools;
    }
    createSelector(name, id, selectOptions, result[type], sortable);
};

function printLayouts() {
    var result = webgis.ajaxSync(webgis.baseUrl + '/rest/printlayouts');
    
    var layouts = [];
    for (var i in result) {
        layouts.push({
            id: /*'print-layout-' +*/ result[i].id,
            name: result[i].name
        });
    }

    return layouts;
}

function createSelector(name, id, selectOptions, result, sortable) {

    if (!selectOptions) selectOptions = { singleSelect: false, selectMode: 'normal' };

    var $div = $('#mapbuilder-container').find(".selector-container[data-id='" + id + "']"), $ul;
    if ($div.length == 0) {
        $div = $("<div class='selector-container'>").appendTo('#mapbuilder-container').attr("data-id", id);
        var $title = $("<div class=webgis-presentation_toc-title>" + name + "</div>").appendTo($div).
            click(function () {
                var me = this;
                $(this).parent().parent().find('.selector-container').each(function (i, e) {
                    if (e != $(me).parent().get(0)) {
                        $(e).find('ul:first').slideUp();
                    }
                });
                $(this).parent().find('ul:first').slideToggle();
            });

        $ul = $("<ul class='webgis-toolbox-tool-item-group-details' style='display:none'>").appendTo($div);
        $("<input type='hidden' id='" + id + "' class='webgis-toolbox-parameter' name='" + id + "' />").appendTo($div);
    } else {
        $ul = $div.find('ul:first');
    }

    var containers = [];
    if (result.length > 0 && result[0].container) {
        for (var i in result) {
            var element = result[i];
            if (element.image === "")
                continue;
            if (jQuery.inArray(element.container, containers) >= 0)
                continue;
            containers.push(element.container);
        }
    } else {
        containers.push("*");
    }

    for (var c in containers) {
        var container = containers[c];

        var $parentList = $ul;
        if (container != "*") {
            console.log(container);
            var $group = $ul.find("li[data-container='" + webgis.encodeXPathString(container) + "']");
            if ($group.length == 0) {
                $group = $("<li class='webgis-presentation_toc-item-group' data-container='" + container + "'><div><span class='webgis-api-icon webgis-api-icon-triangle-1-s' style='position:absolute'></span><span style='position:relative;left:20px'>" + container + "</span></div></li>").appendTo($ul);
                $group.find('div:first')
                    .click(function () {
                        $(this).parent().find("ul:first").slideToggle();
                    });

                $parentList = $("<ul class='webgis-toolbox-tool-item-group-details' style='display:none'>").appendTo($group);
            } else {
                $parentList = $group.find('ul:first');
            }
        }

        for (var i in result) {
            var element = result[i];

            if (container !== "*" && container !== element.container)
                continue;

            if (element.image === "")
                continue;

            var text = element.name ? element.name /*+ "<span style='color:#aaa;position:relative;top:-18px;float:right'>(" + element.id + ")</span>"*/ : element.id;
            if (element.image) {
                element.image = element.image.indexOf('/') >= 0 ? webgis.baseUrl + "/" + element.image : webgis.css.imgResource(element.image,'tools');
                text = "<img src='" + element.image + "'>&nbsp;" + text;
            }

            var $li = $parentList.find(".webgis-toolbox-tool-item[data-id='" + element.id + "']");
            if ($li.length === 0) {
                $li = $li = $("<li class='webgis-toolbox-tool-item'><span>" + text + "</span></li>").appendTo($parentList).
                    attr('data-id', element.id).
                    attr('data-container', container);

                if (element.subname) {
                    $("<br/><span style='color:#aaa'>" + element.subname + "</span>").appendTo($li);
                }

                if (element.click) {
                    $li.click(element.click);
                } else {
                    $li.click(function () {
                        var item = this;
                        $(this).toggleClass('webgis-toolbox-tool-item-selected');
                        $(this).children('.webgis-toolbox-tool-item-options').css('display', $(this).hasClass('webgis-toolbox-tool-item-selected') ? '' : 'none');
                        var $parent = $(this).closest('.selector-container').parent();

                        if ($(this).closest('.selector-container').attr('data-id') == 'extents' && typeof item._element.epsg === 'number') {
                            _selectedEpsg = $(this).hasClass('webgis-toolbox-tool-item-selected') ? item._element.epsg : -1;
                            checkAllEpsg($parent);
                        }

                        calcContainerValues($parent, item);
                        if (window.fireSelectionChanged)
                            fireSelectionChanged(item, item._element, $(this).hasClass('webgis-toolbox-tool-item-selected'));
                    });
                }
                $li.get(0)._element = element;

                /*
                $("<img src='" + webgis.css.img('help-26.png') + "' style='width:26px;height:26px;float:right;position:relative;top:-26px' />").appendTo($li)
                    .click(function (event) {
                        event.stopPropagation();
                        showHelp($(this).closest("li"));
                    });
                    */
            }

            if (element.selected) {
                $li.addClass('webgis-toolbox-tool-item-selected');
            } else {
                $li.removeClass('webgis-toolbox-tool-item-selected');
            }
            if (element.options && element.options.items && element.options.items.length > 0) {
                //var $options_parent = $("<li class=''>").appendTo($parentList);
                var $options_ul = $("<ul class='webgis-toolbox-tool-item-options' data-singleselect='" + (element.options.singleselect ? 'true' : 'false') + "'>").appendTo($li).css('display', element.selected ? '' : 'none');
                for (var o in element.options.items) {
                    var optionItem = element.options.items[o];
                    var $options_li = $("<li class='webgis-toolbox-tool-item-option'><span>" + optionItem.name + "</span></li>").appendTo($options_ul)
                        .attr('data-id', optionItem.id)
                        .attr('data-container', element.id /*container*/)
                        .click(function (event) {
                            event.stopPropagation();

                            var item = this;

                            var singleSelect = $(this).closest('.webgis-toolbox-tool-item-options').attr('data-singleselect') == 'true';
                            if (singleSelect == true) {
                                $(this).closest('.webgis-toolbox-tool-item-options').children('.webgis-toolbox-tool-item-option').each(function (i, e) {
                                    if (e != item)
                                        $(e).removeClass('webgis-toolbox-tool-item-selected');
                                });
                            }

                            $(this).toggleClass('webgis-toolbox-tool-item-selected');
                            var $parent = $(this).closest('.selector-container').parent();

                            calcContainerValues($parent, item);
                            if (window.fireSelectionChanged)
                                fireSelectionChanged();
                        });
                    if (optionItem.subname) {
                        $("<br/><span style='color:#aaa'>" + option.subname + "</span>").appendTo($options_li);
                    }
                }
            }
        }

        if (sortable == true) {
            webgis.require('sortable', function () {
                Sortable.create($parentList.get(0),
                    {
                        onSort: function (e) {
                            var $parent = $parentList.closest('.selector-container').parent();
                            calcContainerValues($parent, ui.item);
                            if (window.fireSelectionChanged)
                                fireSelectionChanged();
                        }
                    });
            });
            //$parentList.sortable({
            //    update: function (event, ui) {
            //        var $parent = $parentList.closest('.selector-container').parent();
            //        calcContainerValues($parent, ui.item);
            //        if (window.fireSelectionChanged)
            //            fireSelectionChanged();
            //    }
            //});
        }
    }

    var div = $div.get(0);
    div.selectOptions = selectOptions;

    if (sortable == true) {
        webgis.require('sortable', function () {
            Sortable.create($ul.get(0));
        })
        //$ul.sortable();
    }
};

function createCustomSelector(name, id) {
    var $div = $('#mapbuilder-container').find(".selector-container[data-id='" + id + "']"), $ul;

    if ($div.length == 0) {
        $div = $("<div>")
            .addClass('selector-container custom')
            .appendTo('#mapbuilder-container')
            .attr("data-id", id);

        var $title = $("<div class=webgis-presentation_toc-title>" + name + "</div>").appendTo($div).
            click(function () {
                var me = this;
                $(this).parent().parent().find('.selector-container').each(function (i, e) {
                    if (e != $(me).parent().get(0)) {
                        $(e).find('ul:first').slideUp();
                    }
                });
                $(this).parent().find('ul:first').slideToggle();
            });

        $ul = $("<ul class='webgis-toolbox-tool-item-group-details' style='display:none'>").appendTo($div);
    }

    return $div.children('.webgis-toolbox-tool-item-group-details');
};

function createCustomSelectorItem($selector, id) {
    var $item = $selector.children("[data-id='" + id + "']");

    if ($item.length == 0) {
        $item = $("<li>")
            .addClass('custom-item')
            .attr('data-id', id)
            .appendTo($selector);
    }

    return $item;
}

function calcContainerValues($parent, clickItem) {
    $parent.find('.selector-container').each(function (div_i, div_e) {
        var $div = $(div_e);
        var selOptions = $div.get(0).selectOptions;
        if (selOptions) {
            if (selOptions.singleSelect) {
                $div.find('.webgis-toolbox-tool-item-selected').each(function (i, e) {
                    if (clickItem && clickItem.parentNode == e.parentNode && e != clickItem) {
                        $(e).removeClass('webgis-toolbox-tool-item-selected');
                    }
                });
            }

            var val = "";
            if (selOptions.selectMode == 'json') {
                val = "[";
                $div.find('.webgis-toolbox-tool-item').each(function (i, e) {
                    if (checkEpsg(e) == true) {
                        if (val != "[") val += ",";
                        val += "{";
                        if (selOptions.selectModeFields && e && e._element) {
                            for (var f = 0; f < selOptions.selectModeFields.length; f++)
                                val += "\"" + selOptions.selectModeFields[f] + "\":\"" + e._element[selOptions.selectModeFields[f]] + "\",";
                        } else {
                            val += "\"id\":\"" + $(this).attr("data-id") + "\",";
                        }
                        val += "\"selected\":" + $(this).hasClass('webgis-toolbox-tool-item-selected');
                        if ($(e).find('.webgis-toolbox-tool-item-option').length > 0) {
                            val += ",\"options\":[";
                            var firstOption = true;
                            $(e).find('.webgis-toolbox-tool-item-option').each(function () {
                                var $option = $(this);
                                if (!firstOption) val + ",";
                                if ($(this).hasClass('webgis-toolbox-tool-item-selected'))
                                {
                                    val += "\"" + $(this).attr("data-id") + "\"";
                                    firstOption = false;
                                }
                                firstOption = false;
                            });
                            val += "]";
                        }
                        val += "}";
                    }
                });
                val += "]";
            } else if (selOptions.selectMode == 'json2') {
                val = "[";
                var container = 'unknown', firstElement = false;
                $div.find('.webgis-toolbox-tool-item-selected').each(function (i, e) {
                    if (checkEpsg(e) == true) {
                        if (container != $(e).attr('data-container')) {
                            container = $(e).attr('data-container');
                            if (val.length > 1) val += "]},";

                            if ($(e).hasClass('webgis-toolbox-tool-item-option')) {
                                val += "{\"" + selOptions.containerName + "\":'" + container + "',\"options\":[";
                            } else {
                                val += "{\"" + selOptions.containerName + "\":'" + container + "',\"" + selOptions.collectionName + "\":[";
                            }
                            firstElement = true;
                        }
                        if (!firstElement) val += ",";
                        if ($(e).hasClass('webgis-toolbox-tool-item-option'))
                            val += '"' + $(e).attr('data-id') + '"';
                        else
                            val += $(e).attr('data-id');
                        firstElement = false;
                    }
                });
                val += "]}]";
            } else {
                $div.find('.webgis-toolbox-tool-item-selected').each(function (i, e) {
                    if (checkEpsg(e) == true) {
                        if (val != "") val += ",";
                        val += $(e).attr('data-id');
                    }
                });
            }

            var v = $('#' + $div.attr("data-id"));
            v.val(val);

            console.log('calcContainerValues', val);
        }
    });
}

function checkAllEpsg($parent) {
    $parent.find('.webgis-toolbox-tool-item').each(function (i, e) {
        checkEpsg(e);
    });
}
function checkEpsg(item) {
    if (item._element && item._element.supportedCrs && item._element.supportedCrs.length > 0) {
        if (_selectedEpsg > 0 && $.inArray(_selectedEpsg, item._element.supportedCrs, 0) < 0) {
            $(item).css('opacity', '.2');
            return false;
        }
        $(item).css('opacity', '');
    }
    return true;
}