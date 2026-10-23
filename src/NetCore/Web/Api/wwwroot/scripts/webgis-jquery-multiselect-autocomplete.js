(function($){
    "use strict"
    $.fn.webgis_autocomplete_multiselect = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on jQuery.webgis_autocomplete_multiselect');
        }
    };

    var defaults = {
        source: '',
        name: 'webgis-autocomplete',
        prefixes: [],
        prefix_separator: '::',
        alwaysIncludeOwner: ''
    };

    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);

            return this.each(function () {
                new initUI(this, options);
            });
        },
        add: function (options) {
            options = $.extend({}, defaults, options);

            $(this).webgis_autocomplete_multiselect('remove', options);

            var prefix = $(this).find('.webgis-autocomplete-multiselect-prefix').val();

            if (options.value !== "*") {
                $(this).find('.webgis-autocomplete-multiselect-prefix').children("option").each(function (i, o) {
                    if ($(o).val() && options.value.indexOf($(o).val()) === 0)
                        prefix = $(o).val();
                });
            }

            //console.log(options.value, options.prefix_separator,  options.value.indexOf(options.prefix_separator));
            if (options.value !== "*" &&
                //options.value.indexOf(prefix) !== 0 &&
                options.value.indexOf(options.prefix_separator) < 0) {  // check if options has already a prefix
                options.value = prefix + options.value;
            }

            var displayValue = options.value;
            if (prefix && displayValue.indexOf(prefix) === 0) {
                displayValue = "<strong>" + prefix + "</strong>" + displayValue.substr(prefix.length, displayValue.length - prefix.length);
            }

            var $valContainer = $(this).find('.webgis-autocomplete-multiselect-value-conatiner');

            var $div = $("<div class='webgis-autocomplete-multiselect-value-item' style='border:1px solid #aaa;background:#eee;padding:5px;margin:2px;display:inline-block' data-value='" + options.value + "'>" +
                displayValue +
                "</div>").appendTo($valContainer);
            $("<div style='width:20px;height:20px;background:#333;color:white;float:right;font-weight:bold;text-align:center;border-radius:3px;cursor:pointer;margin-left:5px'>X</div>").appendTo($div)
                .click(function () {
                    $(this).closest('.webgis-autocomplete-multiselect').webgis_autocomplete_multiselect('remove', {
                        value: $(this).closest('.webgis-autocomplete-multiselect-value-item').attr('data-value')
                    });
                });

            if (options.alwaysIncludeOwner && options.alwaysIncludeOwner !== options.value) {
                $(this).webgis_autocomplete_multiselect('add', { value: options.alwaysIncludeOwner });
            } else {
                $(this).webgis_autocomplete_multiselect('_calc');
            }
        },
        remove: function (options) {
            var $valContainer = $(this).find(".webgis-autocomplete-multiselect-value-item[data-value='" + options.value.replace(/\\/g,'\\\\') + "']").remove();
            $(this).webgis_autocomplete_multiselect('_calc');
        },
        _calc: function (options) {
            var val = '';
            $(this).find('.webgis-autocomplete-multiselect-value-item').each(function (i, e) {
                if (val !== '') val += ',';
                val += $(e).attr('data-value');
            });
            $(this).find('.webgis-autocomplete-multiselect-value').val(val);
        }
    };

    var initUI = function (elem, options) {

        var $elem = $(elem);
        $elem.addClass('webgis-autocomplete-multiselect');

        var $inputRow = $("<tr>").appendTo($("<table style='padding:0px;margin:0px'>").appendTo($elem));

        if (options.prefixes && options.prefixes.length > 0) {
            var $select = $("<select class='webgis-autocomplete-multiselect-prefix' style='width:auto;text-align:right' />");
            $select.appendTo($("<td style='padding:0px'>").appendTo($inputRow));

            for (var i in options.prefixes) {
                var prefix = options.prefixes[i];
                $("<option value='" + prefix + "'>" + prefix + "</option>").appendTo($select);
            }

            $("<option value=''>custom</option>").appendTo($select);
        }

        var $input = $("<input class='webgis-autocomplete-multiselect-input' type='text' style='width:240px' />");
        $input.appendTo($("<td style='padding:0px'>").appendTo($inputRow));

        var $button = $("<button style='font-weight:bold;height:33px;position:relative;top:0px' class='add-button'>+</button>");
        $button.appendTo($("<td style='padding:0px'>").appendTo($inputRow))
            .click(function (event) {
                event.stopPropagation();
                var control = $(this).closest('.webgis-autocomplete-multiselect');
                control.webgis_autocomplete_multiselect('add', { value: control.find('.webgis-autocomplete-multiselect-input').val(), alwaysIncludeOwner: options.alwaysIncludeOwner });
                return false;
            });

        var $valContainer = $("<div class='webgis-autocomplete-multiselect-value-conatiner' />").appendTo($elem);
        var $value = $("<input class='webgis-autocomplete-multiselect-value' type='hidden' name='" + options.name + "' id='" + options.name + "' />").appendTo($elem);

        var suggestions = attachSuggestions($input[0], options, function (value) {
            $elem.webgis_autocomplete_multiselect('add', { value: value, alwaysIncludeOwner: options.alwaysIncludeOwner });
        });

        $input.keydown(function (event) {
            if (event.keyCode === 13) {
                event.preventDefault();
                if (!suggestions.selectCursor()) {
                    $(this).closest('.webgis-autocomplete-multiselect').find('.add-button').trigger('click');
                }
                return false;
            }
        });
    };

    // simple suggestion menu below the input (no external autocomplete library)
    var attachSuggestions = function (input, options, onSelect) {
        var menu = document.createElement('div');
        menu.className = 'webgis-autocomplete-multiselect-menu';
        menu.style.cssText = 'display:none;position:fixed;z-index:99999999;box-sizing:border-box;max-height:300px;overflow-y:auto;background:#fff;border:1px solid #ccc;box-shadow:0 2px 6px rgba(0,0,0,.2);text-align:left';
        document.body.appendChild(menu);

        var timer = null, controller = null, cursor = -1;

        var items = function () { return menu.querySelectorAll('.webgis-autocomplete-multiselect-menu-item'); };
        var isOpen = function () { return menu.style.display === 'block'; };
        var close = function () { menu.style.display = 'none'; cursor = -1; };
        var open = function () {
            var rect = input.getBoundingClientRect();
            menu.style.left = rect.left + 'px';
            menu.style.top = rect.bottom + 'px';
            menu.style.width = rect.width + 'px';
            menu.style.display = 'block';
        };
        var select = function (item) {
            close();
            onSelect(item.dataset.value);
        };
        var setCursor = function (index) {
            var all = items();
            if (all.length === 0) {
                return;
            }
            cursor = (index + all.length) % all.length;
            all.forEach(function (item, i) { item.style.background = i === cursor ? '#e8f0fe' : ''; });
            all[cursor].scrollIntoView({ block: 'nearest' });
        };

        var query = function () {
            var source = options.source;
            var $prefix = $(input).closest('.webgis-autocomplete-multiselect').find('.webgis-autocomplete-multiselect-prefix');
            if ($prefix.length > 0) {
                source += (source.indexOf('?') > 0 ? '&' : '?') + 'prefix=' + encodeURIComponent($prefix.val());
            }
            source += (source.indexOf('?') > 0 ? '&' : '?') + 'term=' + encodeURIComponent(input.value);

            if (controller) {
                controller.abort();
            }
            controller = new AbortController();

            fetch(source, { signal: controller.signal })
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    menu.replaceChildren();
                    cursor = -1;
                    (data || []).slice(0, 12).forEach(function (entry) {
                        var div = document.createElement('div');
                        div.className = 'webgis-autocomplete-multiselect-menu-item';
                        div.style.cssText = 'padding:4px 8px;cursor:pointer';
                        div.dataset.value = typeof entry === 'string' ? entry : (entry.value || entry.label || '');
                        div.textContent = typeof entry === 'string' ? entry : (entry.label || entry.value || '');
                        div.addEventListener('mouseenter', function () { div.style.background = '#e8f0fe'; });
                        div.addEventListener('mouseleave', function () { div.style.background = ''; });
                        menu.appendChild(div);
                    });
                    if (menu.children.length > 0) {
                        open();
                    } else {
                        close();
                    }
                })
                .catch(function () { });
        };

        input.addEventListener('input', function () {
            clearTimeout(timer);
            if (input.value.length < 3) {
                close();
                return;
            }
            timer = setTimeout(query, 250);
        });
        input.addEventListener('keydown', function (e) {
            if (!isOpen()) {
                return;
            }
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                setCursor(cursor + 1);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                setCursor(cursor - 1);
            } else if (e.key === 'Escape') {
                close();
            }
        });
        input.addEventListener('blur', close);
        // mousedown instead of click: fires before the input's blur closes the menu
        menu.addEventListener('mousedown', function (e) {
            var item = e.target.closest('.webgis-autocomplete-multiselect-menu-item');
            if (item) {
                e.preventDefault();
                select(item);
            }
        });
        window.addEventListener('resize', close);
        // fixed menu: close when the page or a dialog scrolls (but not when the menu itself scrolls)
        window.addEventListener('scroll', function (e) {
            if (isOpen() && e.target !== menu) {
                close();
            }
        }, true);

        return {
            // Enter: select the item under the cursor; returns false if there is none
            selectCursor: function () {
                var all = items();
                if (isOpen() && cursor >= 0 && all[cursor]) {
                    select(all[cursor]);
                    return true;
                }
                close();
                return false;
            }
        };
    };

})(jQuery);