(function ($) {
    "use strict";
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
    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            if ($this.length == 0)
                return initUI(this, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        append_ctrl_bbox_info: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            if ($this.length == 0)
                return appendCtrlBBoxInfo(this, options);
            return this.each(function () {
                new appendCtrlBBoxInfo(this, options);
            });
        },
        removeSecondoaryToolUI: function (options) {
            if (options.map) {
                if (options.map.ui.advancedToolBehaviour() === true) {
                    options.map.ui.webgisContainer().find('.webgis-tabs-tab-content').find('.webgis-tooldialog-header.secondary span.close').trigger('click');
                } else {
                    options.map.ui.webgisContainer().webgis_modal('close', { id: constants.serverButtonModalDialogId });
                    options.map.ui.webgisContainer().find('.webgis-tabs-holder').webgis_tabs('hide', { tab: 'tools' });
                }
            }
        },
        empty: function (options) {
            emptyParent($(this), options);
            return $(this);
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
                            break;
                        case "tool_modaldialog_noblocking":
                            modalDialogElement = options.elements[i];
                            hasToolModalDialogElements = true;
                            toolModalHasBlocker = false;
                            toolModalDialogTitle = options.elements[i].targettitle || toolModalDialogTitle;
                            break;
                        case "tool_modaldialog_noblocking_closable":
                            modalDialogElement = options.elements[i];
                            hasToolModalDialogElements = true;
                            toolModalHasBlocker = false;
                            toolModalClosable = true;
                            toolModalDialogTitle = options.elements[i].targettitle || toolModalDialogTitle;
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
                        $parent = options.map.ui.webgisContainer().webgis_modal('content', dialogOptions);

                        if ($parent.length === 0) {
                            options.map.ui.webgisContainer().webgis_modal(dialogOptions);
                            $parent = options.map.ui.webgisContainer().webgis_modal('content', dialogOptions);
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
                    options.map.ui.webgisContainer().webgis_modal('close');
                }
                else {
                    options.map.ui.webgisContainer().webgis_modal({
                        width: modalDialogWidth,
                        height: modalDialogHeight,
                        show: modalDialogVisible,
                        title: modalDialogTitle,
                        slide: false,
                        onclose: () => onCloseTarget(options.map, modalDialogElement),
                    });
                    $targetRoot = $modalContent = options.map.ui.webgisContainer().webgis_modal('content');
                }
            }
            if (hasToolModalDialogElements === true) {
                if (closeToolModalDialog) {
                    options.map.ui.webgisContainer().webgis_modal('close', { id: options.toolDialogId });
                }
                else {
                    options.map.ui.webgisContainer().webgis_modal({
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
                    $targetRoot = $toolModalContent = options.map.ui.webgisContainer().webgis_modal('content', { id: options.toolDialogId });
                }
            }
            if (hasSidebarBottomElements === true) {
                if (closeSidebarBottom) {
                    options.map.ui.webgisContainer().webgis_dockPanel('close', { dock: 'bottom', id: options.toolDialogId });
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
                    options.map.ui.webgisContainer().webgis_dockPanel('close', { dock: 'top', id: options.toolDialogId });
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
                    options.map.ui.webgisContainer().webgis_dockPanel('close', { dock: 'left', id: options.toolDialogId });
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
                    options.map.ui.webgisContainer().webgis_dockPanel('close', { dock: 'right', id: options.toolDialogId + "_aside" });
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
                    options.map.ui.webgisContainer().webgis_modal('fit');
                }
                if (modalDialogVisible === false)
                    options.map.ui.webgisContainer().webgis_modal('hide');
            }
            if (hasToolModalDialogElements === true) {
                options.map.ui.webgisContainer().webgis_modal('fit', { id: options.toolDialogId, dock: 'left' });
                if (toolModalDialogVisible === false)
                    options.map.ui.webgisContainer().webgis_modal('hide', { id: options.toolDialogId });
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

                                        var tool = this.map.getTool($(e).attr('data-tool-id')) || this.map.getActiveTool();
                                        $(e).val(setter.val || $(e).data('initial-value') || this.map.getPersistentToolParameter(tool, this.id));
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

                            if ((val && ($.inArray("*", contition_arguments) >= 0).toString() == contition_result.toString()) ||
                                ($.inArray(val, contition_arguments) >= 0).toString() == contition_result.toString()) {
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

        if (typeof webgis.ui.builder[element.type] === 'function') {
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
                $newElement.add_webgis_form_element_events();
            }
            // a builder may return a different element (e.g. an inner list), which then gets the events and child elements
            const $builderElement = webgis.ui.builder[element.type](map, $newElement, element, options);
            if ($builderElement instanceof $ && $builderElement.length > 0) {
                $newElement = $builderElement;
            }
        }
        else if (element.type === "graphics-info-container") {
            $newElement = $("<div></div>").appendTo($parent);
            if ($.fn.webgis_graphicsInfoContainer) {
                $newElement.webgis_graphicsInfoContainer({ map: options.map });
            }
        }
        else if (element.type === "graphics-info-stage") {
            $newElement = $("<div></div>").appendTo($parent);
            if ($.fn.webgis_graphicsInfoStage) {
                $newElement.webgis_graphicsInfoStage({ map: options.map });
            }
        }
        else if (element.type === "sketch-info-container") {
            // User preference (Burger menu -> Einstellungen -> Benutzer Einstellungen) lets the
            // user turn the sketch info overlay off entirely, or reduce it to only the
            // snapping/construction info (hiding geometry type/segment/section details).
            var sketchInfoDisplayMode = (webgis.usability.userPreferences && webgis.usability.userPreferences.get)
                ? (webgis.usability.userPreferences.get("sketch-info-display-mode") || "default")
                : "default";

            var showSketchInfo = sketchInfoDisplayMode !== "hidden";
            if (webgis.useMobileCurrent() ||                                          // 1. auf kleien mobilegeräten nicht anzeigen
                (webgis.isMobileDevice() && webgis.usability.clickBubble == false))   // 2. auf Mobilen Geräten ohne Bubble => mach keinen Sinn
                showSketchInfo = false;

            if (showSketchInfo) {
                // Preferred: render as an overlay docked above the coordinate display (grows
                // upwards, only visible while the cursor is over the map), instead of taking up
                // space in the tool dialog itself.
                var $sketchInfoTarget = options.map && options.map.sketchInfoOverlayContainer ?
                    options.map.sketchInfoOverlayContainer() :
                    null;

                var sketchInfoMinimal = sketchInfoDisplayMode === "minimal";

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
            }
        }
        else if (element.type === 'sharelink-buttons')
        {
            $newElement = $("<div>")
                .addClass('contains-labels webgis-ui-optionscontainer')
                .appendTo($parent);

            if (element.link) {
                for (var o in webgis.shareOptions) {
                    var shareOption = webgis.shareOptions[o];
                    if (shareOption.available()) {
                        var $imageButton = $("<div>")
                            .addClass('webgis-ui-imagebutton')
                            .css('background-image', 'url(' + shareOption.img() + ')')
                            .appendTo($newElement)
                            .data('link', element.link)
                            .data('subject', element.subject)
                            .data('qr_base64', element.qr_base64)
                            .data('shareOption', shareOption)
                            .click(function () {
                                var shareOption = $(this).data('shareOption');
                                shareOption.share($(this).data('link'), $(this).data('subject'), $(this).data('qr_base64'));
                            });
                        $("<div>")
                            .addClass('webgis-ui-imagebutton-text')
                            .text(shareOption.name)
                            .appendTo($imageButton);
                    }
                }
            }
        }
        else if (element.type === 'literal') {
            $parent.html($parent.html() + webgis.encodeUntrustedHtml(element.literal, true));
        }
        else if (element.type === 'literal-bold') {
            $parent.html($parent.html() + '<strong>' + webgis.encodeUntrustedHtml(element.literal, true) + '</strong>');
        }
        else if (element.type === 'image') {
            $newElement = $("<img " + elementProperties(element, options) + " src='" + element.src + "' />").appendTo($parent);
        }
        else if (element.type === 'input-bbox') {
            $newElement = $("<div " + elementProperties(element, options) + "></div>").appendTo($parent);
            $newElement.webgis_bbox_input({ readonly: element.readonly });
        }
        else if (element.type === 'input-size') {
            $newElement = $("<div " + elementProperties(element, options) + "></div>").appendTo($parent);
            $newElement.webgis_size_input({ readonly: element.readonly });
        }
        else if (element.type === 'condition_div' && element.condition_arguments && element.condition_arguments.length > 0) {
            var result = element.codition_logical_operator === 'and' ? true : false;

            $newElement = $("<div " + elementProperties(element, options) + "></div>")
                .addClass('webgis-dependencies webgis-dependency-' + element.condition_type)
                .attr('data-dependency-arguments', element.condition_arguments)
                .attr('data-dependency-logical_operator', element.condition_logical_operator)
                .attr('data-dependency-condition_result', element.condition_result)
                .attr('data-dependency-element-id', element.contition_element_id)
                .appendTo($parent);

            if (element.condition_type == 'elementvalue') {
                $newElement.addClass('webgis-ui-added')
            }
        }
        else if (element.type === 'opacity-control') {
            $newElement = $("<div " + elementProperties(element, options) + "></div>").appendTo($parent);
            $newElement.webgis_opacity_control({ service: map.getService(element.serviceId) });
        }
        //else if (element.type === "query-builder") { 
        //    $newElement = $("<div " + elementProperties(element, options) + "></div>").appendTo($parent);
        //    $newElement.webgis_queryBuilder({ map: map, id: element.id, field_defs: element.field_defs, show_geometry_option: element.show_geometry_option, event: options.event });
        //}
        else if (element.type === "ui-liveshare") {
            $newElement = $("<div " + elementProperties(element, options) + "></div>").appendTo($parent);
            $newElement.webgis_liveshare();
        }
        else if (element.type === "static-overlay-control") {
            $newElement = $("<div " + elementProperties(element, options) + "></div>").appendTo($parent);
            $newElement.webgis_staticOverlayControl({ map: map, command_buttons: element.command_buttons });
        }
        else if (element.type === "image-selector") {
            $newElement = $("<div " + elementProperties(element, options) + "></div>").appendTo($parent).webgis_image_selector({
                image_urls: element.image_urls,
                image_labels: element.image_labels,
                image_width: element.image_width,
                image_height: element.image_height,
                multi_select: element.multi_select,
                selected: element.value,
            });
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
                $(null).webgis_tool_persist_topic_handler('deserialize', { map: options.map, element: $newElement, id: $newElement.attr('data-tool-id') });
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
            $(null).webgis_tool_persist_topic_handler('serialize', { map: options.map, element: $persistentTopic, id: $persistentTopic.attr('data-tool-id') });
        }

        $parent.empty();
    };

})(webgis.$ || jQuery);

(function ($) {
    $.fn.webgis_collapsable_paragraph = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_collapsable_paragraph');
        }
    };
    var defaults = {
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            if ($this.length === 0)
                return initUI(this, options);
            return this.each(function () {
                new initUI(this, options);
            });
        }
    };
    var initUI = function (item, options) {
        webgis.delayed(function () {
            var $item = $(item);
            var height = $item.height();
            $item.addClass('webgis-collapsable-paragraph collapsed');
            if ($item.height() < height) {
                console.log(height, $item.height())
                $("<div></div>")
                    .addClass('switcher')
                    .appendTo($item)
                    .click(function (e) {
                        e.stopPropagation();
                        $(this).closest('.webgis-collapsable-paragraph').toggleClass('collapsed');
                    });
            } else {
                $item.css('height', 'auto');
            }
        }, 100);
    };
})(webgis.$ || jQuery);

