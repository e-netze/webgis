(function ($) {
    "use strict"
    $.fn.webgis_mapbuilder_serviceSelector = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on jQuery.webgis_mapbuilder_dynamicContentSelector');
        }
    };

    var defaults = {
        serviceIds: [],
        onAddService: null,
        onRemoveService: null,
        checkAvailability: null
    };

    var methods = {
        init: function (options) {
            var settings = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, settings);
            });
        },
    };

    var initUI = function (parent, options) {


        var $parent = $(parent).addClass('webgis-addservices_toc-holder');
        var $cmsUl = $("<ul>").appendTo($parent);

        webgis.ajax({
            type: 'post',
            url: webgis.baseUrl + "/rest/services",
            data: webgis.hmac.appendHMACData({ f: 'json' }),
            success: function (serviceInfos) {

                $.each(serviceInfos.services, function (i, serviceInfo) {

                    var $cmsItem = null;

                    $cmsUl.find('.webgis-addservices_toc-title-text').each(function (i, obj) {
                        if (obj.innerHTML === serviceInfo.container) {
                            $cmsItem = $(obj.parentNode);
                        }
                    });

                    if ($cmsItem == null) {
                        $cmsItem = $("<li>")
                            .addClass('webgis-addservices_toc-title webgis-addservices_toc-collapsable')
                            .appendTo($cmsUl)
                            .click(function () {
                                var $content = $(this).children('.webgis-addservices_toc-content');
                                $content.css('display', $content.css('display') === 'none' ? '' : 'none');
                            });

                        $("<span>")
                            .css('position', 'absolute')
                            .addClass('webgis-addservices_toc-plus webgis-api-icon webgis-api-icon-triangle-1-s')
                            .appendTo($cmsItem);
                        $("<div>")
                            .addClass('webgis-addservices_toc-title-text')
                            .text(serviceInfo.container)
                            .appendTo($cmsItem);
                        var $content = $("<div>")
                            .addClass('webgis-addservices_toc-content')
                            .css('display', 'none')
                            .appendTo($cmsItem);
                        $("<ul>")
                            .css({ margin: "0px 0px 0px 20px", fontSize:'.8em' })
                            .appendTo($content);
                    }

                    var $servicesUl = $cmsItem.find('.webgis-addservices_toc-content ul');

                    var $serviceItem = $("<li>")
                        .addClass('webgis-addservices_toc-item webgis-addservices_toc-checkable')
                        .appendTo($servicesUl)
                        .data('serviceInfo', serviceInfo)
                        .click(function (e) {
                            e.stopPropagation();

                            var $icon = $(this).find('.webgis-addservices_toc-checkable-icon');
                            if ($icon.attr('src').indexOf('check0') > 0) {
                                if (options.onAddService && options.onAddService($(this).data('serviceInfo'), serviceInfos)) {
                                    $icon.attr('src', webgis.css.imgResource("check1.png", "toc"));
                                }
                            } else {
                                if (options.onRemoveService && options.onRemoveService($(this).data('serviceInfo'), serviceInfos)) {
                                    $icon.attr('src', webgis.css.imgResource("check0.png", "toc"));
                                }
                            }
                        });

                    var chk = "<img class='webgis-addservices_toc-checkable-icon' src=" + webgis.css.imgResource($.inArray(serviceInfo.id, options.serviceIds) >= 0 ? "check1.png" : "check0.png", "toc") + ">";
                    var img = "<img src='" + webgis.baseUrl + "/content/api/img/" + serviceInfo.type + "-service.png' style='width:24px;position:relative;top:6px'>";
                    $("<span>" + chk + "&nbsp;" + img + "&nbsp;" + serviceInfo.name + "</span>").appendTo($serviceItem);

                    if (options.checkAvailability && options.checkAvailability(serviceInfo) === false) {
                        $serviceItem.css('opacity', '0.2');
                    }
                });

            }
        });
    };
})(webgis.$ || jQuery);

