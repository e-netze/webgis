webgis.liveshare = new function () {
    "use strict"

    webgis.implementEventController(this);

    var __liveshareToolId = 'webgis.tools.serialization.livesharemap';

    var _firstConnection = true;
    var _connection = null;
    var _sessionId = null;
    var _isSessionOwner = false;
    var _map = null;
    var _progressMessage = "Warte auf Bestätigung...";
    var _simpleGroupDict = [];

    var _suspendNextMapRefresh = false;
    var _suspendNextBaseMapChangedEvent = false;
    var _suspendSendGraphicsOnChange = false;

    var _sessionGraphics = null;
    var _clientGraphics = null;

    var _userMarkers = [];

    var _onError = function (error) {
        if (error && error.errorMessage) {
            webgis.alert('LiveShare Error: ' + error.errorMessage, "error");
        }
    };

    var _anonymousClientname = webgis.localStorage.get('liveshare_anonymous_clientname');

    var _clientName = function () {

        return _anonymousClientname || webgis.clientName() || 'Anonymous';
    }

    var _setUserMarkerPosition = function (connectionId, clientId, latLng, text) {
        if (!_userMarkers[connectionId]) {

            var draggable = true; //_connection.connectionId === connectionId;

            _userMarkers[connectionId] = webgis.createMarker({
                lat: latLng.lat || latLng[0],
                lng: latLng.lng || latLng[1],
                draggable: draggable,
                size: 60,
                username: clientId,
                icon: "liveshare_user"
            });

            _userMarkers[connectionId].addTo(_map.frameworkElement);
            _userMarkers[connectionId]._markerId = connectionId;

            if (draggable) {
                _userMarkers[connectionId].on('drag', function () {
                    var latlng = this.getLatLng();
                    _emitUserMarkerPosition(this._markerId, latlng);
                }, _userMarkers[connectionId]);


            }
            if (connectionId == _connection.connectionId) {
                _map.addMarkerPopup(_userMarkers[connectionId],
                    {
                        text: (text || '<strong>' + clientId + '</strong>') +'<br/><br/>',
                        buttons: [
                            {
                                label: 'Aktuelle Position',
                                onclick: function (map) {
                                    webgis.tools.onButtonClick(_map, { type: 'clientbutton', command: 'currentpos' });
                                }
                            },
                            {
                                label: 'Entfernen',
                                onclick: function (_map, marker) {
                                    if (_connection && _connection.connectionId) {
                                        _removeUserMarker(_connection.connectionId);
                                    } else {
                                        _map.removeMarker(marker);
                                    }
                                }
                            }
                        ]
                    });
            } else {
                _map.addMarkerPopup(_userMarkers[connectionId],
                    {
                        text: (text || '<strong>' + clientId + '</strong>') + '<br/><br/>',
                        buttons: [
                            //{
                            //    label: 'Aktuelle Position',
                            //    onclick: function (map) {
                            //        webgis.tools.onButtonClick(_map, { type: 'clientbutton', command: 'currentpos' });
                            //    }
                            //},
                            {
                                label: 'Entfernen',
                                onclick: function (_map, marker) {
                                    if (_connection && _connection.connectionId) {
                                        _removeUserMarker(marker._markerId);
                                    } else {
                                        _map.removeMarker(marker);
                                    }
                                }
                            }
                        ]
                    });
            }
        } else {
            _userMarkers[connectionId].setLatLng(latLng);
        }

        if (_connection && _connection.connectionId === connectionId) {
            _emitUserMarkerPosition(connectionId, latLng);
        }

        return _userMarkers[connectionId];
    };
    var _emitUserMarkerPosition = function(markerId, latLng) {
        if (_connection && window.livesharehub) {
            window.livesharehub.emitMessage(JSON.stringify({
                command: 'usermarker-pos',
                args: {
                    latlng: [
                        latLng.lat || latLng[0],
                        latLng.lng || latLng[1]
                    ],
                    markerId: markerId
                }
            }), function () { });
        }
    };
    var _removeUserMarker = function (markerId) {
        var userMarkers = []

        for (var i in _userMarkers) {
            if (markerId && markerId != i) {
                userMarkers.push(_userMarkers[i]);
                continue;
            }

            var marker = _userMarkers[i];
            _map.frameworkElement.removeLayer(marker);
        }

        _userMarkers = userMarkers;
    }
    var _hasClientMarkers = function () {
        for (var markerId in _userMarkers) {
            if (markerId !== _connection.connectionId)
                return true;
        }

        return false;
    }

    this.init = function (map, hubUrl) {
        if (_map === null) {
            // init events
            _map = map;
            _map.events.on('refresh', function (channel, sender) {
                if (_suspendNextMapRefresh == true) {
                    _suspendNextMapRefresh = false;
                    //console.log('suspendNextMapRefresh');
                    return;
                }

                if (webgis.liveshare.allowShareExtent) {
                    if (_sessionId && window.livesharehub) {
                        window.livesharehub.emitMessage(JSON.stringify({
                            command: 'refreshmap',
                            args: {
                                center: sender.getCenter(),
                                scale: sender.scale()
                            }
                        }), function () { });
                    }
                }
            });

            _map.events.on('graphics_changed', function (channel, sender) {
                if (webgis.liveshare.allowShareGraphics && _suspendSendGraphicsOnChange === false) {
                    if (_sessionId && window.livesharehub) {

                        var geoJson = _map.graphics.toGeoJson(true);
                        // ToDo: Hier könnte man noch unterscheiden, welche Objekte dazu gekommen sind (_sessionGraphics nachher immer merken und vergleiche => nur neue Schicken... Was tun, wenn was gelöcht wurde?)

                        var graphicsMessage = JSON.stringify({
                            command: 'graphics_changed',
                            args: {
                                geojson: geoJson,
                                replaceelements: true,
                                suppressZoom: true
                            }
                        });

                        window.livesharehub.emitMessage(graphicsMessage, function () { });
                    }
                }
            });

            _map.events.on('ativeclienttool-click', function (channel, sender, ev) {
                if (_connection !== null &&
                    sender.getActiveTool().id === __liveshareToolId) {
                    _setUserMarkerPosition(_connection.connectionId,
                                           _clientName(),
                                           [ev.world.lat, ev.world.lng]);
                }
                //console.log('ativeclienttool-click', sender.getActiveTool().id, ev);
            });

            for (var s in _map.services) {
                var service = _map.services[s];
                service.events.on('onchangevisibility_liveshare', function (channel, sender) {
                    var layerIds = sender.getLayerVisibility();

                    if (webgis.liveshare.allowShareLayerVisibility) {
                        if (_sessionId && window.livesharehub) {
                            window.livesharehub.emitMessage(JSON.stringify({
                                command: 'service_visibility',
                                args: {
                                    serviceId: sender.id,
                                    layerIds: layerIds
                                }
                            }), function () { });
                        }
                    }
                });
            }
        }

        webgis.require("signalr", function () {
            if (_connection === null) {

                _setDialogTitle('Connecting to hub...');
                _connection = new signalR.HubConnectionBuilder()
                    .withUrl(hubUrl + '/signalrhub')
                    .withAutomaticReconnect()
                    .build();

                _connection.start().then(function () {
                    console.log('liveshare hub connection started...');
                    _setDialogTitle('');

                    if (_firstConnection == true) {
                        _firstConnection = false;

                        // ein bisserl ein unschöner Hack...
                        if (webgis.getUrlParameter('liveshare_session')) {
                            webgis.delayed(function () {
                                if ($('.uibutton-join-liveshare').length === 1) {
                                    $("#liveshare-sessionid").val(webgis.getUrlParameter('liveshare_session'));
                                    $('.uibutton-join-liveshare').trigger('click');
                                }
                            }, 500);
                        }
                    }

                    webgis.require("liveshare", function () {
                        //console.log('connection', _connection);

                        livesharehub.initHubClient(
                            _connection,
                            {
                                onReceiveMessage: function (result) {
                                    //console.log('onReceiveMessage', result);

                                    try {
                                        result.message = JSON.parse(result.message);
                                        webgis.liveshare.events.fire('onreceive', result);

                                        switch (result.message.command) {
                                            case 'refreshmap':
                                                if (webgis.liveshare.allowShareExtent) {
                                                    _suspendNextMapRefresh = true;
                                                    _map.setScale(result.message.args.scale, result.message.args.center);
                                                }
                                                break;
                                            case 'basemap_changed':
                                                if (webgis.liveshare.allowShareExtent) {
                                                    _suspendNextBaseMapChangedEvent = true;
                                                    _map.setBasemap(result.message.args.id);
                                                }
                                                break;
                                            case 'graphics_changed':
                                                if (webgis.liveshare.allowShareGraphics) {
                                                    _suspendSendGraphicsOnChange = true;

                                                    if (!livesharehub.isOwner(_sessionId) && _clientGraphics === null) {  // Save current graphics on first event in this session
                                                        _suspendSendGraphicsOnChange = true;
                                                        _clientGraphics = _map.graphics.toGeoJson();
                                                        //console.log('save client graphics...', _clientGraphics);
                                                        _suspendSendGraphicsOnChange = false;
                                                    }

                                                    _map.graphics.fromGeoJson(result.message.args);
                                                    _suspendSendGraphicsOnChange = false;
                                                }
                                                break;
                                            case 'service_visibility':
                                                if (webgis.liveshare.allowShareLayerVisibility) {
                                                    var service = _map.getService(webgis.liveshare.overrides.parseServiceId(result.message.args.serviceId));
                                                    if (service) {
                                                        if (service.isBasemap === true) {
                                                            _map.setBasemap(result.message.args.layerIds.length === 0 ? null : service.id, service.basemapType === 'overlay', /* fireLiveshareEvent */false);

                                                            //
                                                            // UI Mitziehen 
                                                            // (leider ein bisserl ein 'Hack'. Wenn einmal Zeit ist webgis_presentationToc umzuschreiben ;)... kann das hier verschwinden) 
                                                            //
                                                            if (service.basemapType === 'normal') {
                                                                $(_map._webgisContainer)
                                                                    .find('li.webgis-presentation_toc-basemap-item.webgis-presentation_toc-basemap-item-block')
                                                                    .find('.webgis-presentation_toc-basemap-item-img.selected')
                                                                    .removeClass('selected');

                                                                if (result.message.args.layerIds.length !== 0) {
                                                                    $(_map._webgisContainer).find('li.webgis-presentation_toc-basemap-item.webgis-presentation_toc-basemap-item-block')
                                                                        .each(function (i, e) {
                                                                            if (e.service && e.service.id === service.id) {
                                                                                $(e).find(".webgis-presentation_toc-basemap-item-img").addClass('selected');
                                                                            }
                                                                        });
                                                                }
                                                            } else if (service.basemapType === 'overlay') {
                                                                $(_map._webgisContainer)
                                                                    .find('.webgis-presentation_toc-item.webgis-presentation_toc-basemap-item.webgis-presentation_toc-basemap-overlay.webgis-display-block')
                                                                    .each(function (i, e) {
                                                                        if (e.service && e.service.id === service.id) {
                                                                            if (result.message.args.layerIds.length !== 0) {
                                                                                $(e).addClass('checked').find('img').attr('src', webgis.css.imgResource("check1.png", "toc"));;
                                                                            } else {
                                                                                $(e).removeClass('checked').find('img').attr('src', webgis.css.imgResource("check0.png", "toc"));;
                                                                            }
                                                                        }
                                                                    })
                                                            }
                                                            //////
                                                        } else {
                                                            service.setServiceVisibilityDelayed(result.message.args.layerIds, false);
                                                        }
                                                    }
                                                }
                                                break;
                                            case 'usermarker-pos':
                                                var clientId = result.clientId;
                                                var pos = result.message.args.latlng;

                                                _setUserMarkerPosition(result.message.args.markerId, clientId, pos);
                                                break;
                                        }
                                    } catch (e) { console.log('exeception', e) }
                                },
                                onClientJoinedGroup: function (result) {
                                    //console.log('onClientJoinedGroup', result);
                                    if (_isSessionOwner) {
                                        // Owner Syncs (Extent, Layers, MapMarkup, ...)
                                        webgis.liveshare.sync(result.connectionId);
                                    }

                                    //everone syncs his userMarker
                                    if (_userMarkers[_connection.connectionId]) {
                                        _emitUserMarkerPosition(
                                            _connection.connectionId,
                                            _userMarkers[_connection.connectionId].getLatLng());
                                    }

                                    _sessionGraphics = _clientGraphics = null;
                                    webgis.liveshare.events.fire('onclientjoined', result);
                                },
                                onClientLeftGroup: function (result) {
                                    //console.log('onClientLeftGroup', result);

                                    _removeUserMarker(result.connectionId);

                                    webgis.liveshare.events.fire('onclientleft', result);
                                },
                                onReceiveClientInfo: function (result) {
                                    //console.log('onReceiveClientInfo', result);

                                    webgis.liveshare.events.fire('onclientjoined', result);
                                },
                                onJoinedGroup: function (result) {
                                    webgis.hideProgress(_progressMessage);
                                    _sessionId = result.groupId;
                                    _setDialogTitle('LiveID: ' + (_simpleGroupDict[result.groupId] || result.groupId));
                                    webgis.liveshare.events.fire('onclientjoined', result);
                                },
                                onDeniedGroup: function (result) {
                                    webgis.hideProgress(_progressMessage);
                                    webgis.alert("Zugriff auf Liveshare Session wurde verweigert!", "info");
                                    _sessionId = null;
                                    _setDialogTitle('');
                                },
                                onLeftGroup: function () {
                                    if (_clientGraphics && _clientGraphics.features && _clientGraphics.features.length > 0) {
                                        var currentGraphics = _map.graphics.toGeoJson();
                                        if (currentGraphics && currentGraphics.features && currentGraphics.features.length > 0) {
                                            webgis.confirm({
                                                title: 'Liveshare',
                                                message: "Vor der LiveShare Session befanden sich in der aktuellen Karte Zeichnungselement aus Map-Markup. Möchten Sie die Zeichnungs- (Map-Markup) Objekte aus der Live Share Session behalten, oder die ursprüglichen Element wieder herstellen?",
                                                cancelText: 'LiveShare Objekte behalten',
                                                onCancel: function () { _clientGraphics = null; },
                                                okText: 'Ursprügliche Element wiederherstellen',
                                                onOk: function () {
                                                    _map.graphics.fromGeoJson({
                                                        geojson: _clientGraphics, replaceelements: true
                                                    });
                                                    _clientGraphics = null;
                                                }
                                            });
                                        } else {
                                            _map.graphics.fromGeoJson({ geojson: _clientGraphics, replaceelements: true });
                                            _clientGraphics = null;
                                        }
                                    }

                                    webgis.liveshare.events.fire('onleftsession', this);
                                    _sessionId = null;
                                    _removeUserMarker(); // any

                                    _setDialogTitle('');
                                },
                                onGroupRemoved: function (groupId, isOwner) {
                                    //onLeftGroup();

                                    if (isOwner === false) {
                                        webgis.alert("Liveshare Session wurde geschlossen", "info");
                                    }
                                },
                                onConfirmJoinGroup: function (result, onAllow, onDeny) {
                                    //console.log('onConfirmJoinGroup', result);
                                    webgis.confirm({
                                        title: 'Liveshare',
                                        iconUrl: webgis.css.imgResource('fav-100.png'),
                                        message: "Benutzer " + result.clientId + " Zutritt zur aktuellen Sesseion erteilen?",
                                        cancelText: 'Nein',
                                        onCancel: onDeny,
                                        okText: 'Ja',
                                        onOk: onAllow
                                    });
                                },
                                onError: _onError,
                                clientId: function () {
                                    return _clientName();
                                }
                            }
                        );
                    }, hubUrl);
                }).catch(function (err) {
                    _connection = null;
                    return webgis.alert("Liveshare: " + err.toString(), "error");
                });


            }
        });
    };
    this.sync = function (connectionId) {
        _map.events.fire('refresh', _map);
        var hasBasemap = _map.currentBasemapServiceId();

        for (var s in _map.services) {
            var service = _map.services[s];
            if (hasBasemap && service.isBasemap && service.basemapType === "normal" && _map.currentBasemapServiceId() !== service.id) {
                continue;
            }
            service.events.fire('onchangevisibility_liveshare', service);
        }
        _map.events.fire('graphics_changed');
    };
    this.emit = function (jsonObject, callback) {
        if (window.livesharehub) {
            livesharehub.emitMessage(JSON.stringify(jsonObject), callback);
        }
    };
    this.joinSession = function (sessionId) {
        if (sessionId.indexOf('{') == 0) {
            var session = JSON.parse(sessionId);
            if (livesharehub.addGroup(session)) {
                sessionId = livesharehub.getGroupId();
                if (session.simpleGroupId) {
                    _simpleGroupDict[sessionId] = session.simpleGroupId;
                }
            } else {
                wegbis.alert("Fehler beim Erstellen/Hinzufügen der Gruppe...", "Error");
            }
        }
        if (_sessionId) {
            this.leaveSession();
        }

        if (_connection) {
            _isSessionOwner = livesharehub.isOwner(sessionId);

            webgis.delayed(function () {
                webgis.showProgress(_progressMessage, null, {
                    cancel: function () {
                        // ToDo: Cancel 
                    }
                });
                livesharehub.requestJoin(sessionId, _clientName());
            }, _isSessionOwner ? 1 : 500);  // Verzögert, damit progress nicht sofort schließt. In der Regel ist nämlich noch der ToolPrgress offen...
        }
    };
    this.leaveSession = function () {
        if (_sessionId) {
            livesharehub.leave(_sessionId, _clientName(), function () {
                
            });
        }
        _sessionId = null;
    };

    this.sessionId = function () { return _sessionId; }
    this.hasHubConnection = function () {
        return _connection != null && typeof _connection.connectionId === 'string';
    };
    this.isSessionOwner = function() { return _sessionId && window.livesharehub && livesharehub.isOwner(_sessionId); }

    this.closeConnection = function () {
        if (_connection != null) {
            _setDialogTitle('Disconnect from hub...');
            _connection.stop().then(function () {
                console.log('liveshare hub connection stopped...');
                _connection = null;
                _setDialogTitle('');
            }).catch(function (err) {
                return webgis.alert("Liveshare: " + err.toString(), "error");
            });
        }
    };

    this.allowShareExtent = true;
    this.allowShareLayerVisibility = true;
    this.allowShareGraphics = true;

    this.setAnonymousClientname = function (clientName) {
        _anonymousClientname = clientName;
        webgis.localStorage.set('liveshare_anonymous_clientname', clientName);
    };
    this.getClientname = function () {
        return _anonymousClientname || webgis.clientName();
    };

    this.isInitialized = function () {
        return _map !== null && _connection !== null && _connection.connectionId;
    };
    this.setCurrentPosMarker = function () {
        _removeUserMarker(_connection.connectionId);

        var cancelTracker = new webgis.cancelTracker();
        webgis.showProgress('Aktuelle Position wird abgefragt...', null, cancelTracker);

        webgis.currentPosition.get({
            highAccuracy: false,
            maxWatch: webgis.currentPosition.maxWatch,
            onSuccess: function (pos) {
                webgis.hideProgress('Aktuelle Position wird abgefragt...');
                if (!cancelTracker.isCanceled()) {
                    var lng = pos.coords.longitude, lat = pos.coords.latitude, acc = pos.coords.accuracy / (webgis.calc.R) * 180.0 / Math.PI;

                    if (_hasClientMarkers() === false) {
                        // Wenn keine anderen Marker von Clients verhanden sind, dann auf die 
                        // aktuelle Position zoomen
                        _map.zoomTo([lng - acc, lat - acc, lng + acc, lat + acc]);
                    }
                    var marker = _setUserMarkerPosition(_connection.connectionId,
                                                        _clientName(),
                                                        [lat, lng],
                                                        "<h3>Mein Standort</h3>Genauigkeit: " + pos.coords.accuracy + "m<br/>");
                }
            },
            onError: function (err) {
                webgis.hideProgress('Aktuelle Position wird abgefragt...');
                webgis.alert('Ortung nicht möglich: ' + (err ? err.message + " (Error Code " + err.code + ")" : ""), "error");
            }
        });
    };

    this.removeUserMarker = function () {
        if (_connection && _connection.connectionId) {
            _removeUserMarker(_connection.connectionId);
        }
    };

    this.overrides = {
        parseServiceId: function(serviceId) {
            return serviceId;
        }
    }

    // Helper

    var _setDialogTitle = function (title) {
        if (_map) {
            _map.ui.webgisContainer().webgis_dockPanel('set_title', { title: title || 'Live Share Connect', id: __liveshareToolId + "_aside" });
            _map.ui.refreshUIElements();
        }
    }
};