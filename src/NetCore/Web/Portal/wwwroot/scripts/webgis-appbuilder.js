var _map;
var _template = {};

webgis.appBuilder = {
    description: ''
};

$(document).ready(function () {

    webgis.init(function () {
        $('#username').html("Angemeldet: " + webgis.clientName());
        _map = webgis.createMap('dummymap');

        _map.events.on('onrefreshuielements', function (chanel, sender) {

            _template.template = $("#template-selector").val();
            _template.creator = webgis.clientName();

            $('#mapbuilder-container').find('.webgis-toolbox-parameter').each(function (i, e) {
                for (var p in _template.parameters) {
                    if (_template.parameters[p].name == $(e).attr('name'))
                        _template.parameters[p].value = $(e).val()
                }
            });

            var val = JSON.stringify(_template);
            $('.webgis-template-json').val(val);

            checkAllEpsg($("#mapbuilder-container"));
        });

        webgis.appBuilder.description = (_metadata ? _metadata.description : '') || ''
    });

    var $templateSelect=$('#template-selector').change(function () {

        $.ajax({
            url: webgis.url.relative('./appbuilder/templatemeta'),
            data: { template: $(this).val() },
            //data: { template: $(this).val() },
            type: 'post',
            dataType:'json',
            success: function (result) {
                createTemplateUI(result);
            },
            error: function ( jqXHR, textStatus, errorThrown) {
                alert('Es ist ein Fehler aufgetreten: ' + errorThrown);
            }
        });

    });
    
    if (_template == null) {
        $templateSelect.trigger('change');
    } else {
        createTemplateUI(_template);
        $('#webgis-appbuilder-submit').click();
    }
});

