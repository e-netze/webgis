module UI {

    //
    //  PropertyGrid 
    //

    export class PropertyGridItem {
        name: string;
        displayName: string;
        category: string;
        description: string;
        value: any;
        domainValues: string[];
        readonly: boolean;
        hasEditor: boolean;
        isComplex: boolean;
        isPassword?: boolean;
        isSecret?: boolean;
        isHidden: boolean;
        authTagName: string;
        obsolete?: boolean;
    }
    export class PropertyGridOptions {
        properties: PropertyGridItem[];
        path: string;
        subProperty?: string;
    }

    export class PropertyGrid {
        element: JQuery;
        options: PropertyGridOptions;

        constructor(element: JQuery, options: PropertyGridOptions, method?: string) {
            this.element = element;
            this.options = options;

            if (method) {
                return this[method].apply(this);
            }

            this.OnCreate();
        }

        OnCreate() {
            if (this.options.properties == null)
                return;

            this.element.addClass('cms-propertygrid')
                .attr('data-path', this.options.path)
                .attr('data-subProperty', this.options.subProperty);

            // Autocomplete "off" workaround: https://stackoverflow.com/questions/2530/how-do-you-disable-browser-autocomplete-on-web-form-field-input-tag
            // Sonst wird automatisch der User und Passwort des angemeldeteten Benutzers in Passwort/Text Felder eingetragen
            // das Attribute autocomplete = off/new-password/false funktioniert leider nicht immer :(
            // Darum 2 dummy felder einfügen in die das gespeicherte Passwort eingetragen wird...
            //$("<div><input type='text'/><input type='password'/></div>").css('display','none').appendTo(this.element);  // so nicht, weil dann irgendwelche anderen Element verschweinden können ??!!
            var $dummy = $("<div></div>").css({ height: 0, overflow: 'hidden' }).appendTo(this.element);                  // nicht mit display:none
            $("<input type='text' value=''/>").appendTo($dummy);
            $("<input type='password' value=''/>").appendTo($dummy);

            for (var p in this.options.properties) {
                var property = this.options.properties[p];

                if (property.isHidden === true) {
                    var $hidden = $("<input type='hidden' name='" + property.name + "'>").appendTo(this.element);
                    $hidden.val(property.value);
                    continue;
                }

                var category = property.category;
                var categoryCollapsed = false, categoryObsolete = false;
                if (category.indexOf('~~') == 0) {
                    category = category.substr(2);
                    categoryObsolete = true;
                }
                if (category.indexOf('~') == 0) {
                    category = category.substr(1);
                    categoryCollapsed = true;
                }

                var $category = this.element.find("div[data-category='" + category + "']");
                if ($category.length === 0) {
                    $category = $("<div>")
                        .addClass('propertygrid-category')
                        .attr('data-category', category)
                        .appendTo(this.element);
                    var $catTitle = $("<div></div>")
                        .text(category)
                        .addClass('propertygrid-category-title')
                        .appendTo($category);
                    var $infoButton = $("<div>?</div>").addClass('info').appendTo($catTitle)
                        .click(function (e) {
                            e.stopPropagation();
                            $(this).closest('.propertygrid-category')
                                .toggleClass('description')
                                .find('.propertygrid-property-description').slideToggle();
                        })
                    var $catBody = $("<div>").addClass('propertygrid-category-body').appendTo($category);

                    $catTitle.click(function () {
                        $(this).closest('.propertygrid-category').toggleClass('collapsed');
                        $(this).next('.propertygrid-category-body').slideToggle();
                    });
                }
                if (categoryCollapsed === true || categoryObsolete === true) {
                    $category.addClass('collapsed').children('.propertygrid-category-body').css('display', 'none');
                }
                if (categoryObsolete === true) {
                    $category.addClass('obsolete');
                }

                let $propWrapper = $("<div>").addClass('propertygrid-property-wrapper').appendTo($category.find('.propertygrid-category-body'));
                let $label = $("<div><div>")
                    .text(property.displayName)
                    .addClass('propertygrid-label')
                    .appendTo($propWrapper);

                let $value = $("<div>").addClass('propertygrid-value').appendTo($propWrapper);

                if (property.obsolete === true) {
                    $propWrapper.addClass('obsolete');
                }

                if (property.domainValues != null && property.domainValues.length > 0) {
                    var $select = $("<select name='" + property.name + "'>").appendTo($value);
                    for (var i in property.domainValues) {
                        $("<option value='" + property.domainValues[i] + "'></options>")
                            .text(property.domainValues[i])
                            .appendTo($select);
                    }
                    $select.val(property.value);
                } else if (typeof (property.value) === typeof (true)) {
                    var $input = $("<input name='" + property.name + "' type='checkbox'>").appendTo($value);
                    $propWrapper.addClass('borderless');
                    $input.prop('checked', property.value);
                    if (property.readonly === true) {
                        $input.attr('onclick', 'return false;');
                    }
                } else if (jQuery.isArray(property.value)) {
                    var $input = $("<input name='" + property.name + "' type='text' readonly='readonly'>")
                        .attr('data-clickable', 'true')
                        .appendTo($value);

                    $input.val(JSON.stringify(property.value));

                    $input
                        .data('val', property.value)
                        .data('displayname', property.displayName)
                        .click(function () {
                            var editor = new TypeEditorArray($(this), $(this).data('displayname'), $(this).data('val'))
                        });
                } else {
                    var $input = $input = $("<input name='" + property.name +
                            "' type='" + (property.isPassword === true ? 'password' : 'text') +
                            "' autocomplete='" + (property.isPassword === true ? 'new-password' : 'false') + "'>").appendTo($value);
                    
                    $input.val(property.value);

                    if (property.isSecret && property.value) {
                        $input.css('display', 'none');
                        $("<button>")
                            .text("Show secret...")
                            .appendTo($value)
                            .click(function () {
                                var $input = $(this).parent().children('input');

                                if ($input.css('display') === 'none') {
                                    $input.css('display', '');
                                    $(this).text("Hide secret...");
                                } else {
                                    $input.css('display', 'none');
                                    $(this).text("Show secret...");
                                }
                            });
                    }
                    else if (property.isPassword === true) {
                        $input.on("change keyup input", function () {
                            $(this).attr('type',
                                $(this).val() &&
                                    $(this).val().toString().indexOf("{{secret-") === 0 &&
                                    $(this).val().toString().lastIndexOf("}}") === $(this).val().toString().length - 2
                                    ? 'text'
                                    : 'password');
                        });

                        $input.trigger('change');
                    }

                    if (property.readonly === true || property.isComplex === true)
                        $input.attr('readonly', 'readonly');
                }

                if (property.description) {
                    var $descriptionPanel = $("<div>").addClass('propertygrid-property-description').appendTo(/*$category.find('.propertygrid-category-body')*/$propWrapper);
                    var descriptions = property.description.split('\n\n');
                    for (var d = 0; d < descriptions.length; d++) {
                        $descriptionPanel.append($("<p>").html(descriptions[d]));
                    }
                }

                if (property.hasEditor === true) {
                    var $editor = $("<div>...</div>")
                        .addClass('propertygrid-editor-button')
                        .attr('data-property', property.name)
                        .appendTo($propWrapper)
                        .click(function (e) {
                            e.stopPropagation();

                            var $editorButton = $(this);
                            var $propertyGrid = $(this).closest('.cms-propertygrid');
                            var data = CMS.propertyGridData($propertyGrid);
                            var property = ($propertyGrid.attr('data-subProperty') ? $propertyGrid.attr('data-subProperty') + "." : "") + $editorButton.attr('data-property');

                            CMS.api('NodePropertyEditor',
                                { path: $propertyGrid.attr('data-path'), property: property, data: JSON.stringify(data) },
                                function (result) {
                                    (<any>$('body')).modalDialog(<ModalDialogOptions>{
                                        title: result.displayName,
                                        onLoad: function ($content) {
                                            (<any>$content).form(result);
                                        },
                                        onCommit: function ($sender, $modal) {
                                            $sender.addClass('loading');
                                            var $form = $modal.find('.cms-form');

                                            var data = CMS.formData($form);
                                            CMS.api('NodePropertyEditorCommit',
                                                { path: $propertyGrid.attr('data-path'), property: property, data: JSON.stringify(data) },
                                                function (result) {
                                                    $sender.removeClass('loading');
                                                    if (typeof result.value !== 'undefined') {
                                                        var $target = $propertyGrid.find("input[name='" + $editorButton.attr('data-property') + "'], select[name='" + $editorButton.attr('data-property') + "']");
                                                        $target.val(result.value);
                                                    }
                                                    CMS.closeModal($modal);
                                                },
                                                function (error) {
                                                    $sender.removeClass('loading');
                                                    alert(error || "Leider ist ein unbekannter Fehler aufgetreten");
                                                });
                                        }
                                    });
                                },
                                function (error) {
                                    alert(error);
                                });
                        });
                }
                else if (property.isComplex === true) {
                    var $editor = $("<div>...</div>")
                        .addClass('propertygrid-editor-button')
                        .attr('data-property', property.name)
                        .appendTo($propWrapper)
                        .click(function (e) {
                            e.stopPropagation();

                            var $editorButton = $(this);
                            var $propertyGrid = $(this).closest('.cms-propertygrid');
                            var data = CMS.propertyGridData($propertyGrid);
                            var subProperty = ($propertyGrid.attr('data-subProperty') ? $propertyGrid.attr('data-subProperty') + "." : "") + $editorButton.attr('data-property');

                            CMS.api('NodeProperties',
                                { path: $propertyGrid.attr('data-path'), subProperty: subProperty, data: JSON.stringify(data) },
                                function (result) {
                                    (<any>$('body')).modalDialog(<ModalDialogOptions>{
                                        title: result.displayName,
                                        onLoad: function ($content) {
                                            (<any>$content).propertyGrid(<PropertyGridOptions>{ properties: result.properties, path: result.path, subProperty: result.subProperty });
                                        },
                                        onCommit: function ($sender, $modal) {
                                            $sender.addClass('loading');
                                            var $grid = $modal.find('.cms-propertygrid');
                                            var data = CMS.propertyGridData($grid);

                                            CMS.api('NodePropertiesCommit',
                                                { path: $propertyGrid.attr('data-path'), subProperty: subProperty, data: JSON.stringify(data) },
                                                function (result) {
                                                    $sender.removeClass('loading');
                                                    if (typeof result.value !== 'undefined') {
                                                        var $target = $propertyGrid.find("input[name='" + $editorButton.attr('data-property') + "'], select[name='" + $editorButton.attr('data-property') + "']");
                                                        $target.val(result.value);
                                                    }
                                                    CMS.closeModal($modal);
                                                },
                                                function (error) {
                                                    $sender.removeClass('loading');
                                                    alert(error || "Leider ist ein unbekannter Fehler aufgetreten");
                                                }
                                            )
                                        }
                                    });
                                },
                                function (error) {
                                    alert(error);
                                }
                            )
                        });
                }

                if (property.authTagName) {
                    var $authButton = $("<div></div>")
                        .addClass('propertygrid-auth-button')
                        .attr('data-authtag', property.authTagName)
                        .appendTo($propWrapper)
                        .click(function (e) {
                            e.stopPropagation();
                            var $propertyGrid = $(this).closest('.cms-propertygrid');

                            CMS.secureNode($propertyGrid.attr('data-path'), $(this).attr('data-authtag'));
                        });
                }
            }
        }

        RefreshData() {
            var parent = this.element;
            var data = [];

            parent.find('input[name]').each(function (i, e) {
                var name = $(e).attr('name');
                if ($(e).attr('type') == 'checkbox') {
                    data.push({ name: name, value: $(e).is(':checked') });
                } else {
                    data.push({ name: name, value: $(e).val() });
                }
            });
            parent.find('select[name]').each(function (i, e) {
                var name = $(e).attr('name');
                data.push({ name: name, value: $(e).val() });
            });

            return parent.data('propertygrid-data', data);
        }
    }

    //
    //  TypeEditor Array
    //

    class TypeEditorArray {
        control: JQuery;

        constructor(control: JQuery, displayname: string, arr: Array<any>) {
            this.control = control;
            var editor = this;
            CMS.showModal(displayname,
                function ($content, $modal) {
                    $modal.data('editor', editor);
                    var $input = $("<input type='text' placeholder='neuer Wert...'>")
                        .css({ width: '100%', borderRadius: '5px', border: '1px solid #ccc', padding: '5px' })
                        .appendTo($content);

                    let inchMeter = 0.0254;
                    // empiric
                    // ???
                    let dpi = 95.999807999999988673558652928; // inchMeter * 1000.0 / 0.2644583862501058;   // wmts 0.28mm -> 1 Pixel;
                    let dpm = dpi / inchMeter;

                    var $add = $("<button>Hinzufügen</button>").appendTo($content)
                        .css({ marginBottom: '10px' })
                        .click(function () {
                            let val = $(this).prev('input').val();
                            let title = '';

                            if (val) {
                                if (val.toString().trim().indexOf('1:') === 0) {
                                    let floatVal = parseFloat(val.toString().trim().substr(2));

                                    title = val.toString().trim();
                                    val = (floatVal / dpm).toString();  // scale = resolution * dpm => resolution = scale/dpm;
                                }
                                else {
                                    let floatVal = parseFloat(val.toString());

                                    if (!isNaN(floatVal)) {
                                        title = "1:" + (floatVal * dpm).toFixed(0).toLocaleString();
                                    }
                                }

                                var $li = $("<li class='array-element'><input type='text' /><div class='list-info-text'></div><span class='list-remove'>✖</span></li>")
                                        .data('value', val)
                                        .appendTo($(this)
                                        .next('ul'));
                                $li.children('input').val(val);
                                $li.children('.list-info-text').text(title);
                                $(this).prev('input').val(' ');
                            }
                        });


                    let $ul = $("<ul>").addClass('sortable-list').appendTo($content);
                    for (let i = 0; i < arr.length; i++) {
                        let val = arr[i];
                        let numberVal = parseFloat(val);
                        let title = '';

                        if (!isNaN(numberVal)) {
                            title = "1:" + (numberVal * dpm).toFixed(0).toLocaleString();
                        }

                        let $li = $("<li class='array-element'></li>")
                            .attr('title', title)
                            .appendTo($ul);
                        $("<input type='text' />").val(val).appendTo($li);
                        $("<div class='list-info-text'></div>").text(title).appendTo($li);
                        $("<span class='list-remove'>✖</span>").appendTo($li);
                    }
                    CMS.makeSortable($ul);
                },
                function ($sender, $modal) {
                    var array = [];
                    $modal.find('li.array-element').each(function (i, e) {
                        var val = <any>$(e).children('input').val(); //$(e).data('value');
                        try {
                            var n = parseFloat(val);
                            if (n != null && !isNaN(n))
                                val = n;
                        } catch (ex) {}
                        array.push(val);
                    });
                    (<TypeEditorArray>$modal.data('editor')).control.data('val', array).val(JSON.stringify(array));
                    CMS.closeModal($modal);
                },
                null);
        }
    }
}