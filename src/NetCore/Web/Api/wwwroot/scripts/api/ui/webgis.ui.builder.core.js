// WebGIS UI Builder (core)
// Builds the tool/server UI from a ui definition ({ elements: [...] }); the element types are
// created by the builders in webgis.ui.builder.<topic>.js (registry: webgis.ui.builder.js).
// No plugin: a stateless build function, called repeatedly on the same target or without a
// target element (targets resolved by element.target).
//   webgis.ui.build(target, options)                 build/append elements
//   webgis.ui.emptyToolUI($target, options)          persist topic + empty
//   webgis.ui.appendCtrlBBoxInfo(target)             'Ctrl + bbox' tip
//   webgis.ui.removeSecondaryToolUI(map)             close secondary tool dialog
//   webgis.ui.persistentTopic.serialize/deserialize  keep tool topics while switching tools
//   webgis.ui.addFormElementEvents($element)         mark .webgis-form-container dirty
//   webgis.ui.elementProperties(element, options)    attribute string for builders
// $.fn.webgis_uibuilder is kept as a thin jQuery facade (compatibility, UI tests).

(function ($) {
    "use strict";
    var defaults = {
        map: null,
        tool: null,
        elements: null,
        title: 'UI',
        closebutton: true,
        onclose: null,
        oncloseArgument: null,
        append: false,
        event: null,
        toolDialogId: null
    };
    var removeSecondaryToolUI = function (map) {
        if (map) {
            if (map.ui.advancedToolBehaviour() === true) {
                map.ui.webgisContainer().find('.webgis-tabs-tab-content').find('.webgis-tooldialog-header.secondary span.close').trigger('click');
            } else {
                webgis.ui.closeModal(constants.serverButtonModalDialogId);
                if ($.fn.webgis_tabs) {
                    webgis.ui.hideTab(map.ui.webgisContainer().find('.webgis-tabs-holder'), { tab: 'tools' });
                }
            }
        }
    };

    var constants = {
        serverButtonModalDialogId: 'server-button-modal-dialog'
    };

    var initUI = function (parent, options) {
        var $parent = $(parent),
            $targetRoot = $parent,
            $modalContent = null,
            $toolModalContent = null,
            $toolSidebarLeftContent = null,
            $toolSidebarBottomContent = null,
            $toolSidebarTopContent = null,
            $toolAsideContent = null;

        if (options.map && options.tool && options.tool.id && options.suppressOnBuildToolUi !== true) {
            options.map.events.fire('onbuildtoolui', options.tool.id);
        }

        if (!options.toolDialogId) {
            options.toolDialogId = options.map && options.map.getActiveTool() ? options.map.getActiveTool().id : null;
        }

        $parent.addClass('webgis-ui-holder');

        if (options.elements) {
            var hasHolderElements = false,
                hasModalDialogElements = false,
                hasToolModalDialogElements = false,
                hasSidebarLeftElements = false,
                hasSidebarBottomElements = false,
                hasSidebarTopElements = false,
                hasAsideToolElements = false,
                hasToolPersistentTopic = false;

            var modalDialogTitle = options.title,
                toolModalDialogTitle = options.title,
                sidebarLeftTitle = options.title,
                sidebarBottomTitle = options.title,
                sidebarTopTitle = options.title;

            var modalDialogWidth = '330px',
                modalDialogHeight = null;

            var closeModalDialog = false,
                closeToolModalDialog = false,
                closeSidebarLeft = false,
                closeSidebarBottom = false,
                closeSidebarTop = false,
                closeAsideToolElement = false;

            var modalDialogVisible = true,
                toolModalDialogVisible = true,
                toolModalHasBlocker = true,
                toolModalClosable = false;

            // for onTargetClosed... => user closes dialog => action(),
            // a targetClose for sidebar is not implemented for now, 
            // can be done in the same way if needed
            var modalDialogElement = null;  

            for (var i = 0, to = options.elements.length; i < to; i++) {
                if (options.elements[i].target && options.elements[i].target.indexOf('#') === 0) {  // Id selector
                    // set target root if not already set
                    // otherwise cascading combos not work for eg. multiple feature transfers
                    if ($targetRoot.length === 0) {
                        $targetRoot = $(options.elements[i].target);
                    }
                } else {
                    if (options.map) {
                        //console.log('map.fire: onbuildui-' + options.elements[i].target);
                        options.map.events.fire('onbuildui-' + options.elements[i].target);
                    }
                    switch (options.elements[i].target) {
                        case "modaldialog":
                            modalDialogElement = options.elements[i];
                            hasModalDialogElements = true;
                            modalDialogTitle = options.elements[i].targettitle || modalDialogTitle;
                            modalDialogWidth = options.elements[i].targetwidth || modalDialogWidth;
                            modalDialogHeight = options.elements[i].targetheight || modalDialogHeight;
                            closeModalDialog = closeTarget(options.elements[i]);
                            break;
                        case "modaldialog_hidden":
                            modalDialogElement = options.elements[i];
                            hasModalDialogElements = true;
                            modalDialogVisible = false;
                            modalDialogTitle = options.elements[i].targettitle || modalDialogTitle;
                            closeModalDialog = closeTarget(options.elements[i]);
                            break;
                        case "tool_modaldialog":
                            modalDialogElement = options.elements[i];
                            hasToolModalDialogElements = true;
                            toolModalDialogTitle = options.elements[i].targettitle || toolModalDialogTitle;
                            closeToolModalDialog = closeTarget(options.elements[i]);
                            break;
                        case "tool_modaldialog_hidden":
                            modalDialogElement = options.elements[i];
                            hasToolModalDialogElements = true;
                            toolModalDialogVisible = false;
                            toolModalDialogTitle = options.elements[i].targettitle || toolModalDialogTitle;
                            closeToolModalDialog = closeTarget(options.elements[i]);
                            break;
                        case "tool_modaldialog_noblocking":
                            modalDialogElement = options.elements[i];
                            hasToolModalDialogElements = true;
                            toolModalHasBlocker = false;
                            toolModalDialogTitle = options.elements[i].targettitle || toolModalDialogTitle;
                            closeToolModalDialog = closeTarget(options.elements[i]);
                            break;
                        case "tool_modaldialog_noblocking_closable":
                            modalDialogElement = options.elements[i];
                            hasToolModalDialogElements = true;
                            toolModalHasBlocker = false;
                            toolModalClosable = true;
                            toolModalDialogTitle = options.elements[i].targettitle || toolModalDialogTitle;
                            closeToolModalDialog = closeTarget(options.elements[i]);
                            break;
                        case "tool_sidebar_bottom":
                            hasSidebarBottomElements = true;
                            sidebarBottomTitle = options.elements[i].targettitle || sidebarBottomTitle;
                            closeSidebarBottom = closeTarget(options.elements[i]);
                            break;
                        case "tool_sidebar_top":
                            hasSidebarTopElements = true;
                            sidebarTopTitle = options.elements[i].targettitle || sidebarTopTitle;
                            closeSidebarTop = closeTarget(options.elements[i]);
                            break;
                        case "tool_sidebar_left":
                            hasSidebarLeftElements = true;
                            sidebarLeftTitle = options.elements[i].targettitle || sidebarLeftTitle;
                            closeSidebarLeft = closeTarget(options.elements[i]);
                            break;
                        case "tool_aside":
                            hasAsideToolElements = true;
                            closeAsideToolElement = closeTarget(options.elements[i]);
                            break;
                        case "tool_persistent_topic":
                            hasToolPersistentTopic = true;
                            break;
                        default:
                            hasHolderElements = true;
                            break;
                    }
                }
            }

            if (hasHolderElements === true) {
                if (!options.append) {
                    var isMapTool = !options.tool || (options.tool.type !== 'clientbutton' && options.tool.type !== 'serverbutton');
                    if (!isMapTool && options.map.ui.advancedToolBehaviour() === false) {
                        // Open or reuse a modal dialog
                        var dialogOptions = {
                            width: '330px',
                            title: options.title,
                            id: constants.serverButtonModalDialogId
                        };
                        $parent = webgis.ui.getModalContent(dialogOptions.id);

                        if ($parent.length === 0) {
                            webgis.ui.showModal($.extend({ container: options.map.ui.webgisContainer() }, dialogOptions));
                            $parent = webgis.ui.getModalContent(dialogOptions.id);
                        }

                        if (options.append !== true) {
                            emptyParent($parent, options); // muss immer geleert werden, sonst kommt beim Labelling oder VisFilter eine
                            // endlos Aufruf schleife, weil das Initevent immer wieder aufgerufen wird!
                        }
                    } else {
                        if (!isMapTool) {
                            // remove existing secondary dialogs
                            $parent.find('.webgis-tooldialog-header.secondary span.close').trigger('click');
                        } else {
                            emptyParent($parent, options);
                        }
                        if (options.closebutton) {

                            $parent.children('.webgis-tooldialog-header').addClass('collapsed');

                            if ($parent.length > 0) {
                                var title = $("<div style='cursor:pointer'><h2>" + (options.title || 'Dialog') + "<span class='close'></span></h2></div>")
                                    .addClass('webgis-tooldialog-header' + (isMapTool ? ' primary' : ' secondary'))
                                    .appendTo($parent)
                                    .click(function (e) {
                                        var $this = $(this);

                                        if ($this.hasClass('collapsed')) {
                                            $this.parent()
                                                .children('.webgis-tooldialog-header')
                                                .each(function (i, e) {
                                                    $(e).addClass('collapsed');
                                                });
                                            $this.removeClass('collapsed');

                                            // close all secondary
                                            $this.parent().children('.webgis-tooldialog-header.secondary').find('span.close').trigger('click');
                                        } else {
                                            //console.trace('close tool', this.onclose);
                                            if (this.onclose) {
                                                let $form = $this.closest('#tab-tools-content').find('.webgis-form-container');
                                                let me = this;
                                                webgis.confirmDiscardChanges($form, options.map, function () {  // discard changes, eg. when user is editing
                                                    me.onclose(me.oncloseArgument);
                                                });
                                            }
                                        }
                                    }).get(0);
                                $(title).find('span.close')
                                    .click(function (e) {
                                        e.stopPropagation();
                                        var $header = $(this).closest('.webgis-tooldialog-header');
                                        if ($header.hasClass('primary')) {
                                            $header.removeClass('collapsed').trigger('click')
                                        }
                                        else if ($header.hasClass('secondary')) {
                                            $header.parent().children('.webgis-tooldialog-header.primary.collapsed').first().trigger('click');
                                            var $content = $header.next('.webgis-tooldialog-content');
                                            emptyParent($content, options);
                                            $content.remove();
                                            $header.remove();
                                        }
                                    });
                                if ($(title).hasClass('secondary') && options.tool && options.tool.image) {
                                    $(title).css('background-image', 'url(' + webgis.css.imgResource(options.tool.image, 'tools') + ')');
                                } 

                                if (options.map && options.tool && options.tool.help_urlpath) {
                                    $("<div>")
                                        .addClass('help')
                                        .data('map', options.map)
                                        .data('tool', options.tool)
                                        .appendTo($(title))
                                        .click(function (e) {
                                            e.stopPropagation();
                                            var map = $(this).data('map');
                                            var tool = $(this).data('tool') || map.getActiveTool();
                                            webgis.showHelp(tool ?
                                                (map.isDefaultTool(tool) && tool.help_urlpath_defaulttool ? tool.help_urlpath_defaulttool : tool.help_urlpath) :
                                                null, map);
                                        });
                                }
                                title.onclose = options.onclose;
                                title.oncloseArgument = options.oncloseArgument;
                            }

                            $parent = $("<div>")
                                .addClass('webgis-tooldialog-content')
                                .addClass(options.tool && options.tool.id ? options.tool.id.replaceAll('.', '-') : '')
                                .appendTo($parent);
                        }
                        else if (options.title) {
                            $("<div><h2>" + options.title + "</h2></div>").appendTo($parent);
                        }
                    }
                }
            }
            if (hasModalDialogElements === true) {
                if (closeModalDialog) {
                    webgis.ui.closeModal();
                }
                else {
                    webgis.ui.showModal({
                        container: options.map.ui.webgisContainer(),
                        width: modalDialogWidth,
                        height: modalDialogHeight,
                        show: modalDialogVisible,
                        title: modalDialogTitle,
                        slide: false,
                        onclose: () => onCloseTarget(options.map, modalDialogElement),
                    });
                    $targetRoot = $modalContent = webgis.ui.getModalContent();
                }
            }
            if (hasToolModalDialogElements === true) {
                if (closeToolModalDialog) {
                    webgis.ui.closeModal(options.toolDialogId);
                }
                else {
                    webgis.ui.showModal({
                        container: options.map.ui.webgisContainer(),
                        width: '330px',
                        show: toolModalDialogVisible,
                        title: toolModalDialogTitle,
                        hasBlocker: toolModalHasBlocker,
                        slide: false,
                        id: options.toolDialogId,
                        closebutton: toolModalClosable,
                        onclose: () => onCloseTarget(options.map, modalDialogElement),
                        dock: 'left',
                        animate: true
                    });
                    $targetRoot = $toolModalContent = webgis.ui.getModalContent(options.toolDialogId);
                }
            }
            if (hasSidebarBottomElements === true) {
                if (closeSidebarBottom) {
                    webgis.ui.closeDockPanel({ dock: 'bottom', id: options.toolDialogId });
                }
                else {
                    options.map.ui.webgisContainer().webgis_dockPanel({
                        title: sidebarBottomTitle,
                        dock: 'bottom',
                        id: options.toolDialogId,
                        refElement: options.map.ui.mapContainer(),
                        map: options.map
                        //autoResizeBoth: true,
                        //onload: function () { }
                    });
                    $targetRoot = $toolSidebarBottomContent = options.map.ui.webgisContainer().webgis_dockPanel('content', { id: options.toolDialogId });
                }
            }
            if (hasSidebarTopElements === true) {
                if (closeSidebarTop) {
                    webgis.ui.closeDockPanel({ dock: 'top', id: options.toolDialogId });
                }
                else {
                    //options.map.ui.webgisContainer().webgis_dockPanel('close', { dock: 'top', id: options.toolDialogId });
                    options.map.ui.webgisContainer().webgis_dockPanel({
                        title: sidebarTopTitle,
                        dock: 'top',
                        id: options.toolDialogId,
                        refElement: options.map.ui.mapContainer(),
                        map: options.map,
                        autoResizeBoth: true,
                        onload: function () { }
                    });
                    $targetRoot = $toolSidebarTopContent = options.map.ui.webgisContainer().webgis_dockPanel('content', { id: options.toolDialogId });
                }
            }
            if (hasSidebarLeftElements === true) {
                if (closeSidebarLeft) {
                    webgis.ui.closeDockPanel({ dock: 'left', id: options.toolDialogId });
                }
                else {
                    options.map.ui.webgisContainer().webgis_dockPanel({
                        title: sidebarLeftTitle,
                        dock: 'left',
                        id: options.toolDialogId,
                        refElement: options.map.ui.mapContainer(),
                        map: options.map
                    });
                    $targetRoot = $toolSidebarLeftContent = options.map.ui.webgisContainer().webgis_dockPanel('content', { id: options.toolDialogId });
                }
            }
            if (hasAsideToolElements === true) {
                if (closeAsideToolElement) {
                    webgis.ui.closeDockPanel({ dock: 'right', id: options.toolDialogId + "_aside" });
                } else {
                    options.map.ui.webgisContainer().webgis_dockPanel({
                        title: sidebarLeftTitle,
                        //titleImg: webgis.baseUrl+'/'+options.map.getActiveTool().image,
                        dock: 'right',
                        id: options.toolDialogId + "_aside",
                        useIdSelector: true,
                        size: 340,
                        adverseSize: 400,
                        maximizeAdverse: true,
                        canClose: false,
                        refElement: options.map.ui.mapContainer(),
                        map: options.map
                    });
                    $targetRoot = $toolAsideContent = options.map.ui.webgisContainer().webgis_dockPanel('content', { id: options.toolDialogId + "_aside" });
                    $targetRoot.css('padding', '5px');
                }
            }
            if (hasToolPersistentTopic === true) {
                var activeTool = options.map.getActiveTool();
                if (activeTool && activeTool.id) {
                    $targetRoot = $('#' + activeTool.id.replaceAll('.', '-') + '-persistent-topic');
                }
            }

            for (var j = 0, to_j = options.elements.length; j < to_j; j++) {
                var target;
                if (options.elements[j].target && options.elements[j].target.indexOf('#') === 0) {  // Id selector
                    target = $(options.elements[j].target);
                    if (!options.append) {
                        target.empty();
                    }
                } else {
                    switch (options.elements[j].target) {
                        case "modaldialog":
                        case "modaldialog_hidden":
                            target = $modalContent;
                            break;
                        case "tool_modaldialog":
                        case "tool_modaldialog_hidden":
                        case "tool_modaldialog_noblocking":
                        case "tool_modaldialog_noblocking_closable":
                            target = $toolModalContent;
                            break;
                        case "tool_sidebar_bottom":
                            target = $toolSidebarBottomContent;
                            break;
                        case "tool_sidebar_top":
                            target = $toolSidebarTopContent;
                            // better user expererince with (defaulttool) identify... use see something changed
                            fadeIn(target);
                            break;
                        case "tool_sidebar_left":
                            target = $toolSidebarLeftContent;
                            break;
                        case "tool_aside":
                            target = $toolAsideContent;
                            break;
                        case "tool_persistent_topic":
                            var activeTool = options.map.getActiveTool();
                            if (activeTool && activeTool.id) {
                                target = $('#' + activeTool.id.replaceAll('.', '-') + '-persistent-topic');
                                if (!options.append) {
                                    // better user expererince with identify... use see something changed
                                    fadeIn(target);
                                }
                            }
                            break;
                        default:
                            target = $parent;
                            if (options.map) {
                                options.map.ui.uncollapseSidebar();
                            }
                            break;
                    }
                }
                insertElement(options.map, target, options.elements[j], options);

                options.map.events.fire('ui-builder-targetelement-changed', options.map, { target: options.elements[j].target, element: target });
            }

            if (hasModalDialogElements === true) {
                if (!modalDialogHeight) {
                    webgis.ui.fitModal();
                }
                if (modalDialogVisible === false)
                    webgis.ui.hideModal();
            }
            if (hasToolModalDialogElements === true) {
                webgis.ui.fitModal(options.toolDialogId, { dock: 'left' });
                if (toolModalDialogVisible === false)
                    webgis.ui.hideModal(options.toolDialogId);
            }
        }
        if (options.setters) {
            for (var i = 0, to = options.setters.length; i < to; i++) {
                var setter = options.setters[i];
                if (setter.name === 'addclass') {
                    $('#' + setter.id).addClass(setter.val);
                }
                else if (setter.name === 'removeclass') {
                    $('#' + setter.id).removeClass(setter.val);
                }
                else if (setter.name === 'selectoption') {
                    var $optionContainer = $('#' + setter.id);
                    $optionContainer.find('.webgis-ui-option-selected').removeClass('webgis-ui-option-selected');
                    $optionContainer.find("[data-value='" + setter.val + "']").addClass('webgis-ui-option-selected');
                    if ($optionContainer.hasClass('webgis-ui-optionscontainer')) {
                        $optionContainer.get(0).value = setter.val;  // like the default setter: also set the value (sent as tool parameter)
                    }
                }
                else if (setter.name === "_anonymous-user-id") {
                    //console.log(setter.val);
                    webgis.localStorage.setAnonyousUserId(setter.val);
                }
                else {
                    if (setter.id === '_webgis_setter_persistent_parameters_' && options.map) {
                        options.map.applyPersistentToolParameters(setter.val, function ($e) {
                            if ($e.hasClass('webgis-validation')) {
                                $e.webgis_validation('validate');
                            }
                        });
                    }
                    if (setter.id === '_webgis_setter_update_persistent_parameters_') {
                        options.map.updatePersistentToolParameters(setter.val);
                    }
                    $('#' + setter.id).each(function (j, e) {
                        let $e = $(e);

                        if ($e.hasClass('webgis-ui-optionscontainer')) {
                            e.value = setter.val;
                            $e.children().removeClass('webgis-ui-option-selected').each(function (i, c) {
                                var $c = $(c);
                                if ($c.data('_value') == setter.val) {
                                    $c.addClass('webgis-ui-option-selected');
                                    return false;
                                }
                            });
                        }
                        else if ($e.hasClass('webgis-bbox-input-holder')) {
                            $e.webgis_bbox_input('val', setter.val);
                        }
                        else if ($e.hasClass('webgis-size-input-holder')) {
                            $e.webgis_size_input('val', setter.val);
                        }
                        else if ($e.hasClass('webgis-static-overlay-control')) {
                            $e.webgis_staticOverlayControl('val', setter.val);
                        }
                        else {
                            switch (e.nodeName.toLowerCase()) {
                                case "input":
                                case "textarea":  // .html() would only change the default value (not shown after the user edited the textarea)
                                    //console.log('set-input', $(e), $(e).length, setter.val);
                                    $e.val(setter.val);
                                    break;
                                case "select":
                                    // Sollte schon am Server passieren:
                                    // Achtung: setter.val kann auch der Wert des Textes sein. Kann bei Domains vorkommen, weil hier der AGS-Client keine Ahnung hat, was hinter der Domain steht...

                                    //var optionVal = setter.val, optionValFound = false;
                                    //$(e).children.each(function (i, option) {
                                    //    if ($(option).attr('val') === val) {
                                    //        optionValFound = true;
                                    //        optionVal = val;
                                    //    }
                                    //    else if (optionValFound === false && $(option).html() === val) {
                                    //        optionVal = $(option).attr('value');
                                    //    }
                                    //});

                                    if (setter.options && setter.options.length > 0) {
                                        $(e).empty();
                                        for (var o in setter.options) {
                                            var option = setter.options[o];
                                            $("<option value='" + option.value + "'>" + option.label + "</option>").appendTo($(e));
                                        }

                                        // e.map is only set for selects with onchange/persistent parameter => fallback to the builder map
                                        var map = this.map || options.map;
                                        var tool = map ? map.getTool($(e).attr('data-tool-id')) || map.getActiveTool() : null;
                                        $(e).val(setter.val || $(e).data('initial-value') || (map && tool ? map.getPersistentToolParameter(tool, this.id) : null));
                                        if ($(e).hasClass('webgis-validation')) {
                                            $(e).webgis_validation('validate');
                                        }
                                    } else {
                                        if (setter.options && setter.options.length === 0) {
                                            $(e).empty();
                                        }
                                        $(e).val(setter.val).data('initial-value', setter.val);  // initial-value: ursprünglichen Wert speichern. Das macht bei kaskadierenden Auswahllisten Sinn. Wann werden der Wert richtig zugewiesen wein man hin un her schaltet...
                                    }
                                    // do not trigger('change')  => endless loop possible (tool calling themself with toolrequests!!)
                                    break;
                                default:
                                    $e.html(setter.val);
                                    break;
                            }
                        }

                        if ($e.hasClass('webgis-validation')) {
                            $e.webgis_validation('validate');
                        }
                    });
                }
            }
        }

        webgis._appendAutocomplete($parent);
        webgis._appendAutocomplete($modalContent);
        webgis._appendAutocomplete($toolModalContent);

        // select: kaskadierende Auswahllisten
        $targetRoot.find('.webgis-ui-added.webgis-select-has-dependency-field-ids').each(function (i, select) {
            $(select)
                .removeClass('webgis-ui-added')  //to run this only first time after creating
                .change(function () {
                    if ($(this).val()) {
                        $(this).data('initial-value', $(this).val())
                    }
                });
            select.map = options.map;
            select.refillSelect = function (select) {
                webgis.delayed(function (select) {
                    var $select = $(select);

                    //var values = {};
                    //var fieldIds = $select.data('dependency-field-ids') 
                    //for (var i in fieldIds) {
                    //    values[fieldIds[i]] = $('#' + fieldIds).val();
                    //}

                    var arg = { "combo-id": $select.attr('id') };
                    var toolType = 'servertoolcommand', toolId = null;
                    if ($select.data('dependency_field_ids_callback_toolid')) {
                        toolType = 'servertoolcommand_ext';
                        toolId = $select.data('dependency_field_ids_callback_toolid');
                    }

                    webgis.tools.onButtonClick(select.map, { command: '_event_handler_onupdatecombo', type: toolType, id: toolId, map: select.map }, this, null, arg);
                }, 1, select);
            };
            var fieldIds = $(select).data('dependency-field-ids')
            for (var i in fieldIds) {
                $('#' + fieldIds[i])
                    .change(function () {
                        var $this = $(this);
                        $targetRoot.find('.webgis-select-has-dependency-field-ids').each(function (s, select) {
                            if ($.inArray($this.attr('id'), $(select).data('dependency-field-ids')) >= 0) {
                                select.refillSelect(select);
                            }
                        });
                    });
            }

            select.refillSelect(select);
        });
        // contition-divs => depends on element value
        $targetRoot.find('.webgis-ui-added.webgis-dependencies.webgis-dependency-elementvalue').each(function (i, div) {
            var elementId = $(div).removeClass('webgis-ui-added').attr('data-dependency-element-id');
            if (elementId) {
                var onChange = function (element) {
                    var val = $(element).val(), id = $(element).attr('id');

                    $('.webgis-dependencies.webgis-dependency-elementvalue').each(function (i, div) {
                        var elementId = $(div).attr('data-dependency-element-id');
                        if (elementId === id) {
                            var contition_arguments = $(div).attr('data-dependency-arguments').split(',');
                            var contition_result = $(div).attr('data-dependency-condition_result');

                            //console.log('contdition', contition_arguments, contition_result);

                            var matches = (val && $.inArray("*", contition_arguments) >= 0) || $.inArray(val, contition_arguments) >= 0;

                            if (matches.toString() === contition_result.toString()) {
                                $(div).css('display', 'block');
                            } else {
                                $(div).css('display', 'none');
                            }
                        }
                    });
                }

                var $element = $('#' + elementId);
                if ($element.length > 0) {
                    $element.change(function () {
                        onChange(this);
                    });

                    webgis.delayed(function ($element) {
                        onChange($element);
                    }, 100, $element);
                }
            }
        });

        if ($parent.hasClass('webgis-temp-tool-ui-element remove-after-build')) {
            $parent.remove();
        }
    };
    var appendCtrlBBoxInfo = function (parent, options) {
        var $parent = $(parent);
        if ($parent.children('.webgis-tooldialog-content').length === 1)
            $parent = $parent.children('.webgis-tooldialog-content');

        if ($parent.find('.webgis-ctrl-bbox-info').length == 0)
            $("<div><strong>" + webgis.l10n.get("tip") + ":&nbsp;</strong>" + webgis.l10n.get("tip-bbox-tool") + "</div>")
                .addClass("webgis-info webgis-ctrl-bbox-info")
                .appendTo($parent);
    };
    var insertElement = function (map, parent, element, options) {
        if (!parent || !element)
            return;
        var $parent = $(parent);
        if (element.iscollapsable === true) {
            var show = element.collapsestate === 'expanded';
            var $collapsable = $("<div>").appendTo($parent).addClass('webgis-ui-collapsable');

            if (element.expandbahavior === 'exclusive') {
                $collapsable.addClass('webgis-ui-collabsable-exclusive');
            }

            var $collapsableTitle = $("<div>").appendTo($collapsable).addClass('webgis-ui-collapsable-title')
                .click(function () {
                    var $coll = $(this).closest('.webgis-ui-collapsable');
                    var $content = $coll.find('.webgis-ui-collapsable-content');

                    if ($content.css('display') == 'none') {
                        $coll.parent().find('.webgis-ui-collabsable-exclusive').each(function (i, e) {
                            if ($(e).find('.webgis-ui-collapsable-content').css('display') !== 'none') {
                                $(e).find('.webgis-ui-collapsable-title').trigger('click');
                            }
                        });
                        $(this).find("img.collapse-arrow").attr('src', webgis.css.imgResource("expanded-26.png", "ui"));
                        $content.slideDown();

                        $content
                            .find('.webgis-ui-collapsable-autoclick')
                            .trigger('click')
                            .addClass('webgis-loading')
                            .removeClass('webgis-ui-collapsable-autoclick')
                    }
                    else {
                        $(this).find("img.collapse-arrow").attr('src', webgis.css.imgResource("collapsed-26.png", "ui"));
                        $content.slideUp();
                    }
                });
            $("<h2><img src='" + webgis.css.imgResource(show ? "expanded-26.png" : "collapsed-26.png", "ui") + "' class='collapse-arrow' >" +
                (element.type === 'optionscontainer' ? "&nbsp;<div class='webgis-ui-selected-option' style='display:inline-block;height:28px;overflow:hidden'></div>" : "") +
                (element.icon ? "&nbsp;<img src='" + webgis.css.imgResource(element.icon) + "'/>" : "") +
                "&nbsp;" + element.title + "</h2>").appendTo($collapsableTitle);
            $parent = $("<div>").addClass('webgis-ui-collapsable-content').css('display', show ? 'block' : 'none').appendTo($collapsable);
        }
        var $newElement = null;

        if (typeof webgis.ui.builder[element.type + ".create"] === 'function') {
            // type.create  function(map, $parent, element, options) => created jQuery element or null
            //              replaces the whole element creation (no other hooks are used). If nothing is
            //              returned, there are no events, child elements, afterinsert, ... for this element
            const $createdElement = webgis.ui.builder[element.type + ".create"](map, $parent, element, options);
            if ($createdElement instanceof $ && $createdElement.length > 0) {
                $newElement = $createdElement;
            }
        }
        else if (typeof webgis.ui.builder[element.type] === 'function') {
            // builder hooks (all optional), see docs/architecture/ui-builder.md:
            //   type.tagname      tag name (default: div)
            //   type.attributes   additional attributes: string or function(element, options) => string
            //   type.toolid       false => no data-tool-id attribute, even if options.tool is set
            //   type.formelement  true  => mark a surrounding .webgis-form-container dirty on change/keyup
            //   type.properties   false => no elementProperties at all (id, css, style, ... are ignored)
            //   type.afterinsert  function(map, $newElement, element, options), called after the child elements are inserted
            //   type              function(map, $newElement, element, options) => optional jQuery element replacing $newElement
            const tagName = webgis.ui.builder[element.type + ".tagname"] || "div";
            let attributes = webgis.ui.builder[element.type + ".attributes"];
            if (typeof attributes === 'function') {
                attributes = attributes(element, options);
            }
            const propertyOptions = webgis.ui.builder[element.type + ".toolid"] === false ? undefined : options;
            const properties = webgis.ui.builder[element.type + ".properties"] === false ? "" : elementProperties(element, propertyOptions);

            $newElement = $("<" + tagName + (attributes ? " " + attributes : "") + " " + properties + "></" + tagName + ">").appendTo($parent);
            if (webgis.ui.builder[element.type + ".formelement"] === true) {
                addFormElementEvents($newElement);
            }
            // a builder may return a different element (e.g. an inner list), which then gets the events and child elements
            const $builderElement = webgis.ui.builder[element.type](map, $newElement, element, options);
            if ($builderElement instanceof $ && $builderElement.length > 0) {
                $newElement = $builderElement;
            }
        }
        else {
            var repeat = element.repeat || 1;
            for (var i = 0; i < repeat; i++) {
                if (element.type === 'table' &&
                    element.insert_type &&
                    $parent.find('#' + element.id).length > 0) {
                    $newElement = $('#' + element.id);
                    if (element.insert_type === 'replace') {
                        $newElement.empty();
                    }
                } else {
                    $newElement = $("<" + element.type + elementProperties(element, options) + "></" + element.type + ">").appendTo($parent);
                }
            }
        }

        if ($newElement) {
            appendElementEvents(map, options.tool, element, $newElement);
            $newElement.attr('element-target', element.target);
            // Recursive
            if ($newElement && element.elements && element.elements.length > 0) {
                for (var i = 0, to = element.elements.length; i < to; i++) {
                    insertElement(map, $newElement.get(0), element.elements[i], options);
                }
            }
            if (typeof webgis.ui.builder[element.type + ".afterinsert"] === 'function') {
                webgis.ui.builder[element.type + ".afterinsert"](map, $newElement, element, options);
            }
            if (element.type === 'table') {
                if ($newElement.hasClass('webgis-tool-parameter-persistent')) {
                    var elementValue = webgis.tools.getElementValue($newElement.get(0));

                    var tool = options.tool || map.getActiveTool();
                    map.setPersistentToolParameter(tool, $newElement.attr('id'), elementValue);
                }
            }

            if ($newElement.hasClass('webgis-tool-persistent-topic') && $newElement.attr('data-tool-id')) {
                webgis.ui.persistentTopic.deserialize({ map: options.map, element: $newElement, id: $newElement.attr('data-tool-id') });
            }

            if ($newElement.hasClass('webgis-validation')) {
                $newElement.webgis_validation('validate');
                $newElement.change(function () {
                    $newElement.webgis_validation('validate');
                });
            }
        }
    };
    var elementProperties = function (element, options) {
        var css = element.css || '';
        if (element.vis_dependency) {
            css += ' ' + element.vis_dependency;
        }
        switch (element.type) {
            case 'input-text':
            case 'input-date':
            case 'input-textarea':
            case 'input-autocomplete':
            case 'input-number':
                css += ' webgis-input';
                break;
            case 'select':
            case 'querycombo':
            case 'editthemecombo':
            case 'chainagethemecombo':
            case 'labelingcombo':
            case 'sketchselect':
            case 'circleradiuscombo':
            case 'print-query-labelfield-combo':
            case 'print-coordinates-labelfield-combo':
                css += ' webgis-select';
                break;
            case 'button':
                css += ' webgis-button';
                break;
        }
        if (element.visible === false)
            element.style = element.style ? element.style + ';display:none' : 'display:none';
        var ret = '';
        if (element.id)
            ret += " id='" + element.id + "'";
        if (css)
            ret += " class='" + css + "'";
        if (element.parameterservercommands && element.parameterservercommands.length > 0)
            ret += " data-parameter-servercommands='" + element.parameterservercommands.toString() + "'";
        if (element.style)
            ret += " style='" + element.style + "'";
        if (element.type === "td" || (element.value && typeof element.value === "string")) {   // bei Tabelle muss bei jeder Spalte ein data-value Attribut gesetzt werden egal ob was drin seteht... Sonst geht der Coordinatendownload nicht, wenn bei Höhenabfrege nix kommt.
            var val = (element.value || '').replaceAll('\'', '&#39;');
            ret += " data-value='" + val + "'";
            if (element.type === 'input-text' || element.type === 'input-date' || element.type === 'input-autocomplete' || element.type === 'select')
                ret += " value='" + val + "'";
        }
        if (typeof element.minlength === 'number')
            ret += " data-minlength='" + element.minlength + "'";
        if (element.readonly === true)
            ret += " readonly='readonly'";

        if (options && options.tool) {
            ret += " data-tool-id='" + options.tool.id + "'";
        }

        if (element.placeholder) {
            ret += " placeholder='" + element.placeholder + "'";
        }

        return ret;
    };
    // used by builder plugins, which add the element properties to additional elements (e.g. hidden value inputs)
    webgis.ui = webgis.ui || {};
    webgis.ui.elementProperties = elementProperties;

    var appendElementEvents = function (map, tool, element, $newElement) {
        if (!$newElement || $newElement.length === 0)
            return;

        var newElement = $newElement.get(0);
        if (element.onchange) {
            newElement.map = map;
            newElement.onchange_command = element.onchange;
            $newElement.change(function () {
                var tool = this.map.getTool($(this).attr('data-tool-id')) || this.map.getActiveTool();
                if (this.map.getPersistentToolParameter(tool, this.id) !== $(this).val()) {
                    this.map.setPersistentToolParameter(tool, this.id, $(this).val());
                    if (tool.type === 'serverbutton') {
                        webgis.tools.onButtonClick(map, { command: this.onchange_command, type: 'servertoolcommand_ext', id: tool.id, map: this.map/*, originalUiElement: tool.uiElement*/ }, this);
                    } else {
                        webgis.tools.onButtonClick(map, { command: this.onchange_command, type: 'servertoolcommand', id: this.id, map: this.map }, this);
                    }
                }
            });
        }
        else if ($newElement.hasClass('webgis-tool-parameter-persistent')) {
            newElement.map = map;
            if (element.type === 'table') {
                // do nothing
            }
            else {
                $newElement.change(function () {
                    this.map.setPersistentToolParameter(this.map.getActiveTool(), this.id, $(this).val());
                });
            }
        }
        if (element.has_validation && $.fn.webgis_validation) {
            $newElement.webgis_validation({
                minlen: element.validation_minlen,
                regex: element.validation_regex,
                error: element.validation_errormsg,
                required: element.validation_required
            });
        }

        if (element.required_message) {
            $newElement.data('required-message', element.required_message);
        }
    };
    var closeTarget = function (element) {
        return element.type === 'empty' && element.closetarget === true;
    };

    var onCloseTarget = function (map, element) {
        //console.log('onCloseTarget');
        if (map && element && element.targetonclosetype && element.targetonclosecommand) {
            //console.log('send onButtonClick');
            webgis.tools.onButtonClick(map, { type: element.targetonclosetype, command: element.targetonclosecommand });
        }
    }

    var fadeIn = function ($element) {
        $element.removeClass('anim').addClass('fade-in').empty();
        webgis.delayed(function ($e) {
            $e.addClass('anim');
        }, 300, $element);
    };

    var emptyParent = function ($parent, options) {
        //console.log('emptyParent', options);

        var $persistentTopic = $parent.find('.webgis-tool-persistent-topic');
        if ($persistentTopic.length === 1 && $persistentTopic.attr('data-tool-id')) {
            webgis.ui.persistentTopic.serialize({ map: options.map, element: $persistentTopic, id: $persistentTopic.attr('data-tool-id') });
        }

        $parent.empty();
    };

    var addFormElementEvents = function ($element) {
        if ($element.closest('.webgis-form-container').length === 1) {
            $element.bind('change keyup', function () {
                $(this).closest('.webgis-form-container').addClass('webgis-is-dirty');
            })
        }
        return $element;
    };

    webgis.ui.build = function (target, options) {
        return initUI(target, $.extend({}, defaults, options));
    };
    webgis.ui.emptyToolUI = function ($target, options) {
        emptyParent($($target), options);
        return $($target);
    };
    webgis.ui.appendCtrlBBoxInfo = function (target, options) {
        appendCtrlBBoxInfo(target, $.extend({}, defaults, options));
    };
    webgis.ui.removeSecondaryToolUI = removeSecondaryToolUI;
    webgis.ui.addFormElementEvents = addFormElementEvents;

    // jQuery facade (compatibility): $el.webgis_uibuilder(options), ('empty', { map }),
    // ('append_ctrl_bbox_info', options), ('removeSecondoaryToolUI', { map })
    var methods = {
        init: function (options) {
            var $this = $(this);
            if ($this.length == 0)
                return webgis.ui.build(this, options);
            return this.each(function () {
                webgis.ui.build(this, options);
            });
        },
        append_ctrl_bbox_info: function (options) {
            var $this = $(this);
            if ($this.length == 0)
                return webgis.ui.appendCtrlBBoxInfo(this, options);
            return this.each(function () {
                webgis.ui.appendCtrlBBoxInfo(this, options);
            });
        },
        removeSecondoaryToolUI: function (options) {
            removeSecondaryToolUI(options.map);
        },
        empty: function (options) {
            return webgis.ui.emptyToolUI(this, options);
        }
    };
    $.fn.webgis_uibuilder = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_uibuilder');
        }
    };

})(webgis.$ || jQuery);

