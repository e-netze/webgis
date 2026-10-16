// WebGIS UI Plugins (label editor)
// webgis_labelEditor: user-defined layer labeling ("Beschriftung", AGS DynamicLayers)
// Element builders: webgis.ui.builder.labeleditor.js
// Server: E.Standard.WebGIS.Tools.Presentation.UserLabeling, UILabelEditor
// The labeling is stored at the service (service.setLayerLabeling) and serialized with the map.

webgis.ui.definePlugin('webgis_labelEditor', {
    defaults: {
        map: null,
        service_id: null,
        layer_id: null,
        field_defs: [],             // [{ name, alias }]
        max_expression_length: 200,
        max_expression_lines: 3
    },
    init: function () {
        const $ = this.$, o = this.options, me = this;
        const service = o.map ? o.map.getService(o.service_id) : null;
        const current = service ? service.getLayerLabeling(o.layer_id) : null;

        this._service = service;
        this._mode = current ? (current.mode === 'none' ? 'none' : 'custom') : 'default';
        this._labeling = $.extend({
            version: 1,
            mode: 'custom',
            expression: '',
            fontSize: 10,
            fontStyle: 'regular',
            fontColor: '#000000',
            haloColor: '#ffffff'
        }, current && current.mode === 'custom' ? current : {});
        this._lastHaloColor = this._labeling.haloColor || '#ffffff';

        this.$el.addClass('webgis-label-editor');

        // default / none / custom
        const $modes = $("<div>").addClass('webgis-label-editor-modes').appendTo(this.$el);
        for (const mode of ['default', 'none', 'custom']) {
            const $label = $("<label>").addClass('webgis-renderer-editor-check').css({ display: 'block', margin: '4px 0px' }).appendTo($modes);
            $("<input type='radio' name='webgis-label-editor-mode'>")
                .val(mode)
                .prop('checked', this._mode === mode)
                .appendTo($label)
                .on('change.webgis_label_editor', function () {
                    me._mode = $(this).val();
                    me.render();
                });
            $("<span>").text(' ' + webgis.l10n.get('label-editor-mode-' + mode)).appendTo($label);
        }

        this._$content = $("<div>").addClass('webgis-label-editor-content').appendTo(this.$el);

        const $buttonBar = $("<div>")
            .css({ textAlign: 'right', padding: '10px 0px 10px 0px' })
            .appendTo(this.$el);
        $("<button>")
            .addClass('webgis-button uibutton-cancel')
            .text(webgis.l10n.get('cancel'))
            .appendTo($buttonBar)
            .on('click.webgis_label_editor', function () { webgis.ui.closeModal(); });
        $("<button>")
            .addClass('webgis-button')
            .css('margin-left', '8px')
            .text(webgis.l10n.get('apply'))
            .appendTo($buttonBar)
            .on('click.webgis_label_editor', function () { me.apply(); });

        this.render();
    },
    destroy: function () {
        this.$el.find('*').addBack().off('.webgis_label_editor');
    },
    methods: {
        apply: function () {
            if (!this._service) {
                return;
            }

            const layerId = this.options.layer_id;

            if (this._mode === 'default') {
                this._service.setLayerLabeling(layerId, null);
                webgis.ui.closeModal();
                return;
            }

            if (this._mode === 'none') {
                this._service.setLayerLabeling(layerId, { version: 1, mode: 'none' });
                webgis.ui.closeModal();
                return;
            }

            const error = this.validate();
            if (error) {
                webgis.alert(error, 'info');
                return;
            }

            this._service.setLayerLabeling(layerId, this.getLabeling());
            webgis.ui.closeModal();
        },
        getLabeling: function () {
            const l = this._labeling;
            return {
                version: 1,
                mode: 'custom',
                expression: (l.expression || '').replace(/\r\n?/g, '\n').trim(),
                fontSize: parseFloat(l.fontSize) || 10,
                fontStyle: l.fontStyle || 'regular',
                fontColor: l.fontColor || '#000000',
                haloColor: l.haloColor || null
            };
        },
        validate: function () {
            const o = this.options;
            const expression = (this._labeling.expression || '').replace(/\r\n?/g, '\n').trim();

            if (!expression) {
                return webgis.l10n.get('label-editor-error-expression');
            }
            if (expression.length > o.max_expression_length) {
                return webgis.l10n.get('label-editor-error-max-length', { max: o.max_expression_length });
            }
            if (expression.split('\n').length > o.max_expression_lines) {
                return webgis.l10n.get('label-editor-error-max-lines', { max: o.max_expression_lines });
            }

            const fieldNames = (o.field_defs || []).map(f => f.name.toLowerCase());
            for (const match of expression.matchAll(/\[([^\[\]\n]+)\]/g)) {
                if (fieldNames.indexOf(match[1].trim().toLowerCase()) < 0) {
                    return webgis.l10n.get('label-editor-error-unknown-field', { field: match[1] });
                }
            }
            return null;
        },
        render: function () {
            const $ = this.$, o = this.options, me = this, l = this._labeling;
            const $content = this._$content.empty();

            if (this._mode === 'default') {
                $("<div>").addClass('webgis-info').text(webgis.l10n.get('label-editor-default-info')).appendTo($content);
                return;
            }
            if (this._mode === 'none') {
                $("<div>").addClass('webgis-info').text(webgis.l10n.get('label-editor-none-info')).appendTo($content);
                return;
            }

            // expression
            const $expressionRow = this.labeled($content, webgis.l10n.get('label-editor-expression'));
            const $textarea = $("<textarea>")
                .addClass('webgis-input webgis-label-editor-expression')
                .attr({ rows: o.max_expression_lines, maxlength: o.max_expression_length, spellcheck: 'false' })
                .val(l.expression || '')
                .appendTo($expressionRow)
                .on('input.webgis_label_editor change.webgis_label_editor', function () {
                    l.expression = $(this).val();
                });

            const $fieldSelect = $("<select>")
                .addClass('webgis-input webgis-label-editor-field')
                .appendTo($expressionRow)
                .on('change.webgis_label_editor', function () {
                    const fieldName = $(this).val();
                    if (fieldName) {
                        me.insertAtCursor($textarea.get(0), '[' + fieldName + ']');
                        l.expression = $textarea.val();
                    }
                    $(this).val('');
                });
            $("<option>").val('').text(webgis.l10n.get('label-editor-insert-field')).appendTo($fieldSelect);
            for (const fieldDef of o.field_defs || []) {
                $("<option>")
                    .val(fieldDef.name)
                    .text(fieldDef.alias && fieldDef.alias !== fieldDef.name ? fieldDef.alias + ' (' + fieldDef.name + ')' : fieldDef.name)
                    .appendTo($fieldSelect);
            }
            $("<div>")
                .addClass('webgis-label-editor-hint')
                .text(webgis.l10n.get('label-editor-expression-hint', { max: o.max_expression_lines }))
                .appendTo($expressionRow);

            // font
            const $grid = $("<div>").addClass('webgis-renderer-editor-symbol-grid').appendTo($content);

            const $fontSize = $("<select>")
                .addClass('webgis-input')
                .appendTo(this.labeled($grid, webgis.l10n.get('label-editor-font-size')))
                .on('change.webgis_label_editor', function () { l.fontSize = parseFloat($(this).val()); });
            const fontSizes = [8, 10, 12, 14, 18, 24];
            if (fontSizes.indexOf(l.fontSize) < 0) {
                fontSizes.push(l.fontSize);
                fontSizes.sort((a, b) => a - b);
            }
            for (const fontSize of fontSizes) {
                $("<option>").val(fontSize).text(fontSize + ' pt').appendTo($fontSize);
            }
            $fontSize.val(l.fontSize);

            const $fontStyle = $("<select>")
                .addClass('webgis-input')
                .appendTo(this.labeled($grid, webgis.l10n.get('label-editor-font-style')))
                .on('change.webgis_label_editor', function () { l.fontStyle = $(this).val(); });
            for (const fontStyle of ['regular', 'bold', 'italic', 'bolditalic']) {
                $("<option>").val(fontStyle).text(webgis.l10n.get('label-editor-font-style-' + fontStyle)).appendTo($fontStyle);
            }
            $fontStyle.val(l.fontStyle || 'regular');

            // colors
            const $colors = $("<div>").addClass('webgis-renderer-editor-symbol-grid').appendTo($content);

            $("<input type='color'>")
                .val(l.fontColor || '#000000')
                .appendTo(this.labeled($colors, webgis.l10n.get('label-editor-font-color')))
                .on('input.webgis_label_editor change.webgis_label_editor', function () { l.fontColor = $(this).val(); });

            const $haloRow = this.labeled($colors, webgis.l10n.get('label-editor-halo'));
            const $haloColor = $("<input type='color'>")
                .val(this._lastHaloColor)
                .prop('disabled', !l.haloColor)
                .on('input.webgis_label_editor change.webgis_label_editor', function () {
                    me._lastHaloColor = l.haloColor = $(this).val();
                });
            const $haloLabel = $("<label>").addClass('webgis-renderer-editor-check').appendTo($haloRow);
            $("<input type='checkbox'>")
                .prop('checked', !!l.haloColor)
                .appendTo($haloLabel)
                .on('change.webgis_label_editor', function () {
                    const checked = $(this).prop('checked');
                    l.haloColor = checked ? me._lastHaloColor : null;
                    $haloColor.prop('disabled', !checked);
                });
            $haloColor.appendTo($haloLabel).css('margin-left', '6px');
        },
        insertAtCursor: function (textarea, text) {
            const start = typeof textarea.selectionStart === 'number' ? textarea.selectionStart : textarea.value.length;
            const end = typeof textarea.selectionEnd === 'number' ? textarea.selectionEnd : start;
            textarea.value = textarea.value.substring(0, start) + text + textarea.value.substring(end);
            textarea.selectionStart = textarea.selectionEnd = start + text.length;
            textarea.focus();
        },
        labeled: function ($parent, label) {
            const $ = this.$;
            const $div = $("<div>").addClass('webgis-renderer-editor-row').css({ margin: '6px 0px' }).appendTo($parent);
            $("<div>").addClass('webgis-renderer-editor-row-label').text(label).appendTo($div);
            return $div;
        }
    }
});
