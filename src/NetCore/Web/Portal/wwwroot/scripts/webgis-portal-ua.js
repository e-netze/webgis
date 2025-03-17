function changeUserAccess(name) {
    $('body').webgis_modal({
        title: 'User Access: ' + name,
        onload: function ($content) {
            $content.css('padding', '10px');

            var $uaInfo = $("<div>Der Zugriff auf das Element erfolgt über folgende Zugriffsrechte. Ein Anwender muss mit seinen Rechten dabei eine Überprüung über alle übergeordneten Elemente durchlaufen. Ist der Anwender nicht für ein übergordnetes Element berechtigt, ist für ihn auch dieses Element gesprerrt.</div>").appendTo($content);
            var $uaControl = $("<div id='user-authentication-control'></div>").appendTo($content);

            $("<br/><br/><button>Commit</button>").appendTo($content)
                .click(function (e) {
                    var authValues = $(this).closest('.webgis-modal-content').find("[name='user-authentication-values']").val();
                    
                    $.getJSON(webgis.url.relative('proxy/toolmethod/webgis-tools-portal-portal/setuseraccess'),
                        { 'page-id': portal, 'access-name': name, ua: authValues },
                            function (data) {
                                $(null).webgis_modal('close');
                            });
                });

            var owner = window.isOwner && window.currentUsername ? window.currentUsername : null;

            $.ajax({
                url: webgis.url.relative('proxy/toolmethod/webgis-tools-portal-portal/getuseraccess'),
                data: { 'page-id': portal, 'access-name': name },
                dataType: 'json',
                success: function (result) {

                    $.getJSON('./home/SecurityPrefixes/' + portal, null, function(data) {
                        $uaControl.webgis_autocomplete_multiselect({
                            source: './home/SecurityAutocomplete/' + portal,
                            name: 'user-authentication-values',
                            prefixes: data,
                            alwaysIncludeOwner: owner
                        });

                        for(var node in result.nodeAccess) {
                            if (node == portal + "/" + name) {
                                $("<br/><h4>" + node + " &gt;&gt;</h4>").appendTo($uaInfo);
                                var uaItems = result.nodeAccess[node];
                                if (uaItems != null) {
                                    for (var i = 0; i < uaItems.length; i++) {
                                        $uaControl.webgis_autocomplete_multiselect('add', { value: uaItems[i], alwaysIncludeOwner: owner });
                                    }
                                }
                            } else {
                                var uaItems = result.nodeAccess[node];
                                if (uaItems != null && uaItems.length > 0) {
                                    $("<br/><h4>" + node + " &gt;&gt;</h4>").appendTo($uaInfo);
                                    for (var i = 0; i < uaItems.length; i++) {
                                        $("<div style='display:inline-block;background:#eee;border:#aaa 1px solid;margin:2px;padding:5px'>" + uaItems[i] + "</div>").appendTo($uaInfo);
                                    }
                                }
                            }
                        }

                    });
                }
            });
        },
        width: '700px', height: '500px'
    });
};