function createTemplateUI(template) {
    _template = template;
    console.log(template);
    var $container = $('#mapbuilder-container').empty();

    if (_template.template)
        $('#template-selector').val(_template.template);

    for (var p in template.parameters) {
        var parameter = template.parameters[p];

        if (parameter.type) {
            switch (parameter.type.toLowerCase()) {
                case 'extent':
                    createSelectorFromAjax(parameter.title, parameter.name, "/rest/extents?f=json", { singleSelect: true }, false, "extents");
                    break;
                case 'extents':
                    createSelectorFromAjax(parameter.title, parameter.name, "/rest/extents?f=json", false, false, "extents");
                    break;
                case 'services':
                    createSelectorFromAjax(parameter.title, parameter.name, "/rest/services?f=json", false, true, "services");
                    break;
                case 'service':
                    createSelectorFromAjax(parameter.title, parameter.name, "/rest/services?f=json", { singleSelect: true }, true, "services");
                    break;

                case 'searchservices':
                    createSelectorFromAjax(parameter.title, parameter.name, "/rest/search?f=json", false, true, "folders");
                    break;

                case 'editservice':
                    createSelectorFromAjax(parameter.title, parameter.name, "/rest/editservices?f=json", { singleSelect: true }, false, "services");
                    break;
                case 'editservices':
                    createSelectorFromAjax(parameter.title, parameter.name, "/rest/editservices?f=json", false, false, "services");
                    break;

                case 'edittheme':
                    createSelectorFromAjax(parameter.title, parameter.name, "/rest/editthemes?f=json", { singleSelect: true }, false, "themes");
                    break;
                case 'editthemes':
                    createSelectorFromAjax(parameter.title, parameter.name, "/rest/editthemes?f=json", false, false, "themes");
                    break;
                case 'hidden':
                    $("<input type='hidden' class='webgis-input webgis-toolbox-parameter' name='" + parameter.name + "' />").appendTo($container).val(parameter.value ? parameter.value : '');
                    break;
            }

            if (parameter.value) {
                var $paramContainer = $(".selector-container[data-id='" + parameter.name + "']");
                
                var vals=parameter.value.split(',');
                $.each(vals, function (i) {
                    $paramContainer.find(".webgis-toolbox-tool-item[data-id='" + vals[i] + "']").trigger('click');
                });
            }

        } else {
            $div = $("<div class='selector-container'>").appendTo('#mapbuilder-container').attr("data-id", parameter.name);
            var $title = $("<div class=webgis-presentation_toc-title>" + parameter.title + "</div>").appendTo($div).
                click(function () {
                    var me = this;
                    $(this).parent().parent().find('.selector-container').each(function (i, e) {
                        if (e != $(me).parent().get(0)) {
                            $(e).find('.webgis-toolbox-tool-item-group-details').slideUp();
                        }
                    });
                    $(this).parent().find('.webgis-toolbox-tool-item-group-details').slideToggle();
                });

            var $content = $("<div class='webgis-toolbox-tool-item-group-details' style='display:none;padding:5px 10px'><div>").appendTo($div);

            console.log('parameter', parameter);

            switch (parameter.inputType) {
                case 'textarea':
                    $("<textarea rows='8' class='webgis-input webgis-toolbox-parameter' name='" + parameter.name + "' ><textarea>")
                        .appendTo($content)
                        .val(parameter.value ? parameter.value : '');
                    break;
                case 'number':
                    $("<input type='number' class='webgis-input webgis-toolbox-parameter' name='" + parameter.name + "' />")
                        .appendTo($content)
                        .val(parameter.value ? parameter.value : '');
                    break;
                    break;
                case 'json':
                    $("<button class='webgis-button'>JSON Editor</button>")
                        .appendTo($content)
                        .data('jsonExample', parameter.jsonExample)
                        .click(function () {
                            var $hidden = $(this).next('input.webgis-toolbox-parameter');
                            var jsonExample = $(this).data('jsonExample');
                            $('body').webgis_modal({
                                title: 'JSON',
                                onload: function ($content) {
                                    var editor = null;
                                    var $toolbar = $('<div style="position:absolute;left:0px;top:0px;right:0px;height:50px;background-color:#eee;padding:8px;text-align:right;box-sizing:border-box;border-bottom:1px solid #ccc"></div>').appendTo($content);

                                    if (jsonExample) {
                                        $("<button>Add Example</button>")
                                            .addClass('webgis-button')
                                            .appendTo($toolbar)
                                            .click(function () {
                                                var val = editor.getValue();
                                                val += "\n\n\n/** Example\n\n" + JSON.stringify(jsonExample, null, 2) + "\n\n*/";
                                                editor.setValue(val);
                                            });
                                    }

                                    var $saveButton = $("<button>Save</button>")
                                        .addClass('webgis-button')
                                        .attr('disabled','disabled')
                                        .appendTo($toolbar)
                                        .click(function () {
                                            $hidden.val(editor.getValue());
                                            $(this).attr('disabled', 'disabled')
                                        });
                                    
                                    $('<div id="editor" style="position:absolute;left:0px;top:50px;right:0px;bottom:0px;"></div>').appendTo($content);
                                    webgis.require('monaco-editor', function (result) {
                                        editor = monaco.editor.create(document.getElementById('editor'), {
                                            language: 'javascript',
                                            automaticLayout: true,
                                            theme: 'vs'
                                        });

                                        editor.setValue($hidden.val());

                                        editor.getModel().onDidChangeContent((event) => {
                                            $saveButton.removeAttr('disabled');
                                        });
                                    }, result);
                                }
                            });
                        });
                    $("<input type='hidden' class='webgis-input webgis-toolbox-parameter' name='" + parameter.name + "' />")
                        .appendTo($content)
                        .val(parameter.value ? parameter.value : '');
                    break;
                default:
                    $("<input type='text' class='webgis-input webgis-toolbox-parameter' name='" + parameter.name + "' />")
                        .appendTo($content)
                        .val(parameter.value ? parameter.value : '');
                    break;
            }

            //var $parameterHolder = $("<div></div>").addClass('parameter-holder').appendTo($container);
            //$("<div><span class='webgis-input-label'>" + parameter.title + ":</span><div>").addClass('webgis-presentation_toc-title').appendTo($parameterHolder);
            //$("<input type='text' class='webgis-input webgis-collector-parameter' name='" + parameter.name + "' /><br/>").appendTo($parameterHolder).val(parameter.value ? parameter.value : '');
        }
    }

    $("<br/><br/>").appendTo($container);
    $("<div class='webgis-button' id='webgis-appbuilder-submit' style='margin:0px 10px;text-align:center'>Übernehmen »</div>").appendTo($container).
        click(function () {
            var url = '';
            $(this).closest('#mapbuilder-container').find('.webgis-toolbox-parameter').each(function (i, e) {
                if (url.length > 0) url += '|';
                url += $(e).attr('name') + "=" + webgis.encodeURI($(e).val());
            });

            $('#webgis-app-frame').attr('src', './app/' + $('#template-selector').val() + '/~?parameters=' + url);
        })
}

