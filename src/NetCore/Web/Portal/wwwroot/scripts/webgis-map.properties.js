(function ($) {
    "use strict";
    $.fn.webgis_mapProperties = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on jQuery.webgis_mapProperties');
        }
    };

    var defaults = {
    };

    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI($this,options);
            });
        },
        applySettings: function (options) {
            applyColorScheme(options);
            applyCssClass(options);
        }
    };

    var initUI = function ($parent, options) {
        $parent.webgis_modal({
            title: webgis.l10n.get('viewer-settings'),
            width: '800px',
            height: '600px',
            onload: function ($modalContent) {
                $modalContent
                    .data('options', options)
                    .addClass('webgis-properties');

                var $tabs = $("<div>")
                    .addClass('webgis-properties-tabs')
                    .appendTo($modalContent);
                var $contents = $("<div>")
                    .addClass('webgis-properties-contents')
                    .appendTo($modalContent);

                if (globals.isMapAuthor) {
                    addAdminPage($modalContent);
                }

                addCreditsAndInfo($modalContent);

                if (webgis.usability.allowDarkmode === true ||
                    webgis.usability.allowViewerLayoutTemplateSelection === true) {
                    addAppearencePage($modalContent);
                }

                if (webgis.hmac) {
                    if (webgis.hmac.favoritesProgramAvailable() || webgis.hmac.favoritesProgramActive()) {
                        addFavoritesPage($modalContent);
                    }
                }
                
                $tabs
                    .children('.webgis-property-tab')
                    .click(function (e) {
                        showTabContent($(this).closest('.webgis-properties'), { id: $(this).attr('data-id') });
                    });

                $tabs.children(':first').trigger('click');
            }
        });
    };

    var addPropertyPage = function($parent, options) {
        var $tab = $("<div>")
            .attr('data-id', options.id)
            .addClass('webgis-property-tab')
            .text(options.title)
            .appendTo($parent.children('.webgis-properties-tabs'));
        var $content = $("<div>")
            .attr('data-id', options.id)
            .addClass('webgis-property-content')
            .appendTo($parent.children('.webgis-properties-contents'));

        $("<div>")
            .addClass("webgis-property-content-title")
            .text(options.title)
            .appendTo($content);

        return { $tab: $tab, $content: $content };
    };
    var addRestartViewerMessage = function ($parent) {
        var $div = $("<div>")
            .addClass('webgis-restart-viewer-message')
            .css('display', 'none')
            .appendTo($parent);

        $("<p>")
            .text(webgis.l10n.get('viewer-restart-message'))
            .appendTo($div);

        $("<button>")
            .addClass('webgis-button')
            .text(webgis.l10n.get('viewer-restart'))
            .appendTo($div)
            .click(function (e) {
                e.stopPropagation();

                window.location.reload();
            });
    };
    var showRestartViewerMessage = function ($parent) {
        $(".webgis-restart-viewer-message").css('display', 'block');
    };

    var addAppearencePage = function ($parent) {
        let page = addPropertyPage($parent, { id: 'appearence', title: webgis.l10n.get('viewer-appearence') });

        page.$tab.css("backgroundImage", "url(" + webgis.css.imgResource("colorschema-26.png", "tools") + ")");

        if (webgis.usability.allowDarkmode === true) {
            $("<div>")
                .addClass("webgis-property-content-title2")
                .text(webgis.l10n.get('viewer-color-scheme'))
                .appendTo(page.$content);

            let $select = $("<select>")
                .addClass('webgis-input')
                .appendTo(page.$content)
                .change(function () {
                    var options = $parent.data('options')
                    options.colorScheme = $(this).val();

                    applyColorScheme(options);

                    webgis.localStorage.set('map.properties.colorScheme', webgis.css.getColorScheme());
                });

            $("<option value='default'>").text('Default').appendTo($select);
            $("<option value='_bg-dark'>").text('Dark').appendTo($select);
            $("<option value='_bg-light'>").text('Light').appendTo($select);

            $select.val(webgis.css.getColorScheme());
        }

        if (globals.viewerLayoutTemplates &&
            globals.viewerLayoutTemplates.width &&
            globals.viewerLayoutTemplates.templates &&
            globals.viewerLayoutTemplates.templates.length > 1) {

            $("<br><br>").appendTo(page.$content);

            $("<div>")
                .addClass('webgis-property-content-title2')
                .text(webgis.l10n.get('viewer-layout-template'))
                .appendTo(page.$content);

            let $select = $("<select>")
                .addClass('webgis-input')
                .data('template-width', globals.viewerLayoutTemplates.width)
                .appendTo(page.$content)
                .change(function () {
                    var $this=$(this), templateId = $this.val();

                    webgis.localStorage.set(globals.viewerLayoutTemplateStorageKey + $this.data('template-width'), templateId);
                    showRestartViewerMessage($this.parent());
                });

            for (var t in globals.viewerLayoutTemplates.templates) {
                var template = globals.viewerLayoutTemplates.templates[t];

                $("<option>")
                    .attr('value', template.id)
                    .text(template.name)
                    .appendTo($select);
            }

            $select.val(webgis.localStorage.get(globals.viewerLayoutTemplateStorageKey + globals.viewerLayoutTemplates.width));

            addRestartViewerMessage(page.$content);
        }

        if (webgis.usability.allowStyleClassSelection === true) {
            $("<br><br>").appendTo(page.$content);

            $("<div>")
                .addClass("webgis-property-content-title2")
                .text(webgis.l10n.get('viewer-css-style-class'))
                .appendTo(page.$content);

            let $select = $("<select>")
                .addClass("webgis-input")
                .appendTo(page.$content)
                .change(function () {
                    var options = $parent.data('options')
                    options.cssClass = $(this).val();

                    applyCssClass(options);

                    webgis.localStorage.set('map.properties.cssClass', options.cssClass);
                });

            $("<option value='default'>").text('Default').appendTo($select);
            $("<option value='_space-saving'>").text('Space-Saving').appendTo($select);

            $select.val(webgis.localStorage.get("map.properties.cssClass") || 'default');
        }
    };
    var addFavoritesPage = function ($parent) {
        var page = addPropertyPage($parent, { id: 'favorites', title: 'Favorites' });

        page.$tab.css("backgroundImage", "url(" + webgis.css.imgResource("favorites-26.png", "tools") + ")");

        if (webgis.hmac.favoritesProgramAvailable()) {
            $("<button>")
                .addClass('webgis-button')
                .text('Favoriten Programm')
                .appendTo(page.$content)
                .click(function () {
                    webgis.mapExtensions.checkFavoriteStatus(function () {
                    }, true);
                });
        }

        if (webgis.hmac.favoritesProgramActive()) {
            $("<button>")
                .addClass('webgis-button')
                .text('Favoriten zurücksetzen')
                .appendTo(page.$content)
                .click(function () {
                    jQuery.get(portalUrl + '/favorites/resetmessage', function (data) {
                        webgis.confirm({
                            title: 'Favoriten zurücksetzen',
                            height: '450px',
                            message: data,
                            iconUrl: webgis.css.imgResource('fav-100.png'),
                            okText: 'Zurücksetzen (löschen)',
                            cancelText: 'Behalten',
                            onOk: function () {
                                jQuery.get(portalUrl + '/favorites/reset?' + webgis.hmac.urlParameters({}), function (data) {
                                });
                            },
                            onCancel: function () {

                            }
                        });
                    });
                });
        }
    };
    var addAdminPage = function ($parent) {
        var page = addPropertyPage($parent, { id: 'admin', title: 'Admin' });

        page.$tab.css("backgroundImage", "url(" + webgis.css.imgResource("admin-26.png", "ui") + ")");

        var $adminTools = $('#webgis-info-pane .tab-admin').clone(true);
        $adminTools.appendTo(page.$content).css('display', '');
    };
    var addCreditsAndInfo = function ($parent) {
        var page = addPropertyPage($parent, { id: 'credits', title: 'Credits & Info' });

        page.$tab.css("backgroundImage", "url(" + webgis.css.imgResource("copyright-26.png", "tools") + ")");

        var $adminTools = $('#webgis-info-pane .tab-credits').clone(true);
        $adminTools.appendTo(page.$content).css('display', '');
    }

    var showTabContent = function ($parent, options) {
        var $tabs = $parent.children('.webgis-properties-tabs'),
            $contents = $parent.children('.webgis-properties-contents');

        $tabs.children().removeClass('selected');
        $tabs.children(".webgis-property-tab[data-id='" + options.id + "']").addClass('selected');

        $contents.children().css('display', 'none');
        $contents.children(".webgis-property-content[data-id='" + options.id + "']").css('display', 'block');
    };

    var applyColorScheme = function (options) {
        console.log('applyColorScheme');
        var colorScheme = options.colorScheme || webgis.localStorage.get('map.properties.colorScheme') || 'default';

        var map = options.map;
        var $container = $(map._webgisContainer);

        $container.removeClass('light').removeClass('dark');
        $('body').removeClass('webgis-light').removeClass('webgis-dark');

        var basemapOpacity = 1.0;

        if (colorScheme === '_bg-dark') {
            $container.addClass('dark');
            $('body').addClass('webgis-dark');
            basemapOpacity = 0.5;
        } else if (colorScheme === '_bg-light') {
            $container.addClass('light');
            $('body').addClass('_webgis-light');
        }

        webgis.css.changeColorScheme(colorScheme);

        var services = map.services;
        for (var s in services) {
            if (services[s].isBasemap === true && services[s].basemapType === "normal") {
                services[s].setOpacity(Math.min(basemapOpacity, services[s].opacity));
            }
        }

        var basemap = map.getBasemap(map.currentBasemapServiceId());
        if (basemap) {
            $(".webgis-presentation_toc-basemap-opacity .webgis-menu-item-imagebutton." + parseInt(basemap.opacity * 100)).trigger('click');
        }
    };

    var applyCssClass = function (options) {
        let colorScheme = options.cssClass || webgis.localStorage.get('map.properties.cssClass') || 'default';

        var map = options.map;
        var $container = $(map._webgisContainer);

        $container.removeClass('webgis-space-saving');

        if (colorScheme == '_space-saving') {
            $container.addClass('webgis-space-saving');
        }
    };
})(webgis.$ || jQuery);




