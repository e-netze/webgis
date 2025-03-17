(function ($) {
    "use strict";
    $.fn.webgis_queryBuilder = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$webgis_queryBuilder');
        }
    };

    var defaults = {
        map: null,
        id: 'querybuilder',
        field_defs: [],
        show_geometry_option: false,
        event: null
    };

    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        }
    };

    var initUI = function (parent, options) {
        let $parent = $(parent)
            .addClass('webgis-query-builder')
            .data('options', options);

        if (options.show_geometry_option === true) {
            let $geometryOption = $("<select>")
                .addClass('webgis-input webgis-query-builder-geometry-option')
                .css('margin', '4px 4px 16px 4px')
                .appendTo($parent);

            $("<option>")
                .attr('value', 'use')
                .text(webgis.i18n.get('querybuilder-use-geometry'))
                .appendTo($geometryOption);

            $("<option>")
                .attr('value', 'ignore')
                .text(webgis.i18n.get('querybuilder-ignore-geometry'))
                .appendTo($geometryOption);
        }

        let $table = $("<table>")
            .addClass('webgis-query-builder-table')
            .appendTo($parent);

        let $queryParameter = $("<input>")
            .attr('type', 'hidden')
            .attr('id', options.id + '-result')
            .addClass('webgis-tool-parameter')
            .appendTo($parent);

        let $buttonBar = $("<div>")
            .css({ textAlign: 'right', padding: '10px 0px 10px 0px' })
            .appendTo($parent);

        $("<button>")
            .addClass('webgis-button')
            .text(webgis.i18n.get('apply'))
            .appendTo($buttonBar)
            .click(function () {
                var map = options.map;

                //$queryParameter.val(JSON.stringify(queryDefintions($parent)));

                console.log('options.event', options.event);
                var customEventParameters = [];
                customEventParameters[options.id + '-result'] = JSON.stringify(queryDefintions($parent, options));
                var wrappedEvent = new webgis.tools.wrapperEvent(options.event, customEventParameters);

                webgis.tools.onButtonClick(map, { id: map.getActiveTool().id, command: 'querybuilder', type: 'servertoolcommand_ext', map: map, event: wrappedEvent });
            });

        addTableRow($parent);
    }

    var addTableRow = function ($parent) {
        let options = $parent.data('options');
        if (!options.field_defs) {
            return;
        }

        let $table = $parent.find('.webgis-query-builder-table');
        let $tr = $("<tr>")
            .attr('row-id', nextRowId($parent))
            .addClass('webgis-query-builder-table-row')
            .appendTo($table);

        let $tdFields = $("<td>").addClass('fields').appendTo($tr),
            $tdOperator = $("<td>").addClass('operators').appendTo($tr),
            $tdValue = $("<td>").addClass('value').appendTo($tr),
            $tdLogicalOperator = $("<td>").addClass('logical-operators').appendTo($tr);

        let $selectFields = $("<select>")
            .addClass('webgis-input webgis-query-builder-fields')
            .appendTo($tdFields);
        let $selectOperators = $("<select>")
            .addClass('webgis-input webgis-query-builder-operators depends-on-fields')
            .appendTo($tdOperator);
        let $inputValue = $("<input>")
            .addClass('webgis-input webgis-query-builder-value depends-on-fields')
            .attr('type', 'text')
            .appendTo($tdValue);
        let $hiddenValueTemplate = $("<input>")
            .addClass('webgis-input webgis-query-builder-value-template')
            .attr('type', 'hidden')
            .appendTo($tdValue);
        let $selectLogicalOperator = $("<select>")
            .addClass('webgis-input  webgis-query-builder-logical-operators depends-on-fields')
            .appendTo($tdLogicalOperator);

        $("<option>").attr('value', '').text('-- ' + webgis.i18n.get('select-field') + ' --').appendTo($selectFields);

        $("<option>").attr('value', '').appendTo($selectLogicalOperator);
        $("<option>").attr('value', 'and').text("AND").appendTo($selectLogicalOperator);
        $("<option>").attr('value', 'or').text("OR").appendTo($selectLogicalOperator);

        for (var f in options.field_defs) {
            var fieldDef = options.field_defs[f];

            $("<option>")
                .attr('value', fieldDef.name)
                .text(fieldDef.name)
                .appendTo($selectFields);
        }

        $selectFields.change(function (e) {
            var $this = $(this),
                $row = $this.closest('.webgis-query-builder-table-row');

            var fieldDef = $this.val() ? $.grep(options.field_defs, function (e) {
                return e.name === $this.val()
            })[0] : null;

            var $selectOperators = $row.find('.webgis-input.webgis-query-builder-operators').empty();

            if (fieldDef && fieldDef.operators) {
                $row.find('.webgis-input.depends-on-fields').css('display', 'block');
                for (var o in fieldDef.operators) {
                    var operator = fieldDef.operators[o];

                    $("<option>").attr('value', operator).text(operator).appendTo($selectOperators);
                }
            } else {
                $row.find('.webgis-input.depends-on-fields').css('display', '');
            }
        });

        $selectLogicalOperator.change(function (e) {
            var $this = $(this), logialOperator = $this.val(),
                $row = $this.closest('.webgis-query-builder-table-row'),
                rowId = $row.attr('row-id'),
                $table = $row.closest('.webgis-query-builder-table');

            var nextRows = $.grep($table.children('tr'), function (tr) { return $(tr).attr('row-id') > rowId });

            if (!logialOperator) {
                $.each(nextRows, function (i, e) {
                    $(e).remove()
                });
            } else if (nextRows.length === 0) {
                addTableRow($parent);
            }
        });
    };

    var nextRowId = function ($parent) {
        let rowId = 0,
            $table = $parent.find('.webgis-query-builder-table');

        $table.children('tr').each(function (i, tr) {
            rowId = Math.max($(tr).attr('row-id'));
        });

        return rowId + 1;
    };

    var queryDefintions = function ($parent, options) {
        let $table = $parent.find('.webgis-query-builder-table');

        let queryDefs = [];

        $table.children('tr').each(function (i, tr) {
            let $tr = $(tr);

            let field = $tr.find('.webgis-input.webgis-query-builder-fields').val(),
                operator = $tr.find('.webgis-input.webgis-query-builder-operators').val(),
                value = $tr.find('.webgis-input.webgis-query-builder-value').val(),
                logicalOperator = $tr.find('.webgis-input.webgis-query-builder-logical-operators').val();

            if (!field) {
                if (queryDefs.length == 0) {
                    return;
                } else {
                    alert('Not a valid query');
                    return false;
                }
            }

            var fieldDef = $.grep(options.field_defs, function (e) {
                return e.name === field
            })[0];

            queryDefs.push({
                field: field,
                operator: operator,
                value: value,
                value_template: fieldDef ? fieldDef.value_template : null,
                logical_operator: logicalOperator
            });
        });

        var $geometryOption = $parent.find('.webgis-query-builder-geometry-option');

        return {
            geometry_option: $geometryOption.length > 0 ? $geometryOption.val() : null,
            query_defs: queryDefs
        };
    }
})(webgis.$ || jQuery);