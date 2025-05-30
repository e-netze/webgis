(function ($) {
    "use strict";
    $.fn.webgis_queryResultsHolder = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$webgis_queryResultsHolder');
        }
    };
    var defaults = {
        map: null,
    };
    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        showbuttons: function (options) {
            return this.each(function () {
                $(this).find('.webgis-search-result-clearresults').css('display', (options && options.clearresults) ? '' : 'none');
                $(this).find('.webgis-search-result-showtable').css('display', (options && options.showtable) ? '' : 'none');
            });
        }
    };

    var initUI = function (elem, options) {
        if (!options.map)
            return;

        var $elem = $(elem);
        $elem.data('map', options.map);

        $elem.addClass('webgis-search-result-holder');
        $("<div class='webgis-search-result-list'></div>").appendTo($elem);
        options.map._queryResultsHolder = $elem.get(0);

        options.map.events.on('feature-highlighted', function (channel, map, feature) {
            var $holder = $(this);
            $holder
                .find('.'+$.fn.webgis_queryResultsList.selectedItemClass)
                .removeClass($.fn.webgis_queryResultsList.selectedItemClass);
            $holder
                .find(".webgis-result[data-id='" + feature.oid + "']")
                .addClass($.fn.webgis_queryResultsList.selectedItemClass);
        }, elem);
        options.map.events.on('feature-highlight-removed', function (channel, map, feature) {
            var $holder = $(this);
            $holder
                .find('.' + $.fn.webgis_queryResultsList.selectedItemClass)
                .removeClass($.fn.webgis_queryResultsList.selectedItemClass);
        }, elem);
    };
})(webgis.$ || jQuery);