(function ($) {
    $.fn.webgis_bbox_input = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_bbox_input');
        }
    };
    var defaults = {
        readonly: false
    };
    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        },
        val: function (options) {
            if (typeof options === 'string') {
                var $this = $(this);

                var vals = options.split(',');
                if (vals.length == 4) {
                    $this.find('.webgis-bbox-value').val(options);
                    $this.find('.webgis-bbox-minx').val(parseFloat(vals[0]));
                    $this.find('.webgis-bbox-miny').val(parseFloat(vals[1]));
                    $this.find('.webgis-bbox-maxx').val(parseFloat(vals[2]));
                    $this.find('.webgis-bbox-maxy').val(parseFloat(vals[3]));
                }
            }

            return $(this).find('.webgis-bbox-value').val();
        }
    };
    var initUI = function (item, options) {
        var $item = $(item)
            .addClass('webgis-bbox-input-holder');
       

        var $row1 = $("<div>").addClass('row').appendTo($item);
        var $row2 = $("<div>").addClass('row').appendTo($item);
        var $row3 = $("<div>").addClass('row').appendTo($item);

        $("<input type='number' class='webgis-bbox-input webgis-bbox-maxy'>")
            .appendTo($row1);
        $("<input type='number' class='webgis-bbox-input webgis-bbox-minx'>")
            .appendTo($row2);
        $("<input type='number' class='webgis-bbox-input webgis-bbox-maxx'>")
            .appendTo($row2);
        $("<input type='number' class='webgis-bbox-input webgis-bbox-miny'>")
            .appendTo($row3);

        $valueElement = $("<input type='hidden' class='webgis-bbox-value' />")
            .appendTo($item);

        $item.data('webgis-value-element', $valueElement);

        $item.find('.webgis-bbox-input')
            .change(function () {
                var $holder = $(this).closest('.webgis-bbox-input-holder');
                var val = $holder.find('.webgis-bbox-minx').val() + "," +
                          $holder.find('.webgis-bbox-miny').val() + "," +
                          $holder.find('.webgis-bbox-maxx').val() + "," +
                          $holder.find('.webgis-bbox-maxy').val();

                $holder.find('.webgis-bbox-value').val(val);
            });

        if (options.readonly === true) {
            $item.find('.webgis-bbox-input').attr('readonly', 'readonly');
        }
    };
})(webgis.$ || jQuery);