(function ($) {
    "use strict"
    $.fn.webgis_mapbuilder_dynamicContentSelector = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on jQuery.webgis_mapbuilder_dynamicContentSelector');
        }
    };

    var defaults = {
        content:null,
        onselect:null
    };

    var methods = {
        init: function (options) {
            var settings = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, settings);
            });
        },
    };

    var initUI = function (parent, options) {
        var $parent = $(parent);

        $parent.addClass('dyncontent-container');

        $("<h2>")
            .css({ margin: '12px 0px' })
            .text("Allgemine Eigenschaften")
            .appendTo($parent);

        $("<span>Art der Quelle:</span><br/>").appendTo($parent);
        var $select = $("<select class='dyncontent-source-type webgis-input'><option value='geojson'>GeoJson</option><option value='geojson-embedded'>GeoJson (Embedded, eg Solr)</option><option value='georss'>Geo RSS Dienst</option><option value='api-query'>API Abfrage</option></select>").appendTo($parent)
            .change(function () {
                $(this).closest('.dyncontent-container').find('.dyncontent-source-panel').css('display', 'none');
                $(this).closest('.dyncontent-container').find('.panel-' + $(this).val()).fadeIn();
                if ($(this).val() == 'api-query' && $(this).closest('.dyncontent-container').find('.panel-api-query').find('.dyncontent-api-query').find('option').length == 0) {
                    loadApiQueries($(this).closest('.dyncontent-container').find('.panel-api-query').find('.dyncontent-api-query'));
                }
            });

        var $div_geojson = $("<div class='dyncontent-source-panel panel-geojson' style='display:none'></div>").appendTo($parent);
        $("<input class='dyncontent-geojson-source-name webgis-input' style='width:98%' placeholder='Name...'/>").appendTo($div_geojson);
        $("<textarea class='dyncontent-geojson-source-url webgis-input' style='width:98%' placeholder='GeoJson Quelle Url...'></textarea>").appendTo($div_geojson);
        appendDynamicContentParameters($div_geojson, 'geojson');
        $("<br/>").appendTo($div_geojson);
        $("<button class='webgis-button'>Übernehmen</button>").appendTo($div_geojson)
            .click(function () {
                //console.log($(this).closest('.dyncontent-container').find('.dyncontent-source-type').length);
                var onselect = $(this).closest('.dyncontent-container').data('onselect');
                //alert($(this).closest('.dyncontent-container').find('.dyncontent-source-type').val());
                if (onselect) {
                    onselect(
                        $.extend({}, getDynamicContentParameters($(this).closest('.dyncontent-container'), 'geojson'),{
                            id: $(this).closest('.dyncontent-container').data('content-id'),
                            type: $(this).closest('.dyncontent-container').find('.dyncontent-source-type').val(),
                            name: $(this).closest('.dyncontent-source-panel').find('.dyncontent-geojson-source-name').val(),
                            url: $(this).closest('.dyncontent-source-panel').find('.dyncontent-geojson-source-url').val(),
                        }));
                }
            });

        var $div_geojson_embedded = $("<div class='dyncontent-source-panel panel-geojson-embedded' style='display:none'></div>").appendTo($parent);
        $("<input class='dyncontent-geojson-embedded-source-name webgis-input' style='width:98%' placeholder='Name...'/>").appendTo($div_geojson_embedded);
        $("<textarea class='dyncontent-geojson-embedded-source-url webgis-input' style='width:98%' placeholder='GeoJson Quelle Url...'></textarea>").appendTo($div_geojson_embedded);
        appendDynamicContentParameters($div_geojson_embedded, 'geojson-embedded');
        $("<br/>").appendTo($div_geojson_embedded);
        $("<button class='webgis-button'>Übernehmen</button>").appendTo($div_geojson_embedded)
            .click(function () {
                //console.log($(this).closest('.dyncontent-container').find('.dyncontent-source-type').length);
                var onselect = $(this).closest('.dyncontent-container').data('onselect');
                //alert($(this).closest('.dyncontent-container').find('.dyncontent-source-type').val());
                if (onselect) {
                    onselect(
                        $.extend({}, getDynamicContentParameters($(this).closest('.dyncontent-container'), 'geojson-embedded'), {
                            id: $(this).closest('.dyncontent-container').data('content-id'),
                            type: $(this).closest('.dyncontent-container').find('.dyncontent-source-type').val(),
                            name: $(this).closest('.dyncontent-source-panel').find('.dyncontent-geojson-embedded-source-name').val(),
                            url: $(this).closest('.dyncontent-source-panel').find('.dyncontent-geojson-embedded-source-url').val(),
                        }));
                }
            });

        var $div_georss = $("<div class='dyncontent-source-panel panel-georss' style='display:none'></div>").appendTo($parent);
        $("<input class='dyncontent-georss-source-name webgis-input' style='width:98%' placeholder='Name...'/>").appendTo($div_georss);
        $("<textarea class='dyncontent-georss-source-url  webgis-input' style='width:98%' placeholder='GeoRSS Quelle Url...'></textarea>").appendTo($div_georss);
        appendDynamicContentParameters($div_georss, 'georss');
        $("<br/>").appendTo($div_georss);
        $("<button class='webgis-button'>Übernehmen</button>").appendTo($div_georss)
            .click(function () {
                var onselect = $(this).closest('.dyncontent-container').data('onselect');
                if (onselect) {
                    onselect(
                        $.extend({}, getDynamicContentParameters($(this).closest('.dyncontent-container'), 'georss'),{
                            id: $(this).closest('.dyncontent-container').data('content-id'),
                            type: $(this).closest('.dyncontent-container').find('.dyncontent-source-type').val(),
                            name: $(this).closest('.dyncontent-source-panel').find('.dyncontent-georss-source-name').val(),
                            url: $(this).closest('.dyncontent-source-panel').find('.dyncontent-georss-source-url').val(),
                        }));
                }
            });

        var $div_apiQuery = $("<div class='dyncontent-source-panel panel-api-query' style='display:none'></div>").appendTo($parent);
        $("<input class='dyncontent-api-query-name webgis-input' style='width:98%' placeholder='Name...'/>").appendTo($div_apiQuery);
        $("<select class='dyncontent-api-query webgis-input'></select>").appendTo($div_apiQuery)
            .change(function (e) {
                loadQueryItems($(this).closest('.dyncontent-container'));
            });
        $("<div class='dyncontent-api-query-items'></div>").appendTo($div_apiQuery);
        appendDynamicContentParameters($div_apiQuery, 'api-query');
        $("<br/>").appendTo($div_apiQuery);
        $("<button class='webgis-button'>Übernehmen</button>").appendTo($div_apiQuery)
            .click(function () {
                var onselect = $(this).closest('.dyncontent-container').data('onselect');
                if (onselect) {

                    var $itemsPanel = $(this).closest('.dyncontent-container').find('.panel-api-query').find('.dyncontent-api-query-items');
                    var items = [],urlItems='';
                    $itemsPanel.find('input').each(function (i, e) {
                        var item={ id: $(e).attr('name'), val: $(e).val() };
                        items.push(item);
                        if (item.val)
                            urlItems += (urlItems != '' ? '&' : '') + item.id + "=" + item.val;
                    });
                    onselect(
                        $.extend({}, getDynamicContentParameters($(this).closest('.dyncontent-container'), 'api-query'), {
                            id: $(this).closest('.dyncontent-container').data('content-id'),
                            type: $(this).closest('.dyncontent-container').find('.dyncontent-source-type').val(),
                            name: $(this).closest('.dyncontent-source-panel').find('.dyncontent-api-query-name').val(),
                            url: webgis.baseUrl + '/rest/services/' + $(this).closest('.dyncontent-source-panel').find('.dyncontent-api-query').val() + '/query?f=json&' + urlItems,
                            queryid: $(this).closest('.dyncontent-source-panel').find('.dyncontent-api-query').val(),
                            items: items
                        }));
                }
            });

        if (options.content && options.content.type) {
            $select.val(options.content.type);

            $parent.data('content-id', options.content.id);
            
            switch (options.content.type) {
                case 'geojson':
                    $select.trigger('change');
                    $parent.find('.dyncontent-geojson-source-name').val(options.content.name);
                    $parent.find('.dyncontent-geojson-source-url').val(options.content.url);
                    break;
                case 'geojson-embedded':
                    $select.trigger('change');
                    $parent.find('.dyncontent-geojson-embedded-source-name').val(options.content.name);
                    $parent.find('.dyncontent-geojson-embedded-source-url').val(options.content.url);
                    break;
                case 'georss':
                    $select.trigger('change');
                    $parent.find('.dyncontent-georss-source-name').val(options.content.name);
                    $parent.find('.dyncontent-georss-source-url').val(options.content.url);
                    break;
                case 'api-query':
                    loadApiQueries($parent.find('.dyncontent-api-query'), function () {
                        $parent.find('.dyncontent-api-query-name').val(options.content.name);
                        $parent.find('.dyncontent-api-query').val(options.content.queryid).trigger('change');
                        loadQueryItems($parent);
                        $select.trigger('change');
                        if (options.content.items && options.content.items.length > 0) {
                            for (var i in options.content.items) {
                                var item = options.content.items[i];
                                $parent.find('.dyncontent-api-query-items').find("[name='" + item.id + "']").val(item.val);
                            }
                        }
                    });
                    
                    break;
            }

            setDynamicContentParameters($parent, options.content, options.content.type);
        } else {
            $select.trigger('change');
        }

        $parent.data('onselect', options.onselect);
    };

    var loadApiQueries = function ($select, callback) {
        $.ajax({
            url: webgis.baseUrl + '/rest/servicesqueries',
            data: webgis.hmac.appendHMACData({ f: 'json' }),
            type: 'post',
            success: function (result) {
                if (!result.services)
                    return;

                for (var s in result.services) {
                    var service = result.services[s];

                    var $optgroup = $("<optgroup label='" + service.name + " (" + service.id + ")" + "'></optgroup>").appendTo($select);
                    if (service.supportedCrs != null) {
                        // ToDo:
                    }
                    for (var q in service.queries) {
                        var query = service.queries[q];

                        $("<option value='" + service.id + "/queries/" + query.id + "'>" + query.name + "</option>").appendTo($optgroup).data('querydef', query);
                    }
                }

                $select.trigger('change');
                if (callback)
                    callback();
            }
        });
    };

    var loadQueryItems = function ($parent) {
        var $panel = $parent.find('.panel-api-query');
        var $itemsPanel = $panel.find('.dyncontent-api-query-items');
        $itemsPanel.empty();

        var $select = $panel.find('.dyncontent-api-query');
        var $option = $select.find("option[value='" + $select.val() + "']");

        var query = $option.data('querydef');

        if (query == null || query.items == null) {
            return;
        }

        for (var i in query.items) {
            var item = query.items[i];

            var style = '';
            if (item.visible == false)
                style = 'opacity:.5';

            $("<div style='" + style + "'  class='webgis-input-label'>" + item.name + "</div>").appendTo($itemsPanel);
            if (item.autocomplete == true)
                $("<input style='" + style + "' type='text' class='webgis-input webgis-autocomplete' name='" + item.id + "' data-source='" + webgis.baseUrl + "/rest/services/" + $select.val() + "?c=autocomplete&_item=" + item.id + "' />").appendTo($itemsPanel);
            else
                $("<input style='" + style + "' type='text' class='webgis-input' name='" + item.id + "' />").appendTo($itemsPanel);
        }

        webgis._appendAutocomplete($itemsPanel);
    };

    var appendDynamicContentParameters = function ($parent, namespace) {
        var $div = $("<div>")
            .css({ borderTop: '1px solid #f8f8f8' })
            .appendTo($parent);

        $("<h2>")
            .css({ margin: '12px 0px', cursor: 'pointer' })
            .text("» Erweiterte Eigenschaften")
            .appendTo($div)
            .click(function () {
                var $next = $(this).next();
                $next.css('display', $next.css('display') == 'none' ? 'block' : 'none');
            });

        var $propContainer = $("<div>").css('display','none').appendTo($div);
        appendDynamicPropertyCheckbox($propContainer, namespace, 'autozoom', 'Beim Auswählen automatisch auf Ausdehung zoomen',
            'Wenn diese Option gesetzt ist, wird nach dem Anzeigen der Marker in der Karte automatisch auf die Ausdehnung der Ergebnisse gezoomt. Ansonst bleibt der Auschnitt nach dem Anzeigen für den Anwender unverändert.');
        appendDynamicPropertyCheckbox($propContainer, namespace, 'showresult', 'Objekte in der Ergebnisliste anzeigen',
            'Wenn diese Option gestzt ist, werden die Ergebnisse wie eine Abfrage behandelt und die Objekte auch in der Ergebnislist/-tabelle sofort angezeigt. Die Ansicht wechselt dabei sofort zum Reiter Ergebnisse. Ansonsten springt die Ansicht nicht automaisch in die Ergenisliste.');
        appendDynamicPropertyCheckbox($propContainer, namespace, 'autoload', 'Beim Start automatisch laden',
            'Beim Start des Viewers wird der erste dynamische Inhalt, bei dem diese Option gesetzt ist, automatisch geladen. Ansonsten muss der Anwender manuell einen dynamischen Inhalt über das Inhaltsverzeichns (Darstellung) laden.');
        appendDynamicPropertyCheckbox($propContainer, namespace, 'extentdependend', 'Ausschnittsabhängig',
            'Der Inhalt ist vom aktuellen Ausschnitt abhängig und wird bei jedem Zoom/Pan neu geladen');

        // Set Defaults
        setDynamicContentParameters($parent, null, namespace);
    }

    var appendDynamicPropertyCheckbox = function ($parent, namespace, name, label, description) {
        var $label = $("<label>")
            .css({ cursor: 'pointer' })
            .appendTo($parent);

        $("<input type='checkbox' class='dynamic-" + namespace + "-" + name + "'>")
            .css({width:'15px', height:'15px', margin:'0px 2px 0px 0px', padding:0, verticalAlign:'bottom'})
            .appendTo($label);

        $label.append(label);

        if (description) {
            $("<div>")
                .css({ color: '#aaa', fontWeight: 'normal', marginTop: '5px' })
                .text(description)
                .appendTo($label);
        }

        $("<br/>").appendTo($parent);
    }

    var setDynamicContentParameters = function ($parent, content, namespace) {
        $parent.find('.dynamic-' + namespace + '-autozoom').prop('checked', !content || content.autoZoom !== false);  // !content => default = true
        $parent.find('.dynamic-' + namespace + '-showresult').prop('checked', !content || content.showResult !== false);
        $parent.find('.dynamic-' + namespace + '-autoload').prop('checked', !content || content.autoLoad !== false);
        $parent.find('.dynamic-' + namespace + '-extentdependend').prop('checked', content && content.extentDependend === true);
    };

    var getDynamicContentParameters = function ($parent, namespace) {
        var parameters = {
            autoZoom: $parent.find('.dynamic-' + namespace + '-autozoom').is(':checked') === true,
            showResult: $parent.find('.dynamic-' + namespace + '-showresult').is(':checked') === true,
            autoLoad: $parent.find('.dynamic-' + namespace + '-autoload').is(':checked') === true,
            extentDependend: $parent.find('.dynamic-' + namespace + '-extentdependend').is(':checked') === true
        };

        return parameters;
    } 

})(webgis.$ || jQuery);