(function ($) {
    "use strict";
    $.fn.webgis_queryResultsList = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$webgis_queryResultsList');
        }
    };
    var defaults = {
        map: null,
        features: null,
        beforeAdd: null,
        showToolbar: true,
        dynamicContentQuery: null /* optionaler Parameter wird bei Dynamischen Inhalten mitübergeben */
    };
    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        empty: function (options) {
            webgis_search_empty_result_list($(this), options.map);
        }
    };

    $.fn.webgis_queryResultsList.selectedItemClass = 'webgis-geojuhu-result-selected';

    var initUI = function (parent, options) {
        var $parent = $(parent);

        var map = options.map;
        var features = options.features;
        var beforeAdd = options.beforeAdd;
        var dynamicContentQuery = options.dynamicContentQuery, query = null, service = null;

        var includedQueries = [];
        var selectable = map.queryResultFeatures.supportsSelectFeatures();

        for (var f in features) {
            var feature = features[f];
            query = dynamicContentQuery;
            if (!query && feature.oid.substring(0, 1) !== '#') { // Searchservice result....
                var qid = feature.oid.split(':');

                service = map.services[qid[0]];
                if (!service) {
                    continue;
                }
                query = service.getQuery(qid[1]);
                if (!query)
                    continue;
                feature.__queryId = query.id;
                feature.__serviceId = service.id;

                if (selectable === true) {
                    var layer = service.getLayer(query.layerid);
                    if (layer != null && layer.selectable === false)
                        selectable = false;
                }
            }
            if (query && $.inArray(query.name, includedQueries) < 0) {
                includedQueries.push(query.name);
            }
        }

        map.getSelection('selection').remove();
        map.getSelection('custom').remove();

        if (options.showToolbar) {
            var $toolbar = $("<ul>").addClass('webgis-queryresult-tools').css({ margin: 0, padding: 0 }).appendTo($parent);
            if (webgis.queryResultOptions.showMenu === false)
                $toolbar.css('display', 'none');

            // Result Menu
            if (selectable) {
                var $selectButton = $("<li><span><img src='" + webgis.css.imgResource('select_features.png', 'tools') + "'><div class='webgis-toolbox-tool-item-label'>"+webgis.l10n.get("button-select")+"</div></span></li>")
                    .addClass('webgis-toolbox-tool-item webgis-selection-button webgis-toggle-button')
                    .appendTo($toolbar)
                    .click(function () {
                        var map = $(this).closest('.webgis-search-result-holder').data('map');
                        if (map) {
                            map.queryResultFeatures.setSelection($(this).toggleClass('selected').hasClass('selected'), $(this));
                        }
                    });
                if (map.queryResultFeatures.supportsBufferFeatures()) {
                    $("<li><span><img src='" + webgis.css.imgResource('buffer.png', 'tools') + "'><div class='webgis-toolbox-tool-item-label'>"+webgis.l10n.get("button-buffer")+"</div></span></li>").addClass('webgis-toolbox-tool-item').appendTo($toolbar)
                        .click(function () {
                            var map = $(this).closest('.webgis-search-result-holder').data('map');
                            if (map) {
                                map.ui.showBufferDialog();
                            }
                        });
                }
            }

            $("<li><span><img src='" + webgis.css.imgResource('results.png', 'tools') + "'><div class='webgis-toolbox-tool-item-label'>"+webgis.l10n.get("button-table")+"</div></span></li>")
                .addClass('webgis-toolbox-tool-item webgis-dependencies webgis-dependency-queryfeatureshastableproperties')
                .appendTo($toolbar)
                .click(function () {
                    var map = $(this).closest('.webgis-search-result-holder').data('map');
                    if (map) {
                        map.queryResultFeatures.showTable();
                    }
                });

            var $removeButton = $("<li><span><img src='" + webgis.css.imgResource('remove.png', 'tools') + "'><div class='webgis-toolbox-tool-item-label'>"+webgis.l10n.get("button-remove-results")+"</div></span></li>")
                .addClass('webgis-toolbox-tool-item remove-queryresults')
                .appendTo($toolbar)
                .click(function () {
                    var map = $(this).closest('.webgis-search-result-holder').data('map');
                    if (map) {
                        var $holder = $(this).closest('.webgis-search-result-holder'),
                            hasHistory = $.webgis_search_result_histories[map] != null && $.webgis_search_result_histories[map].count() > 0;

                        map.queryResultFeatures.clear(hasHistory);
                        map.getSelection('selection').remove();
                        $holder.find('.webgis-search-result-list').webgis_queryResultsList('empty', { map: map });
                        $holder.get(0).search_term = '';
                        if (hasHistory) {
                            $.webgis_search_result_histories[map].render($holder.find('.webgis-search-result-list'));
                        }
                        else {
                            map.events.fire('hidequeryresults');
                        }
                        map.getSelection('query').remove();
                        map.getSelection('custom').remove();

                        var activeTool = map.getActiveTool();
                        if (activeTool && activeTool.hasui !== true) {  // eg. if tool is "add to selection" clear this tool because it is invisible after deleting all results
                            map.setDefaultTool();
                        }

                        if (options.onRemove) {
                            options.onRemove(map);
                        }
                    }
                });

            if (!map.queryResultFeatures.supportsRemoveFeatures()) {
                $removeButton.css('display', 'none'); // only hide the button. It is used in background tasks but not clickable for the user!!!
            }

            var $addButton, $removeButton;

            if (map.getTool('webgis.tools.addtoselection')) {
                $addButton = $("<li><span><img src='" + webgis.css.imgResource('cursor-plus-26-b.png', 'tools') + "'><div class='webgis-toolbox-tool-item-label'>Selektion erweitern</div></span></li>")
                    .addClass('webgis-toolbox-tool-item')
                    .attr('data-toolid', 'webgis.tools.addtoselection')
                    .appendTo($toolbar)
                    .click(function () {
                        var map = $(this).closest('.webgis-search-result-holder').data('map');
                        if ($(this).hasClass('selected')) {
                            map.setActiveTool(map.getDefaultTool());
                        } else {
                            var tool = map.getTool('webgis.tools.addtoselection');
                            if (tool)
                                map.setActiveTool(tool);
                        }
                    });
            }
            if (map.getTool('webgis.tools.removefromselection')) {
                $removeButton = $("<li><span><img src='" + webgis.css.imgResource('cursor-minus-26-b.png', 'tools') + "'><div class='webgis-toolbox-tool-item-label'>Selektion einschränken</div></span></li>")
                    .addClass('webgis-toolbox-tool-item')
                    .attr('data-toolid', 'webgis.tools.removefromselection')
                    .appendTo($toolbar)
                    .click(function () {
                        var map = $(this).closest('.webgis-search-result-holder').data('map');
                        if ($(this).hasClass('selected')) {
                            map.setActiveTool(map.getDefaultTool());
                        } else {
                            var tool = map.getTool('webgis.tools.removefromselection');
                            if (tool)
                                map.setActiveTool(tool);
                        }
                    });
            }

            switch (map.getToolId(map.getActiveTool())) {
                case 'webgis.tools.addtoselection':
                    if ($addButton)
                        $addButton.addClass('selected');
                    break;
                case 'webgis.tools.removefromselection':
                    if ($removeButton)
                        $removeButton.addClass('selected');
                    break;
            }

            if (webgis.usability.tsp && webgis.usability.tsp.allowOrderFeatures === true && !map.queryResultFeatures.isExtentDependent()) {
                var $tspButton = $("<li><span><img src='" + webgis.css.imgResource('roundtrip-26.png', 'tools') + "'><div class='webgis-toolbox-tool-item-label'>Rundreise</div></span></li>")
                    .addClass('webgis-toolbox-tool-item webgis-dependencies webgis-dependency-calc-tsp')
                    .appendTo($toolbar)
                    .click(function () {
                        var map = $(this).closest('.webgis-search-result-holder').data('map');
                        webgis.tsp.orderFeaturesWithUI(map);
                    });
            }

            if (includedQueries.length === 1) {
                //$("<h3>" + (features.length == 1 ? "Ergebnis" : "Ergebnisse") + " aus " + includedQueries[0] + "</h3>").appendTo($parent);
                $("<h3>" +
                    (webgis.queryResultOptions.showHeadingCount === false ? "" : "<span class='webgis-results-counter'>" + features.length + "</span> aus ") +
                    includedQueries[0] + ":</h3>").appendTo($parent);
            }

            $toolbar.webgis_toolbar('intialize', { map: map });

            if ($toolbar.closest('.webgis-container').find(".webgis-toolbox-tool-item[data-toolid='webgis.tools.editing.edit']").length >= 1) {
                $("<li><span><img src='" + webgis.css.imgResource(webgis.baseUrl + '/rest/toolresource/webgis-tools-editing-edit-edit', 'tools') + "'><div class='webgis-toolbox-tool-item-label'>Bearbeiten (Edit)</div></span></li>")
                    .css('display', 'none')
                    .addClass('webgis-selection-edittool-shortcut')
                    .addClass('webgis-toolbox-tool-item')
                    .appendTo($toolbar)
                    .click(function () {
                        $(this).closest('.webgis-container').find(".webgis-toolbox-tool-item[data-toolid='webgis.tools.editing.edit']").trigger('click');
                    });
            }
        }

        var reorderAble = map.queryResultFeatures.reorderAble();

        var $ul = $("<ol class='webgis-geojuhu-results'>").appendTo($parent);
        for (var f in features) {
            var feature = features[f];
            var query = dynamicContentQuery;
            if (!query && feature.oid.substring(0, 1) !== '#') { // Searchservice result....
                var qid = feature.oid.split(':');
                var service = map.services[qid[0]];
                if (!service)
                    continue;
                query = service.getQuery(qid[1]);
                if (!query)
                    continue;
                if (beforeAdd)
                    beforeAdd(service, query, feature);
            }
            var $li = $("<li data-id='" + feature.oid + "'></li>")
                .addClass('webgis-geojuhu-result webgis-result ' + (query ? query.id : ''))
                .attr('data-id', feature.oid)
                .data('feature', feature)
                .appendTo($ul);

            if (query && query.draggable === true) {
                $li.attr('draggable', true).data('feature', feature).on('dragstart', function (e) {
                    var feature = $(this).data('feature');
                    var data = {
                        type: "FeatureCollection",
                        features: [{
                            type: "Feature",
                            geometry: feature.geometry,
                            properties: feature.drag_properties || (Array.isArray(feature.properties) ? (feature.properties.length > 0 ? feature.properties[0] : {}) : feature.properties),
                            meta: {
                                query: query.id
                            }
                        }]
                    };
                    var json = JSON.stringify(data);
                    e.originalEvent.dataTransfer.setData("text", json);
                });
            }
            if (reorderAble) {
                $("<div>")
                    .addClass('webgis-reorder-handle')
                    .appendTo($li.addClass('reorderable'));
            }
            if (includedQueries.length > 1 && query) {
                $("<div class='webgis-geojuhu-reslut-category'>Ergebnis aus " + query.name + "</div>")
                    .appendTo($li);
            }
            if (map.queryResultFeatures.supportsRemoveFeature() && features.length > 1) {
                $("<div></div>")
                    .addClass('webgis-result-remove')
                    .appendTo($li)
                    .click(function (e) {
                        e.stopPropagation();
                        var map = $(this).closest('.webgis-search-result-holder').data('map');
                        map.queryResultFeatures.removeFeature($(this).closest('li').attr('data-id'));
                    });
            }
            var $featureDiv = $("<div>")
                .addClass('webgis-result-fulltext')
                .appendTo($li);

            var hookFunc = webgis.hooks["query_result_feature"]["default"] || webgis_search_result_list_renderFeature;
            if (feature.__queryId && feature.__serviceId) {
                hookFunc = webgis.hooks["query_result_feature"][feature.__queryId] || hookFunc;
                hookFunc = webgis.hooks["query_result_feature"][feature.__serviceId + ":" + feature.__queryId] || hookFunc;
            }
            hookFunc(map, $featureDiv, feature, webgis_search_result_list_renderFeature);
            var resultImg = map.queryResultFeatures.markerImg(feature._fIndex || f);
            if (resultImg && resultImg.url) {
                $li.css({
                    backgroundImage: webgis.queryResultOptions.showMarker === false ? "none" : "url(" + resultImg.url + ")",
                    backgroundPosition: 'left center',
                    minHeight: resultImg.height,
                    paddingLeft: webgis.queryResultOptions.showMarker === false ? 0 : resultImg.width + 5
                });
            }
            $li.click(function () {
                var $this = $(this);
                var $list = $this.closest('.webgis-geojuhu-results');
                var map = $this.closest('.webgis-search-result-holder').data('map');
                var feature = $this.data('feature');

                if (!feature || !map)
                    return;

                var removeSelection = $(this).hasClass($.fn.webgis_queryResultsList.selectedItemClass);
                $list
                    .find('.' + $.fn.webgis_queryResultsList.selectedItemClass)
                    .removeClass($.fn.webgis_queryResultsList.selectedItemClass);

                map.queryResultFeatures.handlers.featureResultClick(feature,
                    removeSelection,
                    $(this).hasClass('webgis-result-suppresszoom'));

                if (/*$(window).width() < 1024*/ map.ui.tabsInSidebar() === false) {
                    map.events.fire('hidequeryresults');
                }

                if (options.onItemClick) {
                    options.onItemClick(this, feature);
                }
            });
        }
        if (map.queryResultFeatures.selected()) {
            if ($selectButton) {
                $selectButton.trigger('click');
            }
        }
        if (reorderAble) {
            webgis.require('sortable', function () {
                Sortable.create($parent.children('ol.webgis-geojuhu-results').get(0), {
                    animation: 150,
                    ghostClass: 'webgis-sorting',
                    handle: '.webgis-reorder-handle',
                    onSort: function (e) {
                        var orderedIds = [];
                        $parent.children('ol.webgis-geojuhu-results').children('.webgis-geojuhu-result')
                            .each(function (i, item) {
                                orderedIds.push($(item).attr('data-id'));
                            });
                        //console.log(orderedIds);
                        map.queryResultFeatures.reorderFeatures(orderedIds);
                    }
                });
            });
        }

        $('#' + $parent.closest('.webgis-search-result-holder')
            .attr('id') + '-counter')
            .css('display', '')
            .text(features.length);
    };

    var webgis_search_empty_result_list = function ($parent, map) {

        $parent.empty();
        $('#' + $parent.closest('.webgis-search-result-holder').attr('id') + '-counter').css('display', 'none').html('');
        if (map) {
            map.getSelection('query').remove();
        }

        // Uncheck dynamic content toc items
        $(map.ui.webgisContainer())
            .find('.webgis-presentation_toc-dyncontent-item.webgis-presentation_toc-dyncontent-item-selected')
            .removeClass('webgis-presentation_toc-dyncontent-item-selected');
    };

    var webgis_search_result_list_renderFeature = function (map, $parent, feature) {
        var html = '';

        var featureProperties = Array.isArray(feature.properties) ? (feature.properties.length > 0 ? feature.properties[0] : {}) : feature.properties;

        if (featureProperties._fulltext) {
            html = featureProperties._fulltext.replaceAll('\\n', '<br/>');
        }
        else {
            for (var property in featureProperties) {
                if (property.substring(0, 1) == "_")
                    continue;
                if (featureProperties.hasOwnProperty(property)) {
                    // html = map.ui.featureResultTable(feature, 2);
                    if (featureProperties[property]) {
                        html = property + ': ' + featureProperties[property];
                        break;
                    }
                }
            }
            if (html === '' && featureProperties._title) {
                html = featureProperties._title;
            }
        }

        if (featureProperties._distanceString) {
            html += "<br/>📐" + featureProperties._distanceString;
        }

        $parent.html($parent.html() + webgis.secureHtml(html));

        $parent.find("a[target='dialog']").each(function (i, link) {
            var $link = $(link);
            var href = $link.attr('href');
            $link
                .attr('href', '')
                .attr('onclick', "webgis.iFrameDialog('" + href + "','" + $link.html() + "');return false;");
        });
    };

    $.webgis_search_result_histories = [];
})(webgis.$ || jQuery);

