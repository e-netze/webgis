// WebGIS UI Plugins (control_search)
// webgis_control_search: quick search (autocomplete) on a text input, incl. search metadata and content search info

webgis.ui.definePlugin('webgis_control_search', {
    defaults: {
        search_service: null,
        search_categories: '',
        on_select: null,
        on_select_get_original: false,
        on_select_get_original_raw: false,
        on_select_get_original_fullgeometry: false,
        css_display: null,
        on_enter: null,         // function(input): Enter without a cursor item and without a single data item
        position_target: null,  // selector/element used to position the suggestion menu (default: the input)
        meta: 'meta'
    },
    init: function () {
        const $ = this.$, options = this.options;
        var $elem = this.$el;

        if (!$elem.is('input[type="text"]') || !options.search_service)
            return;

        $elem.addClass('clearable x');
        function tog(v) { return v ? 'addClass' : 'removeClass'; }

        $elem.on('mousemove.webgis_control_search', function (e) {
            $(this)[tog(this.offsetWidth - 18 < e.clientX - this.getBoundingClientRect().left)]('onX');
        }).on('touchstart.webgis_control_search click.webgis_control_search', function (ev) {
            if ($(this).hasClass('onX')) {
                ev.preventDefault();
                $(this).removeClass('onX');
                $(this).webgis_autocomplete('search', '');
            }
        });

        $elem.attr('data-service', options.search_service);
        $elem.addClass('webgis-autocomplete');

        let onSelect = null;
        if (options.on_select) {
            $elem.data('data-options', options);
            onSelect = function (sender, item) {
                var options = $(sender).blur().data('data-options');

                if (item.id && item.id.indexOf('#metadata') === 0) {
                    var meta = 'meta' + (item.id.length > 9 ? '_' + item.id.substr(10) : '');
                    //console.log($(sender).data('data-options'));
                    $(sender).webgis_control_search('meta', {
                        search_service: $(sender).data('data-options').search_service,
                        search_categories: $(sender).data('data-options').search_categories,
                        meta: meta,
                        success: function (metadata) {
                            webgis.ui.showModal({
                                title: webgis.l10n.get('quick-search-layers'),
                                onload: function ($content) {
                                    $content.addClass('meta-content').css('padding', '10px');
                                    var copyrightMetadata = [];
                                    for (var m in metadata) {
                                        var meta = metadata[m];
                                        if (meta.id === '__copyright') {
                                            copyrightMetadata.push(meta);
                                            continue;
                                        }
                                        var $header = $("<div><h3><strong>+</strong>&nbsp;" + meta.type_name + "</h3><div>").css({
                                            cursor: 'pointer',
                                            border: '1px solid #aaa',
                                            margin: "1px",
                                            paddingLeft: '10px',
                                            borderRadius: '8px'
                                        }).appendTo($content)
                                            .click(function () {
                                                var $block = $(this).next('.webgis-meta-block');
                                                $(this).find('strong').html($block.css('display') === 'none' ? '-' : '+');
                                                $block.slideToggle();
                                            });
                                        var $block = $("<div class='webgis-meta-block'></div>").css({
                                            background: '#eee',
                                            margin: '1px',
                                            padding: '10px',
                                            borderRadius: '8px',
                                            display: 'none'
                                        }).appendTo($content);
                                        $("<span>" + meta.description + "</span></br/></br/>").appendTo($block);
                                        if (meta.link) {
                                            $("<a target='_blank' href='" + meta.link + "' >Link...</a>").appendTo($block);
                                            $("<br/><br/>").appendTo($block);
                                        }
                                        if (meta.sample) {
                                            $block = $("<div style='background:#aaa;padding:5px;border-radius:4px'></div>").appendTo($block);
                                            webgis.log('samples', meta.sample, meta.sample_separator);
                                            var samples = meta.sample.split(meta.sample_separator ? meta.sample_separator : ',');
                                            for (var s in samples) {
                                                $("<div style='margin-top:5px'>").text(webgis.l10n.get('input-example')).appendTo($block);
                                                $("<input class='webgis-input' style='background:white' readonly='readonly' value='" + samples[s] + "' />").appendTo($block);
                                            }
                                        }
                                    }

                                    if (copyrightMetadata.length > 0) {
                                        var $header = $("<div><h3><strong>+</strong>&nbsp;Copyright</h3><div>").css({
                                            cursor: 'pointer',
                                            border: '1px solid #aaa',
                                            margin: "1px",
                                            paddingLeft: '10px',
                                            borderRadius: '8px'
                                        }).appendTo($content)
                                            .click(function () {
                                                var $block = $(this).next('.webgis-meta-block');
                                                $(this).find('strong').html($block.css('display') === 'none' ? '-' : '+');
                                                $block.slideToggle();
                                            });
                                        var $block = $("<div class='webgis-meta-block'></div>").css({
                                            background: '#eee',
                                            margin: '1px',
                                            padding: '10px',
                                            borderRadius: '8px',
                                            display: 'none'
                                        }).appendTo($content);

                                        for (var m in copyrightMetadata) {
                                            var cr = copyrightMetadata[m].copyright_info;
                                            if (cr) {
                                                $("<h3>" + cr.copyright + "</h3>").appendTo($block);
                                                if (cr.link) {
                                                    $("<a target='_blank'>")
                                                        .attr('href', cr.link)
                                                        .text(cr.link_text ? cr.link_text : cr.link)
                                                        .appendTo($block);
                                                }
                                                $("<p>")
                                                    .text(cr.advice)
                                                    .appendTo($block);
                                            }
                                        }
                                    }
                                },
                                width: '600px'
                            });
                        }
                    });
                    return;
                }

                if (item.id && item.id === "#content-search") {
                    var searchTag = $(sender).val();

                    var map = $(sender).data('map');

                    webgis.confirm({
                        title: webgis.l10n.get('search-contents-services'),
                        iconUrl: webgis.baseUrl + '/content/api/img/content-search.png',
                        message: webgis.l10n.get('content-search-info'),
                        suppressCancel: searchTag === '',
                        cancelText: webgis.l10n.get('no-thanks'),
                        okText: searchTag && map ? webgis.l10n.get('search-in-map-services') : webgis.l10n.get('ok'),
                        onOk: function () {
                            //$('#tab-presentations').trigger('click');
                            //$('.webgis-presentation_toc-search.webgis-content-search-holder')
                            //    .webgis_contentsearch('set', { value: searchTag });
                            if (searchTag) {
                                webgis.ui.showModal({
                                    title: webgis.l10n.get('search-in-services'),
                                    onload: function ($content) {
                                        $content.css('padding', '0px');
                                        $content.webgis_addServicesToc({ map: map, searchTag: searchTag });
                                    }
                                });
                            }
                        }
                    });
                    return;
                }

                if (!item.id || item.id.indexOf('.') < 0) {
                    options.on_select(sender, item, false);
                } if (options.on_select_get_original === false) {
                    options.on_select(sender, item, false);
                } else {
                    webgis.ajax({
                        url: webgis.baseUrl + "/rest/search/" + options.search_service + "/?c=original&render_fields=" + (options.on_select_get_original_raw !== true) + "&full_geometry=" + (options.on_select_get_original_fullgeometry === true) + "&item_id=" + item.id,
                        data: webgis.hmac.appendHMACData({}),
                        type: 'get',
                        success: function (result) {
                            var suppressSendQuickSearchResult = false;

                            if (result && result.type === "FeatureCollection") {
                                // FeatureCollection kann auch leer sein (features==null || features.length=0). Dh. Es gibt keine passendes Feature in der Karte
                                // Orignal muss aber trotzdem geschickt werden, damit im Viewer eventuell bestehende Abfragen aus der Liste entfernt werden
                                // und die Meldung "Keine Features gefunden" erscheint
                                options.on_select(sender, result, true);

                                suppressSendQuickSearchResult = result.suppressSendQuickSearchResult === true;
                                webgis.log('suppressSendQuickSearchResult', suppressSendQuickSearchResult);
                            }

                            if (!suppressSendQuickSearchResult) {
                                //
                                // Item muss immer geschickt werden, damit der Marker auch in der Karte angezeigt, wenn die FeatureCollection leer ist.
                                // Auch Collector_pro, geosearch, smartmap setzen das schicken des Schnellsuche Items voaus!!
                                //
                                options.on_select(sender, item, false);
                            }
                        }
                    });
                }
            };
        }

        $elem.webgis_autocomplete({
            source: webgis.baseUrl + '/rest/search/' + options.search_service + '?c=query&',
            categories: options.search_categories,
            minLength: webgis.usability.quickSearch.minLength || 0,
            debounceDelay: webgis.usability.quickSearch.debounceDelay || 0,
            cssDisplay: options.css_display || 'block',
            positionTarget: options.position_target,
            onSelect: onSelect,
            onCursorChange: function (sender, item) {
                const map = $(sender).data('map');
                if (map && item && item.coords) {
                    webgis._autocompleteMapItem(map, item);
                }
            },
            // Kunde will das unbedingt: beim Mouseover soll schon gezoomt werden
            onItemHover: function (sender, item) {
                const map = $(sender).data('map');
                if (map && item) {
                    webgis._autocompleteMapItemTimer.Start({ map: map, item: item });
                }
            },
            onMenuLeave: function () {
                webgis._autocompleteMapItemTimer.Stop();
            },
            onEnter: function (sender, items) {
                // ignore metadata items (id starts with '#')
                let dataItems = items.filter(function (item) {
                    return item && item.id && item.id.indexOf('#') !== 0;
                });

                if (dataItems.length > 1 && webgis.usability.quickSearch.selectFirstOnEnter === true) {
                    dataItems = [dataItems[0]];
                }

                // only one item left => select it
                if (dataItems.length === 1 && onSelect) {
                    onSelect(sender, dataItems[0]);
                } else if (options.on_enter) {
                    options.on_enter(sender);
                }
            }
        });
    },
    destroy: function () {
        this.$el.off('.webgis_control_search');
    },
    methods: {
        meta: function (options) {
            if (!options.search_service || !options.success)
                return;
            webgis.ajax({
                url: webgis.baseUrl + "/rest/search/" + options.search_service,
                data: webgis.hmac.appendHMACData({ c: options.meta, categories: options.search_categories }),
                type: 'get',
                success: function (result) {
                    options.success(result);
                }
            });
        }
    }
});
