// Custom(js) Features, eg. CustomTools
webgis.custom = new function () {
    this.tools = new function () {
        var _tools = [];
        this.add = function (tool) {
            var customTool = {
                type: tool.tooltype ? 'customtool' : 'custombutton',
                tooltype: tool.tooltype || '',
                id: tool.id ? 'webgis.tools.custom.' + tool.id : 'customtool_' + webgis.guid(),
                name: tool.name || webgis.l10n.get("tool"),
                container: tool.container || webgis.l10n.get("tools"),
                tooltip: tool.tooltip || tool.name || webgis.l10n.get("tool"),
                image: tool.image || 'cursor-plus-26-b.png',
                cursor: tool.cursor || 'pointer',
                command: tool.command || '',
                command_target: tool.command_target,
                modify_event: tool.modify_event || null,  
                description: tool.description || '',
                help_urlpath: tool.help_urlpath || '',
                uiElements: tool.uiElements || []
            };

            for (var i in customTool.uiElements) {
                var uiElement = customTool.uiElements[i];
                if (uiElement.id) {
                    uiElement.css = customTool.id.replaceAll('.', '-');
                }
            }

            _tools.push(customTool);
        };
        this.toArray = function () {
            return _tools;
        };
    };
    this.appMenuItems = new function () {
        var _items = [];
        this.add = function (item) {
            var appMenuItem = {
                id: item.id ? 'webgis.appmenuitem.custom.' + item.id : 'customappmenuitem_' + webgis.guid(),
                name: item.name || webgis.l10n.get("tool"),
                tooltip: item.tooltip || item.name || '',
                image: item.image || '',
                command: item.command || '',
                command_target: item.command_target
            };

            _items.push(appMenuItem);
        };
        this.toArray = function () {
            return _items;
        };
        this.executeCommand = function (item, map) {
            var command = item.command;

            if (item.command_target === 'self') {
                document.location = command;
            }
            else if (item.command_target === 'dialog') {
                webgis.iFrameDialog(command, item.name);
            }
            else if (typeof item.command_target === 'function') {
                item.command_target({ command: command, map: map });
            }
            else {
                window.open(command);
            }
        };
    };
};

webgis.customEvents = {
    beforeCreateMap: null   // function(elementId, options, mapObject)
};