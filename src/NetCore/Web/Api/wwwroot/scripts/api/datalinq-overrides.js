if (window.dataLinq) {

    // compability
    window.webgis_datalinq = window.dataLinq;

    // Overrides
    dataLinq.overrideInit = function (oninit) {
        if (typeof dataLinq.__authObject === 'string') {
            webgis.clientid = dataLinq.__authObject;
        }
        else if (dataLinq.__authObject) {
            webgis.hmac = new webgis.hmacController(dataLinq.__authObject);
        }


        if (!webgis.hmac) {
            var url = webgis.baseUrl + "/rest/requesthmac?clientid=" + webgis.clientid;
            if (typeof webgis.clientid === 'string' &&
                (webgis.clientid.indexOf('https://') === 0 || webgis.clientid.indexOf('http://') === 0 || webgis.clientid.indexOf("//") === 0)) {
                url = webgis.clientid;
            }

            var FF = !(window.mozInnerScreenX === null);

            $.ajax({
                url: url,
                data: { redirect: document.location.toString() },
                async: false,
                type: 'post',
                xhrFields: (FF && url.indexOf('https://localhost/') === 0) ? null : this._ajaxXhrFields,  // Macht Probleme im FF (bei syncronen Calls und bei localhost!?)
                success: function (result) {
                    if (result.success) {
                        if (!result.username && result.authEndpoint) {
                            document.location = result.authEndpoint;
                        };
                        webgis.hmac = new webgis.hmacController(result);
                        $('#datalinq-header').html(result.username);
                    } else {
                        webgis._initialError = result.exception;
                        $('#datalinq-header').html(webgis._initialError);
                        webgis.hmac = new webgis.hmacController();
                    }
                }, error: function (jqXHR, textStatus, errorThrown) {
                    webgis._initialError = 'Error at authorizing: ' + errorThrown;
                    $('#datalinq-header').html(webgis._initialError);
                    webgis.hmac = new webgis.hmacController();
                }
            });
        } else {
            $('#datalinq-header').html(webgis.hmac.userDisplayName());
        }
    };

    dataLinq.overrideUpdateElement = function (parent) {
        if ($(parent).find('.datalinq-map').length > 0) {
            if (!webgis.isReady()) {
                webgis.init(function () {
                    dataLinq.createMapElements($(parent), $(parent).find('.datalinq-map'));
                });
            } else {
                dataLinq.createMapElements($(parent), $(parent).find('.datalinq-map'));
            }
        }
    };

    dataLinq.overrideModifyRequestData = function (dataObject) {
        return webgis.hmac.appendHMACData(dataObject)
    };
    dataLinq.overrideModifyRequestUrlData = function (url) {
        return webgis.hmac.appendHMACDataToUrl(url);
    }

    // Add Custom Functions
    dataLinq.createMapElements = function ($parent, $elements) {
        $elements.each(function (i, e) {
            var map = webgis.createMap($(e), {
                extent: $(e).attr('data-map-extent'),
                services: $(e).attr('data-map-services')
            });
            $(e).data('datalinq-map', map);

            $parent.addClass('datalinq-geo-container');

            // Darstellungsvarianten berücksichtigen
            var presentations = $(e).attr('data-map-presentations');
            if (presentations.length > 0) {
                presentations = presentations.split(",");
                for (var p in presentations) {
                    var presentationName = presentations[p].split('=')[0];
                    // Wenn On/Off => dann Checkbox(Layer), sonst Button(DV)
                    isButton = false, isCheckboxActive = null;
                    if (presentations[p].split('=').length === 1)
                        isButton = true;
                    else
                        isCheckboxActive = presentations[p].split('=')[1] === "on";

                    for (var s in map.services) {
                        var service = map.services[s];
                        for (var sp in service.presentations) {
                            var presentation = service.presentations[sp];
                            if (presentation.id === presentationName) {
                                layerIds = service.getLayerIdsFromNames(presentation.layers);
                                // Wenn DV (Button), dann Service zuerst unsichtbar schalten (Siehe Api: webgis_presentationToc.js)
                                if (isButton) {
                                    service.setServiceVisibility(null); // alle unsichtbar schalten!!! und unten die einzelnen Layer einschalten. In Gruppen können mehrer Darstellungsvarianten aus dem gleichen Dienst vorkommen. Darum zuerst Sichtbarkeit aus und dann die einzellnen wieder einschalten.
                                    service.setLayerVisibility(layerIds, true);
                                } else
                                    service.setLayerVisibility(layerIds, isCheckboxActive);
                                break;
                            }
                        }
                    }
                }
            }

            webgis.delayed(function (arg) {
                var features = {
                    type: "FeatureCollection",
                    features: []
                };
                var minLat = 90, minLng = 180, maxLat = -90, maxLng = -180;
                arg.parent.find('.datalinq-geo-element[data-geo-lat][data-geo-lng]').each(function (j, g) {
                    var lat = $(g).attr("data-geo-lat"), lng = $(g).attr("data-geo-lng");
                    //if (!$(g).attr('id'))
                    //    dataLinq.setId($(g));

                    var maps = $(g).data('maps') || [];
                    maps.push(map);
                    $(g).data('maps', maps);

                    $(g).click(function () {
                        var $this = $(this);

                        $this.parent()
                            .children('.datalinq-geo-element-selected')
                            .removeClass('datalinq-geo-element-selected');

                        $this.addClass('datalinq-geo-element-selected');

                        var maps = $this.data('maps'),
                            lat = parseFloat($this.attr("data-geo-lat")),
                            lng = parseFloat($this.attr("data-geo-lng")),
                            epsilon = 0.0003;

                        var bbox = null;

                        if ($this.attr('data-geo-bbox')) {
                            var bboxParts = $this.attr('data-geo-bbox').split(',');
                            if (bboxParts.length == 4) {
                                bbox = [
                                    parseFloat(bboxParts[0]),
                                    parseFloat(bboxParts[1]),
                                    parseFloat(bboxParts[2]),
                                    parseFloat(bboxParts[3])
                                ];
                            }
                        }

                        for (var m in maps) {
                            maps[m].zoomTo(bbox || [lng - epsilon, lat - epsilon, lng + epsilon, lat + epsilon]);

                            // Recluster delayed => zoom sollte fertig sein bis dahin, sonst "stockt" die Karte
                            webgis.delayed(function () {
                                maps[m].queryResultFeatures.recluster();
                                maps[m].queryResultFeatures.uncluster($this.attr("data-geo-id"), true);
                            }, 700);


                            // Soll Feature highlighted werden? TODO:
                            //var selection = m.getSelection("query");
                            ////selection.setTargetQuery(service, query, id);
                            //selection.setTargetQuery(m.getService("kuvert@ccgis_default"), "kuvert_alle", id);
                        }
                    });

                    minLat = Math.min(minLat, lat);
                    maxLat = Math.max(maxLat, lat);
                    minLng = Math.min(minLng, lng);
                    maxLng = Math.max(maxLng, lng);

                    var popup = $(g).attr("data-geo-popup");
                    if (typeof ($(g).attr("data-geo-link2list")) !== "undefined" && $(g).attr("data-geo-link2list").length > 0)
                        popup += "<br><a onclick=\"dataLinq.showListItem('" + $(g).attr('data-geo-id') + "')\">" + $(g).attr("data-geo-link2list") + "</a>";

                    var featureProperties = {
                        _fulltext: popup
                    };


                    $.each($(g).data(), function (i, v) {
                        if (i.indexOf('geoFeatureAttribute-') === 0) {
                            var property = i.substr('geoFeatureAttribute-'.length);
                            featureProperties[property.toLowerCase()] = v;
                            //console.log(i.toString() + '=', v);
                        }
                    });

                    features.features.push({
                        type: "Feature",
                        oid: $(g).attr('data-geo-id'),
                        geometry: {
                            type: "Point",
                            coordinates: [lng, lat]
                        },
                        properties: featureProperties
                    });
                });

                features.bounds = [minLng, minLat, maxLng, maxLat];

                map.queryResultFeatures.showClustered(features, true);
            }, 1, { parent: $parent, map: map });
        });
    };
}

if (typeof jQuery != 'undefined' && jQuery.fn.jquery >= '3.5.0') {
    jQuery.extend({
        //
        // make it compatible to jQuery < 3.5
        // make <div /> to <div></div>
        // 
        // https://api.jquery.com/jQuery.htmlPrefilter/
        // since jQuery 3.5 function simply returns unfilter => html
        //
        htmlPrefilter: function (html) {
            // See https://github.com/eslint/eslint/issues/3229
            rxhtmlTag = /<(?!area|br|col|embed|hr|img|input|link|meta|param)(([a-z][^\/\0>\x20\t\r\n\f]*)[^>]*)\/>/gi;

            //console.log(html, html.replace(rxhtmlTag, "<$1></$2>"));

            return html.replace(rxhtmlTag, "<$1></$2>");
        }
    });
}