(function ($) {
    "use strict"
    $.fn.webgis_mapbuilder_snapshotSelector = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on jQuery.webgis_mapbuilder_snapshotSelector');
        }
    };

    var defaults = {
        snapshots: null,
        active: null,
        show_properties: false
    };

    var methods = {
        init: function (options) {
            var settings = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, settings);
            });
        },
    };

    var initUI = function (parent, options) {
        var $parent = $(parent);

        $parent.webgis_modal({
            title: options.title || 'Select Snapshot...',
            id:'snapshot_selector',
            width: '640px',
            height: '400px',
            onload: function ($content) {
                if (options.description) {
                    $("<p>")
                        .text(options.description)
                        .appendTo($content);
                }

                var $selector = $("<select>")
                    .addClass('webgis-input')
                    .appendTo($content);

                if (options.snapshots) {
                    for (var s in options.snapshots) {
                        $("<option>")
                            .attr('value', options.snapshots[s].name)
                            .text(options.snapshots[s].name)
                            .appendTo($selector);
                    }
                }

                $selector.val(options.active);
                $selector.select2({
                    tags: true
                });

                var $properties = null;
                if (options.show_properties === true) {
                    $("<br><br>").appendTo($content);
                    $properties = $("<div>").appendTo($content);
                }

                $("<br><br>").appendTo($content);
                $("<button>")
                    .addClass("webgis-button")
                    .text(options.submitText || "Snapshot auswählen")
                    .appendTo($content)
                    .click(function (e) {
                        if (options.onselect) {
                            options.onselect($selector.val());
                        }

                        $parent.webgis_modal('close', { id: 'snapshot_selector' });
                    });

                $selector.val(options.active);
                $selector.select2({
                    tags: true
                });

                $selector.change(function (e) {
                    var active = $selector.val();
                    var snapshot = webgis.firstOrDefault(options.snapshots, function (s) { return s.name === active });

                    if ($properties) {
                        renderSnapshotProperties($properties, snapshot);
                    }
                });

                $selector.trigger('change');
            }
        });
    };

    var renderSnapshotProperties = function ($parent, snapshot) {
        $parent.addClass('webgis-snapshot-property-holder').data('snapshot', snapshot).empty();

        var properties = [
            {
                name: 'Expanded Containers & Groups',
                props: ["presentations.expanded"]
            },
            {
                name: 'Layer/Service Visiblity',
                props: ["presentations.visibility"]
            },
            {
                name: "Map Extend/Scale",
                props: ["map.scale", "map.center", "map.bounds"]
            }
        ];

        var $table = $("<table>").appendTo($parent);

        for (var p in properties) {
            var property = properties[p];

            var $tr = $("<tr>");
            $("<td><strong>" + property.name + ":</strong><td>").appendTo($tr);

            if (_hasOne(snapshot, property.props)) {
                var $td = $("<td>").appendTo($tr);
                $("<button>")
                    .data('props', property.props)
                    .addClass("webgis-button uibutton uibutton-cancel")
                    .text('Unset (remove)')
                    .appendTo($td)
                    .click(function () {
                        _removeAll(snapshot, $(this).data('props'));
                        renderSnapshotProperties($parent, snapshot);
                    });
            } else {
                $("<td>").text("not set").appendTo($tr);
            }

            $tr.appendTo($table);
        }
    };

    var _hasOne = function (obj, props) {
        if (!obj || !props)
            return false;

        for (var p in props) {
            var properties = props[p].split('.');
            var o = obj;
            for (var i = 0; i < properties.length; i++) {
                o = o[properties[i]];
                if (!o) {
                    break;
                }

                if (i == properties.length - 1 && o) {
                    return true;
                }
            }
        }

        return false;
    };

    var _removeAll = function (obj, props) {
        for (var p in props) {
            var properties = props[p].split('.');
            var o = obj;
            if (properties.length === 1 && o) {
                o[properties[0]] = null;
                continue;
            }
            for (var i = 0; i < properties.length - 1; i++) {
                o = o[properties[i]];
                if (!o) {
                    break;
                }

                if (i == properties.length - 2 && o) {
                    o[properties[properties.length - 1]] = null;
                    console.log(properties[properties.length - 1], o);
                }
            }
        }
    }
})(webgis.$ || jQuery);