// WebGIS UI Builder Button Elements
// button, imagebutton, buttongroup, undobutton
//
// Buttons historically get no data-tool-id attribute (".toolid" = false), even if the UI is built for a tool.

webgis.ui.builder["button.tagname"] = "button";
webgis.ui.builder["button.toolid"] = false;
webgis.ui.builder["button"] = (map, $newElement, element, options) => {
    // String(): a missing text historically renders as "null"/"undefined" (was string concatenation)
    $newElement.html(String(element.text));
    $newElement.addClass('uibutton');
    $newElement.data('_value', element.value);

    var button = $newElement.get(0);
    button.buttoncommand = element.buttoncommand;
    button.buttoncommand_argument = element.buttoncommand_argument;
    button.buttontype = element.buttontype;
    button.map = options.map;
    if (element.icon) {
        $newElement.css('background-image', 'url(' + webgis.css.imgResource(element.icon) + ')');
    }
    if (element.ctrl_shortcut) {
        $newElement.attr('data-ctrl-shortcut', element.ctrl_shortcut);
        var shortCutUpperCase = element.ctrl_shortcut.toUpperCase();
        if ($newElement.text().indexOf(shortCutUpperCase) >= 0) {
            $newElement.html($newElement.html().replaceAll(shortCutUpperCase, "<u>" + shortCutUpperCase + "</u>"));
        } else {
            $newElement.html($newElement.html() + " (<u>" + shortCutUpperCase + "</u>)");
        }
    }
    $(button).click(function () {
        var $this = $(this);

        if ($this.hasClass('uibutton-validate-input')) {
            var $validationContainer = $this.closest('.webgis-validation-container');
            $validationContainer.find('.webgis-validation').each(function (i, e) {
                $(e).webgis_validation('validate');
            });

            if ($validationContainer.find('.webgis-validation.webgis-not-valid').length > 0) {
                webgis.alert(webgis.l10n.get('enter-all-required-values'), 'input-validation');
                return;
            }
        }

        var map = this.map;
        switch (this.buttoncommand) {
            case "setgraphicsdistancecircleradius":
                var $radiusControl = $(this).closest('.webgis-ui-optionscontainer').find('.webgis-graphics-distance_circle-radius');
                $this.data('_value', $radiusControl.length === 1 ? parseFloat($radiusControl.val()) : null);
                break;
            case "setgraphicshectolineinterval":
                var $intervalControl = $(this).closest('.webgis-ui-optionscontainer').find('.webgis-graphics-hectoline-interval');
                $this.data('_value', $intervalControl.length === 1 ? parseFloat($intervalControl.val()) : null);
                break;
        }
        webgis.tools.onButtonClick(map,
            {
                command: this.buttoncommand,
                argument: this.buttoncommand_argument,
                type: this.buttontype,
                id: this.id,
                map: map, value: $this.data('_value')
            }, this);
    });
};

webgis.ui.builder["imagebutton.toolid"] = false;
webgis.ui.builder["imagebutton"] = (map, $newElement, element, options) => {
    $newElement.addClass('webgis-ui-imagebutton');
    if (element.src) {
        $newElement.css('background-image', 'url(' + webgis.css.imgResource(element.src) + ')');
    }
    $newElement.data('_value', element.value);

    var button = $newElement.get(0);
    button.buttoncommand = element.buttoncommand;
    button.buttoncommand_argument = element.buttoncommand_argument;
    button.buttontype = element.buttontype;
    button.map = options.map;
    if (element.text) {
        $("<div>").text(element.text).addClass('webgis-ui-imagebutton-text').appendTo($newElement);
        $newElement.attr('title', element.text);
    }
    $(button).click(function () {
        var $this = $(this),
            $optContainer = $(this).closest('.webgis-ui-optionscontainer');

        if ($optContainer.length > 0) {
            var optContainer = $optContainer.get(0);
            optContainer.value = $this.data('_value');
            $optContainer.find('.webgis-ui-option-selected').removeClass('webgis-ui-option-selected');
            $(this).addClass('webgis-ui-option-selected');
            if (optContainer.onChange)
                optContainer.onChange();
        }
        $optContainer.parent().parent().find('.webgis-ui-selected-option').html($(this).clone().css({
            minHeight: '0px', height: '24px', backgroundPosition: 'center', border: 'none'
        }).removeClass('webgis-ui-option-selected'));
        var map = this.map;
        webgis.tools.onButtonClick(map,
            {
                command: this.buttoncommand,
                argument: this.buttoncommand_argument,
                type: this.buttontype,
                id: this.id,
                map: map, value: $this.data('_value')
            }, this);
    });
};

