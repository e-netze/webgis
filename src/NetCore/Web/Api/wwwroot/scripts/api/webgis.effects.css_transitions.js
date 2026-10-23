// WebGIS Effects (CSS transitions)
// Replaces the jQuery effect methods slideUp/slideDown/slideToggle and fadeIn/fadeOut/fadeToggle
// with CSS transitions (styles: default.css, ".webgis-fx-*"). Call sites stay unchanged.
// The classes are only set while a transition runs; the final state is the same as with jQuery
// (inline "display:none" when hidden).
// Opt-out (custom.js): webgis.usability.cssAnimations = false -> original jQuery effects.
(function ($) {
    "use strict";

    if (!$ || !$.fn) {
        return;
    }

    var original = {
        slideUp: $.fn.slideUp,
        slideDown: $.fn.slideDown,
        slideToggle: $.fn.slideToggle,
        fadeIn: $.fn.fadeIn,
        fadeOut: $.fn.fadeOut,
        fadeToggle: $.fn.fadeToggle
    };

    var fxClasses = 'webgis-fx-slide webgis-fx-fade webgis-fx-hiding';

    var useCssTransitions = function () {
        return !(webgis.usability && webgis.usability.cssAnimations === false);
    };

    var defaultDisplayCache = {};
    var defaultDisplay = function (elem) {
        var nodeName = elem.nodeName;
        if (!defaultDisplayCache[nodeName]) {
            var temp = document.body.appendChild(document.createElement(nodeName));
            var display = window.getComputedStyle(temp).display;
            temp.parentNode.removeChild(temp);
            defaultDisplayCache[nodeName] = (!display || display === 'none') ? 'block' : display;
        }
        return defaultDisplayCache[nodeName];
    };

    var computedDisplay = function (elem) {
        return elem.isConnected ? window.getComputedStyle(elem).display : elem.style.display;
    };

    var isHidden = function (elem) {
        return elem.classList.contains('webgis-fx-hiding') ||
            elem.style.display === 'none' ||
            computedDisplay(elem) === 'none';
    };

    var transitionDurationMs = function (elem) {
        if (!elem.isConnected) {
            return 0;
        }
        var max = 0;
        window.getComputedStyle(elem).transitionDuration.split(',').forEach(function (d) {
            d = d.trim();
            var ms = d.indexOf('ms') > 0 ? parseFloat(d) : parseFloat(d) * 1000;
            if (ms > max) {
                max = ms;
            }
        });
        return max;
    };

    // calls callback once, after the transition of elem has ended (fallback: timeout)
    var afterTransition = function (elem, callback) {
        var done = false;
        var finish = function () {
            if (done) {
                return;
            }
            done = true;
            elem.removeEventListener('transitionend', onEnd);
            callback.call(elem);
        };
        var onEnd = function (e) {
            if (e.target === elem) {
                finish();
            }
        };
        elem.addEventListener('transitionend', onEnd);
        setTimeout(finish, transitionDurationMs(elem) + 50);
    };

    var showElement = function (elem) {
        if (elem.style.display === 'none') {
            elem.style.display = '';
        }
        if (computedDisplay(elem) === 'none') {
            elem.style.display = defaultDisplay(elem);
        }
    };

    // jQuery signatures: (duration, easing, complete) or ({ duration, complete })
    var parseArgs = function (args) {
        var result = { instant: false, complete: null };
        for (var i = 0; i < args.length; i++) {
            var arg = args[i];
            if (typeof arg === 'function') {
                result.complete = arg;
            } else if (arg === 0) {
                result.instant = true;
            } else if (arg && typeof arg === 'object') {
                if (arg.duration === 0) {
                    result.instant = true;
                }
                if (typeof arg.complete === 'function') {
                    result.complete = arg.complete;
                }
            }
        }
        return result;
    };

    var run = function (elem, effect, show, options) {
        var $elem = $(elem);

        var complete = function () {
            if (options.complete) {
                options.complete.call(elem);
            }
        };

        if (show !== isHidden(elem)) {
            // already in (or transitioning to) the target state
            setTimeout(complete, 0);
            return;
        }

        var token = {};
        elem._webgisFxToken = token;

        if (options.instant) {
            $elem.removeClass(fxClasses);
            if (show) {
                showElement(elem);
            } else {
                elem.style.display = 'none';
            }
            setTimeout(complete, 0);
            return;
        }

        $elem.removeClass(fxClasses).addClass('webgis-fx-' + effect);
        if (show) {
            showElement(elem);  // @starting-style animates from the collapsed/transparent state
        } else {
            $elem.addClass('webgis-fx-hiding');
        }

        afterTransition(elem, function () {
            if (elem._webgisFxToken !== token) {
                return;  // superseded by a newer effect on this element
            }
            elem._webgisFxToken = null;
            if (!show) {
                elem.style.display = 'none';
            }
            $elem.removeClass(fxClasses);
            complete();
        });
    };

    var defineEffect = function (name, effect, show) {
        $.fn[name] = function () {
            if (!useCssTransitions() && original[name]) {
                webgis.log('effect (jQuery):', name, this);
                this.removeClass(fxClasses);
                return original[name].apply(this, arguments);
            }

            webgis.log('effect (css):', name, this);
            var options = parseArgs(arguments);
            return this.each(function () {
                var targetShow = show === 'toggle' ? isHidden(this) : show;
                run(this, effect, targetShow, options);
            });
        };
    };

    defineEffect('slideDown', 'slide', true);
    defineEffect('slideUp', 'slide', false);
    defineEffect('slideToggle', 'slide', 'toggle');
    defineEffect('fadeIn', 'fade', true);
    defineEffect('fadeOut', 'fade', false);
    defineEffect('fadeToggle', 'fade', 'toggle');

    webgis.effects = webgis.effects || {};
    webgis.effects.afterTransition = afterTransition;
})(webgis.$ || window.jQuery);