(function ($) {
    $.fn.webgis_size_input = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_size_input');
        }
    };
    var defaults = {
        readonly: false
    };
    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        },
        val: function (options) {
            if (typeof options === 'string') {
                var $this = $(this);

                var vals = options.split(',');
                if (vals.length == 2) {
                    $this.find('.webgis-size-value').val(options);
                    $this.find('.webgis-size-x').val(parseFloat(vals[0]));
                    $this.find('.webgis-size-y').val(parseFloat(vals[1]));
                }
            }

            return $(this).find('.webgis-size-value').val();
        }
    };
    var initUI = function (item, options) {
        var $item = $(item).addClass('webgis-size-input-holder');

        $("<input type='number' class='webgis-size-input webgis-size-x'>")
            .appendTo($item);
        $("<div>x</div>").appendTo($item);
        $("<input type='number' class='webgis-size-input webgis-size-y'>")
            .appendTo($item);

        $("<input type='hidden' class='webgis-size-value' />")
            .appendTo($item);

        $item.find('.webgis-size-input')
            .change(function () {
                var $holder = $(this).closest('.webgis-size-input-holder');
                var val = $holder.find('.webgis-size-x').val() + "," +
                          $holder.find('.webgis-size-y').val();

                $holder.find('.webgis-size-value').val(val);
            });

        if (options.readonly === true) {
            $item.find('.webgis-size-input').attr('readonly', 'readonly');
        }
    };
})(webgis.$ || jQuery);

(function ($) {
    $.fn.webgis_general_input = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_general_input');
        }
    };
    var defaults = {
        type: 'text',
        name: null,
        inputElement: 'input',
        label: null,
        placeholder: null,
        onInit:null
    };
    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        },
        val: function (options) {
            return $(this).children("input[name='" + options.name + "']").val();
        },
        clear: function (options) {
            $(this).children("input[name='" + options.name + "']").val('');
        }
    };
    var initUI = function (parent, options) {
        var $parent = $(parent);

        if ($parent.children('.webgis-input').length > 0) {
            $("<br/>").appendTo($parent);
        }

        if (options.label) {
            var $label = $("<div>")
                .addClass("webgis-label")
                .text(options.label + ':')
                .appendTo($parent);

            if (options.description) {
                $("<span>")
                    .css('display', 'inline-block')
                    .addClass('webgis-api-icon webgis-api-icon-help')
                    .appendTo($label);

                var description = options.description, isMarkdown=false;
                if (description.indexOf('md:') === 0) {
                    description = description.substr(3);
                    isMarkdown = true;
                }

                $("<div>")
                    .addClass('webgis-label-description')
                    .html(webgis.encodeUntrustedHtml(description, isMarkdown))
                    .appendTo($parent);

                $label.click(function () {
                    $(this).next('.webgis-label-description').toggleClass('show');
                });
            }
            $("<br/>").appendTo($parent);
        }

        if (options.inputElement) {
            var $input = $("<" + options.inputElement + " name='" + options.name + "' type='" + options.type + "'>")
                .addClass('webgis-input')
                .appendTo($parent);

            if (options.placeholder) {
                $input.attr('placeholder', options.placeholder);
            }
        }

        if (options.onInit) {
            options.onInit($parent, $input);
        }
    };
})(webgis.$ || jQuery);

