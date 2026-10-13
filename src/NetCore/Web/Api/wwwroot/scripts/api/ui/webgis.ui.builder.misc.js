// WebGIS UI Builder Misc Elements
// literal, literal-bold, image, condition_div, opacity-control, ui-liveshare, static-overlay-control,
// image-selector, graphics-info-container, graphics-info-stage, sketch-info-container, sharelink-buttons
// (jQuery plugins: webgis_image_selector.js)
//
// Some of these elements don't create a (single) element in the parent. They use the ".create" hook,
// which replaces the whole element creation (see docs/architecture/ui-builder.md).
// The jQuery plugins webgis_opacity_control, webgis_liveshare, webgis_staticOverlayControl, ... live in
// their own files, because they are also used outside of the UI builder.

(function ($) {
    "use strict";

    // literals are appended to the html of the parent (no element => no events, no child elements).
    // Note: rewriting the parent html drops events of already inserted siblings.
    webgis.ui.builder["literal.create"] = (map, $parent, element) => {
        $parent.html($parent.html() + webgis.encodeUntrustedHtml(element.literal, true));
        return null;
    };
    webgis.ui.builder["literal-bold.create"] = (map, $parent, element) => {
        $parent.html($parent.html() + '<strong>' + webgis.encodeUntrustedHtml(element.literal, true) + '</strong>');
        return null;
    };

    webgis.ui.builder["image.tagname"] = "img";
    webgis.ui.builder["image"] = (map, $newElement, element) => {
        $newElement.attr('src', "" + element.src);
    };

    // only rendered as a dependency div, if there are condition arguments.
    // Otherwise it falls back to the generic element (<condition_div>), like any unknown type.
    webgis.ui.builder["condition_div.create"] = (map, $parent, element, options) => {
        if (element.condition_arguments && element.condition_arguments.length > 0) {
            // the property names (codition_logical_operator, contition_element_id) are historical typos
            const $newElement = $("<div " + webgis.ui.elementProperties(element, options) + "></div>")
                .addClass('webgis-dependencies webgis-dependency-' + element.condition_type)
                .attr('data-dependency-arguments', element.condition_arguments)
                .attr('data-dependency-logical_operator', element.condition_logical_operator)
                .attr('data-dependency-condition_result', element.condition_result)
                .attr('data-dependency-element-id', element.contition_element_id)
                .appendTo($parent);

            if (element.condition_type == 'elementvalue') {
                $newElement.addClass('webgis-ui-added');
            }
            return $newElement;
        }

        let $newElement = null;
        const repeat = element.repeat || 1;
        for (let i = 0; i < repeat; i++) {
            $newElement = $("<" + element.type + webgis.ui.elementProperties(element, options) + "></" + element.type + ">").appendTo($parent);
        }
        return $newElement;
    };

    webgis.ui.builder["opacity-control"] = (map, $newElement, element) => {
        $newElement.webgis_opacity_control({ service: map.getService(element.serviceId) });
    };

    webgis.ui.builder["ui-liveshare"] = (map, $newElement) => {
        $newElement.webgis_liveshare();
    };

    webgis.ui.builder["static-overlay-control"] = (map, $newElement, element) => {
        $newElement.webgis_staticOverlayControl({ map: map, command_buttons: element.command_buttons });
    };

    webgis.ui.builder["image-selector"] = (map, $newElement, element) => {
        return $newElement.webgis_image_selector({
            image_urls: element.image_urls,
            image_labels: element.image_labels,
            image_width: element.image_width,
            image_height: element.image_height,
            multi_select: element.multi_select,
            selected: element.value,
        });
    };

    // graphics containers: plain div (id, css, ... are ignored), plugins are optional (graphics module)
    webgis.ui.builder["graphics-info-container.properties"] = false;
    webgis.ui.builder["graphics-info-container"] = (map, $newElement, element, options) => {
        if ($.fn.webgis_graphicsInfoContainer) {
            $newElement.webgis_graphicsInfoContainer({ map: options.map });
        }
    };

    webgis.ui.builder["graphics-info-stage.properties"] = false;
    webgis.ui.builder["graphics-info-stage"] = (map, $newElement, element, options) => {
        if ($.fn.webgis_graphicsInfoStage) {
            $newElement.webgis_graphicsInfoStage({ map: options.map });
        }
    };

    webgis.ui.builder["sketch-info-container.create"] = (map, $parent, element, options) => {
        // User preference (Burger menu -> Einstellungen -> Benutzer Einstellungen) lets the
        // user turn the sketch info overlay off entirely, or reduce it to only the
        // snapping/construction info (hiding geometry type/segment/section details).
        const sketchInfoDisplayMode = (webgis.usability.userPreferences && webgis.usability.userPreferences.get)
            ? (webgis.usability.userPreferences.get("sketch-info-display-mode") || "default")
            : "default";

        let showSketchInfo = sketchInfoDisplayMode !== "hidden";
        if (webgis.useMobileCurrent() ||                                          // 1. auf kleien mobilegeräten nicht anzeigen
            (webgis.isMobileDevice() && webgis.usability.clickBubble == false))   // 2. auf Mobilen Geräten ohne Bubble => mach keinen Sinn
            showSketchInfo = false;

        if (!showSketchInfo) {
            return null;
        }

        // Preferred: render as an overlay docked above the coordinate display (grows
        // upwards, only visible while the cursor is over the map), instead of taking up
        // space in the tool dialog itself.
        const $sketchInfoTarget = options.map && options.map.sketchInfoOverlayContainer ?
            options.map.sketchInfoOverlayContainer() :
            null;

        const sketchInfoMinimal = sketchInfoDisplayMode === "minimal";
        let $newElement = null;

        if ($sketchInfoTarget) {
            $sketchInfoTarget.empty(); // remove/destroy any previous instance
            $newElement = $("<div></div>").appendTo($sketchInfoTarget);
            if ($.fn.webgis_sketchInfoContainer) {
                $newElement.webgis_sketchInfoContainer({ map: options.map, minimal: sketchInfoMinimal });
            }
            options.map.updateSketchInfoOverlayVisibility();
        } else if (element.allow_fallback !== false) {
            // Fallback (e.g. layouts without a .webgis-currentcoords-container): keep
            // the previous behaviour and render inline in the tool dialog.
            $newElement = $("<div></div>").appendTo($parent);
            if ($.fn.webgis_sketchInfoContainer) {
                $newElement.webgis_sketchInfoContainer({ map: options.map, minimal: sketchInfoMinimal });
            }
        }
        // else: no overlay container available and fallback explicitly disabled
        // (element.allow_fallback === false) -> the element is simply not rendered.

        return $newElement;
    };

    // one image button per available share option (webgis.shareOptions), id, css, ... are ignored
    webgis.ui.builder["sharelink-buttons.properties"] = false;
    webgis.ui.builder["sharelink-buttons"] = (map, $newElement, element) => {
        $newElement.addClass('contains-labels webgis-ui-optionscontainer');

        if (!element.link) {
            return;
        }

        for (const o in webgis.shareOptions) {
            const shareOption = webgis.shareOptions[o];
            if (shareOption.available()) {
                const $imageButton = $("<div>")
                    .addClass('webgis-ui-imagebutton')
                    .css('background-image', 'url(' + shareOption.img() + ')')
                    .appendTo($newElement)
                    .data('link', element.link)
                    .data('subject', element.subject)
                    .data('qr_base64', element.qr_base64)
                    .data('shareOption', shareOption)
                    .click(function () {
                        const shareOption = $(this).data('shareOption');
                        shareOption.share($(this).data('link'), $(this).data('subject'), $(this).data('qr_base64'));
                    });
                $("<div>")
                    .addClass('webgis-ui-imagebutton-text')
                    .text(shareOption.name)
                    .appendTo($imageButton);
            }
        }
    };
})(webgis.$ || jQuery);
