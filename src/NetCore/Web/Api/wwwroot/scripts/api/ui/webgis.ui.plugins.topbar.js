// WebGIS UI Plugins (topbar)
// webgis_topbar (alias: webgis_search), global $.webgis_search_fire_detail
// Topbar of the viewer: app menu button, detail search (query combo + search mask) and quick search input

webgis.ui.definePlugin('webgis_topbar', {
    defaults: {
        map: null,
        service: 'geojuhu',
        quick_search: true,
        quick_search_service: null,
        quick_search_categorie: '',
        detail_search: true,
        app_menu: false
    },
    init: function () {
        const $ = this.$, me = this, Ctor = this.constructor, options = this.options;
        const $elem = this.$el;

        $elem.data('map', options.map);
        $elem.addClass('webgis-topbar-holder webgis-ui-trans');
        $elem.css({ overflow: 'visible', textAlign: 'right', position: 'absolute' });
        var right = 0;
        if (options.app_menu === true) {
            var $menubutton = $("<div style='position:absolute;right:" + right + "px;' id='webgis-app-menu-button' class='webgis-topbar-button webgis-app-menu-button'></div> ")
                .appendTo($elem);
            $("<div>").addClass('bar0').appendTo($menubutton);
            $("<div>").addClass('bar1').appendTo($menubutton);
            $("<div>").addClass('bar2').appendTo($menubutton);
            $("<div>").addClass('bar3').appendTo($menubutton);
            right += 35;
            $("<div class='webgis-app-menu-holder' style='position:absolute;right:0px;top:42px;display:none'>").appendTo($elem);
            $elem.find('#webgis-app-menu-button').on('click.webgis_topbar', function () {
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

            this._onHideAppMenu = function (sender, target) {
                $(target).slideUp();
                $('#webgis-app-menu-button').removeClass('open');
            };
            options.map.events.on('onhideappmenu', this._onHideAppMenu);
        }
        if (options.detail_search === true) {
            $("<div style='position:absolute;right:" + right + "px;background-image:url(" + webgis.css.imgResource('binoculars-26.png', 'ui') + ")' id='webgis-detail-search-button' class='webgis-topbar-button'></div> ").appendTo($elem);
            right += 35;
            $elem.find('#webgis-detail-search-button').on('click.webgis_topbar', function () {
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
            $cmb.get(0).func_buildMask = function (path, query, parent) { return Ctor.buildMask(path, query, parent); };
            $cmb.get(0).func_emtpyMask = function (parent) { return Ctor.emptyMask(parent); };
            $cmb.webgis_queryCombo({
                map: options.map, type: 'query',
                onchange: function () {
                    var opt = this.options[this.selectedIndex];
                    if (opt)
                        Ctor.buildMask(opt.value, opt.query, $(this).closest('.webgis-topbar-holder'));
                    else
                        Ctor.emptyMask($(this).closest('.webgis-topbar-holder'));
                },
                valuefunction: function (service, query) {
                    return service.id + "/queries/" + query.id;
                }
            });
        }
        if (options.quick_search === true) {
            var $inputContainer = $("<div style='position:absolute;top:0px;bottom:0px;right:" + (right + 35) + "px;background-color:var(--webgis-ui-surface);left:0px'></div>").appendTo($elem);
            var $input = $("<input style='width:100%' class='webgis-search-input' type='text' placeholder='" + webgis.l10n.get("quick-search") + "...' />")
                .appendTo($inputContainer)
                .on('click.webgis_topbar', function () {
                    $(this).closest('.webgis-topbar-holder').addClass('webgis-ui-trans-hover').addClass('webgis-topbar-large');
                    if ($(this).data('webgis_autocomplete')) {
                        $(this).webgis_autocomplete('fitMenu');
                    }
                })
                .on('blur.webgis_topbar', function () {
                    $(this).closest('.webgis-topbar-holder').removeClass('webgis-ui-trans-hover').removeClass('webgis-topbar-large');
                });

            var quickSearchServiceString = options.quick_search_service || options.service;
            // distinct the services
            const quickSearchService = quickSearchServiceString.split(',').filter((value, index, array) => array.indexOf(value) === index).toString();

            $input.attr('data-service', quickSearchService);

            if (quickSearchService !== 'geojuhu') {
                $input.data('map', options.map).webgis_control_search({
                    search_service: quickSearchService,
                    search_categories: options.quick_search_categorie,
                    on_select_get_original: true,
                    position_target: webgis.useMobileCurrent() ? '.webgis-topbar-holder' : null,
                    on_enter: function (sender) {
                        Ctor.searchQuery(sender);
                    },
                    on_select: function (sender, result, isOriginal) {
                        var map = $(sender).data('map');
                        if (map) {
                            if (isOriginal) {
                                map.removeMarkerGroup('search-temp-marker');

                                map.queryResultFeatures.clear();

                                // remove Features that that do not has service or query in current map
                                if (result && result.metadata && result.metadata.service_id && result.metadata.query_id &&
                                    map.getQuery(result.metadata.service_id, result.metadata.query_id)) {

                                    if (!result.features || result.features.length == 0) {
                                        webgis.alert(webgis.l10n.get('original-feature-not-found'), "info");
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

                                map.ui.showQueryResults(features, true, true);
                                map.zoomToBoundsOrScale(features.bounds, webgis.featuresZoomMinScale(features))
                            }
                        }
                    }
                });
            }
            $("<div id='webgis-search-button' class='webgis-topbar-button webgis-topbar-button-round' style='position:absolute;right:" + right + "px;background-image:url(" + webgis.css.imgResource('search-26.png', 'ui') + ")'></div> ").appendTo($elem)
                .on('click.webgis_topbar', function () {
                    Ctor.searchQuery(this);
                });
        }

        if (webgis.usability.appendContentSearchToSearchResults === true) {
            webgis.delayed(function () {
                if (me._destroyed) return;
                if ($('.webgis-presentation_toc-search.webgis-content-search-holder').length > 0) {
                    $elem.find('.webgis-search-input.webgis-autocomplete').each(function () {
                        if (!$(this).data('webgis_autocomplete')) return;
                        $(this).webgis_autocomplete('option', 'appendItems', [{
                            id: '#content-search',
                            value: '{0}',
                            label: '{0}',
                            subtext: webgis.l10n.get('search-contents-services'),
                            thumbnail: webgis.css.imgResource('presentations~png', 'toolbar')
                        }]);
                    });
                }
            }, 1500);
        }
    },
    destroy: function () {
        if (this._onHideAppMenu && this.options.map && this.options.map.events) {
            this.options.map.events.off('onhideappmenu', this._onHideAppMenu);
        }
        this._onHideAppMenu = null;
        this.$el.find('*').addBack().off('.webgis_topbar');
    },
    staticMethods: {
        emptyMask: function (parent) {
            const $ = webgis.$ || jQuery;
            var $mask = $(parent).find('.webgis-detail-search-mask');
            $mask.empty();
            return $mask;
        },
        getDependentAutocompletes: function ($input) {
            const $ = webgis.$ || jQuery;
            const $form = $input.closest('form'),
                name = $input.attr('name');

            return $form.find('.webgis-autocomplete').filter(function () {
                const $e = $(this),
                    depends_on = $e.data('depends_on');

                return depends_on &&
                    $e.attr('name') !== name &&
                    $.inArray(name, depends_on) >= 0;
            });
        },
        buildMask: function (path, query, parent) {
            const $ = webgis.$ || jQuery, Ctor = this;
            let $mask = Ctor.emptyMask(parent);
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
                        .webgis_autocomplete({
                            source: webgis.baseUrl + "/rest/services/" + path + "?c=autocomplete&_item=" + item.id,
                            minLength: parseInt(item.autocomplete_minlength) || 0,
                            debounceDelay: webgis.usability.quickSearch.debounceDelay || 0
                        });
                }

                if (item.required === true) {
                    $input.attr('required', 'required');
                }

                if (item.examples) {
                    $input.attr('placeholder', item.examples);
                }

                $input.on("input.webgis_topbar", function () {
                    // clear dependent field on input
                    Ctor.getDependentAutocompletes($(this)).each(function () {
                        const $e = $(this);
                        $e.val("");
                    });
                });
                $input
                    .on("change.webgis_topbar autocomplete-selected.webgis_topbar", function () {
                        // beim Verlassen/Bestätigen: Autocomplete der abhängigen Felder neu anstoßen
                        Ctor.getDependentAutocompletes($(this)).each(function () {
                            const $e = $(this);
                            if ($e.data('webgis_autocomplete')) {
                                $e.webgis_autocomplete('search', '');
                            } else {
                                $e.val('');
                            }
                        });
                    });
            }
            $("<br/><br/><div><div style='display:inline;margin-right:20px' class='webgis-detail-search-hourglass'></div><button type='submit' class='webgis-button webgis-detail-search-button'>" + webgis.l10n.get("search") + "</button></div>")
                .appendTo($form);
        },
        // used by the detail search form action: $.webgis_search_fire_detail(formId, path)
        fireDetail: function (formId, path) {
            const $ = webgis.$ || jQuery;
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
                        args = map.appendRequestParameters(args, serviceId);
                    }
                }
            }

            webgis.ajax({
                url: path,
                type: 'post',
                data: webgis.hmac.appendHMACData(args, true),
                success: function (result) {
                    if (webgis.checkResult(result)) {
                        map.events.fire('onnewfeatureresponse', result);
                        map.ui.showQueryResults(result, true);
                        $holder.find('.webgis-detail-search-holder').slideUp();
                    }
                    $form.find('.webgis-detail-search-hourglass').html('');
                },
                error: function () {
                    webgis.alert(webgis.l10n.get('unknown-error'), 'error');
                    $form.find('.webgis-detail-search-hourglass').html('');
                }
            });
        },
        searchQuery: function (sender) {
            const $ = webgis.$ || jQuery;
            if (sender.getAttribute('data-service') == 'geojuhu') {
                this.geojuhuQuery(sender);
            }
            else {
                const hasAutocomplete = !!$(sender).data('webgis_autocomplete');
                if (!hasAutocomplete || !$(sender).webgis_autocomplete('focusedItem')) {
                    this.serviceQuery(sender);
                    if (hasAutocomplete) {
                        $(sender).webgis_autocomplete('close');
                    }
                }
            }
        },
        serviceQuery: function (sender) {
            const $ = webgis.$ || jQuery;
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
                return;
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
                webgis.ui.emptyQueryResultsListWithFeatureDetails($resultTarget, { map: map });
            } else {
                webgis.ui.emptyQueryResultsList($resultTarget, { map: map });
            }
            $resultTarget.html("<img src='" + webgis.css.imgResource('loader1.gif', 'hourglass') + "' /><span>" + webgis.l10n.get('search-in-progress') + "</span>");

            webgis.ajax({
                url: webgis.baseUrl + '/rest/search/' + $input.attr('data-service'),
                data: webgis.hmac.appendHMACData({ term: term, f: 'geojson', rows: 100 }),
                type: 'post',
                success: function (result) {
                    content.search_term = term;
                    $resultTarget.empty();
                    map.queryResultFeatures.showClustered(result, true);

                    var counter = 0;
                    if (result && result.features && result.features.length > 0) {
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
                        $resultTarget.text(webgis.l10n.get('no-results-found') + '...');
                        $content.webgis_queryResultsHolder('showbuttons', { clearresults: false, showtable: false });
                    }

                    if ($tabControl) {
                        $tabControl
                            .webgis_tab_control('setTabPayload', { id: tabId, payload: { features: result, map: map } })
                            .webgis_tab_control('setTabCounter', { id: tabId, counter: counter })
                            .webgis_tab_control('resize');

                        map.ui._setResultFrameSize();
                    }
                },
                error: function () {
                    $resultTarget.text(webgis.l10n.get('an-error-occurred'));
                }
            });
        },
        geojuhuQuery: function (sender) {
            const $ = webgis.$ || jQuery;
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
                return;
            }
            map.events.fire('showqueryresults', { map: map, buttons: { clearresults: false, showtable: false } });
            var $list = $content.find('.webgis-search-result-list');
            webgis.ui.emptyQueryResultsList($list, { map: map });
            $list.html("<img src='" + webgis.css.imgResource('loader1.gif', 'hourglass') + "' /><span>" + webgis.l10n.get('search-in-progress') + "</span>");
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
                        $list.text(webgis.l10n.get('no-results-found') + '...');
                        $content.webgis_queryResultsHolder('showbuttons', { clearresults: false, showtable: false });
                    }
                },
                error: function () {
                    $list.text(webgis.l10n.get('an-error-occurred'));
                }
            });
        }
    }
});

// compatible to old versions: same plugin (instance data key 'webgis_topbar')
(webgis.$ || jQuery).fn.webgis_search = (webgis.$ || jQuery).fn.webgis_topbar;

// global, used by the action of the detail search form (see buildMask)
(webgis.$ || jQuery).webgis_search_fire_detail = function (formId, path) {
    return (webgis.$ || jQuery).fn.webgis_topbar.plugin.fireDetail(formId, path);
};
