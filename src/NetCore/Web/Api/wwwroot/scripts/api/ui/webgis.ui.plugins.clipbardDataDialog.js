webgis.ui.showClipboardDataDialog = function (options) {
    const o = options;
    var id = webgis.guid();

    $("body").webgis_modal({
        title: o.name,
        id: id,
        onload: function ($content) {

            const $textArea = $("<pre>")
                .css({
                    width: "100%",
                    height: "calc(100% - 165px)",
                    boxSizing: "border-box",
                    backgroundColor: "#ccc",
                    fontFamily: "Consolas, Courier New, monospace",
                    overflow: "auto",
                    padding: "4px"
                })
                .attr("readonly", "readonly")
                .html(o.clipboard_data)
                .appendTo($content);

            const $buttonBar = $("<div>").css("text-align", "right").appendTo($content);

            if (o.downloadUrl && o.downloadData) {
                $("<button>")
                    .text(webgis.l10n.get("download"))
                    .addClass("webgis-button")
                    .appendTo($buttonBar)
                    .on("click.webgis_clipboard_data_dialog", function () {
                        webgis.ajax({
                            type: 'post',
                            url: o.downloadUrl,
                            data: webgis.hmac.appendHMACData(o.downloadData),
                            success: function (result) {
                                if (!result.success) return;

                                if (result.downloadid) {
                                    window.open(webgis.baseUrl + '/rest/download?id=' + result.downloadid + '&n=' + result.name);
                                    return;
                                }
                            }
                        });
                    })
            }

            $("<button>")
                .text(webgis.l10n.get("copy-to-clipboard"))
                .addClass("webgis-button")
                .appendTo($buttonBar)
                .on("click.webgis_clipboard_data_dialog", function () {
                    webgis.copy($textArea);
                })
                //.trigger('click')
                ;

            if (o.description) {
                $("<p>")
                    .addClass("webgis-paragraph")
                    .text(o.description)
                    .appendTo($content);
            }
        },
        width: o.width || '640px'
    });
}