(function ($) {
    $.fn.webgis_map_contextmenu = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_map_contextmenu');
        }
    };
    var defaults = {
        map: null,
        clickX: 0,
        clickY: 0,
        force: false
    };
    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        },
        hide: function () {
            if ($._webgisMapContextMenu) {
                $._webgisMapContextMenu.css('display', 'none');
            }
        }
    };

    $._webgisMapContextMenu = null;

    var initUI = function (parent, options) {
        if (!options.map)
            return;

        var $parent = parent ? $(parent) : $('body');
        var map = options.map;

        var toolContextVisible = false;
        $('body').find('.webgis-contextmenu.webgis-tool-contextmenu').each(function (i, e) {
            if ($(e).css('display') !== 'none') {
                toolContextVisible = true;
            }
        });
        if (toolContextVisible == true) {
            if (options.force) {
                $('body').find('.webgis-contextmenu.webgis-tool-contextmenu').css('display', 'none');
            } else {
                return;
            }
        }

        if ($._webgisMapContextMenu == null) {
            $._webgisMapContextMenu = $("<div class='webgis-contextmenu icons' style='z-index:9999999;display:none;position:absolute;background:#fff' oncontextmenu='return false;'>")
                .appendTo($parent)
                .click(function () {
                    map.resetShowServiceExcusives();
                    $(null).webgis_map_contextmenu('hide')
                });

            map.events.on(['refresh', 'zoomstart', 'movestart','mousedown'], function (e, sender) {
                $(null).webgis_map_contextmenu('hide');
            });
            map.events.on(['onfocusservices', 'onresestfocusservices'], function (e, sender) {
                _refreshItems();
            });
        }

        $._webgisMapContextMenu.empty();
        var $menu = $("<ul>").addClass('webgis-toolbox-tool-item-group-details')
            .css({ padding: 0, margin: 0, minWidth: '200px' })
            .appendTo($._webgisMapContextMenu);

        var $header = $("<li style='font-weight:bold;padding: 16px 10px 10px 10px'>")
            .addClass('webgis-menu-item-title')
            .appendTo($menu);
        $("<i>").text(map.name).appendTo($header);
        $("<div>").addClass('webgis-close-button').appendTo($header);
        

        //  Remove All Visfilters
        $("<li>")
            .text('Alle Darstellungsfilter entfernen')
            .css('background-image', 'url(' + webgis.css.imgResource('filter-remove-26.png', 'tools') + ')')
            .addClass('webgis-tool-button webgis-dependencies webgis-dependency-hasfilters')
            .appendTo($menu)
            .click(function (e) {
                map.unsetAllFilters();
                map.refresh();
                map.ui.refreshUIElements();
            });

        //  Remove all queryresults
        $("<li>")
            .text('Alle (Such)ergebnisse aus der Karte entfernen')
            .css('background-image', 'url(' + webgis.css.imgResource('marker-remove-26.png', 'tools') + ')')
            .addClass('webgis-tool-button webgis-dependencies webgis-dependency-queryresultsexists')
            .appendTo($menu)
            .click(function (e) {
                var removeButton = $(map._webgisContainer).find('.webgis-toolbox-tool-item.remove-queryresults');
                if (removeButton.length > 0) {
                    removeButton.click();
                } else {
                    map.queryResultFeatures.clear(false);
                    map.getSelection('selection').remove();
                }
                map.unloadDynamicContent();
            });

        // reset (service) focus
        $("<li>")
            .text('Focus auf Dienst entfernen')
            .css('background-image', 'url(' + webgis.css.imgResource('focus_remove-26.png', 'tools') + ')')
            .addClass('webgis-tool-button webgis-dependencies webgis-dependency-focused-service-exists')
            .appendTo($menu)
            .click(function (e) {
                map.resetFocusServices();
            });

        
        if (map.services) {
            $("<li style='font-weight:bold'><i>" + webgis.l10n.get("map-services") + "</i></li>")
                .appendTo($menu);

            var sortedServices = map.sortedServices("name");

            // Background
            var $menuItem = $("<li>")
                .text(webgis.l10n.get("basemaps"))
                .addClass('basemap')
                .css('background-image', 'url(' + webgis.css.imgResource('tile-service.png') + ')')
                .appendTo($menu)
                .click(function (e) {
                    e.stopPropagation();
                    var $this = $(this);

                    if ($this.hasClass('subitems-collapsed')) {
                        $this.removeClass('subitems-collapsed');
                        $this.parent().children().css('display', 'block').removeClass('webgis-menu-item-hidden');
                        $this.parent().find('li.webgis-submenu-item').addClass('webgis-menu-item-hidden');
                        map.ui.refreshUIElements();
                    } else {
                        $this.addClass('subitems-collapsed');
                        $this.parent().children('li').addClass('webgis-menu-item-hidden');
                        $this.parent().children('.webgis-menu-item-title').removeClass('webgis-menu-item-hidden');
                        $this.parent().children('.basemap').removeClass('webgis-menu-item-hidden');
                    }
                });

            addBasemapSubItems($menu, map);

            // Map Services
            for (var s in sortedServices) {
                var service = sortedServices[s];

                serviceClass = service.id.replace('')
                if (service.isBasemap === true) {
                    continue;
                }

                if (service.isWatermark()) {
                    continue;
                }

                $menuItem = $("<li>")
                    .addClass(service.guid)
                    .addClass('service-item')
                    .text(service.name)
                    .css('background-image', 'url(' + webgis.css.imgResource('image-service.png')+ ')')
                    .data('service', service)
                    .appendTo($menu)
                    .click(function (e) {
                        e.stopPropagation();
                        var $this = $(this);
                        var service = $(this).data('service');

                        if ($this.hasClass('subitems-collapsed')) {
                            $this.removeClass('subitems-collapsed');
                            $this.parent().children().css('display', 'block').removeClass('webgis-menu-item-hidden');
                            $this.parent().find('li.webgis-submenu-item').addClass('webgis-menu-item-hidden');
                            map.ui.refreshUIElements();
                        } else {
                            $this.addClass('subitems-collapsed');
                            $this.parent().children('li').addClass('webgis-menu-item-hidden');
                            $this.parent().children('.webgis-menu-item-title').removeClass('webgis-menu-item-hidden');
                            $this.parent().children('.' + service.guid).removeClass('webgis-menu-item-hidden');
                        }
                    })
                .mouseenter(function () {
                    var service = $(this).data('service');
                    map.showServiceExclusive(service.id, 0.08);
                })
                .mouseleave(function () {
                    map.resetShowServiceExcusives();
                });

                addServiceSubItems($menu, service);

                if (!service.hasVisibleLayersInScale()) {
                    $menuItem.addClass('webgis-outofscale');
                }
            }
        }

        $menu.find('li').addClass('webgis-toolbox-tool-item');
        $menu.find('li.webgis-submenu-item').addClass('webgis-menu-item-hidden');

        $._webgisMapContextMenu.css('display', 'inline-block');

        var left = Math.max(0, Math.min($(window).width() - $._webgisMapContextMenu.width() - 10, options.clickX)),
            top = Math.max(0, Math.min($(window).height() - $._webgisMapContextMenu.height() - 10, options.clickY)),
            maxHeight = $(window).height() - top - 10;

        console.log('maxHeight', $(window).height() - top - 20);

        $._webgisMapContextMenu.css({
            left: left,
            top: top,
            maxHeight: maxHeight,
            overflow: 'auto'
        });

        _refreshItems();
        map.ui.refreshUIElements();
    };

    var addBasemapSubItems = function ($menu, map) {
        var sortedServices = map.services; // map.sortedServices("name");

        // Basemaps
        var $basemapOpacityItem = null;

        $menuItem = $("<li>")
            .addClass('webgis-submenu-item service basemap')
            .addClass(!map.currentBasemapServiceId() ? 'selected' : '')
            .text(webgis.l10n.get("no-basemap"))
            //.css('background-image', 'url(' + webgis.css.imgResource('tile-service.png') + ')')
            .css({
                paddingLeft: '48px'
            })
            .appendTo($menu)
            .click(function (e) {
                e.stopPropagation();
                var $this = $(this);
              
                map.setBasemap(null, false);
                addOpacityImageContainer($basemapOpacityItem, null);

                $this.parent().children('.webgis-submenu-item.service.basemap.selected').removeClass('selected');

                // Overlay
                map.setBasemap(null, true);
                $this.parent().children('.webgis-submenu-item.service.basemap.checked').removeClass('checked').addClass('unchecked');

                $this.addClass('selected');
            });

        for (var s in sortedServices) {
            var service = sortedServices[s];
            if (service.isBasemap === true && service.basemapType !== 'overlay') {
                $menuItem = $("<li>")
                    .addClass('webgis-submenu-item service basemap')
                    .addClass(service.id === map.currentBasemapServiceId() ? 'selected' : '')
                    .text(service.name)
                    //.css('background-image', 'url(' + webgis.css.imgResource('tile-service.png') + ')')
                    .css({
                        backgroundImage: 'url(' + service.getSampleTileUrl() + ')',
                        backgroundSize: '41px',
                        backgroundPosition: '0px 0px',
                        paddingLeft: '48px'
                    })
                    .data('service', service)
                    .appendTo($menu)
                    .click(function (e) {
                        e.stopPropagation();
                        var $this = $(this);
                        var service = $this.data('service');

                        map.setBasemap(service.id, false);

                        var basemapService = map.getService(map.currentBasemapServiceId());
                        if (basemapService) {
                            addOpacityImageContainer($basemapOpacityItem, basemapService);
                        }

                        $this.parent().children('.webgis-submenu-item.service.basemap.selected').removeClass('selected');
                        $this.addClass('selected');
                    });
            }
        }

        // Overlay Basemaps
        for (var s in sortedServices) {
            var service = sortedServices[s];
            if (service.isBasemap === true && service.basemapType === 'overlay') {
                $menuItem = $("<li>")
                    .addClass('webgis-submenu-item service basemap')
                    .addClass(map.currentBasemapOverlayServiceIds().includes(service.id) ? 'checked' : 'unchecked')
                    .css({
                        backgroundPosition: '13px center',
                        paddingLeft: '48px'
                    })
                    .text(service.name)
                    .data('service', service)
                    .appendTo($menu)
                    .click(function (e) {
                        e.stopPropagation();
                        var $this = $(this);

                        if ($this.hasClass('checked')) {
                            map.setBasemap(null, true);
                            $this.removeClass('checked').addClass('unchecked');
                        } else {
                            var service = $this.data('service');
                            map.setBasemap(service.id, true);
                            $this.removeClass('unchecked').addClass('checked');
                        }
                    });
            }
        }

        $basemapOpacityItem = $("<li>")
            .text(webgis.l10n.get("opacity"))
            .css('background-image', 'url(' + webgis.css.imgResource('opacity_0-26.png', 'tools') + ')')
            .addClass('webgis-submenu-item service basemap')
            .data('service', service)
            .appendTo($menu);

        addOpacityImageContainer($basemapOpacityItem, map.getService(map.currentBasemapServiceId()));
    };

    var addServiceSubItems = function ($menu, service) {
        if (!service || !service.map)
            return;

        $("<li>")
            .text(webgis.l10n.get("remove-service"))
            .css('background-image', 'url(' + webgis.css.imgResource('remove.png', 'tools') + ')')
            .addClass('webgis-submenu-item service')
            .addClass(service.guid)
            .data('service', service)
            .appendTo($menu)
            .click(function () {
                var service = $(this).data('service');
                webgis.confirm({
                    message: webgis.l10n.get('confirm-remove-service'),
                    cancelText: webgis.l10n.get('confirm-remove-service-cancel'),
                    okText: webgis.l10n.get('confirm-remove-service-ok'),
                    onOk: function () {
                        service.map.removeServices([service.id]);
                    }
                });
            })

        $("<li>")
            .text(webgis.l10n.get("service-order"))
            .css('background-image', 'url(' + webgis.css.imgResource('rest/toolresource/webgis-tools-serviceorder-service_order', 'tools') + ')')
            .addClass('webgis-submenu-item service')
            .addClass(service.guid)
            .data('service', service)
            .appendTo($menu)
            .click(function () {
                var service = $(this).data('service');
                webgis.modalDialog(webgis.l10n.get("service-order"),
                    function ($context) {
                        $context.webgis_serviceOrder({ map: service.map, selected: service.id });
                    });
            });

        $("<li>")
            .text(webgis.l10n.get("legend"))
            .css('background-image', 'url(' + webgis.css.imgResource('legend.png', 'tools') + ')')
            .addClass('webgis-submenu-item service')
            .addClass(service.guid)
            .data('service', service)
            .appendTo($menu)
            .click(function () {
                var service = $(this).data('service');
                $.presentationToc.showLegend(this, service.map, [service], service.name);
            });

        var $focusItem = $("<li>")
            .text(webgis.l10n.get("focus-service"))
            .css('background-image', 'url(' + webgis.css.imgResource('focus-26.png', 'tools') + ')')
            .addClass('webgis-submenu-item service')
            .addClass(service.guid)
            .appendTo($menu)
        addFocusImageContainer($focusItem, service);

        var $opacityItem = $("<li>")
            .text(webgis.l10n.get("opacity"))
            .css('background-image', 'url(' + webgis.css.imgResource('opacity_0-26.png', 'tools') + ')')
            .addClass('webgis-submenu-item service')
            .addClass(service.guid)
            .data('service', service)
            .appendTo($menu);
        addOpacityImageContainer($opacityItem, service);
    };

    var addOpacityImageContainer = function ($parent, service) {
        if (!service) {
            $parent.css('display', 'none');
            return;
        }

        $parent
            .addClass('webgis-menu-imagebutton-container-holder')
            .css('display', '');

        $parent.webgis_opacity_control({ service: service });
    };

    var addFocusImageContainer = function ($parent, service) {
        $parent.webgis_focusservice_control({ service: service });
    };

    var _refreshItems = function () {
        if (!$._webgisMapContextMenu)
            return;

        $($._webgisMapContextMenu).find('.webgis-toolbox-tool-item.service-item').each(function (i, item) {
            var $item = $(item);

            var service = $item.data('service');
            if (!service || !service.map)
                return;

            var focusedServices = service.map.focusedServices();
            var imgUrl = webgis.css.imgResource('image-service.png');
            if (focusedServices && focusedServices.ids && $.inArray(service.id, focusedServices.ids) >= 0) {
                imgUrl = webgis.css.imgResource('focus-26.png', 'tools');
            }

            $item.css('background-image', 'url(' + imgUrl + ')')
        });
    };
})(webgis.$ || jQuery);

