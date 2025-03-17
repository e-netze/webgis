// Old -> use webgis_topbar instead!!
(function ($) {
    "use strict";
    $.fn.webgis_topbar = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_search');
        }
    };
    $.fn.webgis_search = $.fn.webgis_topbar; // Compatible to old versions
    var defaults = {
        map: null,
        service: 'geojuhu',
        detail_search: true,
        menu: false
    };
    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        }
    };
    var initUI = function (elem, options) {
        var $elem = $(elem);
        elem._map = options.map;
        $elem.addClass('webgis-search-holder');
        $elem.css({ overflow: 'visible', minWidth: '320px', width: '320px', textAlign: 'right', position: 'absolute' });
        var left = 0;
        if (options.map_collection == true) {
            $("<div style='position:absolute;left:" + left + "px;background-image:url(" + webgis.css.imgResource('menu-26-w.png') + ")' id='webgis-map-collection-button' class='webgis-topbar-button'></div> ").appendTo($elem);
            left += 35;
        }
        if (options.detail_search == true) {
            $("<div style='position:absolute;left:" + left + "px;background-image:url(" + webgis.css.imgResource('binoculars-26.png','ui') + ")' id='webgis-detail-search-button' class='webgis-topbar-button'></div> ").appendTo($elem);
            left += 35;
            $elem.find('#webgis-detail-search-button').click(function () {
                $(this).closest('.webgis-search-holder').find('.webgis-search-result-holder').css('display', 'none');
                var c = $(this).closest('.webgis-search-holder').find('.webgis-detail-search-holder');
                $(c).slideToggle();
            });
            var $div = $("<div class='webgis-detail-search-holder' style='position:absolute;right:0px;top:36px;display:none'>").appendTo($elem);
            var $div2 = $("<div class='webgis-detail-search-combo-holder'></div>").appendTo($div);
            var $cmb = $("<select class='webgis-input webgis-detail-search-combo'>").appendTo($div2);
            $("<div class='webgis-detail-search-mask'></div>").appendTo($div);
            $cmb.get(0).func_buildMask = buildMask;
            $cmb.get(0).func_emtpyMask = emptyMask;
            $cmb.webgis_queryCombo({
                map: options.map, type: 'query',
                onchange: function () {
                    var opt = this.options[this.selectedIndex];
                    if (opt)
                        buildMask(opt.value, opt.query, $(this).closest('.webgis-search-holder'));
                    else
                        emptyMask($(this).closest('.webgis-search-holder'));
                },
                valuefunction: function (service, query) {
                    return service.id + "/queries/" + query.id;
                }
            });
            //$cmb.trigger('change');
            //elem._map.events.on('onaddservice', addService, elem);
            //elem._map.events.on('onremoveservice', removeService, elem);
        }
        var $input = $("<input style='position:absolute;left:" + left + "px;background-color:white;width:100%;max-width:" + (320 - (left + 35)) + "px;right:" + (options.detail_search == false ? "36px" : "71px") + "' class='webgis-search-input' type='text' placeholder='Schnellsuche...' />").appendTo($elem)
            .keyup('enterKey', function (e) {
            if (e.keyCode == 13)
                $.webgis_search_query(this);
        });
        $input.attr('data-service', options.service);
        if (options.service !== 'geojuhu') {
            $input.attr('data-source', webgis.baseUrl + '/rest/search/' + options.service + '?c=query&');
            $input.addClass('webgis-autocomplete');
            $input.get(0)._map = options.map;
        }
        $("<div id='webgis-search-button' class='webgis-topbar-button webgis-topbar-button-round' style='position:absolute;right:0px;background-image:url(" + webgis.css.imgResource('search-26.png', 'ui') + ")'></div> ").appendTo($elem).
            click(function () {
                $.webgis_search_query(this);
            });
        webgis._appendAutocomplete($elem);
    };
    
    var emptyMask = function (parent) {
        var $mask = $(parent).find('.webgis-detail-search-mask');
        $mask.empty();
        return $mask;
    };
    var buildMask = function (path, query, parent) {
        var $mask = emptyMask(parent);
        if (query == null || query.items == null || query.items.length == 0)
            return;
        var formId = 'details_form';
        var $form = $("<br/><form id='details_form' action='javascript:$.webgis_search_fire_detail(\"" + formId + "\",\"" + webgis.baseUrl + "/rest/services/" + path + "?f=json\")'></form>").appendTo($mask);
        $form.get(0).query = query;
        for (var i = 0; i < query.items.length; i++) {
            var item = query.items[i];
            if (item.visible == false)
                continue;

            $("<div class='webgis-input-label'>" + item.name + "</div>").appendTo($form);

            console.log(item);

            const $input = $("<input type='text' class='webgis-input' name='" + item.id + "' />").appendTo($form);
            if (item.autocomplete === true) {
                $input.addClass('webgis-autocomplete');
                $input.attr('data-source', webgis.baseUrl + "/rest/services/" + path + "?c=autocomplete&_item=" + item.id);
            }
            if (item.required === true) {
                $input.attr('required', 'required');
            }
        }
        $("<br/><br/><div><div style='display:inline;margin-right:20px' class='webgis-detail-search-hourglass'></div><button type='submit' class='webgis-button webgis-detail-search-button'>Suchen</button></div>").appendTo($form);
        webgis._appendAutocomplete($form);
    };
    $.webgis_search_fire_detail = function (formId, path) {
        var args = {};
        var $form = $('#' + formId);
        $form.find('.webgis-input').each(function (i, e) {
            args[e.name] = $(e).val();
        });
        var $holder = $form.closest('.webgis-search-holder');
        var map = $holder.get(0)._map;
        if (!map._queryResultsHolder)
            return;
        var queryName = '';
        var $cmb = $holder.find('.webgis-detail-search-combo');
        if ($cmb.length > 0) {
            var opt = $cmb.get(0).options[$cmb.get(0).selectedIndex];
            if (opt && opt.query)
                queryName = opt.query.name;
        }
        $form.find('.webgis-detail-search-hourglass').html("<img src='" + webgis.css.imgResource('loader1.gif', 'hourglass') + "' /><span>Suche " + queryName + "...</span>");
        webgis.ajax({
            url: path,
            type: 'post',
            data: webgis.hmac.appendHMACData(args),
            success: function (result) {
                //alert(JSON.stringify(result));
                $.webgis_search_showresults(map, result, true);
                $holder.find('.webgis-detail-search-holder').slideUp();
                $form.find('.webgis-detail-search-hourglass').html('');
            },
            error: function () {
                webgis.alert('unknown error', 'Fehler');
                $form.find('.webgis-detail-search-hourglass').html('');
            }
        });
    };
    $.webgis_search_showresults = function (map, result, zoom2results, dynamicContentQuery /* optionaler Parameter wird bei Dynamischen Inhalten mitübergeben */) {
        var $resultHolder = $(map._queryResultsHolder);
        map.queryResultFeatures.showClustered(result, zoom2results, $resultHolder.length == 0);
        var $resultHolder = $(map._queryResultsHolder);
        if ($resultHolder.length > 0)
            $resultHolder.get(0).search_term = '';
        $.webgis_search_empty_result_list($resultHolder.find('.webgis-search-result-list'), map);
        if (result && result.features && result.features.length == 0) {
            webgis.alert('Es wurden keine Ergebnisse gefunden','Hinweis');
        }
        else {
            $.webgis_search_result_list($resultHolder.find('.webgis-search-result-list'), map, result.features, false, dynamicContentQuery);
            //$resultHolder.slideDown();
            map.events.fire('showqueryresults', { map: map, buttons: { clearresults: true, showtable: true } });
        }
    };
    $.webgis_search_query = function (sender) {
        if (sender.getAttribute('data-service') == 'geojuhu') {
            $.webgis_search_geojuhu_query(sender);
        }
        else {
            if (sender._focused == null) {
                $.webgis_search_service_query(sender);
                try {
                    $(sender).autocomplete('close');
                }
                catch (e) { }
            }
        }
    };
    $.webgis_search_geojuhu_query = function (sender) {
        var $query = $(sender).closest('.webgis-search-holder');
        var $input = $query.find('.webgis-search-input');
        var term = $input.val();
        if (!term)
            return;
        var map = $query.get(0)._map;
        var content = map._queryResultsHolder;
        if (!content)
            return;
        $query.find('.webgis-detail-search-holder').slideUp();
        var $content = $(content);
        if (content.search_term == term) {
            //$content.slideToggle();
            return;
        }
        else {
            //$content.slideDown();
        }
        map.events.fire('showqueryresults', { map: map, buttons: { clearresults: false, showtable: false } });
        var $list = $content.find('.webgis-search-result-list');
        $.webgis_search_empty_result_list($list, map);
        $list.html("<img src='" + webgis.css.imgResource('loader1.gif', 'hourglass') + "' /><span>Suche läuft...</span>");
        webgis.ajax({
            url: webgis.baseUrl + '/rest/search/geojuhu',
            data: webgis.hmac.appendHMACData({ term: term, services: map.serviceIds().join(','), f: 'json' }),
            type: 'post',
            success: function (result) {
                content.search_term = term;
                $list.empty();
                var cat = [];
                map.queryResultFeatures.showClustered(result, true);
                if (result && result.features && result.features.length > 0) {
                    var $div = $("<div>").appendTo($list);
                    $.webgis_search_result_list($list, map, result.features, function (service, query, feature) {
                        if (!cat[query.id]) {
                            cat[query.id] = 0;
                            var $sdiv = $("<div class='webgis-geojuhu-switch'>").appendTo($div);
                            $("<label id='" + query.id + "'><input id='__geojuhu_switch_" + query.id + "' type='checkbox' checked='checked' />&nbsp;" + query.name + "<span class='webgis-geojuhu-switch-counter' id='counter_" + query.id + "'></span></label>").appendTo($sdiv).
                                find('#__geojuhu_switch_' + query.id).
                                click(function () {
                                $content.find('.' + this.parentNode.id).css('display', $(this).prop('checked') ? '' : 'none');
                                var result = { features: [] };
                                $content.find('.webgis-geojuhu-result').each(function (i, e) {
                                    if ($(e).css('display') !== 'none' && e.feature) {
                                        result.features.push(e.feature);
                                        if (result.bounds == null) {
                                            result.bounds = [e.feature.bounds[0], e.feature.bounds[1], e.feature.bounds[2], e.feature.bounds[3]];
                                        }
                                        else {
                                            result.bounds[0] = Math.min(result.bounds[0], e.feature.bounds[0]);
                                            result.bounds[1] = Math.min(result.bounds[1], e.feature.bounds[1]);
                                            result.bounds[2] = Math.max(result.bounds[2], e.feature.bounds[2]);
                                            result.bounds[3] = Math.max(result.bounds[3], e.feature.bounds[3]);
                                        }
                                    }
                                });
                                map.queryResultFeatures.showClustered(result, true);
                            });
                        }
                        cat[query.id]++;
                    });
                    for (var c in cat) {
                        $list.find('#counter_' + c).html(cat[c]);
                    }
                    $content.webgis_queryResultsHolder('showbuttons', { clearresults: true, showtable: false });
                }
                else {
                    $list.html('Keine Ergebnisse gefunden...');
                    $content.webgis_queryResultsHolder('showbuttons', { clearresults: false, showtable: false });
                }
            },
            error: function () {
                $list.html('Es ist ein Fehler aufgetreten...');
            }
        });
    };
    $.webgis_search_service_query = function (sender) {
        var $query = $(sender).closest('.webgis-search-holder');
        var $input = $query.find('.webgis-search-input');
        var term = $input.val();
        if (!term)
            return;
        var map = $query.get(0)._map;
        var content = map._queryResultsHolder;
        if (!content)
            return;
        $query.find('.webgis-detail-search-holder').slideUp();
        var $content = $(content);
        if (content.search_term == term) {
            //$content.slideToggle();
            return;
        }
        else {
            //$content.slideDown();
        }
        map.events.fire('showqueryresults', { map: map, buttons: { clearresults: false, showtable: false } });
        var $list = $content.find('.webgis-search-result-list');
        $.webgis_search_empty_result_list($list, map);
        $list.html("<img src='" + webgis.css.imgResource('loader1.gif', 'hourglass') + "' /><span>Suche läuft...</span>");
        webgis.ajax({
            url: webgis.baseUrl + '/rest/search/' + $input.attr('data-service'),
            data: webgis.hmac.appendHMACData({ term: term, f: 'geojson', rows: 100 }),
            type: 'post',
            success: function (result) {
                content.search_term = term;
                $list.empty();
                map.queryResultFeatures.showClustered(result, true);
                if (result && result.features && result.features.length > 0) {
                    $.webgis_search_result_list($list, map, result.features);
                    $content.webgis_queryResultsHolder('showbuttons', { clearresults: true, showtable: false });
                }
                else {
                    $list.html('Keine Ergebnisse gefunden...');
                    $content.webgis_queryResultsHolder('showbuttons', { clearresults: false, showtable: false });
                }
            },
            error: function () {
                $list.html('Es ist ein Fehler aufgetreten...');
            }
        });
    };
    $.webgis_search_empty_result_list = function ($parent, map) {
        $parent.empty();
        $('#' + $parent.closest('.webgis-search-result-holder').attr('id') + '-counter').css('display', 'none').html('');
        if (map)
            map.getSelection('query').remove();
    };
    $.webgis_search_result_list = function ($parent, map, features, beforeAdd, dynamicContentQuery) {
        if ($parent == null || $parent.length == 0)
            return;
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
            var $li = $("<li class='webgis-geojuhu-result " + (query ? query.id : "") + "'></li>").appendTo($ul);
            if (query)
                $("<div class='webgis-geojuhu-reslut-category'>Ergebnis aus " + query.name + "</div>").appendTo($li);
            var txt = '';

            var featureProperties = Array.isArray(feature.properties) ? (feature.properties.length > 0 ? feature.properties[0] : {}) : feature.properties;
            if (featureProperties._fulltext) {
                txt = featureProperties._fulltext;
            }
            else {
                for (var property in featureProperties) {
                    if (property.substring(0, 1) === "_")
                        continue;

                    if (featureProperties.hasOwnProperty(property)) {
                        txt = map.ui.featureResultTable(feature, 2);
                    }
                }
            }
            $("<div>" + txt + "</div>").appendTo($li);
            $li.click(function () {
                var feat = this.feature;
                var $list = $(this).closest('.webgis-geojuhu-results');
                var map = $(this).closest('.webgis-search-result-holder').get(0)._map;
                if (!feat || !map)
                    return;
                if ($(this).hasClass('webgis-geojuhu-result-selected')) {
                    var selection = map.getSelection('query');
                    selection.remove();
                    $list
                        .find('.webgis-geojuhu-result-selected')
                        .removeClass('webgis-geojuhu-result-selected');
                }
                else {
                    if (feat.bounds)
                        map.zoomToBoundsOrScale(feat.bounds, webgis.featureZoomMinScale(feat));
                    if (feat.oid) {
                        map.queryResultFeatures.recluster();
                        map.queryResultFeatures.uncluster(feat.oid, true);
                        var qid = feat.oid.split(':');
                        var service = map.services[qid[0]];
                        if (service != null) {
                            var query = service.getQuery(qid[1]);
                            if (query !== null) {
                                var layer = service.getLayer(query.layerid);
                                //if (layer != null && layer.selectable !== false) {
                                if (!layer || layer.selectable !== false) {   // if layer not in service (only visible on backend), hightlight anyway 
                                    var selection = map.getSelection('query');
                                    if (selection)
                                        selection.setTargetQuery(service, query.id, qid[2]);
                                }
                            }
                        }
                        $list.find('.webgis-geojuhu-result-selected').removeClass('webgis-geojuhu-result-selected');
                        $(this).addClass('webgis-geojuhu-result-selected');
                    }
                }
                if ($(window).width() < 1024) {
                    map.events.fire('hidequeryresults');
                }
            }).get(0).feature = feature;
        }
        //var count = map.queryResultFeatures.count();
        //alert(count);
        $('#' + $parent.closest('.webgis-search-result-holder').attr('id') + '-counter').css('display', '').html(features.length);
    };
})(webgis.$ || jQuery);
