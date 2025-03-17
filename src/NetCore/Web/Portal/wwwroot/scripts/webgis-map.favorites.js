(function ($) {
    "use strict";
    $.fn.webgis_favoritesTocContainer = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on jQuery.webgis_presentationToc');
        }
    };

    var defaults = {
        relPathPrefix:'',
        toolid:'webgis.tools.serialization.userfavoritiesmappresentations',
        pageId: '',
        mapName: ''
    };

    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new init(options);
            });
        }
    };

    var init = function (options) {
        
        if ($.fn.webgis_presentationToc && $('.webgis-presentation_toc-holder').length === 1) {
            webgis.ajax({
                progress: 'Lade User Darstellungsvarianten',
                url: webgis.url.relative(options.relPathPrefix + '../proxy/toolmethod/' + options.toolid.replaceAll('.', '-') + '/get-favorite-presentations'),
                type: 'post',
                data: { page: options.pageId, map: options.mapName },
                success: function (result) {
                    var $customContainer = $('.webgis-presentation_toc-holder').webgis_presentationToc('createCustomContainer', { containerName: 'Meine Darstellungsvarianten' });
                    initUI($customContainer, options, result)
                }
            });
        }
    }

    var refresh = function(parent) {
        var options = $(parent).data('options');

        init(options);
    }

    var initUI = function(parent, options, presentations) {
        var $parentList = $(parent).empty();
        $parentList.data('options', options);

        if (!$parentList.hasClass('webgis-user-favorites-presentation-holder')) {
            options.map.events.on('refresh-user-favorite-presenation', function () {
                refresh(this)
            }, $parentList);
            options.map.events.on('maptitlechanged', function () {
                options.mapName = options.map.ui.getMapTitle();
                refresh(this);
            }, $parentList);
        }
        $parentList.addClass('webgis-user-favorites-presentation-holder');

        var $addButton = $("<div class='uibutton-cancel webgis-button uibutton'>Neue Darstellungsvariante...</div>")
            .appendTo($("<li>").appendTo($parentList))
            .click(function () {
                refresh(parent);
                webgis.tools.onButtonClick(options.map, {
                    id: options.toolid,
                    type: 'servertoolcommand_ext',
                    command: 'create-new',
                }, null, null, { page: options.pageId, map: options.mapName });
            });

        for (var p in presentations) {
            var presentation = presentations[p];
            if (!presentation.visibility)
                continue;

            var $li = $("<li>")
                .css({ position:'relative' })
                .addClass('webgis-presentation_toc-item webgis-presentation_tol-custom-item')
                .data('pesentation', presentation)
                .data('map', options.map)
                .appendTo($parentList)
                .click(function (e) {
                    e.stopPropagation();

                    var presentation = $(this).data('pesentation');
                    var map = $(this).data('map');

                    // Alle Dienste ausschalten
                    var allServiceIds = map.serviceIds();
                    for (var s in allServiceIds) {
                        var service = map.getService(allServiceIds[s]);
                        if (service && !service.isBasemap) {
                            service.setServiceVisibility([]);
                        } 
                    }

                    for (var s in presentation.visibility) {
                        var layers = presentation.visibility[s];

                        var service = map.getService(s);
                        if (service) {
                            service.setServiceVisibilityPro2(layers);
                        } else if (presentation.visibility[s] && presentation.visibility[s].length > 0) {  // addService
                            loadServiceInfo([s], function (result) {
                                if (result.services) {
                                    //console.log('addservice', result);
                                    map.addServices(result.services);
                                    for (var i in result.services) {
                                        var service = map.getService(result.services[i].id);
                                        var layers = presentation.visibility[service.id];
                                        service.setServiceVisibilityPro2(layers);
                                    }
                                }
                            });
                        }
                    }
                });
            var $span = $("<span>").addClass('webgis-search-content active').appendTo($li);
            $("<img>").css({ width: '16px', marginRight:'10px' }).attr('src', webgis.css.imgResource("layers-16.png", "toc")).appendTo($span);
            $("<span>").addClass('webgis-text-span nowrap').text(presentation.name).appendTo($span);
            $("<div>")
                .css({ position: 'absolute', right: '0px', top: '0px', width: '16px', height: '16px', color:'#aaa' })
                .text('✖')
                .appendTo($li)
                .click(function (e) {
                    e.stopPropagation();

                    var $li = $(this).closest('.webgis-presentation_toc-item');
                    var name = $li.find('.webgis-text-span').text();

                    webgis.confirm({
                        title: 'Meine Darstellungsvarianten',
                        message: 'Möchten sie die Darstellungsvariante \"' + name + '\" aus der Karte entfernen?',
                        onOk: function () {
                            webgis.ajax({
                                progress: 'Lade User Darstellungsvarianten',
                                url: webgis.url.relative(options.relPathPrefix + '../proxy/toolmethod/' + options.toolid.replaceAll('.', '-') + '/delete-favorite-presentations'),
                                type: 'post',
                                data: { page: options.pageId, map: options.mapName, name: name },
                                success: function (result) {
                                    $li.remove();
                                }
                            });
                        }
                    });
                });
        }
    }

    var loadServiceInfo = function (serviceIds, callback, presentation) {
        webgis.ajax({
            type: 'post',
            data: webgis.hmac.appendHMACData({ ids: serviceIds, f: 'json' }),
            url: webgis.baseUrl + '/rest/serviceinfo',
            success: function (result) {
                if (callback) {
                    callback(result);
                }
            }
        });
    }
})(webgis.$ || jQuery);