(function ($) {
    $.fn.webgis_opacity_control = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_opacity_control');
        }
    };
    var defaults = {
        service: null
    };
    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        },
        refresh: function (options) {
            return this.each(function () {
                new refresh(this);
            });
        }
    };

    $.webgis_opacity_control_globals = [];

    var initUI = function (parent, options) {
        var $parent = $(parent);

        $parent
            .children('.webgis-menu-item-imagebutton-container.opacity')
            .remove();

        var service = getService(options.service);
        if (!service || !service.map)
            return;

        var map = service.map;

        if (!$.webgis_opacity_control_globals[map.guid]) {
            $.webgis_opacity_control_globals[map.guid] = map;
            map.events.on(['onfocusservices', 'onresestfocusservices'], function (e, sender) {
                checkMapFocusSettings();
            });
        }

        var $opacityItemButtonContainer = $("<div>")
            .data('service', options.service)
            .addClass('webgis-opacity-control-holder webgis-menu-item-imagebutton-container opacity')
            .appendTo($parent);

        $("<div>")
            .addClass('webgis-menu-item-imagebutton webgis-dependencies webgis-dependency-focused-service-exists')
            .css('background-image', 'url(' + webgis.css.imgResource('focus_remove-26.png', 'tools') + ')')
            .data('map', map)
            .appendTo($opacityItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                var map = $(this).data('map');
                map.resetFocusServices();
            });

        $("<div>")
            .addClass('webgis-menu-item-imagebutton 25')
            .css('background-image', 'url(' + webgis.css.imgResource('opacity_25-26.png', 'tools') + ')')
            .appendTo($opacityItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                setServiceOpacity(this, 0.25);
            });
        $("<div>")
            .addClass('webgis-menu-item-imagebutton 50')
            .css('background-image', 'url(' + webgis.css.imgResource('opacity_50-26.png', 'tools') + ')')
            .appendTo($opacityItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                setServiceOpacity(this, 0.50);
            });
        $("<div>")
            .addClass('webgis-menu-item-imagebutton 75')
            .css('background-image', 'url(' + webgis.css.imgResource('opacity_75-26.png', 'tools') + ')')
            .appendTo($opacityItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                setServiceOpacity(this, 0.75);
            });
        $("<div>")
            .addClass('webgis-menu-item-imagebutton 100')
            .css('background-image', 'url(' + webgis.css.imgResource('opacity_100-26.png', 'tools') + ')')
            .appendTo($opacityItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                setServiceOpacity(this, 1);
            });

        $opacityItemButtonContainer
            .find('.webgis-menu-item-imagebutton.' + Math.round(service.getOpacity() * 100))
            .addClass('selected');

        checkMapFocusSettings();
        map.ui.refreshUIElements();
    };

    var setServiceOpacity = function (button, opacity) {
        const $button = $(button), $holder = $button.closest('.webgis-opacity-control-holder');
        if ($button.hasClass('disabled'))
            return;

        const service = getService($holder.data('service'));

        applyServiceOpacity(service, opacity);

        $button.parent().children().removeClass('selected');
        $button.addClass('selected');
    };

    var applyServiceOpacity = function (service, opacity) {
        if (service) {
            service.setOpacity(opacity);
            if (service.map && service.isBasemap === true) {
                for (let overlayId of service.map.currentBasemapOverlayServiceIds()) {
                    const overlayService = service.map.getService(overlayId);
                    if (overlayService) {
                        overlayService.setOpacity(opacity);
                    }
                }
            }
        }
    };

    var getService = function (service) {
        if (typeof service === "function") {
            service = service();
        };

        return service;
    };

    var checkMapFocusSettings = function () {
        refresh(null);
    };

    var refresh = function (elem) {
        var $elem = $(elem || 'body');
        if ($elem.hasClass('webgis-opacity-control-holder') === false) {
            $elem = $elem.find('.webgis-opacity-control-holder');
        }

        $elem.each(function (i, e) {
            var $holder = $(e);
            var service = getService($holder.data('service'));

            if (service && service.map) {
                var focusedServices = service.map.focusedServices();

                if (focusedServices) {
                    $holder.css('opacity', .8);

                    var serviceOpacity = focusedServices.ids && $.inArray(service.id, focusedServices.ids) >= 0 ? 1.0 : focusedServices.opacity;

                    $holder.children('.webgis-menu-item-imagebutton')
                        .css('display', 'none')
                        .removeClass('selected');
                    $holder.children('.webgis-menu-item-imagebutton.' + Math.round(serviceOpacity * 100))
                        .addClass('disabled')
                        .css('display', '')
                } else {
                    $holder.removeClass('disabled').css('opacity', '');

                    $holder.children('.webgis-menu-item-imagebutton')
                        .css('display', '')
                        .removeClass('disabled')
                        .removeClass('selected');
                    $holder.children('.webgis-menu-item-imagebutton.' + service.getOpacity() * 100)
                        .addClass('selected');

                    if (service.isBasemap) {
                        applyServiceOpacity(service, service.getOpacity());  // set all overlay basemap to the same opacity
                    }
                }
            }
        });
    }
})(webgis.$ || jQuery);