// buttongroup historically ignores id, css, style, visible, ... (".properties" = false)
webgis.ui.builder["buttongroup.properties"] = false;
webgis.ui.builder["buttongroup"] = (map, $newElement) => {
    $newElement.addClass('webgis-ui-buttongroup');
};
webgis.ui.builder["buttongroup.afterinsert"] = (map, $newElement) => {
    var childrenCount = $newElement.children('.uibutton').length;
    if (childrenCount > 0) {
        $newElement.children('.uibutton').css('width', ((100.0 / childrenCount) - (childrenCount - 1)) + '%');
    }
};

webgis.ui.builder["undobutton.toolid"] = false;
webgis.ui.builder["undobutton"] = (map, $newElement, element) => {
    $newElement
        .addClass('webgis-undobutton')
        .attr('data-undotool', element.undotool)
        .data('map', map);

    if (element.src) {
        $newElement.addClass('webgis-ui-imagebutton');
        $newElement.css('background-image', 'url(' + webgis.css.imgResource(element.src) + ')');
        $("<div>").addClass('webgis-ui-imagebutton-text').text(element.text).appendTo($newElement);
    } else {
        $newElement.addClass('webgis-ui-undobutton').text(element.text);
    }

    $newElement.click(function () {
        var $this = $(this);
        var map = $this.data('map');
        var undoTool = map.getTool($this.attr('data-undotool'));
        if (!undoTool || !undoTool.undos || !undoTool.undos.length)
            return;

        webgis.ui.showModal({
            container: map.ui.webgisContainer(),
            width: '330px',
            title: $this.html(),
            slide: false,
            animate: false,
            closebutton: true,
            dock: 'left',
            id: 'webgis-undo-modal',
            onload: function ($content) {
                var $ul = $('<ul>').addClass('webgis-ui-undo-list').appendTo($content);

                for (var u in undoTool.undos) {
                    var undo = undoTool.undos[u];
                    var $li = $("<li><h2>" + undo.title + "</h2></li>")
                        .addClass('webgis-ui-undo-item')
                        .data('undo', undo)
                        .prependTo($ul)
                        .mouseenter(function (e) {
                            var undo = $(this).data('undo');
                            map.graphics.addPreviewFromJson(undo.preview);
                        })
                        .mouseleave(function (e) {
                            map.graphics.removePreview();
                        })
                        .click(function () {
                            map.graphics.removePreview();
                            webgis.tools.onToolUndoClick(map, undoTool.id, $(this).data('undo'));
                        });

                    if (undo._ticks) {
                        var seconds = (new Date().getTime() - undo._ticks) / 1000.0, spanText;
                        if (seconds < 60) {
                            spanText = "vor " + Math.round(seconds) + " Sekunden...";
                        }
                        else if (seconds < 3600) {
                            spanText = "vor " + Math.round(seconds / 60) + " Minute(n)...";
                        }
                        else {
                            spanText = "vor " + Math.round(seconds / 3600) + " Stunde(n)...";
                        }
                        $("<div>").addClass('webgis-ui-undo-span').html(spanText).appendTo($li);
                    }
                }
            },
            onclose: function () {
                map.graphics.removePreview();
            }
        });
    });
};
