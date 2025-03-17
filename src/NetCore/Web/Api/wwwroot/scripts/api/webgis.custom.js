// Custom(js) Features, eg. CustomTools
webgis.custom = new function () {
    this.tools = new function () {
        var _tools = [];
        this.add = function (tool) {
            var customTool = {
                type: tool.tooltype ? 'customtool' : 'custombutton',
                tooltype: tool.tooltype || '',
                id: tool.id ? 'webgis.tools.custom.' + tool.id : 'customtool_' + webgis.guid(),
                name: tool.name || 'Werkzeug',
                container: tool.container || 'Werkzeuge',
                tooltip: tool.tooltip || tool.name || 'Werkzeug',
                image: tool.image || 'cursor-plus-26-b.png',
                cursor: tool.cursor || 'pointer',
                command: tool.command || '',
                command_target: tool.command_target,
                description: tool.description || '',
                help_urlpath: tool.help_urlpath || '',
                uiElements: tool.uiElements || []
            };

            for (var i in customTool.uiElements) {
                var uiElement = customTool.uiElements[i];
                if (uiElement.id) {
                    uiElement.css = customTool.id;
                }
            }

            _tools.push(customTool);
        };
        this.toArray = function () {
            return _tools;
        };
    };
};

webgis.customEvents = {
    beforeCreateMap: null   // function(elementId, options, mapObject)
};