(function ($) {
    $.fn.webgis_focusservice_control = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_focusservice_control');
        }
    };
    var defaults = {
        service: null
    };

    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        }
    };

    var initUI = function (parent, options) {
        var $parent = $(parent);
        var service = options.service;
        if (!service)
            return;

        $parent
            .addClass('webgis-menu-imagebutton-container-holder')
            .children('.webgis-menu-item-imagebutton-container.focus')
            .remove();

        var $focusItemButtonContainer = $("<div>")
            .addClass('webgis-menu-item-imagebutton-container focus')
            .appendTo($parent);

        $("<div>")
            .addClass('webgis-menu-item-imagebutton webgis-dependencies webgis-dependency-focused-service-exists')
            .css('background-image', 'url(' + webgis.css.imgResource('focus_remove-26.png', 'tools') + ')')
            .data('service', service)
            .appendTo($focusItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                var service = $(this).data('service');
                service.map.resetFocusServices();
            });
        $("<div>")
            .addClass('webgis-menu-item-imagebutton')
            .data('service', service)
            .css('background-image', 'url(' + webgis.css.imgResource('focus_75-26.png', 'tools') + ')')
            .appendTo($focusItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                var service = $(this).data('service');
                service.map.focusServices(service.id, 0.75);
            });
        $("<div>")
            .addClass('webgis-menu-item-imagebutton')
            .data('service', service)
            .css('background-image', 'url(' + webgis.css.imgResource('focus_50-26.png', 'tools') + ')')
            .appendTo($focusItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                var service = $(this).data('service');
                service.map.focusServices(service.id, 0.5);
            });
        $("<div>")
            .addClass('webgis-menu-item-imagebutton')
            .data('service', service)
            .css('background-image', 'url(' + webgis.css.imgResource('focus_25-26.png', 'tools') + ')')
            .appendTo($focusItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                var service = $(this).data('service');
                service.map.focusServices(service.id, 0.25);
            });
        
    };
})(webgis.$ || jQuery);

