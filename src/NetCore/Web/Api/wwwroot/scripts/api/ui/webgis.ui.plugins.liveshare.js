// WebGIS UI Plugins (liveshare)
// webgis_liveshare: liveshare session panel (clients, share options, short messages)
// Element builder: webgis.ui.builder.misc.js ('liveshare')

webgis.ui.definePlugin('webgis_liveshare', {
    defaults: {
        map: null
    },
    init: function () {
        const $ = this.$, $elem = this.$el.addClass('webgis-liveshare-container');

        const $clients = $("<ul>")
            .addClass('liveshare-users')
            .appendTo($elem);

        this._onClientJoined = function (channel, client) {
            $("<li><div class='username'>" + client.clientId + "</div><div class='connection-id'>" + client.connectionId + "</div></li>")
                .attr('data-connection-id', client.connectionId)
                .css('background-image', 'url(' + webgis.baseUrl + '/rest/usermarkerimage?id=' + webgis.encodeURI(client.clientId) + '&width=26)')
                .addClass('liveshare-user-item')
                .appendTo($clients);
        };
        this._onClientLeft = function (channel, client) {
            $clients.find("li[data-connection-id='" + client.connectionId + "']").remove();
        };
        this._onLeftSession = function () {
            webgis.log('onleftsession');
            $clients.empty();
        };
        webgis.liveshare.events.on('onclientjoined', this._onClientJoined);
        webgis.liveshare.events.on('onclientleft', this._onClientLeft);
        webgis.liveshare.events.on('onleftsession', this._onLeftSession);

        $("<div><input type='checkbox' id='liveshare-share-mapextent'><label for='liveshare-share-mapextent' class='webgis-input-label'>Kartenauschnitt teilen</label><div>")
            .addClass('webgis-input-group')
            .appendTo($elem);
        $("<div><input type='checkbox' id='liveshare-share-layervisibility'><label for='liveshare-share-layervisibility' class='webgis-input-label'>Themenschaltung teilen</label></div>")
            .addClass('webgis-input-group')
            .appendTo($elem);

        const chkMapExtent = $elem.find('#liveshare-share-mapextent');
        const chkLayerVisibility = $elem.find('#liveshare-share-layervisibility');

        chkMapExtent.prop('checked', webgis.liveshare.allowShareExtent);
        chkLayerVisibility.prop('checked', webgis.liveshare.allowShareLayerVisibility);

        chkMapExtent.on('click.webgis_liveshare', function () {
            webgis.liveshare.allowShareExtent = $(this).prop('checked');
        });
        chkLayerVisibility.on('click.webgis_liveshare', function () {
            webgis.liveshare.allowShareLayerVisibility = $(this).prop('checked');
        });

        const $messages = $("<div>")
            .addClass('webgis-liveshare-messages')
            .appendTo($elem);

        $("<input placeholder='Kurznachricht...'>")
            .addClass("webgis-input")
            .insertBefore($messages)
            .on('keypress.webgis_liveshare', function (e) {
                if (e.which == 13) {
                    const message = $(this).val();

                    if (message) {
                        $("<div><div>Ich:</div><div class='message'>" + message + "</div></div>")
                            .addClass('self')
                            .prependTo($messages);

                        webgis.liveshare.emit({ command: 'text-message', args: { message: message } });
                    }

                    $(this).val('');
                }
            });

        this._onReceive = function (channel, result) {
            if (result && result.message && result.message.command === "text-message" && result.message.args) {
                $("<div><div>" + result.clientId + ":</div><div class='message'>" + result.message.args.message + "</div></div>")
                    .addClass('client')
                    .prependTo($messages);
            }
        };
        webgis.liveshare.events.on('onreceive', this._onReceive);
    },
    destroy: function () {
        const events = webgis.liveshare && webgis.liveshare.events;
        if (events) {
            events.off('onclientjoined', this._onClientJoined);
            events.off('onclientleft', this._onClientLeft);
            events.off('onleftsession', this._onLeftSession);
            events.off('onreceive', this._onReceive);
        }
        this.$el.find('*').off('.webgis_liveshare');
    }
});
