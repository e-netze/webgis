var livesharehub = new function () {
    "use strict"

    var src = document.getElementById('livesharehub-script') ?
        document.getElementById('livesharehub-script').src.toLowerCase() :
        null;
    src = src ? src.substring(0, src.lastIndexOf('/js/')) : '';

    this.baseUrl = src;
    this.signalRHubUrl = function () {
        return this.baseUrl + '/signalrhub'
    };

    var _const = {
        onReceiveMessage: "ReceiveMessage",
        onClientJoinedGroup: "ClientJoinedGroup",
        onClientLeftGroup: "ClientLeftGroup",
        onReceiveClientInfo: "ReceiveClientInfo",
        onClientRequestsGroupPassword: "ClientRequestsGroupPassword",
        onReceiveGroupClientPassword: "ReceiveGroupClientPassword",
        onJoinGroupDenied: "JoinGroupDenied",
        onRemoveGroup: "RemoveGroup"
    };

    var _maxMessageSize = 30 * 1024;  // 30kb
    var _currentGroupIndex = -1;
    var _groups = [];
    var _clients = [];

    this.getGroupId = function () {
        if (_currentGroupIndex < 0 || _currentGroupIndex >= _groups.length)
            return null;

        return _groups[_currentGroupIndex].groupId;
    };
    this.addGroup = function (group) {
        if (group && group.groupId && !_getGroup(group.groupId)) {
            _groups.push(group);
            _currentGroupIndex = _groups.length - 1;
            return true;
        }
        return false;
    };
    this.isOwner = function (groupId) {
        var group = _getGroup(groupId);
        return (group != null && typeof group.groupOwnerPassword === 'string');
    };
    var _setGroupId = function (groupId) {
        for (var i in _groups) {
            if (_groups[i].groupId == groupId)
                return i;
        }

        _groups.push({ groupId: groupId });
        _currentGroupIndex = _groups.length - 1;
    };
    var _getGroup = function (groupId) {
        for (var i in _groups) {
            if (_groups[i].groupId == groupId)
                return _groups[i];
        }

        return null;
    }

    this.requestGroup = function (callback) {
        var xhttp = new XMLHttpRequest();
        xhttp.onreadystatechange = function () {
            if (this.readyState == 4 && this.status == 200) {
                livesharehub.addGroup(JSON.parse(this.responseText));

                callback();
            }
        };
        xhttp.open("GET", this.baseUrl + '/hubgroup', true);
        xhttp.send();
    };

    var _clientKey = function (groupId, connectionId, clientId) { return groupId + ":" + connectionId + ":" + clientId };
    var _clientIdFromConnectionId = function (connectionId) {
        for (var c in _clients) {
            if (_clients[c].connectionId === connectionId) {
                return _clients[c].clientId;
            }
        }
    }
    var _connection;
    var _options;

    this.initHubClient = function (connection, options) {
        _connection = connection;
        _options = options;

        //console.log(_connection);

        if (options.onReceiveMessage) {
            _connection.on(_const.onReceiveMessage, function (groupId, connectionId, message) {
                options.onReceiveMessage({
                    groupId: groupId,
                    connectoinId: connectionId,
                    message: message,
                    clientId: _clientIdFromConnectionId(connectionId)
                });
            });
        }
        if (options.onClientJoinedGroup) {
            _connection.on(_const.onClientJoinedGroup, function (groupId, connectionId, clientId) {
                if (!_clients[_clientKey(groupId, connectionId, clientId)]) {
                    var client = {
                        groupId: groupId,
                        connectionId: connectionId,
                        clientId: clientId
                    };
                    _clients[_clientKey(groupId, connectionId, clientId)] = client;

                    var currentClientId = typeof options.clientId === "string" ?
                        options.clientId :
                        options.clientId();

                    //console.log('my-clientid', currentClientId);

                    _connection.invoke("SendClientInfo", groupId, currentClientId, false, connectionId)
                        .then(function () {

                        })
                        .catch(function (err) {
                            return console.error(err.toString());
                        });

                    options.onClientJoinedGroup(client);
                }
            });
        }
        if (options.onClientLeftGroup) {
            _connection.on(_const.onClientLeftGroup, function (groupId, connectionId, clientId) {
                if (_clients[_clientKey(groupId, connectionId, clientId)]) {
                    var client = _clients[_clientKey(groupId, connectionId, clientId)];

                    options.onClientLeftGroup(client);

                    _clients[_clientKey(groupId, connectionId, clientId)] = null;
                }

                if (_connection.connectionId === connectionId) {  // removed
                    _leftGroup(groupId);
                }
            });
        }
        if (options.onReceiveClientInfo) {
            _connection.on(_const.onReceiveClientInfo, function (groupId, connectionId, clientId, isOwner) {
                if (!_clients[_clientKey(groupId, connectionId, clientId)]) {
                    var client = {
                        groupId: groupId,
                        connectionId: connectionId,
                        clientId: clientId
                    };
                    _clients[_clientKey(groupId, connectionId, clientId)] = client;

                    options.onReceiveClientInfo(client);
                }
            });
        }

        _connection.on(_const.onClientRequestsGroupPassword, function (groupId, connectionId, clientId) {
            console.log('onClientRequestsGroupPassword');
            var group = _getGroup(groupId);
            if (group && group.groupOwnerPassword && group.groupClientPassword) {
                if (options.onConfirmJoinGroup) {
                    options.onConfirmJoinGroup({
                        groupId: groupId,
                        connectionId: connectionId,
                        clientId: clientId
                    },
                        function () {
                            _connection.invoke("SendGroupClientPassword", groupId, connectionId, group.groupOwnerPassword, group.groupClientPassword);
                        },
                        function () {
                            _connection.invoke("DenyClientRequestJoinGroup", groupId, connectionId);
                            if (options.onDeniedGroup)
                                options.onDeniedGroup({ groupId: groupId });
                        });
                } else {
                    _connection.invoke("SendClientGroupPassword", groupId, connectionId, group.groupOwnerPassword, group.groupClientPassword);
                }
            }
        });

        _connection.on(_const.onReceiveGroupClientPassword, function (groupId, connectionId, clientPassword) {
            _join(groupId, options.clientId(), clientPassword, function () {
                //if (options.onJoinedGroup)
                //    options.onJoinedGroup({
                //        groupId: groupId
                //    });
            });
        });

        _connection.on(_const.onRemoveGroup, function (groupId) {
            var isOwner = livesharehub.isOwner(groupId);
            _leftGroup(groupId);

            if (options.onGroupRemoved)
                options.onGroupRemoved(groupId, isOwner);
        });
    };

    this.emitMessage = function (message, onEmited) {
        if (_connection && message && _currentGroupIndex >= 0) {
            if (message.length > _maxMessageSize) {
                _handleError({ errorMessage: 'Maxium message length exceeded', message: message });
                return;
            };
            _connection.invoke("EmitMessage", this.getGroupId(), message)
                .then(function () {
                    //console.log('message emitied', livesharehub.getGroupId(), message);
                    if (onEmited) {
                        onEmited(message);
                    }
                })
                .catch(function (err) {
                    _handleError({ errorMessage: err.toString(), message: message });
                });
        }
    };

    this.requestJoin = function (groupId, clientId) {
        if (_connection) {
            var group = _getGroup(groupId);
            if (group && group.groupClientPassword) {
                _join(groupId, clientId, group.groupClientPassword);
            } else {
                _connection.invoke("RequestJoinGroup", groupId, clientId);
            }
        }
    };

    var _join = function (groupId, clientId, clientPassword) {
        if (_connection) {
            _connection.invoke("JoinGroup", groupId, clientId, clientPassword)
                .then(function () {
                    _setGroupId(groupId);
                    console.log('jointed to', groupId);
                    if (_options.onJoinedGroup)
                        _options.onJoinedGroup({
                            groupId: groupId,
                            connectionId: _connection.connectionId,
                            clientId: clientId
                        });
                })
                .catch(function (err) {
                    _handleError({ errorMessage: err.toString() });
                });
        }
    };

    var _leftGroup = function (groupId) {
        var groups = [];
        _currentGroupIndex = -1;

        for (var i in _groups) {
            if (_groups[i] && _groups[i].groupId !== groupId) {
                groups.push(_groups[i]);
            }
        }
        _groups = groups;

        var clients = [];

        for (var c in _clients) {
            if (_clients[c] && _clients[c].groupId != groupId) {
                clients.push(_clients[c]);
            }
        }

        _clients = clients;

        if (_options.onLeftGroup)
            _options.onLeftGroup();

        console.log('leftGroup', _groups, _clients);
    };

    this.leave = function (groupId, clientId, onLeft) {
        if (_connection) {
            var group = _getGroup(groupId);
            if (group && group.groupOwnerPassword) {
                this.removeGroup(groupId);
            } else {
                _connection.invoke("LeaveGroup", groupId, clientId)
                    .then(function () {
                        _leftGroup(groupId);
                    })
                    .catch(function (err) {
                        _handleError({ errorMessage: err.toString(), message: message });
                    });
            }
        }
    };

    var _handleError = function (error) {
        if (_options && _options.onError) {
            _options.onError(error);
        } else {
            console.trace('livesharehub-error', error.errorMessage);
        }
    };

    this.removeClient = function (client) {
        if (_connection) {
            var group = _getGroup(client.groupId);
            if (group && group.groupOwnerPassword) {
                _connection.invoke("RemoveClient", client.groupId, client.connectionId, client.clientId, group.groupOwnerPassword)
                    .then(function () {
                    })
                    .catch(function (err) {
                        _handleError({ errorMessage: err.toString(), message: message });
                    });
            }
        }
    };

    this.removeGroup = function (groupId) {
        if (_connection) {
            var group = _getGroup(groupId);
            if (group && group.groupOwnerPassword) {
                _connection.invoke("RemoveGroup", groupId, group.groupOwnerPassword)
                    .then(function () {
                    })
                    .catch(function (err) {
                        _handleError({ errorMessage: err.toString(), message: message });
                    });
            }
        }
    };
};