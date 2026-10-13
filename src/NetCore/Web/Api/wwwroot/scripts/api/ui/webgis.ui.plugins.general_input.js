// WebGIS UI Plugin webgis_general_input
// Labeled input fields (with optional help description) in a container, used by the add services TOC (custom services).
// The first input is created with the plugin options, further inputs are added with the 'add' method:
//   $div.webgis_general_input({ name: 'a', label: 'A' });
//   $div.webgis_general_input('add', { name: 'b', label: 'B' });
//   $div.webgis_general_input('val', { name: 'b' });

webgis.ui.definePlugin('webgis_general_input', {
    defaults: {
        // input options, see staticMethods.inputDefaults
    },
    init: function () {
        this.add(this.options);
    },
    destroy: function () {
        this.$el.children('.webgis-label').off('.webgis_general_input');
    },
    methods: {
        add: function (inputOptions) {
            const $ = this.$;
            const options = $.extend({}, this.constructor.inputDefaults(), inputOptions);
            const $parent = this.$el;
            let $input;

            if ($parent.children('.webgis-input').length > 0) {
                $("<br/>").appendTo($parent);
            }

            if (options.label) {
                var $label = $("<div>")
                    .addClass("webgis-label")
                    .text(options.label + ':')
                    .appendTo($parent);

                if (options.description) {
                    $("<span>")
                        .css('display', 'inline-block')
                        .addClass('webgis-api-icon webgis-api-icon-help')
                        .appendTo($label);

                    var description = options.description, isMarkdown = false;
                    if (description.indexOf('md:') === 0) {
                        description = description.substr(3);
                        isMarkdown = true;
                    }

                    $("<div>")
                        .addClass('webgis-label-description')
                        .html(webgis.encodeUntrustedHtml(description, isMarkdown))
                        .appendTo($parent);

                    $label.on('click.webgis_general_input', function () {
                        $(this).next('.webgis-label-description').toggleClass('show');
                    });
                }
                $("<br/>").appendTo($parent);
            }

            if (options.inputElement) {
                $input = $("<" + options.inputElement + " name='" + options.name + "' type='" + options.type + "'>")
                    .addClass('webgis-input')
                    .appendTo($parent);

                if (options.placeholder) {
                    $input.attr('placeholder', options.placeholder);
                }
            }

            if (options.onInit) {
                options.onInit($parent, $input);
            }
        },
        val: function (options) {
            return this.$el.children("input[name='" + options.name + "']").val();
        },
        clear: function (options) {
            this.$el.children("input[name='" + options.name + "']").val('');
        }
    },
    staticMethods: {
        inputDefaults: function () {
            return {
                type: 'text',
                name: null,
                inputElement: 'input',
                label: null,
                placeholder: null,
                onInit: null
            };
        }
    }
});
