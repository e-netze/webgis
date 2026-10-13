// WebGIS form (jQuery plugin webgis_form)
// Simple input form (labels, inputs, submit button), used by the sketch modules (e.g. construction tools)

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
