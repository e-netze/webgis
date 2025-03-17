(function ($) {
    "use strict";
    $.fn.webgis_validation = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_validation');
        }
    };
    var defaults = {
        minlen: 0,
        required: false,
        regex: null,
        error: null
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        validate: function (options) {
            console.log('validate ' + $(this).attr('id'));
            var $this = $(this).removeClass('validated-parameter');

            var val = $(this).val(), error;

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
                let $errorCtrl = $(this)
                    .addClass('webgis-not-valid')
                    .next('.webgis-validation-error');

                let message = ($this.attr('webgis-validation-error') || error);
                $errorCtrl
                    .addClass('haserror')
                    .text(message);

                if (message.length > 50
                    && $errorCtrl.css('cursor') === 'pointer'
                    && $errorCtrl.hasClass('hasclickevent') === false) {  // if its 'clickable' => include expand options per click
                    $errorCtrl
                        .addClass('hasclickevent')
                        .click(function () {
                            let $this = $(this);
                            $this.css('white-space', $this.css('white-space') === 'nowrap' ? 'normal' : 'nowrap');
                        });
                }

            } else {
                //console.log('parameter is valid!', val);
                $(this)
                    .removeClass('webgis-not-valid')
                    .addClass('validated-parameter')
                    .next('.webgis-validation-error')
                    .text('')
                    .removeClass('haserror');
            }
        }
    };
    var initUI = function (elem, options) {
        var $elem = $(elem);

        $elem.addClass('webgis-validation');

        if (options.minlen) {
            $elem.attr('webgis-validation-minlen', options.minlen);
        }
        if (options.regex) {
            $elem.attr('webgis-validation-regex', options.regex);
        }
        if (options.required) {
            $elem.attr('webgis-validation-required', true);
        }
        if (options.error) {
            $elem.attr('webgis-validation-error', options.error)
        }

        $("<div>")
            .addClass('webgis-validation-error')
            .insertAfter($elem);

        $elem.on('input', function (e) {
            $(this).webgis_validation('validate');
        });
    };
})(webgis.$ || jQuery);