(function ($) {
    "use strict";
    var defaults = {
        map: null,
        id: null,
        element: null,
        clearDeserialzed: true
    };

    var createHandlerPanel = function (options) {
        var $handler = options.map.ui.webgisContainer().find('.webgis-tool-persistent-topic-handler');
        if ($handler.length === 0) {
            $handler = $("<div>")
                .addClass('webgis-tool-persistent-topic-handler')
                .css('display', 'none')
                .appendTo(options.map.ui.webgisContainer());
        }

        return $handler;
    };

    var serialize = function (options) {
        var $handler = createHandlerPanel(options);
        //console.log('serialize', options);

        if (options.element && options.id) {

            if ($(options.element).find('.webgis-tool-parameter-persistent').length > 0) {
                // ignore persisting topics containing webgis-tool-parameter-persistent => see webgis.tools.js line 533
                console.log('persistent topic contains webgis-tool-parameter-persistent => ignored')
                return;
            }

            var $container = $handler.children(".webgis-persistent-container[data-id='" + options.id + "']");
            if ($container.length === 0) {
                $container = $("<div>")
                    .addClass('webgis-persistent-container')
                    .attr('data-id', options.id)
                    .appendTo($handler);
            }

            $container.empty();

            $(options.element).children().each(function (i, e) {
                $(e).clone(true, true)   // .clone( [withDataAndEvents ] [, deepWithDataAndEvents ] )
                    .appendTo($container);

            });
        }
    };

    var deserialize = function (options) {
        var $handler = createHandlerPanel(options);
        //console.log('deserialize', options, $handler.length);

        if (options.element && options.id) {
            var $target = $(options.element);
            var $container = $handler.children(".webgis-persistent-container[data-id='" + options.id + "']");

            //console.log('$container.length', $container.length)

            if ($container.length > 0) {
                $container.children().each(function (i, e) {
                    $(e).clone(true, true)
                        .appendTo($target);
                });

                if (options.clearDeserialzed) {
                    $container.empty();
                }
            }
        }
    };

    webgis.ui.persistentTopic = {
        serialize: function (options) {
            serialize($.extend({}, defaults, options));
        },
        deserialize: function (options) {
            deserialize($.extend({}, defaults, options));
        }
    };
})(webgis.$ || jQuery);
