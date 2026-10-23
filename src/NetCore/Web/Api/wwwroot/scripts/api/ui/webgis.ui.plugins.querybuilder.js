// WebGIS UI Plugins (query builder)
// webgis_queryBuilder
// Element builders: webgis.ui.builder.querybuilder.js

webgis.ui.definePlugin("webgis_queryBuilder", {
    defaults: {
        map: null,
        id: 'querybuilder',
        field_defs: [],
        show_geometry_option: false,
        callback_tool_id: null,
        callback_argument: null,
        callback_visfilter_dependent: false,
        remove_button_label: null,
        whereclause_parts: null,
        values_source: null,
        event: null,
        suppress_tool_dialog: false
    },
    init: function () {
        const $ = this.$;
        const options = this.options;
        const $parent = this.$el
            .addClass('webgis-query-builder');
        const me = this;

        this._distinctValues = {};

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
            .addClass('webgis-query-builder-buttonbar')
            .css({ textAlign: 'right', padding: '10px 0px 10px 0px' })
            .appendTo($parent);

        $("<div>")
            .addClass('webgis-query-builder-busy')
            .css('display', 'none')
            .append($("<img>").attr('src', webgis.css.imgResource('loader1.gif', 'hourglass')))
            .append($("<span>").css('margin-left', '6px').text(webgis.l10n.get('querybuilder-in-progress')))
            .appendTo($buttonBar);

        if (options.remove_button_label) {
            // submits an empty definition => the callback tool removes all conditions (eg. a result restriction)
            $("<button>")
                .addClass('webgis-button uibutton-cancel webgis-query-builder-remove-button')
                .css('margin-right', '8px')
                .text(options.remove_button_label)
                .appendTo($buttonBar)
                .on("click.webgis_querybuilder", function () {
                    me.submit({ geometry_option: null, query_defs: [] });
                });
        }

        $("<button>")
            .addClass('webgis-button')
            .text(webgis.l10n.get('apply'))
            .appendTo($buttonBar)
            .on("click.webgis_querybuilder", function () {
                const queryDefinitions = me.queryDefinitions();
                if (queryDefinitions) {
                    me.submit(queryDefinitions);
                }
            });

        this.addTableRow();
    },
    destroy: function () {
        const me = this;
        this.$el.find('.webgis-query-builder-value-container').each(function (i, container) {
            me.releaseValueInput(me.$(container));
        });
        this.$el.off('.webgis_querybuilder');
    },
    methods: {
        submit: function (queryDefinitions) {
            const options = this.options;
            const map = options.map;
            const me = this;

            if (this._busy === true) {  // slow services: don't send the same request multiple times
                return;
            }
            this.setBusy(true);

            webgis.log('options.event', options.event);
            const customEventParameters = [];
            customEventParameters[options.id + '-result'] = JSON.stringify(queryDefinitions);
            const wrappedEvent = new webgis.tools.wrapperEvent(options.event, customEventParameters);

            webgis.tools.onButtonClick(map, {
                id: options.callback_tool_id || map.getActiveTool().id,
                command: 'querybuilder',
                type: 'servertoolcommand_ext',
                map: map,
                suppressToolDialog: options.suppress_tool_dialog === true,
                argument: options.callback_argument,
                // callback tool is not necessarily a map tool => send the current display filters explicitly
                visfilter_dependent: options.callback_visfilter_dependent === true ? true : undefined,
                event: wrappedEvent,
                onRequestComplete: function () { me.setBusy(false); }
            });
        },
        setBusy: function (busy) {
            this._busy = busy === true;

            const $buttonBar = this.$el.children('.webgis-query-builder-buttonbar');
            $buttonBar.children('button').css('display', this._busy ? 'none' : '');
            $buttonBar.children('.webgis-query-builder-busy').css('display', this._busy ? '' : 'none');
            this.$el.toggleClass('webgis-query-builder-busy-state', this._busy);
        },
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
                $tdLogicalOperator = $("<td>").addClass('logical-operators').appendTo($tr),
                $tdRemove = $("<td>").addClass('remove-row').appendTo($tr);

            $("<button>")
                .attr({ type: 'button', title: webgis.l10n.get('querybuilder-remove-row') })
                .addClass('webgis-query-builder-remove-row-button')
                .text('×')
                .appendTo($tdRemove)
                .on("click.webgis_querybuilder", function () {
                    me.removeTableRow($(this).closest('.webgis-query-builder-table-row'));
                });

            const $selectFields = $("<select>")
                .addClass('webgis-input webgis-query-builder-fields')
                .appendTo($tdFields);
            const $selectOperators = $("<select>")
                .addClass('webgis-input webgis-query-builder-operators depends-on-fields')
                .appendTo($tdOperator);
            const $valueContainer = $("<div>")
                .addClass('webgis-query-builder-value-container')
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

                me.buildValueInput($row);
            });

            $selectOperators.on("change.webgis_querybuilder", function () {
                me.buildValueInput($(this).closest('.webgis-query-builder-table-row'));
            });

            $selectLogicalOperator.on("change.webgis_querybuilder", function (e) {
                const $this = $(this), logialOperator = $this.val(),
                    $row = $this.closest('.webgis-query-builder-table-row'),
                    rowId = parseInt($row.attr('row-id'), 10),
                    $table = $row.closest('.webgis-query-builder-table');

                const nextRows = $.grep($table.children('tr'), function (tr) { return parseInt($(tr).attr('row-id'), 10) > rowId });

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
                    this.buildValueInput($tr, options.whereclause_parts.shift());
                }
                if (options.whereclause_parts.length > 0) {
                    $selectLogicalOperator.val(options.whereclause_parts.shift()).trigger('change');
                }
            }
        },
        removeTableRow: function ($row) {
            const logicalSelector = '.webgis-input.webgis-query-builder-logical-operators';

            if ($row.siblings('tr').length === 0) {  // last remaining row => reset it (an empty definition removes all conditions)
                $row.find('.webgis-input.webgis-query-builder-fields').val('').trigger('change');
                $row.find(logicalSelector).val('');
                return;
            }

            // the previous row takes over the logical operator of the removed row (last row => empty)
            const logicalOperator = $row.find(logicalSelector).val(),
                $prev = $row.prev('tr');

            this.releaseValueInput($row.find('.webgis-query-builder-value-container'));
            $row.remove();

            if ($prev.length > 0) {
                $prev.find(logicalSelector).val(logicalOperator);
            }
        },
        buildValueInput: function ($row, value) {
            const $ = this.$;
            const options = this.options;
            const me = this;

            const field = $row.find('.webgis-input.webgis-query-builder-fields').val(),
                operator = $row.find('.webgis-input.webgis-query-builder-operators').val(),
                fieldDef = field ? $.grep(options.field_defs, function (f) { return f.name === field; })[0] : null,
                $container = $row.find('.webgis-query-builder-value-container');

            if (value === undefined && $row.data('qb-value-field') === field) {
                value = $container.find('.webgis-query-builder-value').val();  // operator changed => keep value
            }
            $row.data('qb-value-field', field);

            this.releaseValueInput($container);

            const $input = $("<input>")
                .addClass('webgis-input webgis-query-builder-value depends-on-fields')
                .attr({ type: 'text', name: 'querybuilder_value', autocomplete: 'off' })
                .val(value || '')
                .appendTo($container);

            if (!fieldDef) {
                return;
            }
            if (operator === ' is null' || operator === ' is not null') {
                $input.val('');  // operator without value => input stays hidden
                return;
            }
            $input.css('display', 'block');

            const fieldType = fieldDef.field_type,
                valuesUrl = function (mode) {
                    return options.values_source +
                        '&field=' + encodeURIComponent(fieldDef.name) +
                        '&mode=' + mode;
                };

            if (operator === ' in ' || operator === ' not in ') {
                $input.attr('placeholder', webgis.l10n.get('querybuilder-enter-values-comma'));
            } else if (fieldType === 'number') {
                $input.attr({ type: 'number', step: 'any', placeholder: webgis.l10n.get('querybuilder-enter-number') });

                if (options.values_source) {
                    this.distinctValues(fieldDef.name, valuesUrl('distinct'), function (values) {
                        if (!values || values.length === 0 || !$.contains(document, $input[0])) {
                            return;
                        }

                        const listId = options.id + '-values-' + $row.attr('row-id');
                        const $datalist = $("<datalist>").attr('id', listId).appendTo($container);
                        for (const v of values) {
                            $("<option>").attr('value', v).appendTo($datalist);
                        }
                        $input.attr('list', listId);
                    });
                }
            } else if (fieldType === 'date') {
                $input.attr('placeholder', webgis.l10n.get('querybuilder-enter-date'));

                webgis.require('flatpickr', function ($input) {
                    if (!$.contains(document, $input[0])) {
                        return;
                    }
                    $input.flatpickr({
                        weekNumbers: true,
                        locale: 'de',
                        dateFormat: 'Y-m-d',  // value sent to the server: DATE 'yyyy-mm-dd'
                        altInput: true,
                        altFormat: 'd.m.Y',
                        altInputClass: 'webgis-input webgis-query-builder-value-display',
                        allowInput: false
                    });
                    $container.find('.webgis-query-builder-value-display')
                        .attr('placeholder', webgis.l10n.get('querybuilder-enter-date'))
                        .css('display', 'block');
                }, $input);
            } else if (fieldType === 'string') {
                $input.attr('placeholder', webgis.l10n.get('querybuilder-enter-text'));

                if (options.values_source) {
                    $input
                        .addClass('webgis-autocomplete')
                        .webgis_autocomplete({
                            source: valuesUrl('autocomplete'),
                            minLength: 1
                        });
                }
            }
        },
        releaseValueInput: function ($container) {
            const $ = this.$;

            $container.find('.webgis-query-builder-value').each(function (i, input) {
                if (input._flatpickr) {
                    input._flatpickr.destroy();
                }
            });
            $container.empty();   // also destroys the autocomplete plugin of the value input
        },
        distinctValues: function (fieldName, url, callback) {
            const cache = this._distinctValues;
            let entry = cache[fieldName];

            if (entry) {
                if (entry.values) {
                    callback(entry.values);
                } else {
                    entry.callbacks.push(callback);
                }
                return;
            }

            entry = cache[fieldName] = { values: null, callbacks: [callback] };
            const resolve = function (values) {
                entry.values = values;
                const callbacks = entry.callbacks;
                entry.callbacks = [];
                for (const cb of callbacks) {
                    cb(values);
                }
            };

            webgis.ajax({
                url: url + webgis.hmac.urlParameters(),
                type: 'get',
                success: function (result) {
                    // truncated => too many values for a list => only number input
                    resolve(result && result.truncated !== true && Array.isArray(result.values)
                        ? result.values
                        : []);
                },
                error: function () {
                    resolve([]);
                }
            });
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
            const fieldDefName = function (field) {
                const fieldDef = $.grep(options.field_defs, function (f) { return f.name === field; })[0];
                return fieldDef ? fieldDef.alias || fieldDef.name : field;
            };

            const queryDefs = [];
            let valid = true;

            $table.children('tr').each(function (i, tr) {
                const $tr = $(tr);

                let field = $tr.find('.webgis-input.webgis-query-builder-fields').val(),
                    operator = $tr.find('.webgis-input.webgis-query-builder-operators').val(),
                    $value = $tr.find('.webgis-input.webgis-query-builder-value'),
                    value = $value.val(),
                    logicalOperator = $tr.find('.webgis-input.webgis-query-builder-logical-operators').val();

                if (!field) {
                    if (queryDefs.length == 0) {
                        return;
                    } else {
                        webgis.alert(webgis.l10n.get('querybuilder-invalid-query'), 'info');
                        valid = false;
                        return false;
                    }
                }

                const noValue = operator === ' is null' || operator === ' is not null';
                if (!noValue && ((value || '').trim() === '' || ($value.length > 0 && $value[0].validity && $value[0].validity.badInput))) {
                    // eg. "1%" in a number input => browser returns an empty value
                    webgis.alert(webgis.l10n.get('querybuilder-invalid-value') + ': ' + (fieldDefName(field)), 'info');
                    valid = false;
                    return false;
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

            if (!valid) {
                return null;
            }

            return {
                geometry_option: $geometryOption.length > 0 ? $geometryOption.val() : null,
                query_defs: queryDefs
            };
        }
    }
});
