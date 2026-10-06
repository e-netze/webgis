var UI;
(function (UI) {
    var FormControl = /** @class */ (function () {
        function FormControl() {
        }
        return FormControl;
    }());
    var FormLabelControl = /** @class */ (function () {
        function FormLabelControl() {
        }
        return FormLabelControl;
    }());
    var FormTableControl = /** @class */ (function () {
        function FormTableControl() {
        }
        return FormTableControl;
    }());
    var FormGroupBoxControl = /** @class */ (function () {
        function FormGroupBoxControl() {
        }
        return FormGroupBoxControl;
    }());
    var FormTextAreaControl = /** @class */ (function () {
        function FormTextAreaControl() {
        }
        return FormTextAreaControl;
    }());
    var KeyValue = /** @class */ (function () {
        function KeyValue() {
        }
        return KeyValue;
    }());
    var FormInputControl = /** @class */ (function () {
        function FormInputControl() {
        }
        return FormInputControl;
    }());
    var FormComboBoxOption = /** @class */ (function () {
        function FormComboBoxOption() {
        }
        return FormComboBoxOption;
    }());
    var FormComboBoxControl = /** @class */ (function () {
        function FormComboBoxControl() {
        }
        return FormComboBoxControl;
    }());
    var FormInfoTextControl = /** @class */ (function () {
        function FormInfoTextControl() {
        }
        return FormInfoTextControl;
    }());
    var FormButtonControl = /** @class */ (function () {
        function FormButtonControl() {
        }
        return FormButtonControl;
    }());
    var FormImageButtonControl = /** @class */ (function () {
        function FormImageButtonControl() {
        }
        return FormImageButtonControl;
    }());
    var FormSecurityButtonControl = /** @class */ (function () {
        function FormSecurityButtonControl() {
        }
        return FormSecurityButtonControl;
    }());
    var FormNavTreeControl = /** @class */ (function () {
        function FormNavTreeControl() {
        }
        return FormNavTreeControl;
    }());
    var FormOptions = /** @class */ (function () {
        function FormOptions() {
            this.buttonClickMethod = "ToolButtonClick";
        }
        return FormOptions;
    }());
    UI.FormOptions = FormOptions;
    var Form = /** @class */ (function () {
        function Form(element, options, method) {
            this.element = element;
            this.options = options;
            if (method) {
                return this[method].apply(this);
            }
            this.OnCreate();
        }
        Form.prototype.OnCreate = function () {
            this.element.empty()
                .attr('data-path', this.options.path)
                .attr('data-name', this.options.name)
                .data('options', this.options)
                .addClass('cms-form');
            // Autocomplete "off" workaround: https://stackoverflow.com/questions/2530/how-do-you-disable-browser-autocomplete-on-web-form-field-input-tag
            // Sonst wird automatisch der User und Passwort des angemeldeteten Benutzers in Passwort/Text Felder eingetragen
            // das Attribute autocomplete = off/new-password/false funktioniert leider nicht immer :(
            // Darum 2 dummy felder einfügen in die das gespeicherte Passwort eingetragen wird...
            //$("<div><input type='text'/><input type='password'/></div>").css('display','none').appendTo(this.element);  // so nicht, weil dann irgendwelche anderen Element verschweinden können ??!!
            var $dummy = $("<div></div>").css({ height: 0, overflow: 'hidden' }).appendTo(this.element); // nicht mit display:none
            $("<input type='text' value=''/>").appendTo($dummy);
            $("<input type='password' value=''/>").appendTo($dummy);
            this.Parse(this.element, this.options.controls);
        };
        Form.prototype.Parse = function (parent, controls) {
            if (!controls)
                return;
            for (var i = 0; i < controls.length; i++) {
                var control = controls[i];
                var $control = null, $body = null;
                var isClickable = false;
                var clickEvent = 'click';
                if (control.type === 'groupbox') {
                    var groupbox = control;
                    $control = $("<div>")
                        .addClass('cms-form-groupbox' + (groupbox.collapsed === true ? ' collapsed' : ''))
                        .appendTo(parent);
                    var $title = $("<div><div>")
                        .text(groupbox.label)
                        .addClass('text')
                        .appendTo($control)
                        .click(function () {
                        $(this).parent().toggleClass('collapsed');
                        $(this)
                            .next('.body')
                            .slideToggle();
                    });
                    $body = $("<div>").addClass('body').appendTo($control);
                    if (groupbox.collapsed === true)
                        $body.css('display', 'none');
                }
                else if (control.type === 'input' || control.type === 'inputpassword' || control.type === 'inputautocomplete') {
                    var input = control;
                    $control = this.InputContainer(input.label, input.required === true).appendTo(parent);
                    var $input = $("<input type='" + (input.isPassword === true ? 'password' : 'text') + "' placeholder='" + (input.placeholder === null || input.placeholder === 'null' ? '' : input.placeholder) + "'" + "' name='" + input.name + "'>").appendTo($control)
                        .data('control-options', input)
                        .val(input.value)
                        .on('blur', function () {
                        UI.Form.prototype.ModifyInput($(this));
                    });
                    $input.attr('autocomplete', input.isPassword === true ? 'new-password' : 'false');
                    if (input.dependsFrom) {
                        var $dep = parent.closest('.cms-form').find("input[name='" + input.dependsFrom + "']");
                        if ($dep.length == 0)
                            $dep = parent.closest('.cms-form').find("select[name='" + input.dependsFrom + "']");
                        if ($dep.length > 0) {
                            if ($dep.data('dep-controls'))
                                $dep.data('dep-controls').push($input);
                            else
                                $dep.data('dep-controls', [$input]);
                            $dep.keyup(function () {
                                UI.Form.prototype.ModifyDependents($(this));
                            });
                        }
                    }
                    if (control.type === 'inputautocomplete') {
                        CMS.addAutocompleteEvents($control.find('input'));
                    }
                }
                else if (control.type === 'textarea') {
                    var textarea = control;
                    $control = this.InputContainer(textarea.label, textarea.required === true).appendTo(parent);
                    var $textarea = $("<textarea></textarea").attr('rows', textarea.rows).attr('name', textarea.name).html(textarea.value).appendTo($control);
                }
                else if (control.type === 'checkbox') {
                    var checkbox = control;
                    $("<input type='checkbox' name='" + checkbox.name + "'>").css('display', 'none').appendTo(parent);
                    $control = $("<div>").addClass('cms-form-checkbox').appendTo(parent);
                    if (checkbox.label)
                        $control.text(checkbox.label);
                    $control.click(function (e) {
                        e.stopPropagation();
                        var $this = $(this).toggleClass('checked');
                        var $checkbox = $this.prev("input[type='checkbox']");
                        $checkbox.prop('checked', $this.hasClass('checked'));
                    });
                    if (checkbox.value === "true") {
                        $control /*.addClass('checked')*/.trigger('click');
                    }
                }
                else if (control.type === 'combobox') {
                    var select = control;
                    $control = this.InputContainer(select.label, select.required === true).appendTo(parent);
                    var $select = $("<select name='" + select.name + "'>").appendTo($control);
                    if (select.options) {
                        for (var o in select.options) {
                            var option = select.options[o];
                            if (option.value === null && option.label === null && select.options.length === 1) // dont show empty lists
                                continue;
                            $("<option value='" + option.value + "'></option>")
                                .text(option.label)
                                .appendTo($select);
                        }
                    }
                    if (select.value != null)
                        $select.val(select.value.toString());
                    if (select.triggerOnChange === true) {
                        isClickable = true;
                        clickEvent = 'change';
                    }
                }
                else if (control.type === 'listbox') {
                    var list = control;
                    $control = this.InputContainer(list.label, list.required === true).appendTo(parent);
                    var $list = $("<ul>")
                        .addClass('cms-listbox')
                        .attr('name', list.name)
                        .data('multiSelect', list.multiSelect === false ? false : true)
                        .data('selectAndCommit', list.selectAndCommit === true ? true : false)
                        .appendTo($control);
                    if (list.height) {
                        $list.css('height', list.height);
                        $list.css('overflow', 'auto');
                    }
                    if (list.selectAndCommit === true) {
                        var $commitButton = $control.closest('.modaldialog-modal').find('.modaldialog-button-commit');
                        if ($commitButton.length > 0) {
                            $list.data('commit-button', $commitButton.css('display', 'none'));
                        }
                    }
                    if (list.options) {
                        var $inputMenu = $("<li>").addClass('menu').appendTo($list);
                        $("<input placeholder='Element(e) suchen...'>")
                            .appendTo($inputMenu)
                            .on('keyup', function () {
                            function checkSearchItem(item, terms) {
                                for (var t in terms) {
                                    if (item.indexOf(terms[t]) < 0)
                                        return false;
                                }
                                return true;
                            }
                            var val = $(this).val().toString().toLocaleLowerCase();
                            var terms = val.split(' ');
                            if (val === '') {
                                $('[data-searchitem]').css('display', '');
                            }
                            else {
                                $('[data-searchitem]').each(function () {
                                    $(this).css('display', checkSearchItem($(this).attr('data-searchitem'), terms) ? '' : 'none');
                                });
                            }
                        });
                        if (list.multiSelect === true) {
                            var $menu = $("<li>").addClass('menu').appendTo($list);
                            $("<button>Alle auswählen</button>").appendTo($menu)
                                .click(function (e) {
                                $(this).closest('.cms-listbox').children('li[data-value]').each(function (i, e) {
                                    var $e = $(e);
                                    if ($e.css('display') !== 'none' && $e.hasClass('selected') === false)
                                        $e.trigger('click');
                                });
                            });
                            $("<button>Alle aufheben</button>").appendTo($menu)
                                .click(function (e) {
                                $(this).closest('.cms-listbox').children('li[data-value]').each(function (i, e) {
                                    var $e = $(e);
                                    if ($e.css('display') !== 'none' && $e.hasClass('selected') === true)
                                        $e.trigger('click');
                                });
                            });
                        }
                        for (var o in list.options) {
                            var option = list.options[o];
                            var $li = $("<li></li>")
                                .text(option.label)
                                .attr('data-searchitem', option.label.toLocaleLowerCase())
                                .attr('data-value', option.value)
                                .appendTo($list)
                                .click(function () {
                                var $list = $(this).parent();
                                if ($list.data('multiSelect') === false)
                                    $list.children('li').removeClass('selected');
                                $(this).toggleClass('selected');
                                var val = [];
                                $list.children('li.selected').each(function (i, e) {
                                    val.push($(e).attr('data-value'));
                                });
                                $list.data('val', val);
                                if ($list.data('selectAndCommit') === true && $list.data('commit-button'))
                                    $list.data('commit-button').trigger('click');
                            });
                            if (option.selected === true)
                                $li.trigger('click');
                        }
                    }
                }
                else if (control.type === "button" || control.type === 'imagebutton') {
                    switch (control.type) {
                        case 'button':
                            var button = control;
                            $control = $("<button></button>")
                                .text(button.label)
                                .data('name', button.name)
                                .appendTo(parent);
                            break;
                        case 'imagebutton':
                            var imageButton = control;
                            $control = $("<div>")
                                .addClass('cms-form-imagebutton')
                                .css({
                                width: imageButton.width,
                                height: imageButton.height,
                                backgroundImage: 'url(' + CMS.toAbsUrl(imageButton.image) + ')'
                            })
                                .data('name', imageButton.name)
                                .appendTo(parent);
                            break;
                    }
                    isClickable = true;
                }
                else if (control.type == "securitybutton") {
                    var secButton = control;
                    $control = $("<div>")
                        .addClass('cms-form-auth-button')
                        .attr('data-path', secButton.path)
                        .attr('data-authtag', secButton.authTag)
                        .appendTo(parent)
                        .click(function (e) {
                        e.stopPropagation();
                        CMS.secureNode($(this).attr('data-path'), $(this).attr('data-authtag'));
                    });
                }
                else if (control.type === 'lazynavtree') {
                    var navTree = control;
                    $control = $("<div>").appendTo(parent);
                    CMS.toLazyNavTree($control, this.options.path, this.options.name, navTree.singleSelect);
                }
                else if (control.type === 'infotext') {
                    var infoText = control;
                    $control = $("<div>")
                        .addClass('info-text')
                        .css('background-color', infoText.bgColor)
                        .appendTo(parent);
                    var textBlocks = infoText.text.split('\n');
                    for (var t in textBlocks) {
                        $("<div>").text(textBlocks[t]).appendTo($control);
                    }
                }
                else if (control.type === 'label') {
                    var label = control;
                    $control = $("<div>").html(label.label).addClass('cms-form-label').appendTo(parent);
                }
                else if (control.type === 'sublabel') {
                    var sublabel = control;
                    $control = $("<div>").html(sublabel.label).addClass('cms-form-sub-label').appendTo(parent);
                }
                else if (control.type === "heading") {
                    var label = control;
                    $control = $("<div>").html(label.label).addClass('cms-form-heading').appendTo(parent);
                }
                else if (control.type === 'table') {
                    var table = control;
                    $control = $("<table>").addClass('cms-form-table').appendTo(parent);
                    if (table.headers) {
                        var $tr = $("<tr>").appendTo($control);
                        for (var h = 0; h < table.headers.length; h++) {
                            var $th = $("<th>").html(table.headers[h]).appendTo($tr);
                            if (table.columnWidths && table.columnWidths.length > h)
                                $th.css('width', table.columnWidths[h]);
                        }
                    }
                }
                else if (control.type === 'table.row') {
                    $control = $("<tr>").addClass('cms-form-table').appendTo(parent);
                }
                else if (control.type === 'table.cell') {
                    $control = $("<td>").addClass('cms-form-table').appendTo(parent);
                }
                else {
                    //$control = $("<div>").appendTo(parent);
                    $control = parent;
                }
                if (control.name)
                    $control.attr('data-control-name', control.name);
                if (isClickable === true || control.isClickable === true) {
                    $control
                        .data('clickMethod', this.options.buttonClickMethod)
                        .data('btn', control.name)
                        .on(clickEvent, function () {
                        var $this = $(this);
                        $this.addClass('loading');
                        var $form = $this.closest('.cms-form');
                        var path = $form.attr('data-path');
                        var name = $form.attr('data-name');
                        var btn = $this.data('name') || $this.data('btn');
                        if (name === '~')
                            name = $this.parent().closest('*[data-control-name]').attr('data-control-name');
                        var data = CMS.formData($form);
                        CMS.api($this.data('clickMethod'), { path: path, name: name, btn: btn, data: JSON.stringify(data) }, function (result) {
                            if (result) {
                                if (result.controls) {
                                    CMS.refreshForm($form, result.controls);
                                }
                                if (result.styleSetters) {
                                    CMS.applyStyleSetters(result.styleSetters);
                                }
                            }
                            $this.removeClass('loading');
                        }, function (error) {
                            $this.removeClass('loading');
                            CMS.alert(error);
                        });
                    });
                }
                if (control.visible === false && $control.get(0) !== this.element.get(0)) {
                    $control.css('display', 'none');
                }
                if (control.enabled === false) {
                    $control.find('input').attr('readonly', 'readonly');
                    $control.find('select').attr('readonly', 'readonly');
                    $control.find('textarea').attr('readonly', 'readonly');
                }
                if ($control && control.controls) {
                    this.Parse($body || $control, control.controls);
                }
            }
            this.element.find('.input-container input')
                .on('keyup', function () {
                UI.Form.prototype.SetRequiredFieldStyles($(this));
            });
            this.element.find('.input-container select')
                .on('change', function () {
                UI.Form.prototype.SetRequiredFieldStyles($(this));
            });
        };
        Form.prototype.SetRequiredFieldStyles = function (input) {
            var $inputContainer = input.closest('.input-container');
            if ($inputContainer.hasClass('required')) {
                if (input.val()) {
                    $inputContainer.addClass('hasvalue').removeClass('error');
                }
                else {
                    $inputContainer.removeClass('hasvalue').addClass('error');
                }
            }
        };
        Form.prototype.InputContainer = function (label, isRequired) {
            var $container = $("<div>").addClass('input-container');
            if (isRequired === true) {
                $container.addClass('required');
            }
            $("<div></div>")
                .text(label)
                .addClass('label')
                .appendTo($container);
            return $container;
        };
        Form.prototype.RefreshForm = function () {
            if (!this.options.controls)
                return;
            if (this.options.controls.length == 1 &&
                this.options.controls[0].name &&
                this.options.controls[0].name == this.element.attr('data-control-name')) {
                // Refresh complete form (eg. Security Control)
                var controls = this.options.controls[0].controls;
                this.options = this.element.data('options');
                this.element.empty();
                this.Parse(this.element, controls);
                return;
            }
            for (var c in this.options.controls) {
                var control = this.options.controls[c];
                if (control.type === 'input') {
                    var input = control;
                    var $control = this.element.find("input[name='" + control.name + "']");
                    $control.val(input.value);
                    UI.Form.prototype.SetRequiredFieldStyles($control);
                }
                else if (control.type === 'textarea') {
                    var textarea = control;
                    var $control = this.element.find("textarea[name='" + control.name + "']");
                    $control.val(textarea.value);
                    UI.Form.prototype.SetRequiredFieldStyles($control);
                }
                else if (control.type === 'combobox') {
                    var select = control;
                    var $control = this.element.find("select[name='" + control.name + "']").empty();
                    if (select.options) {
                        for (var o in select.options) {
                            var option = select.options[o];
                            $("<option value='" + option.value + "'></option>")
                                .text(option.label)
                                .appendTo($control);
                        }
                    }
                    $control.val(select.value).trigger('change');
                }
                else if (control.type === 'listbox') {
                    var list = control;
                    var $list = this.element.find(".cms-listbox[name='" + control.name + "']");
                    $list.children('li[data-value]').remove();
                    for (var o in list.options) {
                        var option = list.options[o];
                        var $li = $("<li></li>")
                            .text(option.label)
                            .attr('data-searchitem', option.label.toLocaleLowerCase())
                            .attr('data-value', option.value)
                            .appendTo($list)
                            .click(function () {
                            var $list = $(this).parent();
                            if ($list.data('multiSelect') === false)
                                $list.children('li').removeClass('selected');
                            $(this).toggleClass('selected');
                            var val = [];
                            $list.children('li.selected').each(function (i, e) {
                                val.push($(e).attr('data-value'));
                            });
                            $list.data('val', val);
                            if ($list.data('selectAndCommit') === true && $list.data('commit-button'))
                                $list.data('commit-button').trigger('click');
                        });
                        if (option.selected === true)
                            $li.trigger('click');
                    }
                }
            }
        };
        Form.prototype.RefreshData = function () {
            var parent = this.element;
            var data = [];
            parent.find('input[name]').each(function (i, e) {
                var name = $(e).attr('name');
                if ($(e).attr('type') == 'checkbox') {
                    data.push({ name: name, value: UI.Form.prototype.VerifyInputValue($(e), $(e).is(':checked')) });
                }
                else {
                    data.push({ name: name, value: UI.Form.prototype.VerifyInputValue($(e), $(e).val()) });
                }
            });
            parent.find('textarea[name]').each(function (i, e) {
                var name = $(e).attr('name');
                data.push({ name: name, value: UI.Form.prototype.VerifyInputValue($(e), $(e).val()) });
            });
            parent.find('select[name]').each(function (i, e) {
                var name = $(e).attr('name');
                data.push({ name: name, value: UI.Form.prototype.VerifyInputValue($(e), $(e).val()) });
            });
            parent.find('.cms-listbox').each(function (i, e) {
                var name = $(e).attr('name');
                data.push({ name: name, value: UI.Form.prototype.VerifyInputValue($(e), JSON.stringify($(e).data('val'))) });
            });
            return parent.data('form-data', data);
        };
        Form.prototype.HasFormErrors = function () {
            var parent = this.element;
            return parent.data('form-haserros', parent.find('.input-container.error').length > 0);
        };
        Form.prototype.VerifyInputValue = function (input, val) {
            var $inputContainer = input.closest('.input-container');
            if ($inputContainer.length == 0 && input.attr('type') !== 'checkbox') {
                return;
            }
            if (!val && $inputContainer.hasClass('required')) {
                $inputContainer.addClass('error');
            }
            else {
                $inputContainer.removeClass('error');
            }
            return val;
        };
        Form.prototype.ModifyInput = function (input) {
            var val = input.val();
            var options = input.data('control-options');
            if (options.modifyMethods) {
                for (var m in options.modifyMethods) {
                    var modifyMethod = options.modifyMethods[m];
                    val = val[modifyMethod]();
                }
            }
            if (options.regexReplace) {
                for (var r in options.regexReplace) {
                    var regexReplace = options.regexReplace[r];
                    var re = new RegExp(regexReplace.key);
                    while (re.test(val)) {
                        val = val.replace(new RegExp(regexReplace.key), regexReplace.value);
                    }
                }
            }
            input.val(val);
            UI.Form.prototype.SetRequiredFieldStyles(input);
        };
        Form.prototype.ModifyDependents = function (input) {
            var dependents = input.data('dep-controls');
            for (var d in dependents) {
                var dep = dependents[d];
                dep.val(input.val());
                this.ModifyInput(dep);
            }
        };
        ;
        return Form;
    }());
    UI.Form = Form;
})(UI || (UI = {}));
