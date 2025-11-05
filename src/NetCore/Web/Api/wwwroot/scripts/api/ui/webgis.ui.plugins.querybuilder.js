webgis.ui.definePlugin("webgis_queryBuilder", {
    defaults: {
        map: null,
        id: 'querybuilder',
        field_defs: [],
        show_geometry_option: false,
        callback_tool_id: null,
        callback_argument: null,
        whereclause_parts: null,
        event: null
    },
    init: function () {
        const $ = this.$;
        const options = this.options;
        const $parent = this.$el
            .addClass('webgis-query-builder');
        const me = this;

        if (options.show_geometry_option === true) {
            let $geometryOption = $("<select>")
                .addClass('webgis-input webgis-query-builder-geometry-option')
                .css('margin', '4px 4px 16px 4px')
                .appendTo($parent);

            $("<option>")
                .attr('value', 'use')
                .text(webgis.l10n.get('querybuilder-use-geometry'))
                .appendTo($geometryOption);

            $("<option>")
                .attr('value', 'ignore')
                .text(webgis.l10n.get('querybuilder-ignore-geometry'))
                .appendTo($geometryOption);
        }

        $("<table>")
            .addClass('webgis-query-builder-table')
            .appendTo($parent);

        $("<input>")
            .attr('type', 'hidden')
            .attr('id', options.id + '-result')
            .addClass('webgis-tool-parameter')
            .appendTo($parent);

        const $buttonBar = $("<div>")
            .css({ textAlign: 'right', padding: '10px 0px 10px 0px' })
            .appendTo($parent);

        $("<button>")
            .addClass('webgis-button')
            .text(webgis.l10n.get('apply'))
            .appendTo($buttonBar)
            .on("click.webgis_querybuilder", function () {
                const map = options.map;

                console.log('options.event', options.event);
                const customEventParameters = [];
                customEventParameters[options.id + '-result'] = JSON.stringify(me.queryDefinitions());
                const wrappedEvent = new webgis.tools.wrapperEvent(options.event, customEventParameters);

                webgis.tools.onButtonClick(map, {
                    id: options.callback_tool_id || map.getActiveTool().id,
                    command: 'querybuilder',
                    type: 'servertoolcommand_ext',
                    map: map,
                    argument: options.callback_argument,
                    event: wrappedEvent
                });
            });

        this.addTableRow();
    },
    destroy: function () {
        //console.log('Destroy time filter list'); 
        this.$el.off('.webgis_querybuilder');
    },
    methods: {
        addTableRow: function () {
            const $ = this.$;
            const options = this.options;

            if (!options.field_defs) {
                return;
            }

            const $parent = this.$el;
            const me = this;

            const $table = $parent.find('.webgis-query-builder-table');
            const $tr = $("<tr>")
                .attr('row-id', this.nextRowId())
                .addClass('webgis-query-builder-table-row')
                .appendTo($table);

            const $tdFields = $("<td>").addClass('fields').appendTo($tr),
                $tdOperator = $("<td>").addClass('operators').appendTo($tr),
                $tdValue = $("<td>").addClass('value').appendTo($tr),
                $tdLogicalOperator = $("<td>").addClass('logical-operators').appendTo($tr);

            const $selectFields = $("<select>")
                .addClass('webgis-input webgis-query-builder-fields')
                .appendTo($tdFields);
            const $selectOperators = $("<select>")
                .addClass('webgis-input webgis-query-builder-operators depends-on-fields')
                .appendTo($tdOperator);
            const $inputValue = $("<input>")
                .addClass('webgis-input webgis-query-builder-value depends-on-fields')
                .attr('type', 'text')
                .appendTo($tdValue);
            const $hiddenValueTemplate = $("<input>")
                .addClass('webgis-input webgis-query-builder-value-template')
                .attr('type', 'hidden')
                .appendTo($tdValue);
            const $selectLogicalOperator = $("<select>")
                .addClass('webgis-input  webgis-query-builder-logical-operators depends-on-fields')
                .appendTo($tdLogicalOperator);

            $("<option>").attr('value', '').text('-- ' + webgis.l10n.get('select-field') + ' --').appendTo($selectFields);

            $("<option>").attr('value', '').appendTo($selectLogicalOperator);
            $("<option>").attr('value', 'and').text("AND").appendTo($selectLogicalOperator);
            $("<option>").attr('value', 'or').text("OR").appendTo($selectLogicalOperator);

            for (let f in options.field_defs) {
                const fieldDef = options.field_defs[f];

                $("<option>")
                    .attr('value', fieldDef.name)
                    .text(fieldDef.alias || fieldDef.name)
                    .appendTo($selectFields);
            }

            $selectFields.on("change.webgis_querybuilder", function (e) {
                const $this = $(this),
                    $row = $this.closest('.webgis-query-builder-table-row');

                const fieldDef = $this.val() ? $.grep(options.field_defs, function (e) {
                    return e.name === $this.val()
                })[0] : null;

                const $selectOperators = $row.find('.webgis-input.webgis-query-builder-operators').empty();

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

            $selectLogicalOperator.on("change.webgis_querybuilder", function (e) {
                const $this = $(this), logialOperator = $this.val(),
                    $row = $this.closest('.webgis-query-builder-table-row'),
                    rowId = $row.attr('row-id'),
                    $table = $row.closest('.webgis-query-builder-table');

                const nextRows = $.grep($table.children('tr'), function (tr) { return $(tr).attr('row-id') > rowId });

                if (!logialOperator) {
                    $.each(nextRows, function (i, e) {
                        $(e).remove()
                    });
                } else if (nextRows.length === 0) {
                    me.addTableRow();
                }
            });

            //console.log(options.whereclause_parts);
            if (options.whereclause_parts) {
                if (options.whereclause_parts.length > 0) {
                    $selectFields.val(options.whereclause_parts.shift()).trigger('change');
                }
                if (options.whereclause_parts.length > 0) {
                    $selectOperators.val(options.whereclause_parts.shift()).trigger('change');
                }
                if (options.whereclause_parts.length > 0) {
                    $inputValue.val(options.whereclause_parts.shift());
                }
                if (options.whereclause_parts.length > 0) {
                    $selectLogicalOperator.val(options.whereclause_parts.shift()).trigger('change');
                }
            }
        },
        nextRowId: function () {
            const $ = this.$;
            const $parent = this.$el;

            let rowId = 0,
                $table = $parent.find('.webgis-query-builder-table');

            $table.children('tr').each(function (i, tr) {
                rowId = Math.max($(tr).attr('row-id'));
            });

            return rowId + 1;
        },
        queryDefinitions: function () {
            const $ = this.$;
            const options = this.options;
            const $parent = this.$el;

            const $table = $parent.find('.webgis-query-builder-table');

            const queryDefs = [];

            $table.children('tr').each(function (i, tr) {
                const $tr = $(tr);

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

            const $geometryOption = $parent.find('.webgis-query-builder-geometry-option');

            return {
                geometry_option: $geometryOption.length > 0 ? $geometryOption.val() : null,
                query_defs: queryDefs
            };
        }
    }
});

webgis.ui.builder['query-builder'] = (map, $newElement, element, options) => {
    $newElement.webgis_queryBuilder({
        map: map,
        id: element.id,
        field_defs: element.field_defs,
        show_geometry_option: element.show_geometry_option,
        callback_tool_id: element.callback_tool_id,
        callback_argument: element.callback_argument,
        whereclause_parts: element.whereclause_parts,
        event: options.event
    });
};