(function ($) {
    $.fn.webgis_form = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_focusservice_control');
        }
    };

    var defaults = {
        name: null,
        input: [
            //{
            //    label :'input',
            //    name: '',
            //    type: 'input',
            //    value: null,
            //    placeholder: null
            //}, ...
        ],
        onSubmit: null,
        submitText: 'Sumit'
    };

    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        }
    };

    $._webgis_form_persistent = [];

    var initUI = function (parent, options) {
        var $parent = $(parent)
            .addClass('webgis-form')
            .click(function (e) {
                e.stopPropagation();  // e.g. if the form is included in an context menu => do not close the menu on click
            });

        for (var i in options.input) {
            var input = options.input[i];

            var $inputGroup = $("<div>")
                .addClass('webgis-input-group')
                .appendTo($parent);

            if (input.label) {
                $("<div>")
                    .addClass('webgis-label')
                    .text(input.label)
                    .appendTo($inputGroup);
                $("<br>")
                    .appendTo($inputGroup);
            }

            if (input.type === 'select') {
                $input = $("<select>")
                    .addClass('webgis-input')
                    .attr('name', input.name)
                    .appendTo($inputGroup);

                if (input.options) {
                    for (var o in input.options) {
                        var option = input.options[o];
                        $("<option>")
                            .attr('value', option.value)
                            .text(option.text)
                            .appendTo($input);
                    }
                }
            } else {
                $input = $("<input>")
                    .addClass('webgis-input')
                    .attr('type', input.type || 'text')
                    .attr('name', input.name)
                    .attr('placeholder', input.placeholder || '')
                    .appendTo($inputGroup)
                    .keypress(function (e) {
                        var key = e.which;
                        if (key === 13)  // the enter key code
                        {
                            $(this).closest('.webgis-form').children('.webgis-form-submit').trigger('click');
                            return false;
                        }

                        $(this).closest('.webgis-form').children('.webgis-form-errors').empty();
                    });
            }

            if (input.value != null) {
                $input.val(input.value);
            } else if (options.name &&
                      $._webgis_form_persistent[options.name] &&
                      $._webgis_form_persistent[options.name][input.name] != null) {
                $input.val($._webgis_form_persistent[options.name][input.name]);
            }

            if (input.required) {
                $input.attr('required', 'required');
            }
        }

        $("<div>")
            .css('color', 'red')
            .addClass('webgis-form-errors')
            .appendTo($parent);

        $("<button>")
            .addClass('webgis-button webgis-form-submit')
            .text(options.submitText)
            .appendTo($parent)
            .click(function (e) {
                var result = {}, errors = '';

                $(this).parent().find('.webgis-input').each(function (i, e) {
                    var $e = $(e), val = $e.val();
                    switch ($e.attr('type')) {
                        case 'number':
                            val = parseFloat(val);
                            break;
                    }

                    if (!val && $e.attr('required') === 'required') {
                        errors += $e.attr('name') + ' is required</br>';
                    }
                    result[$e.attr('name')] = val;

                    if (options.name) {
                        $._webgis_form_persistent[options.name] = $._webgis_form_persistent[options.name] || [];
                        $._webgis_form_persistent[options.name][$e.attr('name')] = val;
                    }
                });

                if (errors) {
                    $(this).closest('.webgis-form').children('.webgis-form-errors').html(errors);
                } else {
                    if (options.onSubmit) {
                        options.onSubmit.apply(parent, [result]);
                    }
                }
            });
    }
})(webgis.$ || jQuery);

