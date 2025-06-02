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

    let initUI = function ($parent, options) {
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

                addUserPreferencesPage($modalContent, options);

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

    let addPropertyPage = function($parent, options) {
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
    let addRestartViewerMessage = function ($parent) {
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
    let showRestartViewerMessage = function ($parent) {
        $parent.children(".webgis-restart-viewer-message").css('display', 'block');
    };

    let addAppearencePage = function ($parent) {
        const page = addPropertyPage($parent, { id: 'appearence', title: webgis.l10n.get('viewer-appearence') });

        page.$tab.css("backgroundImage", "url(" + webgis.css.imgResource("colorschema-26.png", "tools") + ")");

        if (webgis.usability.allowLanguageSelection === true &&
            webgis.l10n.supportedLanguages) {
            const $content = $("<div>").appendTo(page.$content);
            $("<div>")
                .addClass("webgis-property-content-title2")
                .text(webgis.l10n.get('language'))
                .appendTo($content);
            let $select = $("<select>")
                .addClass('webgis-input')
                .appendTo($content)
                .change(function () {
                    const $this = $(this);

                    webgis.localStorage.set('map.properties.language', $this.val());
                    showRestartViewerMessage($this.parent());
                });
            for (const language in webgis.l10n.supportedLanguages) {
                $("<option>")
                    .attr('value', language)
                    .text(webgis.l10n.supportedLanguages[language])
                    .appendTo($select);
            }
            $select.val(webgis.l10n.language);

            addRestartViewerMessage($content);
        }

        if (webgis.usability.allowDarkmode === true) {
            $("<br><br>").appendTo(page.$content);

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

            const $content = $("<div>").appendTo(page.$content);
            $("<div>")
                .addClass('webgis-property-content-title2')
                .text(webgis.l10n.get('viewer-layout-template'))
                .appendTo($content);

            let $select = $("<select>")
                .addClass('webgis-input')
                .data('template-width', globals.viewerLayoutTemplates.width)
                .appendTo($content)
                .change(function () {
                    const $this=$(this), templateId = $this.val();

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

            addRestartViewerMessage($content);
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

    let addUserPreferencesPage = function ($parent, options) {
        const page = addPropertyPage($parent, { id: 'preferences', title: webgis.l10n.get('user-preferences') });
        const $content = $("<div>").appendTo(page.$content);

        page.$tab.css("backgroundImage", "url(" + webgis.css.imgResource("admin-26.png", "ui") + ")");

        for (var key in webgis.usability.userPreferences.all) {
            const pref = webgis.usability.userPreferences.all[key];

            $("<div>")
                .addClass("webgis-property-content-title2")
                .text(webgis.l10n.get(key))
                .appendTo($content);

            if (pref.available && !pref.available(options.map)) {
                $("<div>")
                    .text(webgis.l10n.get("user-perferences-no-available"))
                    .css({ "fontSize": "0.9em", "margin": "7px 2px", "color": "#a00" })
                    .appendTo($content);
                $("<br><br>").appendTo($content);

                continue;
            }

            if (pref.type === 'yes_no') {
                createDefaultYesNoCombo()
                    .data('key', key)
                    .appendTo($content)
                    .change(function () {
                        const $this = $(this);
                        webgis.usability.userPreferences.set($this.data('key'), $this.val());
                    })
                    .val(webgis.usability.userPreferences.get(key));
            }

            $("<div>")
                .text(webgis.l10n.get(key + '-info'))
                .css({ "fontSize": "0.9em", "margin": "7px 2px", "color": "#777" })
                .appendTo($content);

            $("<br><br>").appendTo($content);
        }
    };

    let addFavoritesPage = function ($parent) {
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
    let addAdminPage = function ($parent) {
        const page = addPropertyPage($parent, { id: 'admin', title: 'Admin' });

        page.$tab.css("backgroundImage", "url(" + webgis.css.imgResource("admin-26.png", "ui") + ")");

        const $adminTools = $('#webgis-info-pane .tab-admin').clone(true);
        $adminTools.appendTo(page.$content).css('display', '');
    };
    let addCreditsAndInfo = function ($parent) {
        const page = addPropertyPage($parent, { id: 'credits', title: 'Credits & Info' });

        page.$tab.css("backgroundImage", "url(" + webgis.css.imgResource("copyright-26.png", "tools") + ")");

        const $adminTools = $('#webgis-info-pane .tab-credits').clone(true);
        $adminTools.appendTo(page.$content).css('display', '');
    }


    let showTabContent = function ($parent, options) {
        const $tabs = $parent.children('.webgis-properties-tabs'),
              $contents = $parent.children('.webgis-properties-contents');

        $tabs.children().removeClass('selected');
        $tabs.children(".webgis-property-tab[data-id='" + options.id + "']").addClass('selected');

        $contents.children().css('display', 'none');
        $contents.children(".webgis-property-content[data-id='" + options.id + "']").css('display', 'block');
    };

    let applyColorScheme = function (options) {
        console.log('applyColorScheme');
        const colorScheme = options.colorScheme || webgis.localStorage.get('map.properties.colorScheme') || 'default';

        const map = options.map;
        const $container = $(map._webgisContainer);

        $container.removeClass('light').removeClass('dark');
        $('body').removeClass('webgis-light').removeClass('webgis-dark');

        let basemapOpacity = 1.0;

        if (colorScheme === '_bg-dark') {
            $container.addClass('dark');
            $('body').addClass('webgis-dark');
            basemapOpacity = 0.5;
        } else if (colorScheme === '_bg-light') {
            $container.addClass('light');
            $('body').addClass('_webgis-light');
        }

        webgis.css.changeColorScheme(colorScheme);

        const services = map.services;
        for (const s in services) {
            if (services[s].isBasemap === true && services[s].basemapType === "normal") {
                services[s].setOpacity(Math.min(basemapOpacity, services[s].opacity));
            }
        }

        const basemap = map.getBasemap(map.currentBasemapServiceId());
        if (basemap) {
            $(".webgis-presentation_toc-basemap-opacity .webgis-menu-item-imagebutton." + parseInt(basemap.opacity * 100)).trigger('click');
        }
    };

    let applyCssClass = function (options) {
        let cssClass = options.cssClass || webgis.localStorage.get('map.properties.cssClass') || 'default';

        var map = options.map;
        var $container = $(map._webgisContainer);

        $('body').removeClass('webgis-space-saving');
        $container.removeClass('webgis-space-saving');

        if (cssClass == '_space-saving') {
            $('body').addClass('webgis-space-saving');
            $container.addClass('webgis-space-saving');
        }
    };

    let createDefaultYesNoCombo = function () {
        const $select = $("<select>")
            .addClass('webgis-input');

        $("<option>").attr('value', 'default').text(webgis.l10n.get('default')).appendTo($select);
        $("<option>").attr('value', 'yes').text(webgis.l10n.get('yes')).appendTo($select);
        $("<option>").attr('value', 'no').text(webgis.l10n.get('no')).appendTo($select);

        return $select;
    };
})(webgis.$ || jQuery);

webgis.usability.userPreferences = webgis.usability.userPreferences ||
{
    get: function (key, defaultValue) {
        console.log('get user preference', key, webgis.localStorage.get('user.preferences.' + key, defaultValue));
        return webgis.localStorage.get('user.preferences.' + key, defaultValue);
    },
    set: function (key, value) {
        webgis.localStorage.set('user.preferences.' + key, value);
    },
    has: function (key) {
        const val = webgis.localStorage.get('user.preferences.' + key);
        console.log('has user preference', key, val);
        return val !== undefined && val !== null && val !== '' && val !== 'default';
    },
    all: {
        "show-markers-on-new-queries": {
            type: "yes_no",
            available: function (map) {
                return map && map.ui.getQueryResultTabControl() !== null
            },
        },
        "select-new-query-results": {
            type: "yes_no",
            available: function (map) {
                return map && map.ui.getQueryResultTabControl() !== null
            },
        }, 
    }
};




