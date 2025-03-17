(function (webgis) {
    "use strict"

    webgis.addPlugin(new function(){
        this.onInit = function () {

        };

        this.onMapCreated = function (map, container) {
            
        }
    });
})(webgis);

/*
TODO:

*/


(function($){
    $.fn.webgis_geosearch = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_supersearch');
        }
    };

    var defaults = {
        map_options: null,
        onsave: null,
        map_search_service: null
    };

    //jQuery.ui.autocomplete.prototype._resizeMenu = function () {
    //    var ul = this.menu.element;
    //    ul.outerWidth(this.element.outerWidth());
    //}

    var options;
    var isMobile = window.matchMedia("only screen and (max-width: 800px)");

    var ua = window.navigator.userAgent;
    var resizeTimer = new webgis.timer(function (arg) {
        // Wenn IE -> DIV höhe setzen 
        //if (ua.indexOf("MSIE") > -1 || ua.indexOf("Trident") > -1 || ua.indexOf("Edge") > -1)
            //$("#map").css("height", $(".myTable").height());
        getMap().invalidateSize();  // Karte an aktuelle DIV Größe anpassen (für jeden Browser)
    }, 1000, null);

    var methods = {
        init: function (parameter) {
            var $this = $(this);
            options = $.extend({}, defaults, parameter);

            return this.each(function () {
                new initUI(this);
            });
        }
    };


    var initUI = function (elem) {
        var $elem = $(elem);
        $elem.addClass('webgis-plugin-geosearch-container webgis-container-styles');
        var $myTable = $("<div class='myTable'>" +
                            "<div id='viewSearch'></div>" +
                            "<div id='viewMap'></div>" +
                         "</div>").appendTo($elem);

        $(window).resize(function () {
            isMobile = window.matchMedia("only screen and (max-width: 800px)");
            if ($("#map").length > 0)
                resizeTimer.Start();
        });

        createSearchMask($("#viewSearch"));
    }


    var createSearchMask = function ($target) {
        var $searchContainer = $("<div class='searchContainer'></div").appendTo($target);
        var $logo = $("<div class='logo'></div").appendTo($searchContainer);
        var $geoSearch = $("<div id='geoSearch'></div>").appendTo($searchContainer);
        var $geoSearchInput = $("<input class='webgis-input' name='geoSearch' id='geoSearchInput' type='text' placeholder='" + options.search_placeholder + "'/>").appendTo($geoSearch);
        var $buttonStart = $("<button id='startSearch'>Suche</button>").appendTo($searchContainer);
        var $resultDiv = $("<div class='webgis-search-result-list'></div>").appendTo($target).hide();
        //$geoSearchInput.focus();

        $geoSearchInput.keydown(function (e) {
            // Bei Enter alle Suchergebnisse anzeigen (noch nichts ausgewählt)
            // Wenn ich mit Pfeil-Tasten im Autocomplete rauf / runterfahre und auf Enter klicke, wird "normales" on_select von webgis_control_search getriggert UND startSearch
            // => ich will aber nur jenes Enter, wenn ich nicht im Autocomplete stehe! => Einschränkung unterhalb: nur wenn kein Focus auf Autocomplete ist 
            if (e.which === 13 && $('#geoSearch').find('.tt-cursor').length === 0 && $("ul.ui-autocomplete").find("a.ui-state-focus").length === 0) {
                //console.log('startsearch');
                startSearch($geoSearchInput, $resultDiv);
            }
        });

        //var map = getMap($target);
        $geoSearchInput.webgis_control_search({
            search_service: options.search_service,
            //search_categories: options.search_category,
            on_select_get_original: true,
            on_select_get_original_raw: true,
            on_select: function (sender, feature, original) {
                //console.log('geosearch onselect')
                if (/*typeof (feature._html) != "undefined"*/original === false) {
                    // CSS-Klassen entfernen, damit gleicher Stil wie bei anderer Abfrage
                    var $myFeature = $(webgis.autocompleteItem2Html(feature)).removeClass();
                    $myFeature.find('*').each(function () {
                        $(this).removeClass();
                    });

                    // Feature zusammenstellen, damit gleiches Format wie bei anderer Abfrage
                    var result = {
                        "type": "FeatureCollection",
                        "features": [
                            {
                                "oid": "#service:#default:1",
                                "type": "Feature",
                                "geometry": {
                                    "type": "Point",
                                    "coordinates": feature.coords
                                },
                                "properties": {
                                    "_fulltext": $myFeature.prop('outerHTML'),
                                    "_details": "/rest/search/" + options.search_service + "?c=original&render_fields=true&item_id=" + feature.id
                                },
                                "bounds": feature.bbox
                            }
                        ]
                    };
                    showData(result, $resultDiv);
                }
            }
        });


        $geoSearchInput.focus(function (e) {
            if (isMobile.matches) {
                $(".logo")
                    .css("opacity", 0)
                    .animate(
                        { height: "0px" }
                    );
                $(".searchContainer").animate(
                    { top: (isMobile.matches ? "0px" : 0) }
                ).css({
                    'background-color': '#e0e0e0',
                    'padding-bottom': '5px',
                    'border-bottom': '1px solid #aaa'
                });
                $("#geoSearch").animate(
                    {
                        marginTop: "10px",
                        paddingTop:'10px',
                        marginLeft: (isMobile.matches ? "4px" : 0),
                        maxWidth: (isMobile.matches ? $(this).parent().width() + "px" : 0)
                    },
                    function () {
                        // Wenn mobil, zuerst in Animation den Wert hardcodiert berechnen, dann Stil mit Calc setzen
                        if (isMobile.matches) {
                            //$("#geoSearch").css("maxWidth", "calc(100% - 30px)")
                            webgis._autocompleteFitMenu($('#geoSearchInput'));
                        }
                    }
                );
                $("#startSearch").animate(
                    {
                        marginTop: "10px",
                        marginLeft: (isMobile.matches ? "4px" : 0),
                        marginRight: (isMobile.matches ? "4px" : 0),
                        float: "right"
                    }
                );
            }
        });

        $buttonStart.click(function () {
            startSearch($geoSearchInput, $resultDiv);
        });
    }


    var startSearch = function ($searchField, $resultField) {
        var data = webgis.hmac.appendHMACData({});
        data.term = $searchField.val();
        data.rows = 100;
        data.f = "geojson";
        $searchField.blur();
        webgis.ajax({
            url: webgis.baseUrl + '/rest/search/' + options.search_service,
            data: data,
            type: 'post',
            success: function (result) {
                showData(result, $resultField);
            },
            error: function (result) {
                alert(result);
            },
        });
    }


    var showData = function (result, $target) {
        $('#viewMap').empty();
        var animationTime = 500;
        $(".logo")
            .css("opacity", 0)
            .animate(
                { height: "0px" },
                animationTime
            );
        if (!isMobile.matches) {
            $(".searchContainer").animate(
                {
                    top: (isMobile.matches ? "30px" : 0)
                },
                animationTime
            );
        }

        if (result.features.length === 0) {
            $target.html("<div style='text-align:center;'>Suche lieferte kein Ergebnis ...</div>");
            //$target.html("Suche lieferte kein Ergebnis ...");
        } else {
            var $resultList = $("<ol class='webgis-geojuhu-results-list'></ol>");
            $target.html($resultList);

            for (var f in result.features) {
                var $tableHeader = $("<table class='webgis-geojuhu-result-table-header'><tr><td></td><td class='symbols'></td></tr></table>");
                $listItemMap = $("<div class='webgis-geojuhu-result-map mapsymbol' title='In Karte anzeigen'></div>").appendTo($tableHeader.find("td:eq(1)"));
                if (!isMobile.matches)
                    $listItemToWebgis = $("<div class='webgis-geojuhu-result-map webgissymbol' title='Zu WebGIS 4 springen'></div>").appendTo($tableHeader.find("td:eq(1)"));
                //$tableHeader.find("td:eq(1)").append($listItemMap).append($listItemToWebgis);
                $tableHeader.find("td:eq(0)").append(result.features[f].properties._fulltext);

                var feat = result.features[f];
                var $listItem = $("<li class='webgis-geojuhu-result-list'></li>")
                    .data("details", feat.properties._details)
                    //.data("coords", feat.geometry.coordinates)
                    .data("bounds", feat.bounds)
                    .html($tableHeader)
                    .appendTo($resultList);
                var $listItemDetail = $("<div class='webgis-geojuhu-result-detail'></div>").appendTo($listItem);
            }
        }
        $target.show();
        $target.animate(
            {
                opacity: "1",
                marginTop: "0px"
            },
            animationTime
        );

        $(".webgis-geojuhu-result-list").click(function () {
            showDetails($(this), "result");
        });

        $(".webgis-geojuhu-result-map.mapsymbol").click(function () {
            var url = $(this).closest("li").data('details');
            var id = (url + "&").match("item_id=(.*)\&")[1];
            goToMap(id, $(this).closest("li").data('bounds'));

            $(".webgis-geojuhu-result-list").removeClass("selected");
            $(this).closest("li").addClass("selected");
            showDetails($(this).closest("li"), "map")
            return false;       // kein Event-Bubbling zu Klick auf darunterliegendes DIV, sondern manuell auslösen (Zeile oberhalb)
        });

        $(".webgis-geojuhu-result-map.webgissymbol").click(function () {
            var url = $(this).closest("li").data('details');
            // 	"/rest/search/elastic_allgemein@ccgis_default?c=original&render_fields=true&item_id=843ea2c33c13436c9e6cad119ceeef38.1839"
            var id = (url + "&").match("item_id=(.*)\&")[1];
            getMeta(id, function (result) {
                if (result.service.length > 0 && result.query.length > 0) {
                    var urlWebgisLinkCollection = options.link_collection.replace("{service}", result.service).replace("{query}", result.query);
                    //showLinkCollection(myData, result.query, id.split(".")[1]);
                    webgis.ajax({
                        url: urlWebgisLinkCollection,
                        type: 'get',
                        success: function (data) {
                            showLinkCollection(data, result.query, id.split(".")[1]);
                            //console.log(myData);
                        },
                        error: function (result) {
                            alert(result);
                        }
                    });
                }
            });

            $(".webgis-geojuhu-result-list").removeClass("selected");
            $(this).closest("li").addClass("selected");
            showDetails($(this).closest("li"), "map");
            return false;       // kein Event-Bubbling zu Klick auf darunterliegendes DIV, sondern manuell auslösen (Zeile oberhalb)
        });

        // Wenn Desktop: Gleich nach Suche => erstes Ergebnis in Karte + Details öffnen
        if (isMobile.matches === false)  
            $(".webgis-geojuhu-result-map.mapsymbol:first").click();
        else {  // Bei Mobil nur Details (ohne Karte)
            $(".webgis-geojuhu-result-list:first").click();
        }
    }


    var showDetails = function ($listItem, caller) {
        var detailUrl = $listItem.data('details');
        var $details = $listItem.find(".webgis-geojuhu-result-detail");

        // Ajax NUR, wenn noch leer
        if ($details.html().length > 0) {
            // Wenn bereits ausgefahren und Klick von Kartensymbol => nicht wieder einfahren
            if ($details.is(":visible") === true && caller === "map") { }
            else
                $details.slideToggle();
        } else {
            webgis.ajax({
                url: webgis.baseUrl + detailUrl,
                data: webgis.hmac.appendHMACData({}),
                type: 'get',
                success: function (result) {
                    $details.empty();

                    if (result.success === false) {
                        $details.html("Keine Details verfügbar");
                    } else {
                        if (result.features.length > 0) {
                            var feature = result.features[0], prop = feature.properties;
                            var $tool = $("<div>").css('text-align', 'right').css('padding', '10px 0px').appendTo($details);
                            if (feature.geometry.coordinates && feature.geometry.coordinates.length >= 2) {
                                $("<a target='_blank' class='webgis-button' href='https://maps.google.com/maps?daddr=" + feature.geometry.coordinates[1] + "," + feature.geometry.coordinates[0] + "&l='>Navigation</a>")
                                    .appendTo($tool);
                            }
                            var $table = $("<table class='webgis-result-table feature'></table>");

                            for (var p in prop) {
                                if (/*p !== "_fulltext"*/ p.indexOf('_') !== 0 && prop[p]) {
                                    $("<tr><td class='webgis-result-table-header'>" + p + "</td> <td class='webgis-result-table-cell'>" + prop[p] + "</td></tr>").appendTo($table);
                                }
                            }
                        } else {
                            $("<tr><td class='header'>" + "Keine Details verfügbar" + "</td></tr>").appendTo($table);
                        }
                        $details.append($table);
                    }
                    $details.slideToggle();
                },
                error: function (result) {
                    alert(result);
                }
            });
        }

        /*$details.click(function(e) {
            e.stopPropagation();        // nur Link-Klick möglich => kein Event-Bubbling zur Karte (weil DIV-Klick unterhalb dorthin führen würde)
        });*/
    };

    var goToMap = function (id, bounds) {
        // Service suchen => meta
        getMeta(id, function (result) {
            createMap($("#viewMap"), result.service, function (myMap) {
                if (result.service.length > 0) {
                    var selection = myMap.getSelection("query");
                    selection.setTargetQuery(myMap.getService(result.service), result.query, id.split(".")[1]);
                }
                myMap.zoomToBoundsOrScale(bounds, 1000);
            });
        });
    };

    // Service suchen => meta
    var getMeta = function (id, callback) {
        webgis.ajax({
            url: webgis.baseUrl + '/rest/search/' + options.search_service + "?c=item_meta&f=json&id=" + id,
            data: webgis.hmac.appendHMACData({}),
            type: 'get',
            success: function (result) {
                if (typeof callback === "function") {
                    callback(result);
                }
            },
            error: function (result) {
                alert(result);
            }
        });
    }

    var createMap = function ($target, service, myCallback) {
        $target.empty();
        if (isMobile.matches) {
            $('#viewSearch').hide();
            drawMap();
        } else {
            $('#viewSearch').animate(
                { width: ($(".myTable").width() - 10) / 2 },
                function () {
                    drawMap();
                }
            );
        }

        function drawMap() { 
            if (isMobile.matches)
                $target.addClass('webgis-container').width("100%");
            else
                $target.addClass('webgis-container').width($('#viewSearch').width());
            var $map = $("<div id='map'></div>").appendTo($target);
            //if (ua.indexOf("MSIE") > -1 || ua.indexOf("Trident") > -1 || ua.indexOf("Edge") > -1)
                //$map.css("height", $(".myTable").height()); // Höhe manuell setzen für IE
            $("<div id='backToSearch' class='buttonMap'></div>").appendTo($target);
            if (!isMobile.matches)
                $("<div id='fullScreen' class='buttonMap'></div>").appendTo($target);
            $("<div class='webgis-tool-button-bar shadow' data-tools='webgis.tools.navigation.currentPos' style='margin: 5px;top: 100px;margin:1px;position:absolute;'></div>").appendTo($target);
            $("<div id='search' style='position:absolute;right:0px;top:0px;z-index:99;text-align:right;'></div>").appendTo($target);

            $("<div style='z-index:9998;position:absolute;right:0px;width:320px;bottom:0px;height:24px;background:#aaa'><div style='position:absolute;left:24px;top:0px;bottom:0px;right:0px' id='hourglass'></div></div>").appendTo($target);

            // Gesuchten Dienst hinzufügen, falls noch nicht dabei
            var myOptions = {
                extent: options.map_options.extent,
                services: options.map_options.services
            };
            if (service.length > 0 && myOptions.services.indexOf(service) == -1)
                myOptions.services = myOptions.services + "," + service


            var map = webgis.createMap($map, myOptions);
            $target.data('map', map);

            map.ui.createHourglass('hourglass');
            var tabs = map.ui.createTabs('.webgis-container', {
                left: null, right: 0, bottom: 24, top: null, width: 320,
                add_presentations: true,
                add_settings: false,
                add_tools: true,
                add_queryResults: false,
                options_presentations: {
                    gdi_button: false
                }
            });    

            if (options.map_search_service != null && options.map_search_service.length > 0) {
                map.ui.createSearch('search', {
                    service: options.map_search_service,
                });
            }



            $("#backToSearch").click(function () {
                $('#viewSearch').show();
                if (isMobile.matches) {
                    $target.width(0);
                    $target.empty();
                } else {
                    $('#viewSearch').animate(
                        { width: $(".myTable").width() },
                        function () {
                            $target.empty();
                        }
                    );
                    $('#viewMap').animate({ width: 0 });
                }
            });

            $("#fullScreen").click(function () {
                if (!isMobile.matches) {
                    $('#viewSearch').toggle();
                    $(this).toggleClass("off");
                    if ($('#viewSearch').is(":visible") === false)                  
                        //$('#viewMap').width('100%');
                        $('#viewMap').width($(".myTable").width() - 10);
                    else 
                        $('#viewMap').width('50%');
                    getMap().invalidateSize();  // Karte an aktuelle DIV Größe anpassen (für jeden Browser)
                }
            });

            myCallback(map);
        }
    }

    var getMap=function() {
        return $("#viewMap").data('map');  
    }

    var showLinkCollection = function (data, query, oid) {
        var $webgisList = $("<div class='webgis-list' data-query'" + query + "' data-oid='" + oid + "'></div>");
        if (data.collection.length > 0) {
            data.collection.forEach(function (e, i) {
                var $webgisListItem = $("<div class='webgis-listitem'></div>").appendTo($webgisList);
                var $webgisListItemHeader = $("<h2 class='webgis-link-collection-header'>" + e.name + "</div>").appendTo($webgisListItem);
                var $webgisLinkContainer = $("<div class='webgis-link-container'></div>").appendTo($webgisListItem);
                e.links.forEach(function (h, j) {
                    var url = h.href.replaceUrlParam("mode", "noselect").replaceUrlParam("abfragethema", query).replaceUrlParam("oid", oid);
                    var $anchor = $("<a class='webgis-link' href='" + url + "' target='_blank'></a>").appendTo($webgisLinkContainer);
                    var $tile = $("<div class='webgis-link-tile'></div>").appendTo($anchor);
                    //var $tile = $("<div class='webgis-link-tile' data-url='" + h.href + "'></div>").appendTo($webgisLinkContainer);
                    var $tileImage = $("<div class='webgis-link-tile-image'></div>").appendTo($tile);
                    if (h.thumbnail)
                        $tileImage.css("background-image", "url(" + h.thumbnail + ")");
                    var $tileText = $("<div class='webgis-link-tile-text'></div>").appendTo($tile);
                    var $tileHeading = $("<div class='webgis-link-tile-heading'>" + h.name + "</div>").appendTo($tileText);
                    var $tileDesc = $("<div class='webgis-link-tile-desc'>" + h.description + "</div>").appendTo($tileText);
                });
            });
        } else {
            $("<span>Keine Links verfügbar</span").appendTo($webgisList);
        }

        $('body').webgis_modal({
            title: 'WebGIS 4 Aufrufe',
            width: '80%', height: '80%',
            onload: function ($content) {
                //$content.css('padding', '10px');
                $content.html($webgisList);

                /*
                // die Redirections in der href-URL auflösen
                $(".webgis-link").click(function() {
                    var $url = $(this);
                    var query = $("webgis-list").data("query");
                    var oid = $("webgis-list").data("oid");
                    resolveWebgisRedirection($(this).attr("href"), function(resolvedUrl) {
                        console.log("final url:   " + resolvedUrl);
                        $url.attr("href", resolvedUrl);
                        $url.click();
                    });
                    console.log("fertisch");
                    return false;       // kein Event-Bubbling zu Klick auf darunterliegendes DIV, sondern manuell auslösen (Zeile oberhalb)
                });
                */
            }
        });
    }

    /*
    var resolveWebgisRedirection = function (url, callback) {
        webgis.ajax({
            url: url +  "&sidformat=json",
            data: webgis.hmac.appendHMACData({}),
            type:'get',
            success:function(result) {
                if (result.redirect[0].url.indexOf("SN") > -1) {
                    resolveWebgisRedirection(result.redirect[0].url, callback);
                } else {
                    if(typeof callback == "function"){
                        callback(result.redirect[0].url);
                    }                    
                }
            },
            error:function(result) {
                console.log("errrorrrr");
                alert(result);
            }
        });          
    }
    */

    String.prototype.replaceUrlParam = function (paramName, paramValue) {
        if (paramValue == null)
            paramValue = '';

        var pattern = new RegExp('\\b(' + paramName + '=).*?(&|$)');
        if (this.search(pattern) >= 0) {
            return this.replace(pattern, '$1' + paramValue + '$2');
        }
        return this + (this.indexOf('?') > 0 ? '&' : '?') + paramName + '=' + paramValue;
    };

})(webgis.$ || jQuery);