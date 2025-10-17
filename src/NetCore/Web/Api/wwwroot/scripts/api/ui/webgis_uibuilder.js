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

        if (options.map && options.tool && options.tool.id) {
            options.map.events.fire('onbuildtoolui');
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
                    // do nothing
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
                        $(this).find("img").attr('src', webgis.css.imgResource("expanded-26.png", "ui"));
                        $content.slideDown();

                        $content
                            .find('.webgis-ui-collapsable-autoclick')
                            .trigger('click')
                            .addClass('webgis-loading')
                            .removeClass('webgis-ui-collapsable-autoclick')
                    }
                    else {
                        $(this).find("img").attr('src', webgis.css.imgResource("collapsed-26.png", "ui"));
                        $content.slideUp();
                    }
                });
            $("<h2><img src='" + webgis.css.imgResource(show ? "expanded-26.png" : "collapsed-26.png", "ui") + "'>" +
                (element.type === 'optionscontainer' ? "&nbsp;<div class='webgis-ui-selected-option' style='display:inline-block;position:relative;top:7px;height:28px;overflow:hidden'></div>" : "") +
                "&nbsp;" + element.title + "</h2>").appendTo($collapsableTitle);
            $parent = $("<div>").addClass('webgis-ui-collapsable-content').css('display', show ? 'block' : 'none').appendTo($collapsable);
        }
        var $newElement = null;
        if (element.type === 'label') {
            var labels = element.label.split('|');
            $newElement = $("<div" + elementProperties(element) + "></div>").addClass('webgis-label').appendTo($parent);

            const toTxtFunc = element.is_trusted === true
                ? webgis.asMarkdownOrRawHtml
                : webgis.asMarkdownOrText;

            if (labels.length == 1) {
                $newElement.html(toTxtFunc(element.label));
            } else {
                for (var label in labels) {
                    $("<p>")
                        .html(toTxtFunc(labels[label]))
                        .appendTo($newElement);
                }
            }
        }
        else if (element.type === 'title') {
            $newElement = $("<h1" + elementProperties(element) + "></h1>").html(element.label).appendTo($parent);
        }
        else if (element.type === 'input-text') {
            $newElement = $("<input type='text' " + elementProperties(element) + "/>").appendTo($parent).add_webgis_form_element_events();
            if (element.label)
                $newElement.val(element.label);
            if ($newElement.hasClass('webgis-print-textelement') && !$newElement.val()) {
                $newElement.val(map.getPersistentToolParameter('webgis.tools.print', "LAYOUT_TEXT_" + $newElement.attr('id')));
            }
        }
        else if (element.type === 'input-textarea') {
            $newElement = $("<textarea rows='5' " + elementProperties(element) + "></textarea>").appendTo($parent).add_webgis_form_element_events();
            if (element.value)
                $newElement.text(element.value);
        }
        else if (element.type === 'input-date') {
            $newElement = $("<input type='text' " + elementProperties(element) + "/>").appendTo($parent).add_webgis_form_element_events();
            if (element.label)
                $newElement.val(element.label);

            webgis.require('flatpickr', function ($newElement) {
                $newElement.flatpickr({
                    weekNumbers: true,
                    locale: 'de',
                    dateFormat: element.date_only === true ? "d.m.Y" : "d.m.Y H:i",
                    enableTime: element.date_only !== true,
                    time_24hr: true
                });
            }, $newElement);
        }
        else if (element.type === 'input-number') {
            $newElement = $("<input type='number' " + elementProperties(element) + " min='" + element.minValue + "' max='" + element.maxValue + "' step='" + element.stepWidth + "' />")
                .appendTo($parent)
                .add_webgis_form_element_events();
            if (element.value)
                $newElement.val(element.value);
        }
        else if (element.type === 'hidden') {
            $newElement = $("<input type='hidden' " + elementProperties(element) + "/>").appendTo($parent).val(element.value);
            if ($newElement.hasClass("webgis-map-anonymous-user-id")) {
                var currentId = webgis.localStorage.getAnonymousUserId();
                if (!currentId) {
                    //console.log('set current anoymous id: ' + currentId);
                    webgis.localStorage.setAnonyousUserId($newElement.val());
                } else {
                    //console.log('existing anoymous id: ' + currentId);
                    $newElement.val(currentId);
                }
            }
        }
        else if (element.type === 'input-autocomplete') {
            $newElement = $("<input type='text' " + elementProperties(element) + "/>").appendTo($parent);
            $newElement.addClass('webgis-autocomplete').attr('data-source', element.source);
            if (element.label)
                $newElement.val(element.label);
        }
        else if (element.type === 'button') {
            $newElement = $("<button " + elementProperties(element) + ">" + element.text + "</button>").appendTo($parent);
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
                        webgis.alert("Bitte geben Sie alle erfordlichen Werte ein!", "Eingabevalidierung");
                        return;
                    }
                }

                var $parent = $this.closest('.webgis-ui-holder');
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
        }
        else if (element.type === 'imagebutton') {
            $newElement = $("<div " + elementProperties(element) + ">").addClass('webgis-ui-imagebutton').appendTo($parent);
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
                var $this= $(this),
                    $optContainer = $(this).closest('.webgis-ui-optionscontainer');

                if ($optContainer.length > 0) {
                    var optContainer = $optContainer.get(0);
                    optContainer.value = $this.data('_value'); //$(this).attr('data-value');
                    $optContainer.find('.webgis-ui-option-selected').removeClass('webgis-ui-option-selected');
                    $(this).addClass('webgis-ui-option-selected');
                    if (optContainer.onChange)
                        optContainer.onChange();
                }
                $optContainer.parent().parent().find('.webgis-ui-selected-option').html($(this).clone().css({
                    minHeight: '0px', height: '24px', /*backgroundSize: 'auto 24px',*/ backgroundPosition: 'center', border: 'none'
                }).removeClass('webgis-ui-option-selected'));
                var $parent = $(this).closest('.webgis-ui-holder');
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
        }
        else if (element.type === 'buttongroup') {
            $newElement = $("<div class='webgis-ui-buttongroup'></div>").appendTo($parent);
        }
        else if (element.type === 'undobutton') {
            $newElement = $("<div " + elementProperties(element) + "></div>")
                .addClass('webgis-undobutton')
                .attr('data-undotool', element.undotool)
                .data('map',map)
                .appendTo($parent);

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

                map.ui.webgisContainer().webgis_modal({
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
                                    var undo = $(this).data('undo')
                                    //console.log('highlight', $(this).data('highlight-feature')) 
                                    map.graphics.addPreviewFromJson(undo.preview);
                                })
                                .mouseleave(function (e) {
                                    //console.log('removehight');
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
                                    spanText = "vor " + Math.round(seconds/60) + " Minute(n)...";
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
        }
        else if (element.type === 'menu') {
            $newElement = $("<ul style='list-style:none;padding:0px;' class='webgis-toolbox-tool-item-group-details'></ul>")
                .appendTo($parent);
            if (element.header) {
                $("<li><h2>" + element.header + "</h2></li>").appendTo($newElement);
            }
            if (element.collapsable) {
                $newElement.addClass('collapsable');
            }
        }
        else if (element.type === 'menuitem') {
            var imgSrc = element.icon_large || element.icon ? webgis.css.imgResource(element.icon_large || element.icon) : webgis.css.imgResource('enter-26.png', 'ui');

            var top = -1;
            
            $newElement = $("<li " + elementProperties(element) + "><div>" + element.text + "</div></li>")
                .addClass('webgis-toolbox-tool-item')
                .css({
                    padding: element.icon_large ? '36px 4px 4px 105px' : '12px 4px 12px 32px'
                })
                .appendTo($parent);

            if (element.text2) {
                $("<div>")
                    .addClass('text2')
                    .text(element.text2)
                    .appendTo($newElement);
            }

            if (element.subtext) {
                $("<div>")
                    .addClass('subtext')
                    .text(element.subtext)
                    .appendTo($newElement);
            }

            if (imgSrc.indexOf('data:') == 0) {
                $newElement.css({
                    position: 'relative'
                });
                $("<img src='" + imgSrc + "'>")
                    .css({
                        position: 'absolute',
                        left: 3, top: 8, maxHeight: 22
                    })
                    .appendTo($newElement)
            } else {
                $newElement.css({
                    backgroundImage: 'url(' + imgSrc + ')',
                    backgroundPosition: '2px 10px',
                    //backgroundSize: 'auto 26px',
                    backgroundRepeat: 'no-repeat',
                });
            }

            if (element.icon_large) {
                $newElement.css({
                    backgroundSize: '90px',
                    height: '110px',
                    whiteSpace: 'normal'
                });
            }

            if (element.removable === true) {
                $("<div>")
                    .addClass('remove remove-element')
                    .text('✖')
                    .appendTo($newElement);
            }

            if (element.show_checkbox) {
                var p = element.value.split(':');
                if (p.length === 2) {  // query
                    var service = map.getService(p[0]);
                    if (service) {
                        var query = service.getQuery(p[1]);
                        if (query && query.associatedlayers && query.associatedlayers.length > 0) {
                            var layers = $.map(query.associatedlayers, function (l) { return l.id });
                            var hasVisibleLayers = service.checkLayerVisibility(layers) !== 0;

                            $("<div>")
                                .data('service', service)
                                .data('layers', layers)
                                .addClass('webgis-checkbox')
                                .css('backgroundImage', 'url(' + webgis.css.imgResource(hasVisibleLayers ? 'check1.png' : 'check0.png', 'toc') + ')')
                                .appendTo($newElement
                                    .addClass('checkable')
                                    .css({
                                        paddingLeft: '70px',
                                        backgroundPosition: '36px center'
                                    })
                                )
                                .click(function (e) {
                                    e.stopPropagation();

                                    var service = $(this).data('service');
                                    var layers = $(this).data('layers');
                                    var hasVisibleLayers = service.checkLayerVisibility(layers) !== 0;

                                    service.setLayerVisibilityDelayed(layers, hasVisibleLayers ? false : true);
                                    $(this).css('backgroundImage', 'url(' + webgis.css.imgResource(hasVisibleLayers ? 'check0.png' : 'check1.png', 'toc') + ')');
                                });
                        }
                    }
                }
            }

            appendCallback($newElement, element, options);

            if (element.highlight_feature) {
                if (typeof element.highlight_feature === 'string') {
                    element.highlight_feature = webgis.$.parseJSON(element.highlight_feature);
                }

                $newElement
                    .data('highlight-feature', element.highlight_feature)
                    .mouseenter(function (e) {
                        //console.log('highlight', $(this).data('highlight-feature')) 
                        map.graphics.addPreviewFromJson($(this).data('highlight-feature').features[0].geometry);
                    })
                    .mouseleave(function (e) {
                        //console.log('removehight');
                        map.graphics.removePreview();
                    }).
                    click(function (e) {
                        map.graphics.removePreview();
                    });
            }

            if ($parent.hasClass('collapsable')) {
                
            }
        }
        else if (element.type === 'checkable-div') {
            $newElement = $("<div " + elementProperties(element, options) + "></div>")
                .addClass('webgis-checkable-div')
                .appendTo($parent)
                .click(function (e) {
                    e.stopPropagation();

                    var $this = $(this);
                    $this.addClass('checked');
                    $this
                        .children('.webgis-checkable-div-status')
                        .val('checked');
                });

            $("<div>")
                .addClass('checkbox')
                .appendTo($newElement)
                .click(function (e) {
                    e.stopPropagation();

                    var $parent = $(this).parent();
                    $parent.toggleClass('checked');
                    $parent
                        .children('.webgis-checkable-div-status')
                        .val($parent.hasClass('checked') ? 'checked' : '');
                });
            var $valueElement = $("<input class='webgis-checkable-div-status' type='hidden' " + elementProperties(element, options) + "/>")
                .val($newElement.hasClass('checked') ? 'checked' :  '')
                .appendTo($newElement);

            $newElement.data('webgis-value-element', $valueElement);
        }
        else if (element.type === 'option-list') {
            var $container = $("<div>")
                .addClass('webgis-ui-option-list-holder')
                .appendTo($parent);

            $("<input type='hidden'" + elementProperties(element, options) + " />")
                .addClass('webgis-ui-option-list-value')
                .appendTo($container);

            $newElement = $("<ul>")
                .addClass('webgis-ui-option-list')
                .appendTo($container);
                
        }
        else if (element.type === 'option-list-item') {
            $newElement = $("<li>")
                .addClass('webgis-ui-option-list-item')
                .data('value', element.value)
                .appendTo($parent);

            $newElement.click(function (e) {
                e.stopPropagation();

                $(this)
                    .closest('.webgis-ui-option-list-holder')
                    .find('.webgis-ui-option-list-value')
                    .val($(this).data('value'));

                $(this).parent().children('.selected').removeClass('selected');
                $(this).addClass('selected');
            });

            if ($parent.children('.webgis-ui-option-list-item').length === 1)
                $newElement.trigger('click');
        }
        else if (element.type === 'querycombo') {
            $newElement = $("<select " + elementProperties(element, options) + "></select>").appendTo($parent);
            $newElement.webgis_queryCombo({ map: options.map, 'type': element.combotype, customitems: element.customitems });
        }
        else if (element.type === 'editthemecombo') {
            $newElement = $("<select " + elementProperties(element, options) + "></select>").appendTo($parent);
            $newElement.webgis_editthemeCombo({ map: options.map, customitems: element.customitems, dbrights: element.dbrights });
        }
        else if (element.type === 'editthemetree') {
            $newElement = $("<div " + elementProperties(element, options) + "></div>").appendTo($parent);
            $newElement.webgis_editthemeTree({ id: element.id, map: options.map, customitems: element.customitems, dbrights: element.dbrights });
        }
        else if (element.type === 'chainagethemecombo') {
            $newElement = $("<select " + elementProperties(element, options) + "></select>").appendTo($parent);
            $newElement.webgis_chainageCombo({ map: options.map });
        }
        else if (element.type === 'labelingcombo') {
            $newElement = $("<select " + elementProperties(element, options) + "></select>").appendTo($parent);
            $newElement.webgis_labelingCombo({ map: options.map, val: element.value });
        }
        else if (element.type === 'circleradiuscombo') {
            $newElement = $("<select " + elementProperties(element, options) + "></select>").appendTo($parent)
                .data('map', map)
                .change(function () {
                var map = $(this).data('map');
                map.showCircleMarker(null, parseInt($(this).val()));
            });
            var radii = element.radii || webgis.const.circleMarkerRadii;
            for (var r in radii) {
                var radius = radii[r];
                $("<option value='" + radius + "' " + (radius === map._circleMarkerOptions.radius ? "selected='selected'" : "") + ">" + radius.toString().numberWithCommas() + " m</option>").appendTo($newElement);
            }
        }
        else if (element.type === 'print-query-labelfield-combo') {
            $newElement = $("<select " + elementProperties(element, options) + "></select>").appendTo($parent);
            $("<option value=''>--- Beschriften (optional) ---</option>").appendTo($newElement);
            var feature = map.queryResultFeatures.first();
            //console.log('feature', feature);
            if (feature && feature.properties) {
                var featureProperties = Array.isArray(feature.properties) ?
                    (feature.properties.length > 0 ? feature.properties[0] : {}) :
                    feature.properties;

                for (var property in featureProperties) {
                    //console.log('property', property);
                    if (isPrintLabelProperty(property, featureProperties[property])) {
                        $("<option value='" + property + "'>" + printLabelPropertyDisplayname(property) + "</option>").appendTo($newElement);
                    }
                }
            }
            $newElement.webgis_multi_select({ separator: ';', map: map });
        }
        else if (element.type === 'print-coordinates-labelfield-combo') {
            $newElement = $("<select " + elementProperties(element, options) + "></select>").appendTo($parent);

            if (!element.showCoordinatePairsOnly) {
                $("<option value=''>--- Beschriftung (optional) ---</option>").appendTo($newElement);
                $newElement.webgis_multi_select({ separator: ';', map: map });
            }
            var feature = map.queryResultFeatures.firstToolResult('webgis.tools.coordinates');
            if (feature && feature.properties) {
                
                for (var property in feature.properties) {
                    var propertyValue = feature.properties[property];
                    if (isPrintLabelProperty(property, propertyValue)) {
                        let isPair = true;
                        if (propertyValue instanceof Array) {
                            // Koordinatenpaar => genau 2 Werte müssen angegeben sein.
                            var count = 0;
                            $.each(propertyValue, function (i, val) { if (val) count++; });
                            if (count !== 2) {
                                isPair = false;
                            }
                        } else {
                            isPair = false;
                        }
                        if (element.showCoordinatePairsOnly === true && isPair == false) {
                            continue;
                        }
                        $("<option value='" + property + "'>" + printLabelPropertyDisplayname(property) + "</option>").appendTo($newElement);
                    }
                }
            }
        }
        else if (element.type === 'sketchselect') {
            var currentToolSketches = map.currentToolSketches();

            $newElement = $("<select " + elementProperties(element, options) + "></select>")
                .data('current-tool-sketches', currentToolSketches)
                .data('map', map)
                .appendTo($parent);

            $("<option value=''>--- nicht anzeigen ---</option>").appendTo($newElement);
            
            for (var c in currentToolSketches) {
                var currentToolSketch = currentToolSketches[c];
                $("<option value='" + currentToolSketch.id + "'>" + currentToolSketch.name + "</option>").appendTo($newElement);
            }

            $newElement.change(function () {
                var val = $(this).val(), map = $(this).data('map');
                var currentToolSketch = $.grep($(this).data('current-tool-sketches'), function (n, i) { return n.id === val });
                if (currentToolSketch.length === 1) {
                    map.sketch.load(currentToolSketch[0].storedSketch, map.getActiveTool() ? map.getActiveTool().id : null);
                } else {
                    map.sketch.remove();
                }
            });
        }
        else if (element.type === 'select') {
            $newElement = $("<" + element.type + elementProperties(element, options) + "></" + element.type + ">")
                .appendTo($parent)
                .add_webgis_form_element_events();
            if (element.allow_addvalues === true) {
                $newElement.webgis_extCombo({ inputType: 'number' });
            }
            if (element.options && element.options.length > 0) {
                for (var o = 0; o < element.options.length; o++) {
                    $("<option value='" + element.options[o].value + "'" + (element.defaultvalue && element.options[o].value == element.defaultvalue ? "selected='selected'" : "") +">" + element.options[o].label + "</option>").appendTo($newElement);
                }
            }
            else if ($newElement.hasClass('webgis-map-scales-select')) {
                var scale = map.scale();
                var scales = map.scales(), selectedScale = -1;
                for (var s in scales) {
                    var scaleValue = Math.round(scales[s] / 10) * 10;
                    $("<option value='" + scaleValue + "'>1:" + scaleValue.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ".") + "</option>").appendTo($newElement); // mit tausender Punkten

                    if (selectedScale < 0 || scale < scaleValue) {
                        selectedScale = scaleValue;
                    }
                }
                $newElement.val(selectedScale);
            }
            var select = $newElement.get(0);
            if (element.changecommand && element.changecommand !== 'unknown') {
                select.changetype = element.changetype;
                select.toolId = element.changetool || select.id;
                select.changecommand = element.changecommand;
                select.map = options.map;
                $newElement.change(function () {
                    webgis.tools.onButtonClick(this.map, { command: this.changecommand, type: this.changetype, id: this.toolId, map: this.map, value: $(this).val() }, this);
                });
            }
            if (element.dependency_field_ids && element.dependency_field_ids.length > 0) {
                $newElement
                    .addClass('webgis-ui-added')
                    .addClass('webgis-select-has-dependency-field-ids')
                    .data('dependency-field-ids', element.dependency_field_ids)
                    .data('dependency_field_ids_callback_toolid', element.dependency_field_ids_callback_toolid);
            }
            if ($newElement.hasClass('webgis-print-show-query-markers-select')) {
                $newElement.change(function () {
                    map.queryResultFeatures.setMarkerVisibility($(this).val() === 'show' ? true : false);

                });
            }
            if ($newElement.hasClass('webgis-print-show-coordinate-markers-select')) {
                $newElement.change(function () {
                    map.queryResultFeatures.setToolMarkerVisibility('webgis.tools.coordinates',$(this).val() === 'show' ? true : false);
                });
            }
            if ($newElement.hasClass('webgis-print-show-chainage-markers-select')) {
                $newElement.change(function () {
                    map.queryResultFeatures.setToolMarkerVisibility('webgis.tools.chainage',$(this).val() === 'show' ? true : false);
                });
            }

            if (element.allow_pro_behaviour === true && webgis.usability.select_pro_behaviour === "select2") {
                webgis.require('select2', function ($select) {
                    $select.select2({ width: '100%' });
                }, $newElement);
            }
        }
        else if (element.type === 'optionscontainer') {
            $newElement = $("<div " + elementProperties(element, options) + ">").appendTo($parent).addClass('webgis-ui-optionscontainer');
            if (element.allow_null_values === true) {
                $newElement.addClass('allow-null-values');
            }
            $newElement.get(0).value = element.value;
        }
        else if (element.type === 'click-toggle')
        {
            $newElement = $newElement = $("<div " + elementProperties(element, options) + ">")
                .appendTo($parent)
                .webgis_clickToggle({ toggleStyle: element.togglestyle.split(','), toggleStyleValue: element.togglestylevalue.split(','), resetSiblings: element.resetsiblings });
        }
        else if (element.type === 'drop-list') {
            $newElement = $("<div " + elementProperties(element, options) + ">").attr('id', element.id + "_droplist").attr('data-id', element.id).appendTo($parent).data('map', options.map).addClass('webgis-ui-droplist');
            $("<input type='hidden' " + elementProperties(element, options) + "/>").appendTo($parent).val(element.value);
            $newElement
                .on('dragover', function (e) {
                e.preventDefault();
            })
                .on('drop', function (e) {
                    e.preventDefault();
                    var map = $(this).data('map');
                    var values = e.originalEvent.dataTransfer.getData('text').split(';');
                    for (var v in values) {
                        var value = values[v];
                        if ($(this).children("div[data-dropvalue='" + value + "']").length > 0)
                            continue;
                        var $item = $("<div><span>" + value + "</span></div>").addClass('webgis-ui-droplist-item').attr('data-dropvalue', value).appendTo($(this));
                        map.events.fire('ondrop-add', map, { id: $(this).attr('data-id'), value: value });
                        $("<div>X</div>").addClass('webgis-ui-droplist-item-x').appendTo($item)
                            .click(function () {
                            var $list = $(this).closest('.webgis-ui-droplist');
                            var map = $list.data('map');
                            map.events.fire('ondrop-remove', map, { id: $(this).closest('.webgis-ui-droplist').attr('data-id'), value: $(this).closest('.webgis-ui-droplist-item').attr('data-dropvalue') });
                            $(this).closest('.webgis-ui-droplist-item').remove();
                            $list.data('calcVal')($list);
                        });
                    }
                    $(this).data('calcVal')($(this));
                })
                .data('calcVal', function ($elem) {
                var $target = $elem.parent().children("input[id='" + $elem.attr('data-id') + "']");
                var val = '';
                $elem.children('.webgis-ui-droplist-item').each(function () {
                    if (val !== '')
                        val += ',';
                    val += $(this).attr('data-dropvalue');
                });
                $target.val(val);
            });
        }
        else if (element.type === 'upload-file') {
            $newElement = $("<button>" + (element.text ? element.text : webgis.l10n.get("file-upload")) + "</button>").addClass('webgis-button').appendTo($parent)
                .attr('id', element.id)
                .click(function (event) {
                event.stopPropagation();
                var $input = $('#' + $(this).attr('id') + "_file_input").trigger('click');
            });
            var $form = $("<div></div>").css('display', 'none').appendTo($parent);
            var $input = $("<input type='file' name='" + element.id + "' id='" + element.id + "_file_input' />").appendTo($form)
                .data('servercommand', element.servercommand)
                .data('toolid', element.toolid)
                .data('map', map);
            $input.change(function (e) {
                var map = $(this).data('map');
                for (var i in this.files) {
                    var file = this.files[i];
                    if (!file || !file.size)
                        continue;

                    webgis.tools.uploadFile(map, $(this).data('toolid'), $(this).data('servercommand'), $(this).attr('name'), file);
                }
            });
        }
        else if (element.type === 'upload-file-control') {
            $newElement = $("<div></div>").appendTo($parent).webgis_control_upload({
                edit_service: element.edit_service,
                edit_theme: element.edit_theme,
                field_name: element.field_name,
                hidden_class: element.css,
                onload: function (sender, result) {
                    console.log(result);
                }
            });
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
            var showSketchInfo = true;
            if (webgis.useMobileCurrent() ||                                          // 1. auf kleien mobilegeräten nicht anzeigen
                (webgis.isMobileDevice() && webgis.usability.clickBubble == false))   // 2. auf Mobilen Geräten ohne Bubble => mach keinen Sinn
                showSketchInfo = false;

            if (showSketchInfo) {  
                $newElement = $("<div></div>").appendTo($parent);
                if ($.fn.webgis_sketchInfoContainer) {
                    $newElement.webgis_sketchInfoContainer({ map: options.map });
                }
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
        else if (typeof webgis.ui.builder[element.type] === 'function') {
            const tagName = webgis.ui.builder[element.type + ".tagname"] || "div";
            $newElement = $("<" + tagName + " " + elementProperties(element, options) + "></" + tagName + ">").appendTo($parent);
            webgis.ui.builder[element.type](map, $newElement, element, options);
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

                if (element.type === 'buttongroup') {
                    var childrenCount = $newElement.children('.uibutton').length;
                    if (childrenCount > 0) {
                        $newElement.children('.uibutton').css('width', ((100.0 / childrenCount) - (childrenCount - 1)) + '%');
                    }
                }
            }
            if (element.type === 'optionscontainer') {
                var newElement = $newElement.get(0);
                if (!newElement.value && !$newElement.hasClass('allow-null-values')) {
                    var defaultValue = null;
                    for (var d in webgis.usability.optionContainerDefault) {
                        var ocd = webgis.usability.optionContainerDefault[d];

                        if (ocd.id === element.id) {
                            defaultValue = ocd.value;
                        }
                    }
                    if (defaultValue && $newElement.children("[data-value='" + defaultValue + "']").length === 1) {
                        $newElement.children("[data-value='" + defaultValue + "']").trigger('click');
                    } else {
                        $newElement.children(':first-child').trigger('click');
                    }
                }
                else {
                    newElement.setValue(newElement.value);
                }
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
    var appendCallback = function ($newElem, element, options) {
        if (!element || !element.callback)
            return;
        if (options.map && element.callback.tool) {
            element.callback.tool = options.map.getTool(element.callback.tool.id) || element.callback.tool; // assign original tool, if possible
        }
       
        $newElem
            .data('_callback', element.callback)
            .data('_type', element.type)
            .data('_value', element.value)
            .data('_map', options.map)
            .data('_event', options.event)
            .click(function (e) {
                //console.log('newElement.click', this);
                var $this = $(this);

                if ($this.data('_type') === 'menuitem') {
                    e.stopPropagation();

                    var $menu = $(this).closest('ul');
                    if ($menu.hasClass('collapsable')) {
                        var collapsed = $menu.hasClass('collapsed');
                        $menu.toggleClass('collapsed');

                        if (collapsed === true) {
                            options.map.events.fire('ui-builder-collapsable-expanded', options.map, { $element: $menu });
                            return;
                        } else {
                            $menu.children().removeClass('selected-collapsed');
                            $(this).addClass('selected-collapsed');
                        }
                    }
                }
                var cb = $this.data('_callback');
                var val = $this.data('_value');
                var custom = {};

                custom[$this.data('_type') + "-value"] = val;
                if ($(this).attr('data-item-command'))
                    custom[$this.data('_type') + "-item-command"] = $(this).attr('data-item-command');

                if (cb.type === 'toolevent' && cb.tool) {
                    webgis.tools.sendToolRequest($this.data('_map'), cb.tool, cb.type, $this.data('_event'), custom);
                }
                else if (cb.type === 'servertoolcommand') {
                    webgis.tools.onButtonClick($this.data('_map'), { command: cb.command, type: 'servertoolcommand', id: cb.tool.id, map: $this.data('_map') }, this, null, custom);
                }
                else if (cb.type === 'servertoolcommand_ext') {
                    webgis.tools.onButtonClick($this.data('_map'), { command: cb.command, type: 'servertoolcommand_ext', id: cb.tool.id, map: $this.data('_map') }, this, null, custom);
                }

                if ($this.data('_type') === 'menuitem') {
                    var $menu = $(this).closest('ul');

                    if ($menu.attr('element-target') === 'modaldialog') {
                        options.map.ui.webgisContainer().webgis_modal('close');
                    }
                }
            });
        $newElem.children('.remove-element')
            .click(function (e) {
                e.stopPropagation();

                var $parent = $(this).parent();
                $parent.attr('data-item-command', 'remove-element').trigger('click');
                $parent.remove();
            });
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
            if (element.type === 'optionscontainer') {
                newElement.onChange = function () {
                    this.map.setPersistentToolParameter(this.map.getActiveTool(), this.id, this.value);
                };
                newElement.value = (map.getPersistentToolParameter(map.getActiveTool(), newElement.id)) || newElement.value;
            }
            if (element.type === 'table') {
                // do nothing
            }
            else {
                $newElement.change(function () {
                    this.map.setPersistentToolParameter(this.map.getActiveTool(), this.id, $(this).val());
                });
            }
        }
        if (element.type === 'optionscontainer') {
            newElement.setValue = function (val) {
                $(this).children().each(function (i, e) {
                    var $e = $(e);

                    if ($e.data('_value') == newElement.value ||
                        ( // if(value is an Object compare the id of this object... (ie. Redining Symbol Selector...)
                        $e.data('_value') && $e.data('_value').id && newElement.value && newElement.value.id && $e.data('_value').id === newElement.value.id)) {
                        $e.trigger('click');
                        return false;
                    }
                });
            };
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

    var isPrintLabelProperty = function(property, propertyValue) {
        if (property.indexOf('_') === 0) {
            switch (property) {
                case '_fulltext':
                case '_label':
                    break;
                default:
                    return false;
            }
        }

        if (typeof propertyValue === 'string') {
            propertyValue = propertyValue.trim();
            if (propertyValue.length > 0 && propertyValue[0] === "<" && propertyValue[propertyValue.length - 1] === ">") {
                return false;
            }
        }

        return true;
    };
    var printLabelPropertyDisplayname = function (property) {
        switch (property) {
            case '_fulltext':
                return '(Kurz) Zusammenfassung';
            case '_label':
                return 'Beschriftung';
            default:
                return property;
        }
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
    $.fn.webgis_multi_select = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_multi_select');
        }
    };

    var defaults = { separator: ',', map: null };

    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        }
    };

    var initUI = function (select, options) {
        let $select = $(select);

        let $multiSelect = $("<div>")
            .insertBefore($select);
        let $input = $("<input type='hidden'>")
            .attr('id', $select.attr('id'))
            .appendTo($multiSelect);

        $select.appendTo($multiSelect);
        $select.removeAttr('id');

        $("<div>") // $itemsContainer
            .addClass('webgis-multiselect-container')
            .appendTo($multiSelect);
        
        if ($select.hasClass('webgis-tool-parameter')) {
            $select.removeClass('webgis-tool-parameter');
            $input.addClass('webgis-tool-parameter');
        }

        if ($select.hasClass('webgis-tool-parameter-persistent')) {
            $select.removeClass('webgis-tool-parameter-persistent');
            $input.addClass('webgis-tool-parameter-persistent');

            if (options.map) {
                var val = options.map.getPersistentToolParameter(options.map.getActiveTool(), $input.attr('id'));
                if (val) {
                    var vals = val.split(options.separator);
                    for (let i = 0; i < vals.length; i++) {
                        addItem(vals[i], $multiSelect, options)
                    }
                }
                resetValue($multiSelect, options);
            }
        }
            
        $select.change(function () {
            addItem($select.val(), $multiSelect, options)

            resetValue($multiSelect, options);
            $select.val('');
        });
    };

    var addItem = function (val, $parent, options) {
        if (!val) return;

        $itemsContainer = $parent.children('.webgis-multiselect-container');
        let exists = false;
        $itemsContainer.children().each(function (i, e) {
            if ($(e).data('value') === val) {
                exists = true;
            }
        });
        if (exists) return;

        let $item = $("<div>")
            .addClass('webgis-multiselect-continer-item')
            .data('value', val)
            .text(val)
            .appendTo($itemsContainer);
        $("<div>")
            .addClass('remove')
            .text('X')
            .appendTo($item)
            .click(function () {
                $(this).closest('.webgis-multiselect-continer-item').remove();
                resetValue($parent, options);
            });
    }

    var resetValue = function ($parent, options) {
        var val = '';

        $itemsContainer = $parent.children('.webgis-multiselect-container');
        $itemsContainer.children().each(function (i, e) {
            if (val)
                val += options.separator;
            val += $(e).data('value');
        });

        $parent.children('input').val(val);
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