(function ($) {
    "use strict";
    $.fn.webgis_queryResultListWithFeatureDetails = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$webgis_queryResultListWithFeatureDetails');
        }
    };

    var defaults = {
        map: null,
        features: null
    };

    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        empty: function (options) {
            $(this).find('td.details').empty();
            $(this).find('td.list').webgis_queryResultsList('empty', options);
        }
    };

    var initUI = function (parent, options) {
        var $parent = $(parent).addClass('webgis-result-');

        var $table = $("<div>")
            .css({ position: 'absolute', inset: '0px 0px 0px 0px', overflow: 'hidden' })
            .appendTo($parent);

        var $listColumn = $("<div>")
            .css({ position: 'absolute', left: 0, top: 0, bottom: 0, width: 320, overflow: 'auto', paddingLeft: 8 })
            .addClass('list webgis-scroll-offset-element')
            .appendTo($table);

        var $detailsColumn = $("<div>")
            .css({ position: 'absolute', inset: '0px auto 0px 320px', paddingLeft: 8, overflow: 'auto' })
            .addClass('details')
            .appendTo($table);

        options.onItemClick = function (sender, feature) {
            onItemClick($parent, options.map, feature);
        };

        $("<div>")
            .appendTo($listColumn)
            .webgis_queryResultsList(options);

        if (options.map) {
            options.map.events.off('onmarkerclick', $.fn.webgis_queryResultListWithFeatureDetails.onMarkerClick);
            options.map.events.on('onmarkerclick', $.fn.webgis_queryResultListWithFeatureDetails.onMarkerClick, $parent);
        }
    }

    $.fn.webgis_queryResultListWithFeatureDetails.onMarkerClick = function (channel, sender, feature) {
        onItemClick($(this), sender, feature);
    };

    var onItemClick = function ($parent, map, feature) {
        var $detailsColumn = $parent.find('div.details');

        if (feature.oid.indexOf('#service:#default:') == 0 && feature.properties && feature.properties._details) {
            // Quicksearch result with details
            webgis.showFeatureResultDetails(map.guid, feature.properties._details, function (success, detailedFeature, title, metadata) {
                if (success == false) {
                    delete feature.properties._details;
                }

                showFeatureTable(map, $detailsColumn, detailedFeature || feature, title, metadata);
            });
        } else {
            showFeatureTable(map, $detailsColumn, feature);
        }
    };

    var showFeatureTable = function (map, $target, feature, title, featureMetadata) {
        $target.empty();

        if (title) {
            $("<h2>").text(title).appendTo($target);
        }

        map.queryResultFeatures.showFeatureTable(feature, '', $target, featureMetadata);
    };
})(webgis.$ || jQuery);

