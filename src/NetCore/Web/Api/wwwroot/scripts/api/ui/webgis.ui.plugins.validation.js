// WebGIS UI Plugins (validation)
// webgis_validation: input validation (required, minlen, regex) with an error message element
// Created by webgis_uibuilder.js for elements with has_validation

webgis.ui.definePlugin('webgis_validation', {
    defaults: {
        minlen: 0,
        required: false,
        regex: null,
        error: null
    },
    init: function () {
        const $ = this.$, o = this.options, $elem = this.$el;

        $elem.addClass('webgis-validation');

        if (o.minlen) {
            $elem.attr('webgis-validation-minlen', o.minlen);
        }
        if (o.regex) {
            $elem.attr('webgis-validation-regex', o.regex);
        }
        if (o.required) {
            $elem.attr('webgis-validation-required', true);
        }
        if (o.error) {
            $elem.attr('webgis-validation-error', o.error);
        }

        const $errorMessageElement = $("<div>")
            .addClass('webgis-validation-error')
            .insertAfter($elem);
        $elem.data('errorMessageElement', $errorMessageElement);

        $elem.on('input.webgis_validation', () => this.validate());
    },
    destroy: function () {
        this.$el.off('.webgis_validation');
    },
    methods: {
        validate: function () {
            const $ = this.$, $this = this.$el.removeClass('validated-parameter');

            const val = $this.val();
            let error;

            if ($this.attr('webgis-validation-required') && !val) {
                error = "Eingabe ist erforderlich";
            }
            else if ($this.attr('webgis-validation-minlen') && parseInt($this.attr('webgis-validation-minlen')) > val.length) {
                error = "Eingabe muss mindestens " + $this.attr('webgis-validation-minlen') + " Zeichen lang sein";
            }
            else if ($this.attr('webgis-validation-regex') && !val.match($this.attr('webgis-validation-regex'))) {
                error = "Die Eingabe erfüllt nicht den regulären Ausdruck: " + $this.attr('webgis-validation-regex');
            }

            if (error) {
                const $errorCtrl = $this
                    .addClass('webgis-not-valid')
                    .data('errorMessageElement');

                const message = ($this.attr('webgis-validation-error') || error);
                $errorCtrl
                    .addClass('haserror')
                    .text(message);

                if (message.length > 50
                    && $errorCtrl.css('cursor') === 'pointer'
                    && $errorCtrl.hasClass('hasclickevent') === false) {  // if its 'clickable' => include expand options per click
                    $errorCtrl
                        .addClass('hasclickevent')
                        .on('click.webgis_validation', function () {
                            const $this = $(this);
                            $this.css('white-space', $this.css('white-space') === 'nowrap' ? 'normal' : 'nowrap');
                        });
                }
            } else {
                $this
                    .removeClass('webgis-not-valid')
                    .addClass('validated-parameter')
                    .data('errorMessageElement')
                    .text('')
                    .removeClass('haserror');
            }
        }
    }
});
