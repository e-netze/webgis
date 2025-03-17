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
        $input.keydown(function (event) {
            if(event.keyCode === 13) {
                event.preventDefault();
                $(this).closest('.webgis-autocomplete-multiselect').find('.add-button').trigger('click');
                return false;
            }
        });

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

        if ($.fn.typeahead) {
            $input.on({
                'typeahead:select': function (e, item) {
                    $(this).closest('.webgis-autocomplete-multiselect').webgis_autocomplete_multiselect('add', { value: item, alwaysIncludeOwner: options.alwaysIncludeOwner });
                },
                'keyup': function (e) {
                    if (e.keyCode === 13) {
                        $(this).typeahead('close');
                    }
                }
            })
                .typeahead({
                    hint: false,
                    highlight: false,
                    minLength: 3
                },
                    {
                        limit: Number.MAX_VALUE,
                        async: true,
                        source: function (query, processSync, processAsync) {
                            var $element = $(this.$el[0].parentElement.parentElement).children(".webgis-autocomplete-multiselect-input").first(); // Ugly!!!
                            var $prefix = $(this.$el[0].parentElement.parentElement).closest('.webgis-autocomplete-multiselect').find('.webgis-autocomplete-multiselect-prefix');

                            var source = $element.data('webgis-multiselect-source');
                            if ($prefix.length > 0) {
                                source += (source.indexOf('?') > 0 ? '&' : '?') + 'prefix=' + $prefix.val();
                            }

                            return $.ajax({
                                url: source,
                                type: 'get',
                                data: { term: query },
                                success: function (data) {
                                    //console.log(data);
                                    data = data.slice(0, 12);
                                    //console.log(data);
                                    processAsync(data);
                                },
                                error: function () {
                                }
                            });
                        }
                    }).data('webgis-multiselect-source', options.source);
                
        } else if ($.fn.autocomplete) {
            $input.autocomplete({
                search: function (event, ui) {
                    var source = $(this).data('webgis-multiselect-source');
                    var $prefix = $(this).closest('.webgis-autocomplete-multiselect').find('.webgis-autocomplete-multiselect-prefix');
                    if ($prefix.length > 0) {
                        source += (source.indexOf('?') > 0 ? '&' : '?') + 'prefix=' + $prefix.val();
                    }
                    $(this).autocomplete('option', 'source', source);
                },
                minLength: 3,
                select: function (event, ui) {
                    $(this).closest('.webgis-autocomplete-multiselect').webgis_autocomplete_multiselect('add', { value: ui.item.value, alwaysIncludeOwner: options.alwaysIncludeOwner });
                }
            }).data('webgis-multiselect-source', options.source);
        }
    };

})(jQuery);