(function ($) {
    $.fn.webgis_image_selector = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_image_selector');
        }
    };

    var defaults = {
        image_urls: [],
        image_labels: null,
        image_width: null,
        image_height: null,
        selected: null
    };

    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        }
    };

    var initUI = function (parent, options) {
        var $parent = $(parent)
            .addClass('webgis-image-selector');

        console.log(options);

        var $valueElement = $("<input type='hidden'>").appendTo($parent);
        $parent.data('webgis-value-element', $valueElement);

        for (var i in options.image_urls) {
            var imageUrl = options.image_urls[i];

            var $imgContainer = $("<div>")
                .data('imageUrl', imageUrl)
                .addClass('webgis-image-selector-image-container')
                .click(function (e) {
                    e.stopPropagation();

                    var $this = $(this);
                    if (options.multi_select) {
                        $this.toggleClass('selected');
                    } else {
                        var isSelected = $this.hasClass('selected');
                        $parent.children('.webgis-image-selector-image-container').removeClass('selected');
                        if (!isSelected) {
                            $this.addClass('selected');
                        }
                    }

                    _resetValue($parent, $valueElement);
                });

            var $img = $("<div>")
                .addClass('webgis-image-selector-image')
                .css({ backgroundImage: 'url("' + webgis.css.imgResource(imageUrl) + '")' })
                .appendTo($imgContainer);

            var $label = $("<div>")
                .addClass("webgis-image-selector-image-label");

            if (options.image_width) {
                $imgContainer.css('width', options.image_width);
                $label.css('width', options.image_width);
                $img.css('width', options.image_width);
            }

            if (options.image_height) {
                $imgContainer.css('height', options.image_labels ? options.image_height + 30 : options.image_height);
                $img.css('height', options.image_height);
            }

            if (options.image_labels && i < options.image_labels.length) {
                $label.text(options.image_labels[i])
                      .appendTo($imgContainer);
            }

            if (options.selected && options.selected === imageUrl) {
                $imgContainer.addClass('selected');
            }

            $imgContainer.appendTo($parent);
        }

        _resetValue($parent, $valueElement);
    };

    var _resetValue = function ($parent, $valueElement) {
        $valueElement.val('');
        var val = '';
        $parent.children('.webgis-image-selector-image-container.selected').each(function (i, e) {
            if (val) {
                val += ',';
            }

            val += $(e).data('imageUrl');
        });

        $valueElement.val(val);
    }
})(webgis.$ || jQuery);

(function ($) {
    $.fn.webgis_tool_persist_topic_handler = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_persist_topic_handler');
        }
    };

    var defaults = {
        map: null,
        id: null,
        element: null,
        clearDeserialzed: true
    };

    var methods = {
        init: function (options) {
            return this.each(function () {
                new createHandlerPanel($.extend({}, defaults, options));
            });
        },
        serialize: function (options) {
            serialize($.extend({}, defaults, options));
        },
        deserialize: function (options) {
            deserialize($.extend({}, defaults, options));
        }
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
})(webgis.$ || jQuery);

(function ($) {
    $.fn.add_webgis_form_element_events = function () {
        var $this = $(this);
        if ($this.closest('.webgis-form-container').length === 1) {
            $this.bind('change keyup', function () {
                $(this).closest('.webgis-form-container').addClass('webgis-is-dirty');
            })
        }
        return this;
    }
})(webgis.$ || jQuery);
