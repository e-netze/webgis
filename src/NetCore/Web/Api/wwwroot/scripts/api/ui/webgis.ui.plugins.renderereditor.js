// WebGIS UI Plugins (renderer editor)
// webgis_rendererEditor: user-defined layer renderer ("Darstellung", AGS DynamicLayers)
// Element builders: webgis.ui.builder.renderereditor.js
// Server: E.Standard.WebGIS.Tools.Presentation.UserRenderer, UIRendererEditor
// The renderer is stored at the service (service.setLayerRenderer) and serialized with the map.

webgis.ui.definePlugin('webgis_rendererEditor', {
    defaults: {
        map: null,
        service_id: null,
        layer_id: null,
        geometry_type: 'polygon',   // point|line|polygon
        field_defs: [],             // [{ name, alias, numeric }]
        max_classes: 100,
        callback_tool_id: null
    },
    init: function () {
        const $ = this.$, o = this.options, me = this;
        const service = o.map ? o.map.getService(o.service_id) : null;

        this._service = service;
        this._renderer = (service && service.getLayerRenderer(o.layer_id))
            || { version: 1, type: 'simple', symbol: this.newSymbol(0) };
        this._mode = service && service.hasLayerRenderer(o.layer_id) ? 'custom' : 'default';

        this.$el.addClass('webgis-renderer-editor');

        // default / custom
        const $modes = $("<div>").addClass('webgis-renderer-editor-modes').appendTo(this.$el);
        for (const mode of ['default', 'custom']) {
            const $label = $("<label>").addClass('webgis-renderer-editor-check').css({ display: 'block', margin: '4px 0px' }).appendTo($modes);
            $("<input type='radio' name='webgis-renderer-editor-mode'>")
                .val(mode)
                .prop('checked', this._mode === mode)
                .appendTo($label)
                .on('change.webgis_renderer_editor', function () {
                    me._mode = $(this).val();
                    me.render();
                });
            $("<span>").text(' ' + webgis.l10n.get('renderer-editor-mode-' + mode)).appendTo($label);
        }

        this._$content = $("<div>").addClass('webgis-renderer-editor-content').appendTo(this.$el);

        const $buttonBar = $("<div>")
            .css({ textAlign: 'right', padding: '10px 0px 10px 0px' })
            .appendTo(this.$el);
        $("<button>")
            .addClass('webgis-button uibutton-cancel')
            .text(webgis.l10n.get('cancel'))
            .appendTo($buttonBar)
            .on('click.webgis_renderer_editor', function () { webgis.ui.closeModal(); });
        $("<button>")
            .addClass('webgis-button')
            .css('margin-left', '8px')
            .text(webgis.l10n.get('apply'))
            .appendTo($buttonBar)
            .on('click.webgis_renderer_editor', function () { me.apply(); });

        this.render();
    },
    destroy: function () {
        this.$el.find('*').addBack().off('.webgis_renderer_editor');
    },
    methods: {
        apply: function () {
            if (!this._service) {
                return;
            }

            if (this._mode === 'default') {
                this._service.setLayerRenderer(this.options.layer_id, null);
                webgis.ui.closeModal();
                return;
            }

            const error = this.validate();
            if (error) {
                webgis.alert(error, 'info');
                return;
            }

            this._service.setLayerRenderer(this.options.layer_id, this.getRenderer());
            webgis.ui.closeModal();
        },
        getRenderer: function () {
            const r = this._renderer;
            if (r.type === 'uniqueValue') {
                const renderer = { version: 1, type: 'uniqueValue', field: r.field, items: r.items || [] };
                if (r.defaultSymbol) {
                    renderer.defaultSymbol = r.defaultSymbol;
                    if (r.defaultLabel) renderer.defaultLabel = r.defaultLabel;
                }
                return renderer;
            }
            if (r.type === 'classBreaks') {
                const renderer = { version: 1, type: 'classBreaks', field: r.field, classBreaks: r.classBreaks || [] };
                if (typeof r.minValue === 'number' && !isNaN(r.minValue)) {
                    renderer.minValue = r.minValue;
                }
                if (r.defaultSymbol) {
                    renderer.defaultSymbol = r.defaultSymbol;
                    if (r.defaultLabel) renderer.defaultLabel = r.defaultLabel;
                }
                return renderer;
            }
            return { version: 1, type: 'simple', symbol: r.symbol || this.newSymbol(0) };
        },
        validate: function () {
            const r = this._renderer;
            if (r.type === 'classBreaks') {
                return this.validateClassBreaks();
            }
            if (r.type !== 'uniqueValue') {
                return null;
            }
            if (!r.field) {
                return webgis.l10n.get('renderer-editor-error-field');
            }
            const items = r.items || [];
            if (items.length === 0 && !r.defaultSymbol) {
                return webgis.l10n.get('renderer-editor-error-no-classes');
            }
            if (items.length > this.options.max_classes) {
                return webgis.l10n.get('renderer-editor-error-max-classes', { max: this.options.max_classes });
            }
            const values = {};
            for (const item of items) {
                if (values[item.value] === true) {
                    return webgis.l10n.get('renderer-editor-error-duplicate-value', { value: item.value });
                }
                values[item.value] = true;
            }
            return null;
        },
        validateClassBreaks: function () {
            const r = this._renderer;
            if (!r.field) {
                return webgis.l10n.get('renderer-editor-error-field');
            }
            const classBreaks = r.classBreaks || [];
            if (classBreaks.length === 0) {
                return webgis.l10n.get('renderer-editor-error-no-class-breaks');
            }
            if (classBreaks.length > this.options.max_classes) {
                return webgis.l10n.get('renderer-editor-error-max-classes', { max: this.options.max_classes });
            }
            let previous = typeof r.minValue === 'number' ? r.minValue : null;
            for (const classBreak of classBreaks) {
                if (typeof classBreak.maxValue !== 'number' || isNaN(classBreak.maxValue)) {
                    return webgis.l10n.get('renderer-editor-error-class-value');
                }
                if (previous !== null && classBreak.maxValue < previous) {
                    return webgis.l10n.get('renderer-editor-error-class-order');
                }
                previous = classBreak.maxValue;
            }
            return null;
        },
        numericFieldDefs: function () {
            return (this.options.field_defs || []).filter(function (f) { return f.numeric === true; });
        },
        render: function () {
            const $ = this.$, o = this.options, me = this, r = this._renderer;
            const $content = this._$content;

            webgis.ui.destroyPluginsDeep($content);
            $content.find('*').off('.webgis_renderer_editor');
            $content.empty();

            if (this._mode === 'default') {
                $("<div>").addClass('webgis-info').text(webgis.l10n.get('renderer-editor-default-info')).appendTo($content);
                return;
            }

            // renderer type (unique values/classes only, if there are allowed (numeric) fields)
            const numericFieldDefs = this.numericFieldDefs();
            const types = ['simple'];
            if (o.field_defs && o.field_defs.length > 0) types.push('uniqueValue');
            if (numericFieldDefs.length > 0) types.push('classBreaks');
            if (types.indexOf(r.type) < 0) {
                r.type = 'simple';
            }

            if (types.length > 1) {
                const $type = $("<select>").addClass('webgis-input').appendTo(this.labeled($content, webgis.l10n.get('renderer-editor-type')));
                for (const type of types) {
                    $("<option>").val(type).text(webgis.l10n.get('renderer-editor-type-' + type.toLowerCase())).appendTo($type);
                }
                $type.val(r.type).on('change.webgis_renderer_editor', function () {
                    r.type = $(this).val();
                    if (r.type === 'uniqueValue') {
                        r.field = r.field || o.field_defs[0].name;
                        r.items = r.items || [];
                    } else if (r.type === 'classBreaks') {
                        if (!numericFieldDefs.some(function (f) { return f.name === r.field; })) {
                            r.field = numericFieldDefs[0].name;
                            r.classBreaks = [];
                            delete r.minValue;
                        }
                        r.classBreaks = r.classBreaks || [];
                    } else {
                        r.symbol = r.symbol || me.newSymbol(0);
                    }
                    me.render();
                });
            }

            if (r.type === 'uniqueValue') {
                this.renderUniqueValue($content);
            } else if (r.type === 'classBreaks') {
                this.renderClassBreaks($content);
            } else {
                r.symbol = r.symbol || this.newSymbol(0);
                this.renderSymbolEditor($("<div>").addClass('webgis-renderer-editor-symbol').appendTo($content), r.symbol);
            }
        },
        renderUniqueValue: function ($content) {
            const $ = this.$, o = this.options, me = this, r = this._renderer;

            const $field = $("<select>").addClass('webgis-input').appendTo(this.labeled($content, webgis.l10n.get('renderer-editor-field')));
            for (const field of o.field_defs) {
                $("<option>").val(field.name).text(field.alias || field.name).appendTo($field);
            }
            $field.val(r.field).on('change.webgis_renderer_editor', function () {
                r.field = $(this).val();
                r.items = [];
                me.render();
            });

            const $tools = $("<div>").css({ margin: '8px 0px' }).appendTo($content);
            $("<button>")
                .addClass('webgis-button uibutton-cancel')
                .text(webgis.l10n.get('renderer-editor-load-values'))
                .appendTo($tools)
                .on('click.webgis_renderer_editor', function () { me.loadValues(); });
            $("<button>")
                .addClass('webgis-button uibutton-cancel')
                .css('margin-left', '8px')
                .text(webgis.l10n.get('renderer-editor-add-value'))
                .appendTo($tools)
                .on('click.webgis_renderer_editor', function () {
                    r.items.push({ value: '', label: '', symbol: me.newSymbol(r.items.length) });
                    me.render();
                });

            const $table = $("<table>").addClass('webgis-renderer-editor-items').css({ width: '100%', borderCollapse: 'collapse' }).appendTo($content);
            $("<tr>")
                .append($("<th>"))
                .append($("<th>").css('text-align', 'left').text(webgis.l10n.get('renderer-editor-value')))
                .append($("<th>").css('text-align', 'left').text(webgis.l10n.get('renderer-editor-label')))
                .append($("<th>"))
                .appendTo($table);

            r.items.forEach(function (item, index) {
                me.renderItemRow($table, item, function () {
                    r.items.splice(index, 1);
                    me.render();
                });
            });

            this.renderOtherValues($content);
        },
        renderClassBreaks: function ($content) {
            const $ = this.$, o = this.options, me = this, r = this._renderer;
            r.classBreaks = r.classBreaks || [];

            const $field = $("<select>").addClass('webgis-input').appendTo(this.labeled($content, webgis.l10n.get('renderer-editor-field')));
            for (const field of this.numericFieldDefs()) {
                $("<option>").val(field.name).text(field.alias || field.name).appendTo($field);
            }
            $field.val(r.field).on('change.webgis_renderer_editor', function () {
                r.field = $(this).val();
                r.classBreaks = [];
                delete r.minValue;
                me.render();
            });

            // classification: number of classes + color ramp => equal intervals between min/max of the field
            const $classify = $("<div>").addClass('webgis-renderer-editor-symbol-grid').appendTo($content);
            const $count = $("<input type='number'>")
                .addClass('webgis-input')
                .attr({ min: 1, max: o.max_classes, step: 1 })
                .css('width', '80px')
                .val(this._classCount || 5)
                .appendTo(this.labeled($classify, webgis.l10n.get('renderer-editor-class-count')))
                .on('change.webgis_renderer_editor', function () {
                    let v = parseInt($(this).val());
                    if (isNaN(v)) v = 5;
                    v = Math.min(o.max_classes, Math.max(1, v));
                    $(this).val(v);
                    me._classCount = v;
                });
            const $ramp = $("<select>").addClass('webgis-input').appendTo(this.labeled($classify, webgis.l10n.get('renderer-editor-color-ramp')));
            for (const ramp in this.colorRamps()) {
                $("<option>").val(ramp).text(webgis.l10n.get('renderer-editor-color-ramp-' + ramp)).appendTo($ramp);
            }
            $ramp.val(this._colorRamp || 'yellow-red').on('change.webgis_renderer_editor', function () {
                me._colorRamp = $(this).val();
            });
            $("<button>")
                .addClass('webgis-button uibutton-cancel')
                .css({ alignSelf: 'flex-end', margin: '6px 0px' })
                .text(webgis.l10n.get('renderer-editor-calc-classes'))
                .appendTo($classify)
                .on('click.webgis_renderer_editor', function () {
                    me._classCount = parseInt($count.val()) || 5;
                    me._colorRamp = $ramp.val();
                    me.calcClasses();
                });

            const $min = $("<input type='number'>")
                .addClass('webgis-input')
                .css('width', '120px')
                .val(typeof r.minValue === 'number' ? r.minValue : '')
                .appendTo(this.labeled($content, webgis.l10n.get('renderer-editor-min-value')))
                .on('change.webgis_renderer_editor', function () {
                    const v = parseFloat($(this).val());
                    if (isNaN(v)) {
                        delete r.minValue;
                        $(this).val('');
                    } else {
                        r.minValue = v;
                    }
                });

            const $tools = $("<div>").css({ margin: '8px 0px' }).appendTo($content);
            $("<button>")
                .addClass('webgis-button uibutton-cancel')
                .text(webgis.l10n.get('renderer-editor-add-class'))
                .appendTo($tools)
                .on('click.webgis_renderer_editor', function () {
                    const last = r.classBreaks.length > 0
                        ? r.classBreaks[r.classBreaks.length - 1].maxValue
                        : (typeof r.minValue === 'number' ? r.minValue : 0);
                    r.classBreaks.push({ maxValue: last, label: '', symbol: me.newSymbol(r.classBreaks.length) });
                    me.render();
                });

            const $table = $("<table>").addClass('webgis-renderer-editor-items').css({ width: '100%', borderCollapse: 'collapse' }).appendTo($content);
            $("<tr>")
                .append($("<th>"))
                .append($("<th>").css('text-align', 'left').text(webgis.l10n.get('renderer-editor-max-value')))
                .append($("<th>").css('text-align', 'left').text(webgis.l10n.get('renderer-editor-label')))
                .append($("<th>"))
                .appendTo($table);

            r.classBreaks.forEach(function (classBreak, index) {
                me.renderItemRow($table, classBreak, function () {
                    r.classBreaks.splice(index, 1);
                    me.render();
                }, null, 'maxValue');
            });

            this.renderOtherValues($content);
        },
        calcClasses: function () {
            const o = this.options, me = this, r = this._renderer;
            if (!r.field) {
                return;
            }

            const tool = {
                id: o.callback_tool_id || webgis.tools.presenation.userRenderer,
                type: 'servertoolcommand_ext',
                command: 'field_range',
                map: o.map,
                visfilter_dependent: true,  // only values of the currently filtered features
                suppressToolDialog: true,
                argument: JSON.stringify({ serviceId: o.service_id, layerId: o.layer_id, field: r.field })
            };
            tool.handleToolResponseCustomHandler = function (response) {
                if (!response || typeof response.min !== 'number' || typeof response.max !== 'number') {
                    webgis.alert(response && response.exception
                        ? response.exception
                        : webgis.l10n.get('renderer-editor-no-field-range'), response && response.exception ? 'error' : 'info');
                    return;
                }

                const count = Math.max(1, Math.min(o.max_classes, me._classCount || 5));
                const min = response.min, max = response.max;
                const step = (max - min) / count;
                // round the breaks to a precision that fits the class width
                const decimals = step > 0 ? Math.min(6, Math.max(0, 2 - Math.floor(Math.log10(step)))) : 0;
                const round = function (v) { return Number(v.toFixed(decimals)); };
                const colors = me.rampColors(me._colorRamp || 'yellow-red', count);

                r.minValue = min;
                r.classBreaks = [];
                let lower = min;
                for (let i = 0; i < count; i++) {
                    const upper = i === count - 1 ? max : round(min + step * (i + 1));
                    const symbol = me.newSymbol(i);
                    symbol.color = colors[i];
                    r.classBreaks.push({ maxValue: upper, label: me.formatNumber(lower) + ' - ' + me.formatNumber(upper), symbol: symbol });
                    lower = upper;
                }
                me.render();
            };

            webgis.tools.onButtonClick(o.map, tool, null, null, null);
        },
        colorRamps: function () {
            return {
                'yellow-red': ['#ffffb2', '#fd8d3c', '#bd0026'],
                'green-red': ['#1a9641', '#ffffbf', '#d7191c'],
                'white-blue': ['#f7fbff', '#6baed6', '#08306b'],
                'white-green': ['#f7fcf5', '#74c476', '#00441b'],
                'blue-red': ['#2c7bb6', '#ffffbf', '#d7191c'],
                'grey': ['#f0f0f0', '#969696', '#252525']
            };
        },
        rampColors: function (ramp, count) {
            const ramps = this.colorRamps();
            const stops = ramps[ramp] || ramps['yellow-red'];
            const toRgb = function (hex) {
                const v = parseInt(hex.substring(1), 16);
                return [(v >> 16) & 255, (v >> 8) & 255, v & 255];
            };
            const toHex = function (rgb) {
                return '#' + rgb.map(function (c) { return ('0' + Math.round(c).toString(16)).slice(-2); }).join('');
            };
            const colors = [];
            for (let i = 0; i < count; i++) {
                const t = count === 1 ? 0.5 : i / (count - 1);
                const pos = t * (stops.length - 1);
                const index = Math.min(stops.length - 2, Math.floor(pos));
                const f = pos - index;
                const a = toRgb(stops[index]), b = toRgb(stops[index + 1]);
                colors.push(toHex([a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f, a[2] + (b[2] - a[2]) * f]));
            }
            return colors;
        },
        formatNumber: function (v) {
            return String(Number(Number(v).toFixed(6)));
        },
        // symbol for all other values (unique value) or values outside the classes (class breaks)
        renderOtherValues: function ($content) {
            const $ = this.$, me = this, r = this._renderer;
            const $other = $("<label>").addClass('webgis-renderer-editor-check').css({ display: 'block', margin: '12px 0px 4px 0px' }).appendTo($content);
            $("<input type='checkbox'>")
                .prop('checked', !!r.defaultSymbol)
                .appendTo($other)
                .on('change.webgis_renderer_editor', function () {
                    if ($(this).prop('checked')) {
                        r.defaultSymbol = me.newSymbol(-1);
                    } else {
                        delete r.defaultSymbol;
                        delete r.defaultLabel;
                    }
                    me.render();
                });
            $("<span>").text(' ' + webgis.l10n.get('renderer-editor-other-values')).appendTo($other);

            if (r.defaultSymbol) {
                const $otherTable = $("<table>").css({ width: '100%', borderCollapse: 'collapse' }).appendTo($content);
                const otherItem = { value: null, label: r.defaultLabel || '', symbol: r.defaultSymbol };
                this.renderItemRow($otherTable, otherItem, null, function (label) { r.defaultLabel = label; });
            }
        },
        // valueKey: 'value' (unique value, text) or 'maxValue' (class breaks, number)
        renderItemRow: function ($table, item, onRemove, onLabelChanged, valueKey) {
            const $ = this.$, me = this;
            valueKey = valueKey || 'value';
            const $row = $("<tr>").appendTo($table);
            const $editorRow = $("<tr>").css('display', 'none').appendTo($table);
            const $editorCell = $("<td colspan='4'>").appendTo($editorRow);

            const $swatch = this.swatch(item.symbol)
                .attr('title', webgis.l10n.get('renderer-editor-edit-symbol'))
                .on('click.webgis_renderer_editor', function () {
                    if ($editorRow.css('display') === 'none') {
                        $editorCell.empty();
                        me.renderSymbolEditor($editorCell, item.symbol, function () {
                            me.swatch(item.symbol, $swatch);
                        });
                        $editorRow.css('display', '');
                    } else {
                        $editorRow.css('display', 'none');
                    }
                });
            $("<td>").css('width', '36px').append($swatch).appendTo($row);

            const $valueCell = $("<td>").appendTo($row);
            if (item[valueKey] === null) {
                $valueCell.text(webgis.l10n.get('renderer-editor-other-values'));
            } else if (valueKey === 'maxValue') {
                $("<input type='number'>")
                    .addClass('webgis-input')
                    .val(item.maxValue)
                    .appendTo($valueCell)
                    .on('change.webgis_renderer_editor', function () {
                        const v = parseFloat($(this).val());
                        if (isNaN(v)) {
                            $(this).val(item.maxValue);
                        } else {
                            item.maxValue = v;
                        }
                    });
            } else {
                $("<input type='text'>")
                    .addClass('webgis-input')
                    .val(item.value)
                    .appendTo($valueCell)
                    .on('change.webgis_renderer_editor', function () {
                        if (item.label === item.value) item.label = $(this).val();
                        item.value = $(this).val();
                    });
            }

            $("<input type='text'>")
                .addClass('webgis-input')
                .val(item.label || '')
                .attr('placeholder', valueKey === 'value' ? (item.value || '') : '')
                .appendTo($("<td>").appendTo($row))
                .on('change.webgis_renderer_editor', function () {
                    item.label = $(this).val();
                    if (onLabelChanged) onLabelChanged(item.label);
                });

            const $removeCell = $("<td>").css('width', '30px').appendTo($row);
            if (onRemove) {
                $("<div>")
                    .addClass('webgis-renderer-editor-remove')
                    .text('✕')
                    .attr('title', webgis.l10n.get('remove'))
                    .css({ cursor: 'pointer', textAlign: 'center' })
                    .appendTo($removeCell)
                    .on('click.webgis_renderer_editor', onRemove);
            }
        },
        // compact symbol editor (native inputs), changes the symbol object directly
        renderSymbolEditor: function ($target, symbol, onChange) {
            const $ = this.$, me = this, geometryType = this.options.geometry_type;
            let $grid;
            // each group (fill/symbol, outline) is rendered in its own row
            const newRow = function () {
                $grid = $("<div>").addClass('webgis-renderer-editor-symbol-grid').appendTo($target);
            };
            const changed = function () { if (onChange) onChange(); };

            const addColor = function (key, label) {
                $("<input type='color'>")
                    .val(symbol[key] || '#000000')
                    .appendTo(me.labeled($grid, label))
                    .on('input.webgis_renderer_editor change.webgis_renderer_editor', function () {
                        symbol[key] = $(this).val();
                        changed();
                    });
            };
            const addNumber = function (key, label, min, max, step, defaultValue) {
                $("<input type='number'>")
                    .addClass('webgis-input')
                    .attr({ min: min, max: max, step: step })
                    .css('width', '80px')
                    .val(symbol[key] !== undefined ? symbol[key] : defaultValue)
                    .appendTo(me.labeled($grid, label))
                    .on('change.webgis_renderer_editor', function () {
                        let v = parseFloat($(this).val());
                        if (isNaN(v)) v = defaultValue;
                        v = Math.min(max, Math.max(min, v));
                        $(this).val(v);
                        symbol[key] = v;
                        changed();
                    });
            };
            const addSelect = function (key, label, values, l10nPrefix) {
                const $select = $("<select>").addClass('webgis-input').appendTo(me.labeled($grid, label));
                for (const value of values) {
                    $("<option>").val(value).text(webgis.l10n.get(l10nPrefix + value)).appendTo($select);
                }
                $select.val(symbol[key] || values[0]).on('change.webgis_renderer_editor', function () {
                    symbol[key] = $(this).val();
                    changed();
                });
            };
            const addOpacity = function () {
                $("<input type='number'>")
                    .addClass('webgis-input')
                    .attr({ min: 0, max: 100, step: 5 })
                    .css('width', '80px')
                    .val(Math.round((symbol.opacity !== undefined ? symbol.opacity : 1) * 100))
                    .appendTo(me.labeled($grid, webgis.l10n.get('opacity') + ' (%)'))
                    .on('change.webgis_renderer_editor', function () {
                        let v = parseFloat($(this).val());
                        if (isNaN(v)) v = 100;
                        v = Math.min(100, Math.max(0, v));
                        $(this).val(v);
                        symbol.opacity = v / 100;
                        changed();
                    });
            };

            switch (geometryType) {
                case 'point':
                    newRow();
                    addSelect('marker', webgis.l10n.get('renderer-editor-marker'), ['circle', 'square', 'diamond', 'triangle', 'cross', 'x'], 'renderer-editor-marker-');
                    addNumber('size', webgis.l10n.get('renderer-editor-size') + ' (pt)', 1, 64, 1, 8);
                    addColor('color', webgis.l10n.get('renderer-editor-color'));
                    addOpacity();
                    newRow();
                    addColor('outlineColor', webgis.l10n.get('renderer-editor-outline-color'));
                    addNumber('outlineWidth', webgis.l10n.get('renderer-editor-outline-width') + ' (pt)', 0, 20, 0.5, 1);
                    break;
                case 'line':
                    newRow();
                    addColor('color', webgis.l10n.get('renderer-editor-color'));
                    addNumber('width', webgis.l10n.get('renderer-editor-width') + ' (pt)', 0, 20, 0.5, 2);
                    addSelect('lineStyle', webgis.l10n.get('renderer-editor-line-style'), ['solid', 'dash', 'dot', 'dashdot'], 'renderer-editor-line-style-');
                    break;
                default:
                    newRow();
                    addColor('color', webgis.l10n.get('renderer-editor-fill-color'));
                    addOpacity();
                    addSelect('fillStyle', webgis.l10n.get('renderer-editor-fill-style'), ['solid', 'none', 'horizontal', 'vertical', 'cross', 'forwarddiagonal', 'backwarddiagonal', 'diagonalcross'], 'renderer-editor-fill-style-');
                    newRow();
                    addColor('outlineColor', webgis.l10n.get('renderer-editor-outline-color'));
                    addNumber('outlineWidth', webgis.l10n.get('renderer-editor-outline-width') + ' (pt)', 0, 20, 0.5, 1);
                    addSelect('outlineStyle', webgis.l10n.get('renderer-editor-line-style'), ['solid', 'dash', 'dot', 'dashdot'], 'renderer-editor-line-style-');
                    break;
            }
        },
        loadValues: function () {
            const o = this.options, me = this, r = this._renderer;
            if (!r.field) {
                return;
            }

            const tool = {
                id: o.callback_tool_id || webgis.tools.presenation.userRenderer,
                type: 'servertoolcommand_ext',
                command: 'distinct_values',
                map: o.map,
                visfilter_dependent: true,  // only values of the currently filtered features
                suppressToolDialog: true,
                argument: JSON.stringify({ serviceId: o.service_id, layerId: o.layer_id, field: r.field })
            };
            tool.handleToolResponseCustomHandler = function (response) {
                if (!response || !response.values) {
                    if (response && response.exception) {
                        webgis.alert(response.exception, 'error');
                    }
                    return;
                }

                const existing = {};
                for (const item of r.items) existing[item.value] = true;
                for (const value of response.values) {
                    if (existing[value] || r.items.length >= o.max_classes) continue;
                    r.items.push({ value: value, label: value, symbol: me.newSymbol(r.items.length) });
                }
                me.render();

                if (response.truncated === true) {
                    webgis.alert(webgis.l10n.get('renderer-editor-values-truncated', { max: o.max_classes }), 'info');
                }
            };

            webgis.tools.onButtonClick(o.map, tool, null, null, null);
        },
        newSymbol: function (index) {
            const palette = ['#e41a1c', '#377eb8', '#4daf4a', '#984ea3', '#ff7f00', '#ffff33', '#a65628', '#f781bf',
                '#66c2a5', '#fc8d62', '#8da0cb', '#e78ac3', '#a6d854', '#ffd92f', '#e5c494', '#b3b3b3'];
            const color = index < 0 ? '#999999' : palette[index % palette.length];

            switch (this.options.geometry_type) {
                case 'point':
                    return { color: color, opacity: 1, marker: 'circle', size: 8, outlineColor: '#000000', outlineWidth: 0.5 };
                case 'line':
                    return { color: color, width: 2, lineStyle: 'solid' };
                default:
                    return { color: color, opacity: 0.6, fillStyle: 'solid', outlineColor: '#000000', outlineWidth: 0.5, outlineStyle: 'solid' };
            }
        },
        // creates a swatch or restyles an existing one ($swatch)
        swatch: function (symbol, $swatch) {
            const $ = this.$;
            $swatch = ($swatch || $("<div>").addClass('webgis-renderer-editor-swatch'))
                .attr('style', '')
                .css({ width: '26px', height: '18px', boxSizing: 'border-box', cursor: 'pointer' });
            const rgba = function (hex, opacity) {
                const v = parseInt((hex || '#000000').substring(1), 16);
                return 'rgba(' + ((v >> 16) & 255) + ',' + ((v >> 8) & 255) + ',' + (v & 255) + ',' + (opacity !== undefined ? opacity : 1) + ')';
            };
            const borderStyle = function (style) {
                return style === 'dot' ? 'dotted' : (style === 'dash' || style === 'dashdot' ? 'dashed' : 'solid');
            };

            switch (this.options.geometry_type) {
                case 'point':
                    $swatch.css({
                        width: '18px', margin: '0px 4px',
                        background: rgba(symbol.color, symbol.opacity),
                        border: Math.min(3, symbol.outlineWidth || 0) + 'px solid ' + (symbol.outlineColor || 'transparent'),
                        borderRadius: symbol.marker === 'circle' ? '50%' : '0px',
                        transform: symbol.marker === 'diamond' ? 'rotate(45deg) scale(0.8)' : ''
                    });
                    break;
                case 'line':
                    $swatch.css({
                        height: '10px',
                        borderBottom: Math.max(1, Math.min(6, symbol.width || 1)) + 'px ' + borderStyle(symbol.lineStyle) + ' ' + symbol.color
                    });
                    break;
                default:
                    $swatch.css({
                        background: symbol.fillStyle === 'none' ? 'transparent' : rgba(symbol.color, symbol.opacity),
                        border: Math.min(3, symbol.outlineWidth || 0) + 'px ' + borderStyle(symbol.outlineStyle) + ' ' + (symbol.outlineColor || 'transparent')
                    });
                    break;
            }
            return $swatch;
        },
        labeled: function ($parent, label) {
            const $ = this.$;
            const $div = $("<div>").addClass('webgis-renderer-editor-row').css({ margin: '6px 0px' }).appendTo($parent);
            $("<div>").addClass('webgis-renderer-editor-row-label').text(label).appendTo($div);
            return $div;
        }
    }
});
