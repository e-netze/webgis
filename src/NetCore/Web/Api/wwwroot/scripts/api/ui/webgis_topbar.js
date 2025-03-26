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
        quick_search: true,
        quick_search_service: null,
        quick_search_categorie: '',
        detail_search: true,
        app_menu: false
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        }
    };
    var initUI = function (elem, options) {
        var $elem = $(elem);
        $elem.data('map', options.map);
        $elem.addClass('webgis-topbar-holder webgis-ui-trans');
        $elem.css({ overflow: 'visible', textAlign: 'right', position: 'absolute' });
        var right = 0;
        if (options.app_menu === true) {
            //$("<div style='position:absolute;right:" + right + "px;background-image:url(" + webgis.css.imgResource('menu-26-b.png') + ")' id='webgis-app-menu-button' class='webgis-topbar-button'></div> ").appendTo($elem);
            var $menubutton = $("<div style='position:absolute;right:" + right + "px;' id='webgis-app-menu-button' class='webgis-topbar-button webgis-app-menu-button'></div> ")
                .appendTo($elem);
            $("<div>").addClass('bar0').appendTo($menubutton);
            $("<div>").addClass('bar1').appendTo($menubutton);
            $("<div>").addClass('bar2').appendTo($menubutton);
            $("<div>").addClass('bar3').appendTo($menubutton);
            right += 35;
            var $div = $("<div class='webgis-app-menu-holder' style='position:absolute;right:0px;top:42px;display:none'>").appendTo($elem);
            $elem.find('#webgis-app-menu-button').click(function () {
                var $holder = $(this).closest('.webgis-topbar-holder');
                $holder.find('.webgis-detail-search-holder').css('display', 'none');
                var $bar = $(this).closest('.webgis-topbar-holder');
                var c = $bar.find('.webgis-app-menu-holder');
                if (c.length > 0 && c.css('display') == 'none') {
                    $holder.addClass('webgis-ui-trans-hover');
                    var m = $bar.data('map');
                    m.events.fire('onshowappmenu', c.get(0));
                    $('#webgis-app-menu-button').addClass('open');
                }
                else {
                    $holder.removeClass('webgis-ui-trans-hover');
                    $('#webgis-app-menu-button').removeClass('open');
                }
                $(c).slideToggle();
            });
            options.map.events.on('onhideappmenu', function (sender, target) {
                //$holder.find('.webgis-detail-search-holder').removeClass('webgis-ui-trans-hover');
                $(target).slideUp();
                $('#webgis-app-menu-button').removeClass('open');
            });
        }
        if (options.detail_search === true) {
            $("<div style='position:absolute;right:" + right + "px;background-image:url(" + webgis.css.imgResource('binoculars-26.png','ui') + ")' id='webgis-detail-search-button' class='webgis-topbar-button'></div> ").appendTo($elem);
            right += 35;
            $elem.find('#webgis-detail-search-button').click(function () {
                var $holder = $(this).closest('.webgis-topbar-holder');
                $holder.find('.webgis-app-menu-holder').css('display', 'none');
                $holder.find('.webgis-search-result-holder').css('display', 'none');
                var c = $(this).closest('.webgis-topbar-holder').find('.webgis-detail-search-holder');
                if (c.length > 0 && c.css('display') == 'none') {
                    $holder.addClass('webgis-ui-trans-hover');
                }
                else {
                    $holder.removeClass('webgis-ui-trans-hover');
                }
                $(c).slideToggle();
            });
            var $div = $("<div class='webgis-detail-search-holder' style='position:absolute;right:0px;top:42px;display:none'>").appendTo($elem);
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
                        buildMask(opt.value, opt.query, $(this).closest('.webgis-topbar-holder'));
                    else
                        emptyMask($(this).closest('.webgis-topbar-holder'));
                },
                valuefunction: function (service, query) {
                    return service.id + "/queries/" + query.id;
                }
            });
            //$cmb.trigger('change');
            //$elem.data('map').events.on('onaddservice', addService, elem);
            //$elem.data('map').events.on('onremoveservice', removeService, elem);
        }
        if (options.quick_search === true) {
            var $inputContainer = $("<div style='position:absolute;top:0px;bottom:0px;right:" + (right + 35) + "px;background-color:white;left:0px'></div>").appendTo($elem);
            var $input = $("<input style='width:100%' class='webgis-search-input' type='text' placeholder='"+webgis.l10n.get("quick-search")+"...' />")
                .appendTo($inputContainer)
                .data('autocomplete-onenter', function (e) {
                    webgis_search_query(this)
                })
                .click(function () {
                    $(this).closest('.webgis-topbar-holder').addClass('webgis-ui-trans-hover').addClass('webgis-topbar-large');
                    webgis._autocompleteFitMenu(this);
                })
                .blur(function () {
                    $(this).closest('.webgis-topbar-holder').removeClass('webgis-ui-trans-hover').removeClass('webgis-topbar-large');
                });

            if (webgis.useMobileCurrent()) {
                $input.data('control-parent-selector', '.webgis-topbar-holder');
            }

            var quickSearchServiceString = options.quick_search_service || options.service;
            //console.log('search-services', quickSearchServiceString);
            // distinct the services
            const quickSearchService = quickSearchServiceString.split(',').filter((value, index, array) => array.indexOf(value) === index).toString();
            //console.log('distincted search-services', quickSearchService)

            $input.attr('data-service', quickSearchService);

            if (quickSearchService !== 'geojuhu') {
                $input.data('map', options.map).webgis_control_search({
                    search_service: quickSearchService,
                    search_categories: options.quick_search_categorie,
                    on_select_get_original: true,
                    on_select: function (sender, result, isOriginal) {
                        var map = $(sender).data('map');
                        if (map) {
                            if (isOriginal) {
                                map.removeMarkerGroup('search-temp-marker');

                                map.queryResultFeatures.clear();

                                // remove Features that that do not has service or query in current map
                                if (result && result.metadata && result.metadata.service_id && result.metadata.query_id &&
                                    map.getQuery(result.metadata.service_id, result.metadata.query_id)) {
                                    //console.log('map has query...');

                                    if (!result.features || result.features.length == 0) {
                                        webgis.alert("Daten für das Original GIS Element wurden nicht gefunden. Es besteht eventuell ein Schiefstand in den Daten...", "info");
                                    } else {
                                        map.ui.showQueryResults(result, true, false);
                                    }
                                    result.suppressSendQuickSearchResult = true;
                                } else {
                                    // Features entfernen => Kein Objekt übernommen, damit auch noch das "einfachen" Schnellsuchergebnisses 
                                    // geschickt wird (siehe webgis_controls_search.js)
                                    result.features = [];
                                }
                            }
                            else {
                                var img = result.thumbnail ? "<img style='max-height:40px' src='" + result.thumbnail + "' />" : "";
                                var features = {
                                    type: 'FeatureCollection',
                                    features: [
                                        {
                                            oid: result.id,
                                            type: 'Feature',
                                            geometry: {
                                                type: 'Point',
                                                coordinates: result.coords
                                            },
                                            properties: {
                                                _fulltext: "<table><tr><td>" + img + "</td><td style='vertical-align:top'><span>" + result.label + "</span><br/><span style='color:#aaa;font-size:0.8em'>" + result.subtext + "</span></td></tr></table>",
                                                _label: result.label
                                            },
                                            bounds:
                                                result.bbox && Array.isArray(result.bbox) && result.bbox.length == 4 ?
                                                    result.bbox :
                                                    [result.coords[0], result.coords[1], result.coords[0], result.coords[1]]
                                        }
                                    ],
                                    bounds:
                                        result.bbox && Array.isArray(result.bbox) && result.bbox.length == 4 ?
                                            result.bbox :
                                            [result.coords[0], result.coords[1], result.coords[0], result.coords[1]]
                                };

                                //console.log('on_select', features, result);
                                map.ui.showQueryResults(features, true, true);
                                map.zoomToBoundsOrScale(features.bounds, webgis.featuresZoomMinScale(features))
                                //webgis._autocompleteMapItem(map, result);
                            }
                        }
                    }
                });
            }
            //$input.attr('data-service', quickSearchService);
            //if (quickSearchService != 'geojuhu') {
            //    $input.attr('data-source', webgis.baseUrl + '/rest/search/' + quickSearchService + '?c=query&');
            //    $input.addClass('webgis-autocomplete');
            //    $input.data('map') = options.map;
            //}
            $("<div id='webgis-search-button' class='webgis-topbar-button webgis-topbar-button-round' style='position:absolute;right:" + right + "px;background-image:url(" + webgis.css.imgResource('search-26.png','ui') + ")'></div> ").appendTo($elem).
                click(function () {
                    webgis_search_query(this);
                });
        }

        webgis._appendAutocomplete($elem);

        if (webgis.usability.appendContentSearchToSearchResults === true) {
            webgis.delayed(function () {
                if ($('.webgis-presentation_toc-search.webgis-content-search-holder').length > 0) {
                    $elem.find('.webgis-search-input.webgis-autocomplete').data('data-append-items', [{
                        id: '#content-search',
                        value: '{0}',
                        label: '{0}',
                        subtext: 'Nach Inhalten/Diensten suchen...',
                        thumbnail: webgis.css.imgResource('presentations~png', 'toolbar')
                    }]);
                }
            }, 1500);
        }
    };

    var emptyMask = function (parent) {
        var $mask = $(parent).find('.webgis-detail-search-mask');
        $mask.empty();
        return $mask;
    };
    
    let buildMask = function (path, query, parent) {
        let $mask = emptyMask(parent);
        if (query == null || query.items == null || query.items.length == 0)
            return;

        let formId = 'details_form';
        $("<br/>").appendTo($mask);
        let $form = $("<form id='details_form' action='javascript:$.webgis_search_fire_detail(\"" + formId + "\",\"" + webgis.baseUrl + "/rest/services/" + path + "?f=json\")'></form>").appendTo($mask);
        $form.get(0).query = query;

        for (var i = 0; i < query.items.length; i++) {
            let item = query.items[i];

            if (item.visible === false) {
                continue;
            }
            $("<div>")
                .addClass('webgis-input-label')
                .text(item.name)
                .appendTo($form);

            let $input = $("<input>")
                .attr('type', 'text')
                .addClass('webgis-input')
                .attr('name', item.id)
                .appendTo($form);

            if (item.autocomplete === true) {
                $input
                    .addClass('webgis-autocomplete')
                    .data('depends_on', item.autocomplete_depends_on)
                    .attr('data-source', webgis.baseUrl + "/rest/services/" + path + "?c=autocomplete&_item=" + item.id)
                    .attr('data-minlength', item.autocomplete_minlength)
            }

            if (item.required === true) {
                $input.attr('required', 'required');
            }

            if (item.examples) {
                $input.attr('placeholder', item.examples);
            }

            $input.change(function () {
                let $input = $(this),
                    $form = $input.closest('form'),
                    name = $input.attr('name');

                //console.log('changed', $input.val(), $form, $form.find('.webgis-autocomplete').length);
                $form.find('.webgis-autocomplete').each(function (i, e) {
                    var $e = $(e),
                        depends_on = $e.data('depends_on');

                    //console.log('depends_on', depends_on);
                    if (depends_on && $e.attr('name') !== name) {
                        if ($.inArray(name, depends_on) >= 0) {
                            if ($e.val() == '') {  // if empty, change value to force a refresh
                                $e.val('~');       // this is a dummy request => will not triggered to server (see webgis.js line ~ 2078)
                                webgis._triggerAutocomplete($e);
                            }
                            $e.val('');
                            webgis._triggerAutocomplete($e);
                        }
                    }
                });
            });
        }
        $("<br/><br/><div><div style='display:inline;margin-right:20px' class='webgis-detail-search-hourglass'></div><button type='submit' class='webgis-button webgis-detail-search-button'>" + webgis.l10n.get("search") + "</button></div>")
            .appendTo($form);

        webgis._appendAutocomplete($form);
    };

    $.webgis_search_fire_detail = function (formId, path) {
        var args = {};
        var $form = $('#' + formId);
        $form.find('.webgis-input').each(function (i, e) {
            args[e.name] = $(e).val();
        });

        var $holder = $form.closest('.webgis-topbar-holder');
        var map = $holder.data('map');
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

        if (!args.filters) {
            var pos1 = path.toLowerCase().indexOf('/services/');
            if (pos1 > 0) {
                pos1 += 10; //'/services/'.length;
                var pos2 = path.indexOf('/', pos1 + 1);
                if (pos2 > 0) {
                    var serviceId = path.substring(pos1, pos2);
                    //console.log('submit form serviceId', serviceId);
                    args = map.appendRequestParameters(args, serviceId);
                }
            }
        }
        
        webgis.ajax({
            url: path,
            type: 'post',
            data: webgis.hmac.appendHMACData(args, true),
            success: function (result) {
                //alert(JSON.stringify(result));
                if (webgis.checkResult(result)) {
                    map.ui.showQueryResults(result, true);
                    $holder.find('.webgis-detail-search-holder').slideUp();
                }
                $form.find('.webgis-detail-search-hourglass').html('');
            },
            error: function () {
                webgis.alert('unknown error', 'error');
                $form.find('.webgis-detail-search-hourglass').html('');
            }
        });
    };

    var webgis_search_query = function (sender) {
        if (sender.getAttribute('data-service') == 'geojuhu') {
            webgis_search_geojuhu_query(sender);
        }
        else {
            if (sender._focused == null) {
                webgis_search_service_query(sender);
                try {
                    $(sender).autocomplete('close');
                }
                catch (e) { }
            }
        }
    };

    var webgis_search_service_query = function (sender) {
        //console.log('webgis_search_service_query');
        var $query = $(sender).closest('.webgis-topbar-holder');
        var $input = $query.find('.webgis-search-input');
        var term = $input.val();
        
        if (!term) {
            return;
        }

        var map = $query.data('map');
        var content = map._queryResultsHolder;
        if (!content) {
            return;
        }
        $query.find('.webgis-detail-search-holder').slideUp();
        var $content = $(content);
        if (content.search_term === term) {
            //$content.slideToggle();
            return;
        }
        else {
            //$content.slideDown();
        }
        map.events.fire('showqueryresults', { map: map, buttons: { clearresults: false, showtable: false } });

        var $resultTarget, $tabControl = map.ui.getQueryResultTabControl(), tabId = 'webgis-search-tabcontrol-tab';
        if ($tabControl) {
            var $tab = $tabControl.webgis_tab_control('getTab', { id: tabId });
            if (!$tab) {
                $tabControl.webgis_tab_control('add', {
                    id: tabId,
                    title: webgis.l10n.get("quick-search") + "...",
                    select: true,
                    pinable: false,
                    counter: 5,
                    onCreated: function ($control, $tab, $content) {
                        $resultTarget = $content.addClass('webgis-search-result-holder');
                        $resultTarget.data('map', map);

                        $tab.data('suppressShowQueryResults', true);
                    },
                    onSelected: function ($control, $tab, tabOptions) {
                        if ($tab.data('suppressShowQueryResults')) {
                            $tab.data('suppressShowQueryResults', false);
                        } else {
                            var map = tabOptions.payload.map, features = tabOptions.payload.features;

                            if (map && features) {
                                map.unloadDynamicContent();
                                map.queryResultFeatures.showClustered(features, true)
                                map.ui.refreshUIElements();
                            }
                        }
                    },
                    onRemoved: function ($control, tabOptions) {
                         if (tabOptions.selected) {
                            map.queryResultFeatures.clear(false);
                        }

                        map.ui._setResultFrameSize($control);
                    }
                });
            } else {
                $resultTarget = $tabControl.webgis_tab_control('getContent', { id: tabId });
                $tab.data('suppressShowQueryResults', true);
                $tabControl
                    .webgis_tab_control('setTabPayload', { id: tabId, payload: {} })
                    .webgis_tab_control('select', {
                        id: tabId,
                        counter: 0
                    });
            }
        } else {
            $resultTarget = $content.find('.webgis-search-result-list');
        }

        if ($tabControl) {
            $resultTarget.webgis_queryResultListWithFeatureDetails('empty', { map: map });
        } else {
            $resultTarget.webgis_queryResultsList('empty', { map: map });
        }
        $resultTarget.html("<img src='" + webgis.css.imgResource('loader1.gif', 'hourglass') + "' /><span>Suche läuft...</span>");

        webgis.ajax({
            url: webgis.baseUrl + '/rest/search/' + $input.attr('data-service'),
            data: webgis.hmac.appendHMACData({ term: term, f: 'geojson', rows: 100 }),
            type: 'post',
            success: function (result) {
                //console.log('search-result', result);
                content.search_term = term;
                $resultTarget.empty();
                map.queryResultFeatures.showClustered(result, true);

                var counter = 0;
                if (result && result.features && result.features.length > 0) {
                    //$.webgis_search_result_list($list, map, result.features);
                    var options = {
                        map: map,
                        features: result.features,
                        onRemove: function (map) {
                            $tabControl.webgis_tab_control('remove', { id: tabId });
                        }
                    };
                    if ($tabControl) {
                        $resultTarget.webgis_queryResultListWithFeatureDetails(options);
                    } else {
                        $resultTarget.webgis_queryResultsList(options);
                    }
                    $content.webgis_queryResultsHolder('showbuttons', { clearresults: true, showtable: false });
                    counter = result.features.length;
                }
                else {
                    $resultTarget.html('Keine Ergebnisse gefunden...');
                    $content.webgis_queryResultsHolder('showbuttons', { clearresults: false, showtable: false });
                }

                if ($tabControl) {
                    var $tab = $tabControl.webgis_tab_control('getTab', { id: tabId });

                    $tabControl
                        .webgis_tab_control('setTabPayload', { id: tabId, payload: { features: result, map: map } })
                        .webgis_tab_control('setTabCounter', { id: tabId, counter: counter })
                        .webgis_tab_control('resize');

                    map.ui._setResultFrameSize();
                }
            },
            error: function () {
                $resultTarget.html('Es ist ein Fehler aufgetreten...');
            }
        });
    };

    var webgis_search_geojuhu_query = function (sender) {
        var $query = $(sender).closest('.webgis-topbar-holder');
        var $input = $query.find('.webgis-search-input');
        var term = $input.val();
        if (!term)
            return;
        var map = $query.data('map');
        var content = map._queryResultsHolder;
        if (!content)
            return;
        $query.find('.webgis-detail-search-holder').slideUp();
        var $content = $(content);
        if (content.search_term === term) {
            //$content.slideToggle();
            return;
        }
        else {
            //$content.slideDown();
        }
        map.events.fire('showqueryresults', { map: map, buttons: { clearresults: false, showtable: false } });
        var $list = $content.find('.webgis-search-result-list');
        $list.webgis_queryResultsList('empty', { map: map });
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

                    $list.webgis_queryResultsList({
                        map: map, features: result.features,
                        beforeAdd: function (service, query, feature) {
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
                        }
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
})(webgis.$ || jQuery);
