(function ($) {
    "use strict";
    $.fn.webgis_control_search = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_control_search');
        }
    };
    var defaults = {
        search_service: null,
        search_categories: '',
        on_select: null,
        on_select_get_original: false,
        on_select_get_original_raw: false,
        on_select_get_original_fullgeometry: false,
        css_display: null,
        meta: 'meta'
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
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
            return this;
        }
    };
    var initUI = function (elem, options) {
        var $elem = $(elem);

        if (!$elem.is('input[type="text"]') || !options.search_service)
            return;
        if (options.css_display)
            $elem.data('css-display', options.css_display);

        $elem.addClass('clearable x');
        function tog(v) { return v ? 'addClass' : 'removeClass'; }

        $elem.on('mousemove', function (e) {
            $(this)[tog(this.offsetWidth - 18 < e.clientX - this.getBoundingClientRect().left)]('onX');
        }).on('touchstart click', function (ev) {
            if ($(this).hasClass('onX')) {
                ev.preventDefault();
                $(this).removeClass('onX').val('');
                webgis._triggerAutocomplete(this);
            }
        });

        $elem.attr('data-service', options.search_service);
        $elem.attr('data-source', webgis.baseUrl + '/rest/search/' + options.search_service + '?c=query&');
        $elem.attr('data-search-categories', options.search_categories).attr('data-minlength', 0);
        $elem.addClass('webgis-autocomplete');

        if (options.on_select) {
            $elem
              .data('data-options', options)
              .data('autocomplete-onselect', function (sender, item) {
                var options = $(sender).blur().data('data-options');

                if (item.id && item.id.indexOf('#metadata') === 0) {
                    var meta = 'meta' + (item.id.length > 9 ? '_' + item.id.substr(10) : '');
                    //console.log($(sender).data('data-options'));
                    $(sender).webgis_control_search('meta', {
                        search_service: $(sender).data('data-options').search_service,
                        search_categories: $(sender).data('data-options').search_categories,
                        meta: meta,
                        success: function (metadata) {
                            $('body').webgis_modal({
                                title: 'Schnellsuche Themen',
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
                                            console.log('samples', meta.sample, meta.sample_separator);
                                            var samples = meta.sample.split(meta.sample_separator ? meta.sample_separator : ',');
                                            for (var s in samples) {
                                                $("<div style='margin-top:5px'>Beispiel für die Eingabe:</div>").appendTo($block);
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
                        title: 'Nach Inhalten/Dienste suchen...',
                        iconUrl: webgis.baseUrl + '/content/api/img/content-search.png',
                        message: webgis.i18n.get('content-search-info'),
                        suppressCancel: searchTag === '',
                        cancelText: 'Nein, danke',
                        okText: searchTag && map ? 'In (Karten)diensten suchen' : 'Ok',
                        onOk: function () {
                            //$('#tab-presentations').trigger('click');
                            //$('.webgis-presentation_toc-search.webgis-content-search-holder')
                            //    .webgis_contentsearch('set', { value: searchTag });
                            if (searchTag) {
                                $('body').webgis_modal({
                                    title: 'In Diensten suchen',
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
                                console.log('suppressSendQuickSearchResult', suppressSendQuickSearchResult);
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
            });
        }
        webgis._appendAutocomplete($elem);
    };
})(webgis.$ || jQuery);