(function ($) {
    "use strict";
    $.fn.webgis_queryResultsTable = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$webgis_queryResultsList');
        }
    };

    $.fn.webgis_queryResultsTable.selectedRowClass = 'webgis-table-result-selected';

    var defaults = {
        map: null,
        features: null,
        reorderable: false,
        addCheckboxes: false,
        addZoomTo: false,
        appendAdvancedFeatureButtons: false
    };
    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        selectRow: function (options) {
            var $this = $(this);

            $this.find('.' + $.fn.webgis_queryResultsTable.selectedRowClass).removeClass($.fn.webgis_queryResultsTable.selectedRowClass);
            var $row = $this.find(".webgis-result[data-id='" + options.dataId + "']")
                .addClass($.fn.webgis_queryResultsTable.selectedRowClass);

            if (options.scrollTo === true) {
                var $scrollElement = $row.closest('.webgis-scroll-offset-element');
                webgis.scrollTo($scrollElement.length === 1 ? $scrollElement : $this, $row);
            }
        },
        unselectRows: function (options) {
            //console.log('unselect', options);
            if (options.map) {
                $(options.map._webgisContainer)
                    .find('.webgis-result[data-id].' + $.fn.webgis_queryResultsTable.selectedRowClass)
                    .removeClass($.fn.webgis_queryResultsTable.selectedRowClass);            
            }
        }
    };

    var initUI = function (target, options) {
        var $target = $(target);
        var map = options.map;
        var features = options.features;

        if (!features) {
            return "Keine Abfrageergebnisse vorhanden.";
        }

        var tableFields = features.metadata && features.metadata.table_fields ? features.metadata.table_fields : null;
        var hiddenProperties = [];

        var hasUnionFeatures = false;
        for (var i = 0, to = features.features.length; i < to; i++) {
            var feature = features.features[i];
            if (Array.isArray(feature.properties) && feature.properties.length > 1) {
                hasUnionFeatures = true;
                break;
            }
        }

        var filterToolId = 'webgis.tools.presentation.visfilter';
        var editAttributesToolId = 'webgis.tools.editing.updatefeature';
        var editToolId = 'webgis.tools.editing.edit'
        var editToolIdClass = 'webgis-tools-editing-edit';
        var identityToolId = 'webgis.tools.identify';

        var reorderable = options.reorderable === true && hasUnionFeatures === false;

        var $tab = $("<table>")
            .addClass('webgis-result-table features')
            .appendTo($target);

        if (features.features.length > 0) {
            var property;
            // Header Row
            var headerProperties = Array.isArray(features.features[0].properties) ? features.features[0].properties[0] : features.features[0].properties;
            var $tr = $("<tr>").appendTo($tab);

                var $td = $("<td>")
                    .addClass('webgis-result-table-header webgis-result-table-menucell')
                    .attr('colspan', 3)
                    .appendTo($tr);

            if (options.addCheckboxes) {
                $("<div class='menubutton inline checkbox'></div>").appendTo($td);
            }
            if (options.addZoomTo) {
                $("<div class='menubutton inline zoom'></div>").appendTo($td);
            }
            if (features.metadata && features.metadata.has_attachments) {
                $("<div class='menubutton inline attachments has-attachments trigger-click'></div>")
                    .data('map', map)
                    .data('oids', features.features.map(function (f) { return f.oid; }))
                    //.css('background-image', 'url(' + webgis.css.imgResource('attachments-26-b.png', 'tools') + ')')
                    .appendTo($td)
                    .click(function () {
                        var args = [], map = $(this).data('map');
                        args["feature-oids"] = $(this).data('oids');

                        $(this)
                            .closest('table')
                            .find('.menubutton.attachments')
                            .removeClass('has-attachments')
                            .addClass('query-has-attachments'); 

                        webgis.tools.onButtonClick(map, { command: 'query_has_attachments', type: 'servertoolcommand_ext', id: identityToolId, map: map }, this, null, args);

                        $(this).remove();
                    });
            }

            if (options.appendAdvancedFeatureButtons) {
                // vis-filters buttons
                if (features.metadata && features.metadata.applicable_filters && map.getTool(filterToolId)) {
                    //$("<div>")
                    //    .addClass('menubutton')
                    //    .addClass('webgis-dependencies webgis-dependency-hasfilters')
                    //    .attr('title', 'Alle entfernen')
                    //    .attr('data-applicable-filters', features.metadata.applicable_filters)
                    //    .data('map', map)
                    //    .data('oid', feature.oid)
                    //    .css('background-image', 'url(' + webgis.css.imgResource('filter-remove-26.png', 'tools') + ')')
                    //    .appendTo($td)
                    //    .click(function () {
                    //        var args = [];
                    //        //args["feature_oid"] = $(this).data('oid');
                    //        webgis.tools.onButtonClick(map, { command: 'unsetfeaturefilter', type: 'servertoolcommand_ext', id: filterToolId, map: $(this).data('map') }, this, null, args);
                    //    });
                }
            }

            if (hasUnionFeatures) {
                $("<td>")
                    .addClass('webgis-result-table-header')
                    .appendTo($tr);
            }

            for (property in headerProperties) {
                if (property.substring(0, 1) === "_" && property !== '_distanceString')
                    continue;

                var tableField = tableFields ? $.grep(tableFields, function (f) { return f.name === property }) : null;
                if (tableField && tableField.length === 1) {
                    if (tableField[0].visible === false) {
                        hiddenProperties.push(property);
                        continue;
                    }
                }

                if (headerProperties.hasOwnProperty(property)) {
                    $("<td>")
                        .addClass('webgis-result-table-header')
                        .text(property === '_distanceString' ? '📐' : property)
                        .appendTo($tr)
                }
            }

            var hasHiddenProperties = hiddenProperties.length > 0;

            // Rows
            for (var i = 0, to = features.features.length; i < to; i++) {
                var feature = features.features[i];

                if (!feature.__queryId && feature.oid && feature.oid.indexOf(':') > 0) {
                    var qid = feature.oid.split(':');
                    var service = map.services[qid[0]];
                    if (service) {
                        var query = service.getQuery(qid[1]);
                        if (query) {
                            feature.__queryId = query.id;
                            feature.__serviceId = service.id;
                        }
                    }
                }

                var propertiesArray = Array.isArray(feature.properties) ? feature.properties : [feature.properties];
                if (propertiesArray.length == 0) propertiesArray = [{}];
                var isUnion = propertiesArray.length > 1, unionIndex = 0;

                for (var p in propertiesArray) {
                    var featureProperties = propertiesArray[p];

                    var $tr = $('<tr>')
                        .addClass('webgis-result')
                        .attr('data-id', feature.oid)
                        .data('feature-bbox', feature.bounds)
                        .appendTo($tab)
                        .contextmenu(function (e) {
                            e.stopPropagation();
                            _showRowContextMenu($(this), e);
                            return false;
                        });

                    if (unionIndex === 0) {
                        var img = map.queryResultFeatures.markerImg(feature._fIndex);

                        // Checkbox...
                        var $td = $("<td>")
                            .addClass('webgis-result-table-menucell checkbox')
                            .attr('colspan', 3)
                            .appendTo($tr);

                        if (options.addCheckboxes) {
                            $("<div>")
                                .addClass('menubutton inline checkbox' + (feature._tableChecked === true ? ' checked' : ''))
                                .appendTo($td);
                        }
                        if (options.addZoomTo && feature.bounds) {
                            $("<div>")
                                .addClass('menubutton inline zoom')
                                .attr('shortcut', 'z')
                                .attr('title', webgis.l10n.get("zoom-to") + "...")
                                .appendTo($td);

                            $("<div>")
                                .addClass('menubutton pan')
                                .attr('shortcut', 'c')
                                .attr('title', webgis.l10n.get("center") + "...")
                                .appendTo($td);
                        }

                        if (options.appendAdvancedFeatureButtons) {
                            // edit attributes, edit update, edit delete
                            if (feature.__queryId && feature.__serviceId) {
                                var editthemes = map.getEditThemes(feature.__serviceId, feature.__queryId);

                                if (editthemes) {
                                    for (var e in editthemes) {
                                        var edittheme = editthemes[e];

                                        // edit attributes
                                        $("<div>")
                                            .addClass('menubutton inline webgis-dependencies webgis-dependency-not-activetool ' + editToolIdClass)
                                            .attr('title', edittheme.name + ': ' + webgis.l10n.get("edit-attributes"))
                                            .attr('shortcut', 'e')
                                            .data('edittheme', edittheme)
                                            .data('map', map)
                                            .data('oid', feature.oid)
                                            .css('background-image', 'url(' + webgis.css.imgResource('edit-attributes-26.png', 'tools') + ')')
                                            .appendTo($td)
                                            .click(function (e) {
                                                e.stopPropagation();
                                                var args = [], map = $(this).data('map');
                                                args["feature-oid"] = $(this).data('oid')
                                                args["edittheme"] = $(this).data('edittheme').themeid;
                                                args["edit-map-scale"] = map.scale();
                                                args["edit-map-crsid"] = map.calcCrs().id;

                                                map.setViewFocus($(this).closest('tr').data('feature-bbox'), 0.3);

                                                webgis.tools.onButtonClick(map, { command: 'editattributes', type: 'servertoolcommand_ext', id: editAttributesToolId, map: map }, this, null, args);
                                            });
                                        //edit update
                                        $("<div>")
                                            .addClass('menubutton inline webgis-dependencies webgis-dependency-activetool ' + editToolIdClass)
                                            .attr('title', edittheme.name + ": " + webgis.l10n.get("edit-geometry-and-attributes"))
                                            .attr('shortcut', 'e')
                                            .data('edittheme', edittheme)
                                            .data('map', map)
                                            .data('oid', feature.oid)
                                            .css('background-image', 'url(' + webgis.css.imgResource('webgis-tools-editing-edit-update.png', 'tools') + ')')
                                            .appendTo($td)
                                            .click(function (e) {
                                                e.stopPropagation();
                                                var args = [], map = $(this).data('map');
                                                args["feature-oid"] = $(this).data('oid')
                                                args["edittheme"] = $(this).data('edittheme').themeid;
                                                args["edit-map-scale"] = map.scale();
                                                args["edit-map-crsid"] = map.calcCrs().id;

                                                // there should view focus_sketch form server command reponse
                                                //map.setViewFocus($(this).closest('tr').data('feature-bbox'), 0.3);
                                                webgis.tools.onButtonClick(map, { command: 'updatefeature', type: 'servertoolcommand', id: editToolId, map: map }, this, null, args);
                                            });
                                        //edit delete
                                        $("<div>")
                                            .addClass('menubutton webgis-dependencies webgis-dependency-activetool ' + editToolIdClass)
                                            .attr('title', edittheme.name + ": " + webgis.l10n.get("edit-delete"))
                                            .attr('shortcut', 'd')
                                            .data('edittheme', edittheme)
                                            .data('map', map)
                                            .data('oid', feature.oid)
                                            .css('background-image', 'url(' + webgis.css.imgResource('webgis-tools-editing-edit-delete.png', 'tools') + ')')
                                            .appendTo($td)
                                            .click(function (e) {
                                                e.stopPropagation();
                                                var args = [], map = $(this).data('map');
                                                args["feature-oid"] = $(this).data('oid')
                                                args["edittheme"] = $(this).data('edittheme').themeid;
                                                args["edit-map-scale"] = map.scale();
                                                args["edit-map-crsid"] = map.calcCrs().id;

                                                map.setViewFocus($(this).closest('tr').data('feature-bbox'), 0.3);
                                                webgis.tools.onButtonClick(map, { command: 'deletefeature', type: 'servertoolcommand', id: editToolId, map: map }, this, null, args);
                                            });
                                    }
                                }
                            }

                            // vis-filters buttons
                            if (features.metadata && features.metadata.applicable_filters && map.getTool(filterToolId)) {
                                $.each(features.metadata.applicable_filters.split(','),
                                    function (i, filterId) {
                                        if (filterId.indexOf('~') > 0) {
                                            var service = map.getService(filterId.split('~')[0]);
                                            if (service) {
                                                var filter = service.getFilter(filterId.split('~')[1]);
                                                if (filter) {
                                                    $("<div>")
                                                        .addClass('menubutton')
                                                        .attr('title', filter.name)
                                                        .data('map', map)
                                                        .data('oid', feature.oid)
                                                        .data('filterid', filterId)
                                                        .css('background-image', 'url(' + webgis.css.imgResource('filter-26.png', 'tools') + ')')
                                                        .appendTo($td)
                                                        .click(function (e) {
                                                            e.stopPropagation();

                                                            var args = [];
                                                            args["feature_oid"] = $(this).data('oid');
                                                            args["filter_id"] = $(this).data('filterid');
                                                            webgis.tools.onButtonClick(map, { command: 'setfeaturefilter', type: 'servertoolcommand_ext', id: filterToolId, map: map }, this, null, args);
                                                        });
                                                }
                                            }
                                        }
                                    });

                                $("<div>")
                                    .addClass('menubutton')
                                    .addClass('webgis-dependencies webgis-dependency-hasfilters')
                                    .attr('title', 'Alle entfernen')
                                    .attr('data-applicable-filters', features.metadata.applicable_filters)
                                    .data('map', map)
                                    .data('oid', feature.oid)
                                    .css('background-image', 'url(' + webgis.css.imgResource('filter-remove-26.png', 'tools') + ')')
                                    .appendTo($td)
                                    .click(function () {
                                        var args = [];
                                        args["feature_oid"] = $(this).data('oid');
                                        webgis.tools.onButtonClick(map, { command: 'unsetfeaturefilter', type: 'servertoolcommand_ext', id: filterToolId, map: $(this).data('map') }, this, null, args);
                                    });
                            }

                            if (features.metadata && features.metadata.has_attachments) {
                                $("<div>")
                                    .addClass('menubutton inline attachments')
                                    .attr("id", 'attachments-' + feature.oid.replaceAll('@','-').replaceAll(':', '-'))
                                    .attr('title', 'Attachements')
                                    .data('map', map)
                                    .data('oid', feature.oid)
                                    //.css('background-image', 'url(' + webgis.css.imgResource('attachments-26-b.png', 'tools') + ')')
                                    .appendTo($td)
                                    .click(function () {
                                        var args = [], map = $(this).data('map');
                                        args["feature-oid"] = $(this).data('oid');

                                        webgis.tools.onButtonClick(map, { command: 'show_attachments', type: 'servertoolcommand_ext', id: identityToolId, map: map }, this, null, args);
                                    });
                            }

                            // custom buttons, eg. google navigation
                            if (webgis.usability.singleResultButtons) {
                                for (var b in webgis.usability.singleResultButtons) {
                                    var singleResultButton = webgis.usability.singleResultButtons[b];

                                    if (singleResultButton.url) {
                                        var url = singleResultButton.url;
                                        if (feature.geometry && feature.geometry.coordinates && feature.geometry.coordinates.length >= 2) {
                                            url = url.replaceAll('{lon}', feature.geometry.coordinates[0]);
                                            url = url.replaceAll('{lat}', feature.geometry.coordinates[1]);
                                        }

                                        $("<a>")
                                            .addClass('menubutton')
                                            .css('background-image', 'url(' + singleResultButton.img + ')')
                                            .attr('href', url)
                                            .attr('target', '_blank')
                                            .attr('title', singleResultButton.name)
                                            .appendTo($td);
                                    }
                                }
                            }
                        }

                        $("<div><img src='" + img.url + "' style='max-height:32px'/></div>")
                            .addClass('menubutton inline marker')
                            .appendTo($td);

                        if (reorderable) {
                            $("<div>")
                                .addClass('webgis-reorder-handle')
                                .appendTo($td);
                            $tr.addClass('reorderable');
                        }

                        if (hasUnionFeatures) {
                            $("<td class='webgis-result-table-menucell index'>" + (unionIndex + 1) + "</td>").appendTo($tr);
                        }
                    } else {
                        $("<td class='webgis-result-table-menucell sub' colspan='3'></td>").appendTo($tr);
                        $("<td class='webgis-result-table-menucell sub index'>" + (unionIndex + 1) + "</td>").appendTo($tr);
                    }

                    for (property in headerProperties) {
                        if (property.substring(0, 1) === "_" && property !== '_distanceString')
                            continue;

                        if (hasHiddenProperties && $.inArray(property, hiddenProperties) >= 0)
                            continue;

                        var $td = $("<td class='webgis-result-table-cell'>")
                            .appendTo($tr)
                            .click(_event_cellClick);

                        if (featureProperties.hasOwnProperty(property)) {
                            var text = featureProperties[property];

                            
                            if (text.indexOf("<a ") >= 0) {  // ToDo: kann man das besser machen?
                                const $link = $(text);
                                if ($link.attr('target') === 'dialog') {
                                    const href = $link.attr('href');
                                    $link
                                        .attr('href', '')
                                        .attr('onclick', "webgis.iFrameDialog('" + href + "','" + $link.text() + "');return false;");
                                    text = $link.prop('outerHTML');
                                }
                                $td.html(text);
                            } else if (text.indexOf("<img ") === 0) {
                                const $image = $(text);
                                const src = $image.attr('src');
                                if (src) {
                                    const $img = $("<img>").attr('src', src).appendTo($td);

                                    var tableField = tableFields ? $.grep(tableFields, function (f) { return f.name === property }) : null;
                                    
                                    if (tableField && tableField.length === 1) {
                                        if (tableField[0].image_width) { $img.css('width', tableField[0].image_width + 'px'); }
                                        if (tableField[0].image_height) { $img.css('height', tableField[0].image_height + 'px'); }
                                    }
                                }
                            } else {
                                $td.text(featureProperties[property]);
                            }
                        }
                    }

                    unionIndex++;e
                }
            }

            if (reorderable) {
                webgis.require('sortable', function () {
                    Sortable.create($tab.get(0), {
                        handle: '.webgis-reorder-handle',
                        onSort: function (e) {
                            var orderedIds = [];
                            $tab.children('tr.reorderable')
                                .each(function (i, item) {
                                    orderedIds.push($(item).attr('data-id'));
                                });
                            //console.log(orderedIds);
                            map.queryResultFeatures.reorderFeatures(orderedIds);
                        }
                    });
                });
            }
        }

        var $triggerClickItems = $tab.find('.trigger-click');
        if ($triggerClickItems.length > 0) {
            webgis.delayed(function ($triggerClickItems) {
                $triggerClickItems.trigger('click');
            }, 500, $triggerClickItems)
        }
    };

    var _event_cellClick = function (e) {
        if (!e || !e.originalEvent)
            return;

        var $this = $(this), offset = $this.offset();

        var pos = {
            right: $this.outerWidth() - (e.originalEvent.pageX - offset.left),
            top: e.originalEvent.pageY - offset.top
        }
        //console.log(pos);
        if (pos.right >= 4 && pos.right <= 28) {
            e.stopPropagation();
            webgis.copy($this);
        }
    };

    var _showRowContextMenu = function ($tr, e) {
        if (!$.fn.webgis_contextMenu)
            return false;

        var items = [];
        $tr.find('.menubutton')
            .each(function (i, menubutton) {
                var $menubutton = $(menubutton);
                var cssInlineStyle = $menubutton.attr('style');
                //console.log(cssInlineStyle);

                if (!cssInlineStyle || cssInlineStyle.replaceAll(' ', '').indexOf('display:none;') < 0) {
                    if ($menubutton.attr('title')) {
                        items.push({
                            icon: $menubutton.css('backgroundImage'),
                            text: $menubutton.attr('title'),
                            data: $menubutton,
                            shortcut: $menubutton.attr('shortcut')
                        });
                    }
                }
            });

        $tr.find('td > a')
            .each(function (i, a) {
                var $a = $(a);

                if (!$a.hasClass('menubutton')) {
                    items.push({
                        text: $a.text(),
                        data: $a
                    });
                }
            });

        $("body").webgis_contextMenu({
            sender: $tr,
            clickEvent: e,
            items: items,
            callback: function ($menubutton) {
                if ($menubutton.attr('href')) {  // works for links
                    $menubutton[0].click();
                } else {
                   $menubutton.trigger('click');
                }
            }
        });
    };
})(webgis.$ || jQuery);