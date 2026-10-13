// WebGIS UI Plugin webgis_form
// Simple input form (labels, inputs, submit button), used by the sketch modules (e.g. construction tools).
// Values of named forms are remembered (webgis.$._webgis_form_persistent) and prefilled next time.

webgis.ui.definePlugin('webgis_form', {
    defaults: {
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
    },
    init: function () {
        const $ = this.$;
        const options = this.options;
        const parent = this.$el[0];
        const persistent = this.constructor.persistentValues();

        const $parent = this.$el
            .addClass('webgis-form')
            .on('click.webgis_form', function (e) {
                e.stopPropagation();  // e.g. if the form is included in an context menu => do not close the menu on click
            });

        for (var i in options.input) {
            var input = options.input[i], $input;

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
                    .on('keypress.webgis_form', function (e) {
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
                      persistent[options.name] &&
                      persistent[options.name][input.name] != null) {
                $input.val(persistent[options.name][input.name]);
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
            .on('click.webgis_form', function (e) {
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
                        persistent[options.name] = persistent[options.name] || [];
                        persistent[options.name][$e.attr('name')] = val;
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
    },
    destroy: function () {
        this.$el.off('.webgis_form');
    },
    staticMethods: {
        // remembered input values of all named forms: { formName: { inputName: value } }
        persistentValues: function () {
            const $ = webgis.$ || jQuery;
            $._webgis_form_persistent = $._webgis_form_persistent || [];
            return $._webgis_form_persistent;
        }
    }
});
