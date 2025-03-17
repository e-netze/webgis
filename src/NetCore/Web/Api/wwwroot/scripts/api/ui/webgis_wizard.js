(function ($) {
    "use strict";
    $.fn.webgis_wizard = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_wizard');
        }
    };
    var defaults = {
        steps: null,
        current_step: 1
    };
    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        setstep: function (options) {
            var $elem = $(this);
            var steps = parseInt($elem.attr('data-wizard-steps'));
            var currentStep = options.current_step || parseInt($elem.attr('data-wizard-current-step'));
            $elem.find('.webgis-wizard-next-button').remove();
            $elem.find('.webgis-wizard-tab').remove();
            $elem.find('.webgis-wizard-bottom').remove();
            var $currentElem = null;
            for (var s = 1; s <= steps; s++) {
                if (s === currentStep) {
                    /*$currentElem = $elem.find('.webgis-wizard-step' + s).css({
                        display: '',
                        position: ''
                    });*/
                    $currentElem = $elem.find('.webgis-wizard-step' + s).fadeIn();
                }
                else {
                    $elem.find('.webgis-wizard-step' + s).css('display', 'none');
                }
            }
            var opStep1 = currentStep > 1 ? .4 / (currentStep - 1) : 0, opStep2 = currentStep < steps ? .4 / (steps - currentStep) : 0;
            for (var s = 1; s <= currentStep; s++) {
                $("<div class='webgis-wizard-tab " + (s === currentStep ? 'webgis-wizard-tab-active' : '') + "'><div>" + (steps > 1 ? s + ".&nbsp;&nbsp;" : "") + ($elem.children('.webgis-wizard-step' + s).attr('data-wizard-step-title') || '') + "</div></div>")
                    .css({
                    position: 'absolute',
                    left: (s - 1) * 36, top: 0,
                    height: $elem.height(),
                    opacity: 1 - opStep1 * (currentStep - s)
                }).prependTo($elem).attr('data-step', s)
                    .click(function () {
                    $(this).closest('.webgis-wizard').webgis_wizard('setstep', { current_step: parseInt($(this).attr('data-step')) });
                });
            }
            for (var s = currentStep + 1; s <= steps; s++) {
                $("<div class='webgis-wizard-tab webgis-wizard-tab" + s + "'><div>" + s + ".&nbsp;&nbsp;" + ($elem.children('.webgis-wizard-step' + s).attr('data-wizard-step-title') || '') + "</div></div>")
                    .css({
                    position: 'absolute',
                    right: (steps - s) * 36, top: 0,
                    height: $elem.height(),
                    opacity: 1 - opStep2 * Math.abs(currentStep - s)
                }).appendTo($elem).attr('data-step', s)
                    .click(function () {
                    $(this)
                        .css({
                        left: $(this).position().left, right: ''
                    })
                        .animate({
                        left: s * 36
                    }, 300, function () {
                        $(this).closest('.webgis-wizard').webgis_wizard('setstep', { current_step: parseInt($(this).attr('data-step')) });
                    });
                });
            }
            var $button = $("<div class='webgis-wizard-bottom'>")
                .css({
                position: 'absolute',
                left: currentStep * 36, right: (steps - currentStep) * 36,
                bottom: 0, height: 55
            }).appendTo($elem);
            if (currentStep < steps) {
                $("<div class='button webgis-wizard-next-button' style='display:inline-block;float:right;margin:10px' data-next='" + parseInt(currentStep + 1) + "'>Next &raquo;</div>").appendTo($button)
                    .click(function () {
                    $(this).closest('.webgis-wizard').find('.webgis-wizard-tab' + $(this).attr('data-next')).trigger('click');
                });
            }
            var submitMethod = $elem.attr('data-wizard-submit-method') || 'submit';
            var submitTitle = $elem.attr('data-wizard-submit-title') || 'Submit';
            if (submitMethod === 'submit') {
                $("<button type='submit' style='margin:10px'>" + submitTitle + "</button>").appendTo($button);
            }
            if ($currentElem) {
                $currentElem.css({
                    position: 'absolute',
                    left: currentStep * 36, top: 0,
                    height: $elem.height()
                });
            }
        },
        next: function (options) {
        }
    };
    var initUI = function (elem, options) {
        var $elem = $(elem);
        $elem.addClass('webgis-wizard');
        $elem.attr('data-wizard-steps', $elem.children('.webgis-wizard-step').length);
        $elem.children('.webgis-wizard-step').each(function (i, e) {
            $(this).addClass('webgis-wizard-step' + (i + 1));
        });
        if (options.current_step)
            $elem.attr('data-wizard-current-step', options.current_step);
        var steps = parseInt($elem.attr('data-wizard-steps'));
        var height = 0;
        for (var s = 1; s <= steps; s++) {
            height = Math.max($elem.find('.webgis-wizard-step' + s).height(), height);
        }
        if ($elem.css('position') !== 'absolute') {
            $elem.css({
                position: 'relative',
                height: height + 65
            });
        }
        $elem.webgis_wizard('setstep', options);
    };
})(window.webgis && window.webgis ? window.webgis.$ : jQuery);
