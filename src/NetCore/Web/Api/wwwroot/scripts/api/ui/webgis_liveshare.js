(function ($) {
    "use strict";
    $.fn.webgis_liveshare = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_liveshare');
        }
    };
    var defaults = {
        map: null
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        }
    };
    var initUI = function (elem, options) {
        var $elem = $(elem).addClass('webgis-liveshare-container');

        var $clients = $("<ul>")
            .addClass('liveshare-users')
            .appendTo($elem);

        webgis.liveshare.events.on('onclientjoined', function (channel, client) {
            $("<li><div class='username'>" + client.clientId + "</div><div class='connection-id'>" + client.connectionId + "</div></li>")
                .attr('data-connection-id', client.connectionId)
                .css('background-image', 'url(' + webgis.baseUrl + '/rest/usermarkerimage?id=' + webgis.encodeURI(client.clientId) + '&width=26)')
                .addClass('liveshare-user-item')
                .appendTo($clients);
        });
        webgis.liveshare.events.on('onclientleft', function (channel, client) {
            $clients.find("li[data-connection-id='" + client.connectionId + "']").remove();
        });
        webgis.liveshare.events.on('onleftsession', function () {
            console.log('onleftsession');
            $clients.empty();
        });

        $("<div><input type='checkbox' id='liveshare-share-mapextent'><label for='liveshare-share-mapextent' class='webgis-input-label'>Kartenauschnitt teilen</label><div>")
            .addClass('webgis-input-group')
            .appendTo($elem);
        $("<div><input type='checkbox' id='liveshare-share-layervisibility'><label for='liveshare-share-layervisibility' class='webgis-input-label'>Themenschaltung teilen</label></div>")
            .addClass('webgis-input-group')
            .appendTo($elem);

        var chkMapExtent = $elem.find('#liveshare-share-mapextent');
        var chkLayerVisibility = $elem.find('#liveshare-share-layervisibility');

        chkMapExtent.prop('checked', webgis.liveshare.allowShareExtent);
        chkLayerVisibility.prop('checked', webgis.liveshare.allowShareLayerVisibility);

        chkMapExtent.click(function () {
            webgis.liveshare.allowShareExtent = $(this).prop('checked');
        });
        chkLayerVisibility.click(function () {
            webgis.liveshare.allowShareLayerVisibility = $(this).prop('checked');
        });

        var $messages = $("<div>")
            .addClass('webgis-liveshare-messages')
            .appendTo($elem);

        $("<input placeholder='Kurznachricht...'>")
            .addClass("webgis-input")
            .insertBefore($messages)
            .keypress(function (e) {
                if (e.which == 13) {
                    var message = $(this).val();

                    if (message) {
                        $("<div><div>Ich:</div><div class='message'>" + message + "</div></div>")
                            .addClass('self')
                            .prependTo($messages);

                        webgis.liveshare.emit({ command: 'text-message', args: { message: message } });
                    }

                    $(this).val('');
                }
            });

        webgis.liveshare.events.on('onreceive', function(channel, result) {
            if (result && result.message && result.message.command === "text-message" && result.message.args) {
                $("<div><div>" + result.clientId + ":</div><div class='message'>" + result.message.args.message + "</div></div>")
                    .addClass('client')
                    .prependTo($messages);
            }
        });
    };
})(webgis.$ || jQuery);