function fireSelectionChanged(element, data, selected) {
    if (!selected)
        return;

    var parameterName = $(element).closest('.selector-container').attr('data-id');
    var parameter = getTemplateParameter(parameterName);
    if (parameter != null && parameter.setters) {
        for (var s in parameter.setters) {
            var setter = parameter.setters[s];
            if (data[setter.value] != null) {
                $('.webgis-toolbox-parameter[name="' + setter.parameter + '"]').val(data[setter.value]);
            }
        }
    }
}

function getTemplateParameter(name) {
    for (var p in _template.parameters) {
        var parameter = _template.parameters[p];
        if (parameter.name == name) {
            return parameter;
        }
    }
    return null;
}

function publishApp(portalId, appCategory, appName) {
    customParameters = [];
    customParameters['page-id'] = portalId;
    customParameters['app-category'] = appCategory || '';
    customParameters['app-name'] = appName || '';
    customParameters['app-description'] = webgis.appBuilder.description || '';
    webgis.tools.onButtonClick(_map, { id: 'webgis.tools.portal.publishapp', type: 'serverbutton', map: _map, uiElement: document.body, customParameters: customParameters });
}

function editAppDescription() {
    var editor = null, dialogId = 'edit-app-desciption-dialog';
    $("body").webgis_modal({
        title: "App Description",
        id: dialogId,
        onload: function ($content) {

            // Top
            var $top = $('<div style="position:absolute;left:0px;top:0px;right:0px;height:50px;padding:5px;background:#efefef"></div>').appendTo($content);

            $("<button>Übernehmen</button>")
                .addClass('uibutton')
                .appendTo($top)
                .click(function () {
                    webgis.appBuilder.description = editor.getValue();
                    $("body").webgis_modal('close', { id: dialogId });
                });

            // Editor
            $('<div id="editor" style="position:absolute;left:0px;top:50px;right:0px;bottom:0px;"></div>').appendTo($content);
            webgis.require('monaco-editor', function (result) {
                editor = monaco.editor.create(document.getElementById('editor'), {
                    language: 'text',
                    automaticLayout: true,
                    theme: 'vs'
                });

                editor.setValue(webgis.appBuilder.description);
            }, /*result*/ "");
        }
    });
};

if (!window.webgis) {
    webgis = {};
    webgis.url = new function () {

        this.encodeString = function (v) {

            if (v && v.indexOf) {
                while (v.indexOf('<') != -1) v = v.replace('<', '&lt;');
                while (v.indexOf('>') != -1) v = v.replace('>', '&gt;');
            }

            v = encodeURI(v);

            while (v.indexOf('&') != -1) v = v.replace('&', '%26');
            while (v.indexOf('+') != -1) v = v.replace('+', '%2b');
            while (v.indexOf('#') != -1) v = v.replace('#', '%23');
            while (v.indexOf('=') != -1) v = v.replace('=', '%3d');

            return v;
        };

        this.relative = function (url) {
            var loc = document.location.toString().split('?')[0];
            if (loc.endsWith('/'))
                return '../' + url;
            return url;
        };
    };
}

String.prototype.endsWith = function (suffix) {
    return this.indexOf(suffix, this.length - suffix.length) !== -1;
};

