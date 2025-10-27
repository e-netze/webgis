var webgis = new function () {
    "use strict";
    var $ = window.jExt || window.jQuery;
    this.jQuery = $;
    this.$ = $;
    
    this.net = 'standard';
    this.api_version = '{{currentJavascriptVersion}}';
    this.mapFramework = 'leaflet'; 
    this.mapFramework_version = '1.9.4';
    var src = document.getElementById('webgis-api-script') ? document.getElementById('webgis-api-script').src.toLowerCase() : null;
    src = src ? src.substring(0, src.lastIndexOf('/scripts/api/')) : '';
    this.baseUrl = src;
    this.gdiScheme = '';
    this.sketchSnappingSchemeId = '9bc1f309-7f7b-4051-b89e-28ce7096d6cf';
    // console.log('webgis-api-baseurl: ' + this.baseUrl);
    this.maps = [];

    this._defaultToolPrefix = '__default__';

    this.options = {
        load_markercluster: true,
        load_label: false,
        load_proj: true,
        prefer_fetch_api: true
    };
    this.loadedScripts = {
        mapFramework: false,
        mapFramework_dependencies: false,
        markerCluster: false,
        label: false,
        proj: false
    };
    this._ajaxXhrFields = {
        withCredentials: true
    };
    this._initialError = null;
    this.encodeUntrustedHtml = function (html, isMarkdown) {
        html = webgis.sanitizeHtml(html);

        var result = html.replaceAll('<', '&lt;').replaceAll('>', '&gt;');
        if (isMarkdown)
            result = webgis.simpleMarkdown.render(result);

        return result;
    };
    this.asMarkdownOrText = function (txt) {
        txt = webgis.emptyIfSuspiciousHtml(webgis.sanitizeHtml(txt));

        let isMarkdown = txt.indexOf('md:') === 0;
        if (isMarkdown) {
            txt = txt.substr(3);
        }

        return webgis.encodeUntrustedHtml(txt, isMarkdown);
    };
    this.asMarkdownOrRawHtml = function (txt) {
        txt = webgis.emptyIfSuspiciousHtml(txt);

        let isMarkdown = txt.indexOf('md:') === 0;
        if (isMarkdown) {
            return webgis.simpleMarkdown.render(txt.substr(3));
        }

        return txt;
    };
    this.isSuspiciousHtml = function (html) {
        // script-tag case insensitiv
        var regex = /<\s*script[^>]*>/gi; // /<\s*script[^>]*>(.*?)<\s*\/\s*script>/gi;
        if (regex.test(html)) {
            return true;
        }

        // test
        //console.log(webgis.isSuspiciousHtml('<img src=x onerror=alert(1)>')); // true
        //console.log(webgis.isSuspiciousHtml('<svg onload="evil()">...</svg>')); // true
        //console.log(webgis.isSuspiciousHtml('<a href="javascript:alert(1)">click</a>')); // true
        //console.log(webgis.isSuspiciousHtml('<p>harmless text</p>')); // false
        //console.log(webgis.isSuspiciousHtml('<iframe src="http://evil">')); // true

        if (!html || typeof html !== 'string') return false;

        // 1) Detect <script> tags (case-insensitive)
        var scriptRe = /<\s*script\b[^>]*>([\s\S]*?)<\s*\/\s*script\s*>/i;
        if (scriptRe.test(html)) {
            return true;
        }

        // 2) Detect inline event-handler attributes such as onload=, onerror=, onclick=, etc.
        // Looks for attributes starting with "on" inside any HTML tag.
        // Accepts values in double quotes, single quotes, or unquoted.
        var onAttrRe = /<[^>]*\b(on[a-zA-Z]+\s*)=\s*(?:"[^"]*"|'[^']*'|[^\s>]+)/i;
        if (onAttrRe.test(html)) {
            return true;
        }

        // 3) Detect javascript: URIs in href, src, or similar attributes.
        // Example: <a href="javascript:alert(1)">
        var jsUriRe = /\b(?:href|src|xlink:href|style|data-?src)\s*=\s*(?:"\s*javascript:|'\s*javascript:|javascript:)/i;
        if (jsUriRe.test(html)) {
            return true;
        }

        // 4) Detect any attribute value that contains javascript: or data:text/html
        // This is a more general catch-all for encoded or obfuscated forms.
        var anyJsUriRe = /(=\s*(?:"[^"]*"|'[^']*'|[^\s>]+))/ig;
        var match;
        while ((match = anyJsUriRe.exec(html)) !== null) {
            var val = match[1];
            if (/javascript\s*:/i.test(val) || /data\s*:\s*text\/html/i.test(val)) {
                return true;
            }
        }

        // 5) Detect dangerous tags that can execute code or load remote content.
        // Includes iframe, object, embed, frame, meta, and link.
        var dangerousTagsRe = /<\s*(iframe|object|embed|frame|meta|link)\b[^>]*>/i;
        if (dangerousTagsRe.test(html)) {
            return true;
        }

        // 6) Detect srcdoc attribute (can contain full HTML, often used for injection)
        var srcdocRe = /\b(srcdoc)\s*=\s*(?:"[^"]*"|'[^']*'|[^\s>]+)/i;
        if (srcdocRe.test(html)) {
            return true;
        }

        // No suspicious patterns found
        return false;
    };
    this.emptyIfSuspiciousHtml = function (html) {   
        return webgis.isSuspiciousHtml(html) ? "" : html;
    };
    this.secureHtml = function (html) {
        this.sanitizeHtml(html);
    };
    this.sanitizeHtml = function (html) {
        if (window.DOMPurify) {
            console.log('DOMPurify Sanitizing HTML...', html);
            return DOMPurify.sanitize(html);
        }
        return webgis.emptyIfSuspiciousHtml(html) || 'suppressing dangerous result...';
    };

    this.alert = function (message, title, onClose) {
        title = title || 'WebGIS Meldung';

        var height = Math.max(200, message.countChar('\n') * 28);

        $('body').webgis_modal({
            title: webgis.l10n.get(title),
            onload: function ($content) {
                var imgUrl = '';
                if (title.toLowerCase() === 'info') {
                    imgUrl = webgis.css.imgResource('info-100.png');
                } else if (title.toLowerCase() === 'error') {
                    imgUrl = webgis.css.imgResource('error-100.png');
                }
                if (imgUrl !== '') {
                    $("<img src='" + imgUrl + "' >").css({
                        padding: '5px',
                        float: 'left'
                    }).appendTo($content);
                }
                var p = $('<p>')
                    .css('font-size', '1.1em')
                    .appendTo($content)
                    .html(webgis.encodeUntrustedHtml(message, true).replaceAll('\n', '<br/>').replaceAll('  ', '&nbsp;&nbsp;'));
            },
            width: '640px', height: height + 'px',
            onclose: onClose
        });
    };
    this.toastMessage = function (title, message, onclick, className) {
        var $toastMessage = $("<div>")
            .addClass('webgis-toast-message')
            .appendTo('body')
            .click(function () {
                if (onclick)
                    onclick();
            });
        if (className)
            $toastMessage.addClass(className);

        $("<div>")
            .addClass('message-title')
            .text(title)
            .appendTo($toastMessage);
        $("<div>")
            .addClass('message-text')
            .text(message)
            .appendTo($toastMessage);
        $("<div>x</div>")
            .addClass('close-button')
            .appendTo($toastMessage)
            .click(function (e) {
                e.stopPropagation();
                $(this).parent().remove();
            });

        // Do the CSS transision animation
        webgis.delayed(function ($toastMessage) {
            $toastMessage.addClass('show');

            webgis.delayed(function () {
                $toastMessage.removeClass('show');
                webgis.delayed(function () {
                    $toastMessage.remove();
                }, 300);
            }, 5000);
        }, 10, $toastMessage);
    };
    this.confirm = function (options) {
        var title = options.title || 'WebGIS Meldung';
        var id = webgis.guid();
        $('body').webgis_modal({
            title: title,
            id: id,
            closebutton: false,
            blockerclickclose: false,
            onload: function ($content) {

                var p = $('<div>')
                    .css({
                        position: 'absolute',
                        left: 0, top: 0, right: 0, bottom: 45,
                        padding: 12,
                        overflow: 'auto'
                    })
                    .appendTo($content);

                if (options.iconUrl) {
                    $("<img src='" + options.iconUrl + "' >").css({
                        padding: '5px',
                        float: 'right'
                    }).appendTo(p);
                }

                var message = options.message;
                if (message.indexOf('md:') === 0) {
                    message = message.substr(3);
                    $("<p>").css({ fontSize: '1.1em' }).html(webgis.encodeUntrustedHtml(message, true)).appendTo(p);
                } else {
                    $("<p>").css({ fontSize: '1.1em' }).html(webgis.encodeUntrustedHtml(message).replaceAll('\n', '<br/>')).appendTo(p);
                }

                var buttons = $("<div>").css({ position: 'absolute', bottom: 0, left: 0, right: 0, textAlign: 'right', padding: 15 }).appendTo($content);
                if (options.suppressCancel !== true) {
                    $("<button>" + (options.cancelText || 'Nein') + "</button>")
                        .css({ minWidth: 120, margin: 4 })
                        .addClass('webgis-button uibutton-cancel uibutton')
                        .appendTo(buttons)
                        .click(function () {
                            var func = function () {
                                if (options.onCancel) options.onCancel(this);
                                $(null).webgis_modal('close', { id: id });
                            };
                            if (options.cancelNoneBlocking) {
                                $(this).addClass('loading');
                                webgis.delayed(func, 1);
                            } else {
                                func();
                            }
                        });
                }
                if (options.maybeText || options.onMaybe) {
                    $("<button>" + (options.maybeText || 'Vielleicht') + "</button>")
                        .css({ minWidth: 120, margin: 4 })
                        .addClass('webgis-button uibutton-cancel uibutton')
                        .appendTo(buttons)
                        .click(function () {
                            var func = function () {
                                if (options.onMaybe)
                                    options.onMaybe(this);
                                $(null).webgis_modal('close', { id: id });
                            };
                            if (options.maybeNoneBlocking) {
                                $(this).addClass('loading');
                                webgis.delayed(func, 1);
                            } else {
                                func();
                            }
                        });
                }
                if (options.suppressOk !== true) {
                    $("<button>" + (options.okText || 'Ja') + "</button>")
                        .css({ minWidth: 120, margin: 4 })
                        .addClass('webgis-button uibutton')
                        .appendTo(buttons)
                        .click(function () {
                            var func = function () {
                                if (options.onOk) options.onOk(this);
                                $(null).webgis_modal('close', { id: id });
                            };
                            if (options.okNoneBlocking) {
                                $(this).addClass('loading');
                                webgis.delayed(func, 1);
                            } else {
                                func();
                            }
                        });
                }
            },
            width: options.width || '640px', height: options.height || '350px'
        });
    };
    this.confirmDiscardChanges = function(sender, map, discardFunc) {
        var $form = $(sender).hasClass('webgis-form-container') ?
            $(sender) :
            $(sender).closest('.webgis-form-container');

        if ($form.length === 1) {
            var activeTool = map.getActiveTool();

            if ($form.hasClass('webgis-is-dirty') ||
                (activeTool && activeTool.tooltype.indexOf('sketch') === 0 && map.sketch._isDirty === true)) {

                webgis.confirm({
                    title: activeTool.name,
                    height: '270px',
                    message: webgis.l10n.get('discard-changes'),
                    okText: webgis.l10n.get('yes'),
                    cancelText: webgis.l10n.get('no'),
                    onOk: discardFunc
                });

                return;
            }
        }

        discardFunc();
    };
    this.showTip = function (tipId) {
        if (webgis.usability.showTips === true && webgis.localStorage.usable() && webgis.localStorage.get('confirmation-' + tipId) !== 'understood') {
            var tipText = webgis.l10n.get(tipId);
            if (!tipText)
                reutrn;

            this.confirm({
                title: webgis.l10n.get('tip'),
                message: tipText,
                okText: webgis.l10n.get('understood'),
                cancelText: webgis.l10n.get('later'),
                onOk: function () {
                    webgis.localStorage.set('confirmation-' + tipId, 'understood');
                }
            });
        }
    };
    this.showHelp = function (urlPath, map) {
        var url = (urlPath && urlPath.indexOf('http://') !== 0 && urlPath.indexOf('https://') !== 0)
            ? urlPath ? webgis.help.rootUrl + urlPath : webgis.help.url  // relative url
            : urlPath;  // absolute Url

        if ($.fn.webgis_dockPanel) {
            $('.webgis-container').webgis_dockPanel({
                title: 'Hilfe',
                dock: 'right',
                size: 640,
                maxSize: '40%',
                id: 'webgis-help-dock-panel',
                useIdSelector: true,
                refElement: map && map.ui ? map.ui.mapContainer() : null,
                map: map,
                onload: function ($content, $dockPanel) {
                    $content.css('overflow', 'hidden');
                    $("<iframe src='" + url + "'></iframe>")
                        .css({ position: 'absolute', left: '0px', top: '0px', right: '0px', bottom: '0px', width: '100%', height: '100%', border: 'none' })
                        .appendTo($content);
                }
            });
        }
    };
    this.clientid = null;
    this.clientName = function () {
        if (webgis.hmac)
            return webgis.hmac.userName();
        return 'unknown';
    };
    this.init = function (oninit) {
        webgis._initialError = null;

        if (this.useMobileCurrent() === true) {
            $('body').addClass('webgis-device-mobile');
        } else {
            $('body').addClass('webgis-device-desktop');
        }

        webgis.showProgress('Initialisierung...');
        if (!webgis.hmac) {
            if (this.clientid) {
                var url = this.baseUrl + "/rest/requesthmac?clientid=" + this.clientid;
                if (this.clientid.indexOf('https://') == 0 || this.clientid.indexOf('http://') == 0 || this.clientid.indexOf('//') == 0)
                    url = this.clientid;
                var FF = !(window.mozInnerScreenX == null);

                webgis.ajax({
                    url: url,
                    async: true,
                    type: 'post',
                    xhrFields: (FF && url.indexOf('https://localhost/') == 0) ? null : this._ajaxXhrFields,
                    success: function (result) {
                        if (result.success) {
                            webgis.hmac = new webgis.hmacController(result);
                            webgis.hmac.setCurrentBranch(webgis.localStorage.get('currentBranch'));
                        }
                        else {
                            webgis._initialError = result.exception;
                        }
                    }, error: function (jqXHR, textStatus, errorThrown) {
                        webgis._initialError = 'Error at authorizing: ' + errorThrown;
                    }
                });
            }
            else {
                //webgis.hmac = new webgis.hmacController();
                webgis._initialError = 'Unknown client';
            }
        } else {
            webgis.hmac.setCurrentBranch(webgis.localStorage.get('currentBranch'));
        }

        if (!webgis._initialError) {
            if (webgis.mapFramework === 'leaflet') {
                if (!window.L) {
                    $("head").append($("<link rel='stylesheet' type='text/css' media='screen' />"));
                    webgis.loadScript(webgis.baseUrl + '/scripts/leaflet-' + webgis.mapFramework_version + '/leaflet.js', webgis.baseUrl + '/scripts/leaflet-' + webgis.mapFramework_version + '/leaflet.css', function () {
                        webgis.loadedScripts.mapFramework = true;
                        // webgis service layer
                        if (!L.imageOverlay.webgis_service) {
                            var addPlugins = '';
                            if (webgis.usability.cooperativeGestureHandling === true) {
                                addPlugins = "-gesture-handling";
                            }
                            webgis.loadScript(webgis.baseUrl + '/scripts/leaflet/plugins/leaflet-plugins' + addPlugins + '.min.js?' + webgis.api_version, webgis.baseUrl + '/scripts/leaflet/plugins/leaflet-plugins' + addPlugins + '.min.css', function () {
                                webgis.loadedScripts.mapFramework_dependencies = true;
                            });
                        }
                        else {
                            webgis.loadedScripts.mapFramework_dependencies = true;
                        }
                        // Marker Cluster
                        if (webgis.options.load_markercluster) {
                            if (!L.MarkerClusterGroup) {
                                webgis.loadScript(webgis.baseUrl + '/scripts/leaflet-' + webgis.mapFramework_version + '/leaflet.markercluster.js', webgis.baseUrl + '/scripts/leaflet-' + webgis.mapFramework_version + '/leaflet.markercluster.default.css', function () {
                                    webgis.loadedScripts.markerCluster = true;
                                });
                            }
                            else {
                                webgis.loadedScripts.markerCluster = true;
                            }
                        }
                        // Label
                        if (webgis.options.load_label) {
                            if (!L.Label) {
                                webgis.loadScript(webgis.baseUrl + '/scripts/leaflet-' + webgis.mapFramework_version + '/leaflet.label-src.js', webgis.baseUrl + '/scripts/leaflet-' + webgis.mapFramework_version + '/leaflet.label.css', function () {
                                    webgis.loadedScripts.label = true;
                                });
                            }
                            else {
                                webgis.loadedScripts.label = true;
                            }
                        }
                        // Proj
                        if (webgis.options.load_proj) {
                            if (!window.proj4) {
                                webgis.loadScript(webgis.baseUrl + '/scripts/proj4/proj4-2.3.3.js', null, function () {
                                    // proj4leaflet 'neu' passt nicht mehr zur 0.7.7 -> darum hier auch immer die Versionsnummer angeben
                                    webgis.loadScript(webgis.baseUrl + '/scripts/leaflet-' + webgis.mapFramework_version + '/proj4leaflet.js', null, function () {
                                        webgis.loadedScripts.proj = true;
                                    });
                                });
                            }
                            else {
                                webgis.loadedScripts.proj = true;
                            }
                        }
                        // Vtc
                        if (webgis.options.load_vtc) {
                            if (!L.maplibreGL) {
                                webgis.loadScript(webgis.baseUrl + '/lib/maplibre-gl/dist/maplibre-gl.js', webgis.baseUrl + '/lib/maplibre-gl/dist/maplibre-gl.css', function () {
                                    console.log('maplibre-gl.js loaded...')
                                    webgis.loadScript(webgis.baseUrl + '/lib/maplibre-gl-leaflet/leaflet-maplibre-gl.js', null, function () {
                                        console.log('leaflet-maplibre-gl.js loaded...')
                                        webgis.loadedScripts.vtc = true;
                                    });
                                });
                            } else {
                                webgis.loadedScripts.vtc = true;
                            }
                        }
                    });
                }
                else
                {
                    webgis.loadedScripts.mapFramework = true;
                    // webgis service layer
                    if (!L.imageOverlay.webgis_service) {
                        webgis.loadScript(webgis.baseUrl + '/scripts/leaflet/plugins/webgisservicelayer.js', null, function () {
                            webgis.loadedScripts.mapFramework_dependencies = true;
                        });
                    }
                    else {
                        webgis.loadedScripts.mapFramework_dependencies = true;
                    }
                    // Marker Cluster
                    if (webgis.options.load_markercluster) {
                        if (!L.MarkerClusterGroup) {
                            webgis.loadScript(webgis.baseUrl + '/scripts/leaflet.markercluster.js', webgis.baseUrl + '/content/leaflet.markercluster.default.css', function () {
                                webgis.loadedScripts.markerCluster = true;
                            });
                        }
                        else {
                            webgis.loadedScripts.markerCluster = true;
                        }
                    }
                    // Label
                    if (webgis.options.load_label) {
                        if (!L.Label) {
                            webgis.loadScript(webgis.baseUrl + '/scripts/leaflet-' + webgis.mapFramework_version + '/leaflet.label.js', webgis.baseUrl + '/scripts/leaflet-' + webgis.mapFramework_version + '/leaflet.label.css', function () {
                                webgis.loadedScripts.label = true;
                            });
                        }
                        else {
                            webgis.loadedScripts.label = true;
                        }
                    }
                    // Proj
                    if (webgis.options.load_proj) {
                        if (!window.proj4) {
                            webgis.loadScript(webgis.baseUrl + '/scripts/proj4/proj4-2.3.3.js', null, function () {
                                webgis.loadScript(webgis.baseUrl + '/scripts/proj4/proj4leaflet.js', null, function () {
                                    webgis.loadedScripts.proj = true;
                                });
                            });
                        }
                        else {
                            webgis.loadedScripts.proj = true;
                        }
                    }
                    // Vtc
                    if (webgis.options.load_vtc) {
                        if (!L.maplibreGL) {
                             webgis.loadScript(webgis.baseUrl + '/lib/maplibre-gl/dist/maplibre-gl.js', webgis.baseUrl + '/lib/maplibre-gl/dist/maplibre-gl.css', function () {
                                webgis.loadScript(webgis.baseUrl + '/lib/maplibre-gl-leaflet/leaflet-maplibre-gl.js', null, function () {
                                    webgis.loadedScripts.vtc = true;
                                });
                            });
                        } else {
                            webgis.loadedScripts.vtc = true;
                        }
                    }
                }
                // custom
                webgis.loadedScripts.customJs = true;
                if (webgis.loadCustomScripts && webgis.loadCustomScripts.length > 0) {
                    webgis.loadedScripts.customJs = false;
                    var customJsScriptIndex = 0;

                    function loadNextCustomScript() {
                        webgis.loadScript(webgis.loadCustomScripts[customJsScriptIndex], null, function () {
                            console.log('custom script loaded: ' + webgis.loadCustomScripts[customJsScriptIndex]);
                            customJsScriptIndex++;
                            if (customJsScriptIndex == webgis.loadCustomScripts.length) {
                                webgis.loadedScripts.customJs = true;
                            } else {
                                loadNextCustomScript();
                            }
                        });
                    }

                    loadNextCustomScript();
                } else {
                    webgis.loadedScripts.customJs = true;
                }
            }
        }
        this.initTimer = new webgis.timer(function () {
            if (webgis.isReady() === true) {
                webgis.hideProgress('Initialisierung...');
                if (webgis._initialError) {
                    webgis.alert('webGIS kann nicht Initialisiert werden!\n\n' + webgis._initialError, 'error');
                }
                else {
                    webgis._executeOnInit();
                    if (oninit) {
                        //try {
                        oninit();
                        //} catch (e) {
                        //    alert('Error: ' + e);
                        //}
                    }
                }
            }
            else {
                //alert(JSON.stringify(webgis.loadedScripts));
                webgis.initTimer.Start();
            }
        }, 100);
        this.initTimer.Exec();

        webgis.delayed(function () {
            if (webgis.security.allowEmbeddingMessages) {
                try {
                    if (window.parent) {
                        window.parent.postMessage({ event: 'webgis-ready' }, '*');
                    }
                } catch(e) { }
            }
        }, 2000);
    };
    this.initTimer = null;
    this.isReady = function () {
        console.log(webgis.loadedScripts.vtc);
        if (webgis._initialError)
            return true;
        if (!webgis.hmac)
            return false;
        if (!webgis.loadedScripts.mapFramework || !webgis.loadedScripts.mapFramework_dependencies) {
            return false;
        }
        if (webgis.options.load_markercluster && !webgis.loadedScripts.markerCluster) {
            return false;
        }
        if (webgis.options.load_proj && !webgis.loadedScripts.proj) {
            return false;
        }
        if (webgis.options.load_vtc && !webgis.loadedScripts.vtc) {
            return false;
        }
        if (webgis.loadCustomScripts && webgis.loadCustomScripts.length > 0 && !webgis.loadedScripts.customJs) {
            return false;
        }

        
        return true;
    };
    this.loadScript = function (url, css, callback, arg, type) {
        if (css)
            $("head").append($("<link rel='stylesheet' href='" + css + "' type='text/css' media='screen' />"));
        var script = document.createElement("script");
        script.type = type || "text/javascript";
        if (script.readyState) { //IE
            script.onreadystatechange = function () {
                if (script.readyState == "loaded" || script.readyState == "complete") {
                    script.onreadystatechange = null;
                    if (callback)
                        callback(arg);
                }
            };
        }
        else { //Others
            script.onload = function () {
                if (callback)
                    callback(arg);
            };
        }
        script.src = url;
        document.getElementsByTagName("head")[0].appendChild(script);
    };
    this.require = function (lib, callback, arg) {
        var loading = false;
        if (lib === "flatpickr") {
            if (!window.flatpickr) {
                loading = true;
                this.loadScript(webgis.baseUrl + '/scripts/flatpickr/flatpickr.de.min.js', webgis.baseUrl + '/scripts/flatpickr/flatpickr.min.css', callback, arg);
            }
        }
        else if (lib === "leaflet-minimap") {
            if (!L.Control.MiniMap) {
                loading = true;
                this.loadScript(webgis.baseUrl + '/scripts/leaflet/leaflet-minimap/control.minimap.min.js', webgis.baseUrl + '/scripts/leaflet/leaflet-minimap/control.minimap.css', callback, arg);
            }
        }
        else if (lib === "leaflet-side-by-side") {
            if (!L.Control.sideBySide) {
                loading = true;
                this.loadScript(webgis.baseUrl + '/scripts/leaflet/leaflet-side-by-side/leaflet-side-by-side.js', webgis.baseUrl + '/scripts/leaflet/leaflet-side-by-side/range.css', callback, arg);
            }
        }
        else if (lib === "signalr") {
            if (!window.signalR) {
                loading = true;
                this.loadScript(webgis.baseUrl + '/scripts/signalr/dist/browser/signalr.js', '', callback, arg);
            }
        }
        else if (lib === "liveshare") {
            if (!window.livesharehub) {
                loading = true;
                this.loadScript(webgis.baseUrl + '/lib/livesharehub/livesharehub_debug.js', '', callback, arg);
                //this.loadScript(arg +'/js/livesharehub.js', '', callback, arg);
            }
        }
        else if (lib === "webgis-three-d") {
            if (!webgis.threeD) {
                loading = true;
                this.loadScript(webgis.baseUrl + '/scripts/api/three_d.js', '', callback, arg, 'module');
            }
        }
        else if (lib == "nav-compass") {
            if (!window.navCompass) {
                loading = true;
                this.loadScript(webgis.baseUrl + '/lib/nav-compass/src/js/nav-compass.js', webgis.baseUrl + '/lib/nav-compass/src/css/nav-compass.css', callback, arg);
            }
        }
        else if (lib === "sortable") {
            if (!window.Sortable) {
                loading = true;
                this.loadScript(webgis.baseUrl + '/lib/sortable/sortable.min.js', '', callback, arg);
            }
        }
        else if (lib === "select2") {
            if(!$.fn.select2) {
                loading = true;
                this.loadScript(webgis.baseUrl + '/lib/select2/dist/js/select2.min.js', webgis.baseUrl + '/lib/select2/dist/css/select2.min.css', function () {
                    //$.fn.select2.defaults.set('theme', 'webgis');
                    callback(arg);
                }, arg);
            }
        }
        else if (lib === "monaco-editor") {
            if (!window.monaco) {
                loading = true;
                //window.require = window.require || {};
                //window.require.paths = window.require.paths || {};
                //window.require.paths.vs = webgis.baseUrl + '/lib/monaco-editor/min/vs';
                var orginal_require = window.require;
                window.require = { paths: { vs: webgis.baseUrl + '/lib/monaco-editor/min/vs' } };
                console.log('window.require', window.require);

                webgis.loadScript(webgis.baseUrl + '/lib/monaco-editor/min/vs/loader.js', '', function (arg) {
                    console.log('loaded', webgis.baseUrl + '/lib/monaco-editor/min/vs/loader.js');

                    webgis.loadScript(webgis.baseUrl + '/lib/monaco-editor/min/vs/editor/editor.main.js', '', function (arg) {
                        console.log('loaded', webgis.baseUrl + '/lib/monaco-editor/min/vs/editor/editor.main.js');

                        var counter = 0;
                        var timer = new webgis.timer(function () {
                            if (window.monaco) {
                                //console.log('got it...');
                                window.require = orginal_require;
                                //console.log(window.require);
                                callback(arg);
                            } else {
                                //console.log('restart');
                                counter++;
                                if (counter > 50) {
                                    webgis.alert("Sorry, can't load editor", "error");
                                } else {
                                    timer.start();
                                }
                            }
                        }, 100);

                        timer.start();
                    }, arg);
                }, arg);
            }
        }
        if (loading === false)
            callback(arg);
    };
    this.delayed = function (callback, duration, arg) {
        var timer = new webgis.timer(callback, duration ? duration : 1, arg);
        timer.Start();
    };
    this.tryDelayed = function (callback, duration, tryCountMax) {
        var timer = new webgis.timer(function (t) {
            if (!callback()) {
                if (t.tryCount < t.tryCountMax)
                    timer.Start();
            }
            t.tryCount++;
        }, duration ? duration : 1, null);
        timer.SetArgument(timer);
        timer.tryCount = 1;
        timer.tryCountMax = tryCountMax;
        timer.Start();
    };
    this._delayedToggle = [];
    this.delayedToggle = function (id, callback, duration, arg) {
        if (webgis._delayedToggle[id]) {
            this.delayedToggleStop(id);
        }
        else {
            webgis._delayedToggle[id] = new webgis.timer(function (f) {
                webgis._delayedToggle[f.id] = null;
                f.callback(f.arg);
            }, duration ? duration : 1, { id: id, callback: callback, arg: arg });
            webgis._delayedToggle[id].Start();
        }
    };
    this.delayedToggleStop = function (id) {
        if (webgis._delayedToggle[id]) {
            webgis._delayedToggle[id].Stop();
            webgis._delayedToggle[id] = null;
        }
    };
    var _crsCache = [];
    this.createCRS = function (id, name, p4params, properties) {
        if (webgis.mapFramework == 'leaflet') {
            var crs = { id: id, name: name, resolutions: properties.resolutions, frameworkElement: new L.Proj.CRS('EPSG:' + id, p4params, properties) };
            this._srefs[id] = crs.frameworkElement;
            if (properties && properties.extent && crs.frameworkElement.projection && !crs.frameworkElement.projection.bounds) {
                // ???

                crs.frameworkElement.projection.bounds = L.bounds(L.point(properties.extent[0], properties.extent[1]), L.point(properties.extent[2], properties.extent[3]));
                crs.frameworkElement.infinite = false;
            }
            else { // new with Leaflet 1.0.0
                crs.frameworkElement.infinite = crs.frameworkElement.projection.bounds ? false : true;
            }
            return crs;
        }
    };

    this.createMap = function (elemId, options) {
        return this._createMap(elemId, options, null);
    };
    this.createMapFromJson = function (elementId, refMapJson, onReady, mapName, appendServices) {
        if (!refMapJson)
            return null;
        var mapJson;
        if (refMapJson.isRefParameter) {
            if (typeof refMapJson.value === 'string')
                refMapJson.value = this.$.parseJSON(refMapJson.value);
            mapJson = refMapJson.value;
        }
        else {
            if (typeof refMapJson === 'string')
                refMapJson = this.$.parseJSON(refMapJson);
            mapJson = refMapJson;
        }

        var crs = mapJson.crs;
        var services = '', queries = [], layers = [], customParameters = null;
        for (var s in mapJson.services) {
            if (services != '')
                services += ',';

            services += mapJson.services[s].id;
            var serviceQueries = mapJson.services[s].queries;

            if (serviceQueries && serviceQueries.length > 0) {
                for (var q in serviceQueries) {
                    queries.push({
                        service: mapJson.services[s].id,
                        query: serviceQueries[q].id,
                        visible: serviceQueries[q].visible
                    });
                }
            }

            layers[mapJson.services[s].id] = mapJson.services[s].layers;

            if (mapJson.services[s].custom_request_parameters) {
                for (var c in mapJson.services[s].custom_request_parameters) {
                    if (!customParameters) customParameters = {};
                    customParameters['custom.' + mapJson.services[s].id + '.' + c] = mapJson.services[s].custom_request_parameters[c];
                }
            }
        }

        if (appendServices) {
            var servicesArray = services.split(','), appendServicesArray = appendServices.split(',');
            for (var i in appendServicesArray) {
                var appendServiceId = appendServicesArray[i].trim();
                if (appendServiceId && $.inArray(appendServiceId, servicesArray) < 0) {
                    servicesArray.push(appendServiceId);
                }
            }

            services = servicesArray.toString();
        }

        var map = webgis.createMap(elementId, {
            extent: crs.id,
            services: services,
            queries: queries,
            layers: layers,
            dynamiccontent: mapJson.dynamiccontent,
            name: mapName,
            custom: customParameters,
            query_results: mapJson.query_results,
            visfilters: mapJson.visfilters
        });

        // Set Opacity, Order
        for (var s in mapJson.services) {
            var jsonService = mapJson.services[s];
            var service = map.getService(jsonService.id);
            if (service) {
                service.updateProperties(jsonService);
            }
        }

        // Focus Services
        if (mapJson.focused_services && mapJson.focused_services.ids && mapJson.focused_services.ids.length > 0) {
            map.focusServices(mapJson.focused_services.ids, mapJson.focused_services.getOpacity());
        }

        var mapIsReady = false;
        if (onReady) {
            map.events.on('refresh', function () {
                mapIsReady = true;
            });
        }

        var hasAutoZoomDynamicContent = false;
        if (mapJson.dynamiccontent) {
            for (var d in mapJson.dynamiccontent) {
                var dynamicContent = mapJson.dynamiccontent[d];
                if (!dynamicContent.suppressAutoLoad) {  // first theme to load
                    hasAutoZoomDynamicContent = dynamicContent.autoZoom !== false;
                    break;
                }
            }
        }

        //console.log('hasAutoZoomDynamicContent', hasAutoZoomDynamicContent);
        if (hasAutoZoomDynamicContent == false) {
            map.setScale(mapJson.scale, mapJson.center);
        }
        map.deserializeUI(mapJson);
        //map.ui.deserialize(mapJson.ui);
        if (onReady) {
            var createMapTimer = new webgis.timer(function () {
                if (mapIsReady === true) {
                    onReady(map);
                }
                else {
                    createMapTimer.Start();
                }
            }, 10);
            createMapTimer.Start();
        }

        return map;
    };
    this._createMap = function (elemId, options, mapObject) {
        if (webgis.customEvents.beforeCreateMap) {
            webgis.customEvents.beforeCreateMap(elemId, options, mapObject);
        }

        if (!(typeof elemId === 'string')) {
            var id = $(elemId).attr('id');
            if (!id) {
                id = 'map_' + webgis.guid();
                $(elemId).attr('id', id);
            }
            elemId = id;
        }
        webgis.showProgress('Karte wird geladen...', elemId);
        options = this.loadOptions(options);
        var map = mapObject;

        if (this.mapFramework == 'leaflet') {
            var leafletMap = null;
            if (options.extent && typeof options.extent === 'string') {
                var extentName = options.extent;
                options.extent = webgis.extentInfo(options.extent);
                options.crs = webgis.createCRS(options.extent.epsg, extentName, options.extent.p4, options.extent);
            }
            if (!options.bounds && options.extent) {
                options.bounds = options.extent.extent;
            }
            if (options.crs && options.crs.frameworkElement) {
                leafletMap = L.map(elemId, {
                    maxZoom: options.crs.resolutions.length - 1,
                    minZoom: 0,
                    zoomDelta: webgis.usability.zoom.zoomDelta,
                    zoomSnap: webgis.usability.zoom.zoomSnap,
                    crs: options.crs.frameworkElement,
                    continuousWorld: false,
                    worldCopyJump: false,
                    bounceAtZoomLimits: false,
                    tapTolerance: 25,
                    clickTolerance: 25,
                    gestureHandling: webgis.usability.cooperativeGestureHandling
                });
                //console.log(leafletMap.dragging._draggable.options);  // clickTolerance -> wird dann in der Map über _enableSketchClickHandling gesetzt...

                if(webgis.usability.zoom.setMaxBounds === true &&
                   options.extent && options.extent.epsg && options.extent.extent) {
                    //console.log(options.extent);

                    var southWest = webgis.unproject(options.extent.epsg, [options.extent.extent[0], options.extent.extent[1]]),
                        northEast = webgis.unproject(options.extent.epsg, [options.extent.extent[2], options.extent.extent[3]]),
                        bounds = L.latLngBounds(L.latLng(southWest[1],southWest[0]), L.latLng(northEast[1],northEast[0]));

                    //console.log(bounds);
                    leafletMap.setMaxBounds(bounds);
                }
            }
            else {
                leafletMap = L.map(elemId, {});
            }

            if (map == null) {
                map = new webgis.map(leafletMap, options.crs, options.extent, options.name);
            }
            else {
                map.init(leafletMap, options.crs, options.extent, options.name);
            }

            map.setCalcCrs(webgis.calc._crsId);

            leafletMap._webgis5Name = map.name;
            this.maps[elemId] = map;

            //console.log('map created: ' + map.guid, this.maps);

            if (options.services && typeof options.services === 'string') {
                const serviceInfos = webgis.serviceInfo(options.services, options.custom);

                if (serviceInfos.success === false) {
                    webgis.alert('Error on webgis.serviceInfo(): ' + (serviceInfos.exception || 'Unknown Error'), "error");
                    return;
                }

                options.services = serviceInfos.services;
                options.unknownservices = serviceInfos.unknownservices;
                options.unauthorizedservices = serviceInfos.unauthorizedservices;
                options.copyright = serviceInfos.copyright;
            }
            if (options.queries) {
                map.setQueryDefinitions(options.queries);
            }
            if (options.copyright) {
                map.addCopyright(options.copyright);
            }
            if (options.services) {
                webgis.events.fire('before-map-add-services', webgis, map);
                map.addServices(options.services, options.layers ? options.layers : null);
                webgis.events.fire('after-map-add-services', webgis, map);
            }
            if (options.bounds) {
                var b;
                if (options.crs && options.crs.frameworkElement && options.crs.frameworkElement.projection) {
                    //alert(bounds[0] + " " + bounds[1]);
                    var sw = options.crs.frameworkElement.projection.unproject(L.point(options.bounds[0], options.bounds[1]));
                    var ne = options.crs.frameworkElement.projection.unproject(L.point(options.bounds[2], options.bounds[3]));
                    var b = L.latLngBounds(sw, ne);
                    //alert(sw.lat + ' ' + sw.lng + "\n" + ne.lat + " " + ne.lng);
                }
                else {
                    b = L.latLngBounds(L.latLng(options.bounds[1], options.bounds[0]), L.latLng(options.bounds[3], options.bounds[2]));
                }

                leafletMap.fitBounds(b, {
                    animate: true
                });
            }
            if (options.dynamiccontent && options.dynamiccontent.length > 0) {
                map.addDynamicContent(options.dynamiccontent, true);
            }
            if (Array.isArray(options.visfilters)) {
                map._visfilters = options.visfilters;
            }
        }
        webgis.hideProgress('Karte wird geladen...');
        if (options.unknownservices && options.unknownservices.length > 0) {
            $('body').webgis_modal({
                title: 'Unbekannte Dienste',
                onload: function ($content) {
                    $content.html('<h2>Warnung!</h2><p>Es konnten nicht alle Dienste geladen werden. Das bedeutet, dass Inhalte in der Karte Fehlen oder nicht angezeigt werden können. Eventuell ist das System nicht richtig Konfiguriert. Sollte der Meldung öfter auftreten, melden sie sicht beim Administrator. Folgende Dienste sind nicht in der Karte vorhanden:');
                    var $ul = $("<ul>").appendTo($content);
                    for (var i in options.unknownservices) {
                        $("<li>" + options.unknownservices[i] + "</li>").appendTo($ul);
                    }
                },
                width: '330px', height: '400px'
            });
        }
        if (options.unauthorizedservices && options.unauthorizedservices.length > 0) {
            webgis.toastMessage("Hinweis", "Nicht berechtigte Dienste in der Karte werden ausgeblendet...",
                function () {
                    $('body').webgis_modal({
                        title: 'Nicht berechtigte Dienste',
                        onload: function ($content) {
                            $content.html('<h2>Hinweis</h2><p>In diese Karte wurde vom Kartenautor Dienste eingefügt, für die Sie keine Berechtigung besitzen. Diese Dienste werden in der Karte nicht angezeigt. Auf die Funktionsweise der Karte sollte das keinen Einfluss nehmen. Folgende Dienste sind für Sie nicht in der Karte vorhanden (falls sie einen dieser Dienste benötigen, wenden sie sich and den Administrator):');
                            var $ul = $("<ul>").appendTo($content);
                            for (var i in options.unauthorizedservices) {
                                $("<li>" + options.unauthorizedservices[i] + "</li>").appendTo($ul);
                            }
                        },
                        width: '330px', height: '400px'
                    });
                });
        }
        webgis._executeOnMapCreated(map, map._webgisContainer);
        if (options.enabled === false)
            map.enable(false);

        if (webgis.security._embeddingMessagesTarget) {

            webgis.security._embeddingMessagesTarget.postMessage({
                event: 'map-added',
                mapId: elemId
            });

            map.events.on('refresh', function (channel, sender, event) {
                if (webgis.security._embeddingMessagesTarget) {
                    webgis.security._embeddingMessagesTarget.postMessage({
                        event: 'map-refresh',
                        mapId: $(sender.elem).attr('id'),
                        scale: sender.scale(),
                        center: sender.getCenter(),
                        bounds: sender.getExtent()
                    });
                }
            });
        }

        if (options.query_results) {
            var query_results = Array.isArray(options.query_results) ? options.query_results : [options.query_results];

            var $tabControl = map.ui.getQueryResultTabControl();
            if ((!$tabControl || !map.ui.appliesQueryResultsUI()) && query_results.length > 1) {
                query_results = [query_results[0]]
            }

            var queryFunc = function (query_result) {
                if (query_result && query_result.oids && query_result.metadata) {
                    webgis.ajax({
                        url: webgis.baseUrl + '/rest/services/' + query_result.metadata.service + '/queries/' + query_result.metadata.query,
                        type: 'post',
                        data: webgis.hmac.appendHMACData({ '#oids#': query_result.oids.toString(), f: 'json', c: 'query' }),
                        success: function (result) {
                            if (result && result.features) {
                                result.metadata = result.metadata || {};
                                result.metadata.connect = query_result.metadata.connect;
                                result.metadata.tool = query_result.metadata.tool;
                                result.metadata.startPoint = query_result.metadata.startPoint;
                                result.metadata.reorderAble = query_result.metadata.reorderAble;
                                result.metadata.custom_selection = query_result.metadata.custom_selection;
                                result.metadata.selected = query_result.metadata.selected;

                                if (query_result.tab) {
                                    result.tab = {
                                        id: query_result.tab.id,
                                        title: query_result.tab.title,
                                        pinned: query_result.tab.pinned,
                                        selected: query_result.tab.selected,
                                        addSilent: query_result.tab.selected !== true
                                    };
                                }

                                // Check if Hashcodes changed and set a warning if so
                                if (query_result.hashcodes
                                    && query_result.hashcodes.length === query_result.oids.length) {
                                    let hasChanged = query_result.hashcodes.length !== result.features.length;

                                    for (var i in query_result.oids) {
                                        const oid = query_result.metadata.service + ":" + query_result.metadata.query + ":" + query_result.oids[i].toString();
                                        const hashCode = query_result.hashcodes[i];
                                        let found = false;

                                        for (let f in result.features) {
                                            const feature = result.features[f];
                                            
                                            if (feature.oid == oid) {
                                                const featureHashCode = webgis.hmac._featureGeoHashCode(feature);
                                                if (hashCode != featureHashCode) {
                                                    console.log('hashCode not ident', hashCode, featureHashCode);
                                                }
                                                hasChanged |= (hashCode != featureHashCode) ? true : false;
                                                found = true;
                                                break;
                                            }
                                        }

                                        if (!found) {
                                            hasChanged = true;
                                            break;
                                        }
                                    }

                                    if (hasChanged) {
                                        result.metadata.warnings = result.metadata.warnings || [];
                                        result.metadata.warnings.push(webgis.l10n.get('results-has-changed-warning'));
                                    }
                                }

                                map.events.fire('onnewfeatureresponse', result);

                                if (map.ui && map.ui.appliesQueryResultsUI()) {
                                    map.ui.showQueryResults(result, true);
                                }
                                else {
                                    map.queryResultFeatures.showClustered(result, true, true);
                                }
                            }
                        }
                    });
                }
            }

            for (var q in query_results) {
                var query_result = query_results[q];

                webgis.delayed(queryFunc, 1000 * q, query_result);
            }
        }

        return map;
    };
    this._removeMap = function (mapToRemove) {
        var maps = [];
        if (mapToRemove && mapToRemove.guid) {
            for (var m in this.maps) {
                var map = this.maps[m];
                if (mapToRemove.guid !== map.guid) {
                    maps[m] = this.maps[m];
                }
            }
        }
        this.maps = maps;

        //console.log('map removed (destroyed): ' + mapToRemove.guid, this.maps);
    };
    this.getMap = function (mapId) {
        for (var id in this.maps) {
            if (id == mapId)
                return this.maps[id];
        }
        return null;
    };
    this.getMapByGuid = function (mapGuid) {
        for (var m in this.maps) {
            if (this.maps[m] && this.maps[m].guid === mapGuid)
                return this.maps[m];
        }
        return null;
    }

    this.createMarker = function (options) {
        options = options || {};
        var icon = this.createMarkerIcon(options);

        if (webgis.mapFramework == "leaflet") {
            if (options.draggable) {
                var marker = L.marker(L.latLng(options.lat, options.lng), {
                    draggable: true,
                    icon: icon || L.icon({
                        iconUrl: webgis.css.imgResource('marker_tool_w.png', 'markers'),
                        iconSize: [25, 41],
                        iconAnchor: [12, 0],
                        popupAnchor: [0, 0],
                    })
                });

                return marker;
            }


            if (icon) {
                return options.label
                    ? L.labelMarker(L.latLng(options.lat, options.lng), { icon: icon, label: options.label })
                    : L.marker(L.latLng(options.lat, options.lng), { icon: icon })
            }
            return L.marker(L.latLng(options.lat, options.lng));

        }
    };
    this.tryHighlightMarker = function (marker, highlight) {
        var icon = marker && marker.getIcon ? marker.getIcon() : null;
        if (icon && icon.options && icon.options.iconUrl) {
            var iconUrl = this.highlightMarkerUrl(icon.options.iconUrl, highlight);
            icon.options.iconUrl = iconUrl;
            marker.setIcon(icon);
        }
    };
    this.highlightMarkerUrl = function (iconUrl, highlight) {
        if (iconUrl && iconUrl.indexOf('/rest/' > 0)) {
            if (highlight) {
                if (iconUrl.indexOf("hc=ffc,ee0,000") < 0) {
                    iconUrl += (iconUrl.indexOf('?') > 0 ? '&' : '?') + 'hc=ffc,ee0,000';
                }
            } else {
                iconUrl = iconUrl.replaceAll('hc=ffc,ee0,000', '');
            }
        }

        return iconUrl;
    }
    this.createMarkerIcon = function (options) {
        var icon = null;
        if (webgis.mapFramework === "leaflet") {
            if (options.icon === "currentpos_red") {
                icon = L.icon({ iconUrl: webgis.css.imgResource('position_red.png', 'markers'), iconSize: [38, 38], iconAnchor: [19, 19], popupAnchor: [0, -20] });
            }
            else if (options.icon === "currentpos_green") {
                icon = L.icon({ iconUrl: webgis.css.imgResource('position_green.png', 'markers'), iconSize: [38, 38], iconAnchor: [19, 19], popupAnchor: [0, -20] });
            }
            else if (options.icon === "sketch_vertex") {
                icon = L.icon({
                    iconUrl: webgis.markerIcons["sketch_vertex"].url(options.vertex),
                    iconSize: webgis.markerIcons["sketch_vertex"].size,
                    iconAnchor: webgis.markerIcons["sketch_vertex"].anchor,
                    popupAnchor: webgis.markerIcons["sketch_vertex"].popupAnchor,
                    className: options.draggable ? "webgis-sketch-draggable-vertex" : ''
                });
            }
            else if (options.icon === "sketch_vertex_fixed") {
                icon = L.icon({
                    iconUrl: webgis.markerIcons["sketch_vertex_fixed"].url(options.vertex),
                    iconSize: webgis.markerIcons["sketch_vertex_fixed"].size,
                    iconAnchor: webgis.markerIcons["sketch_vertex_fixed"].anchor,
                    popupAnchor: webgis.markerIcons["sketch_vertex_fixed"].popupAnchor,
                    className: options.draggable ? "webgis-sketch-draggable-vertex" : ''
                });
            }
            else if (options.icon === "sketch_vertex_selected") {
                icon = L.icon({
                    iconUrl: webgis.markerIcons["sketch_vertex_selected"].url(options.vertex),
                    iconSize: webgis.markerIcons["sketch_vertex_selected"].size,
                    iconAnchor: webgis.markerIcons["sketch_vertex_selected"].anchor,
                    popupAnchor: webgis.markerIcons["sketch_vertex_selected"].popupAnchor,
                    className: options.draggable ? "webgis-sketch-draggable-vertex" : ''
                });
            }
            else if (options.icon === "sketch_vertex_text") {
                icon = L.icon({
                    iconUrl: webgis.markerIcons["sketch_vertex_text"].url(),
                    iconSize: webgis.markerIcons["sketch_vertex_text"].size,
                    iconAnchor: webgis.markerIcons["sketch_vertex_text"].anchor,
                    popupAnchor: webgis.markerIcons["sketch_vertex_text"].popupAnchor,
                    className: options.draggable ? "webgis-sketch-draggable-vertex" : ''
                });
            }
            else if (options.icon === "sketch_mover") {
                icon = L.icon({
                    iconUrl: webgis.markerIcons["sketch_mover"].url(),
                    iconSize: webgis.markerIcons["sketch_mover"].size,
                    iconAnchor: webgis.markerIcons["sketch_mover"].anchor,
                    popupAnchor: webgis.markerIcons["sketch_mover"].popupAnchor,
                    className: "webgis-sketch-snapping-mover"
                });
            }
            else if (options.icon === "watcher") {
                icon = L.icon({
                    iconUrl: webgis.markerIcons["watcher"].url(options.angle),
                    iconSize: webgis.markerIcons["watcher"].size,
                    iconAnchor: webgis.markerIcons["watcher"].anchor,
                    popupAnchor: webgis.markerIcons["watcher"].popupAnchor
                });
            }
            else if (options.icon === "marker_draggable_bottom") {
                icon = L.icon({
                    iconUrl: webgis.markerIcons["marker_draggable_bottom"].url(),
                    iconSize: webgis.markerIcons["marker_draggable_bottom"].size,
                    iconAnchor: webgis.markerIcons["marker_draggable_bottom"].anchor,
                    popupAnchor: webgis.markerIcons["marker_draggable_bottom"].popupAnchor
                });
            }
            else if (options.icon === "dynamic_content") {
                var markerIcon = webgis.markerIcons["dynamic_content"][options.name] || webgis.markerIcons["dynamic_content"]["default"];
                var index = (typeof options.index !== 'undefined') ? parseInt(options.index) : 0;

                icon = L.icon({
                    iconUrl: markerIcon.url(index, options.feature),
                    iconSize: markerIcon.size(index, options.feature),
                    iconAnchor: markerIcon.anchor(index, options.feature),
                    popupAnchor: markerIcon.popupAnchor(index, options.feature)
                });
            }
            else if (options.icon === "dynamic_content_extenddependent") {
                var markerIcon = webgis.markerIcons["dynamic_content_extenddependent"][options.name] || webgis.markerIcons["dynamic_content_extenddependent"]["default"];
                var index = (typeof options.index !== 'undefined') ? parseInt(options.index) : 0;

                icon = L.icon({
                    iconUrl: markerIcon.url(index, options.feature),
                    iconSize: markerIcon.size(index, options.feature),
                    iconAnchor: markerIcon.anchor(index, options.feature),
                    popupAnchor: markerIcon.popupAnchor(index, options.feature)
                });
            }
            else if (options.icon === "query_result") {
                try {
                    var markerIcon = webgis.markerIcons["query_result"]["default"];
                    var index = (typeof options.index !== 'undefined') ? parseInt(options.index) : 0;

                    if (options.feature && options.feature.oid && options.feature.oid.indexOf(':') >= 0) {
                        var p = options.feature.oid.split(':');

                        if (webgis.markerIcons["query_result"][p[0] + ":" + p[1]]) {
                            markerIcon = webgis.markerIcons["query_result"][p[0] + ":" + p[1]];
                        }
                        else if (webgis.markerIcons["query_result"][p[1]]) {
                            markerIcon = webgis.markerIcons["query_result"][p[1]];
                        }
                    }
                    if (options.queryToolId) {
                        if (webgis.markerIcons["query_result"][options.queryToolId]) {
                            markerIcon = webgis.markerIcons["query_result"][options.queryToolId];
                        }
                    }
                    let iconUrl = markerIcon.url(index, options.feature);
                    if (!iconUrl && markerIcon.className) {
                        icon = L.divIcon({
                            className: markerIcon.className(index, options.feature),
                            iconSize: markerIcon.size(index, options.feature),
                            iconAnchor: markerIcon.anchor(index, options.feature),
                            popupAnchor: markerIcon.popupAnchor(index, options.feature)
                        });
                    } else {
                        icon = L.icon({
                            iconUrl: iconUrl,
                            iconSize: markerIcon.size(index, options.feature),
                            iconAnchor: markerIcon.anchor(index, options.feature),
                            popupAnchor: markerIcon.popupAnchor(index, options.feature)
                        });
                    }
                }
                catch (e) {
                    console.log(e);
                    icon = L.icon({
                        iconUrl: webgis.css.imgResource('marker_blue.png', 'markers'),
                        iconSize: [25, 41],
                        iconAnchor: [12, 41],
                        popupAnchor: [0, -42]
                    });
                }
            }
            else if (options.icon === "liveshare_user" && options.size && options.username) {
                var width = options.size || 32, height = parseInt(width * 1.2);
                icon = L.icon({
                    iconUrl: webgis.baseUrl + '/rest/usermarkerimage?id=' + options.username + '&width=' + width,
                    iconSize: [width, height],
                    iconAnchor: [parseInt(width / 2), height],
                    popupAnchor: [0, -height - 1]
                });
            }
            else if (options.icon === "blue" || options.icon == "") {
                icon = L.icon({
                    iconUrl: webgis.css.imgResource('marker_blue.png', 'markers'),
                    iconSize: [25, 41],
                    iconAnchor: [12, 41],
                    popupAnchor: [0, -42]
                });
            }
            else if (typeof options.icon === "string") {
                icon = L.icon({
                    iconUrl: options.icon.toLowerCase().indexOf("http://") == 0 || options.icon.toLowerCase().indexOf("https://") == 0 ? webgis.css.imgResource(options.icon, 'markers') : webgis.css.img(options.icon),
                    iconSize: options.iconSize,
                    iconAnchor: options.iconAnchor,
                    popupAnchor: options.popupAnchor
                });
            }
            else if (options.icon && options.icon.iconUrl && options.icon.iconSize) {
                icon = L.icon({
                    iconUrl: options.icon.iconUrl.toLowerCase().indexOf("http://") == 0 || options.icon.iconUrl.toLowerCase().indexOf("https://") == 0 ? webgis.css.imgResource(options.icon.iconUrl, 'markers') : webgis.css.img(options.icon.iconUrl),
                    iconSize: options.icon.iconSize,
                    iconAnchor: options.icon.iconAnchor,
                    popupAnchor: options.icon.popupAnchor
                });
            }
        }

        return icon;
    };
    this.modifyMapOptions = function (options, modifyServiceNameFunc, modifyExtentNameFunc) {
        if (modifyServiceNameFunc) {
            options.services = modifyServiceNameFunc(options.services);

            if (options.layers) {
                var layers = [];
                for (var l in options.layers) {
                    layers[modifyServiceNameFunc(l)] = options.layers[l];
                }
                options.layers = layers;
            }
            if (options.queries) {
                for (var q in options.queries) {
                    options.queries[q].service = modifyServiceNameFunc(options.queries[q].service);
                }
            }
        }
        if (modifyExtentNameFunc) {
            options.extent = modifyExtentNameFunc(options.extend);
        }
    };
    this.markerIcons = [];
    this.markerIcons["currentpos_red"] = {
        url: function () { return webgis.css.imgResource('position_red.png', 'markers'); },
        size: [38, 38], anchor: [19, 19], popupAnchor: [0, -20]
    };
    this.markerIcons["currentpos_green"] = {
        url: function () { return webgis.css.imgResource('position_green.png', 'markers'); },
        size: [38, 38], anchor: [19, 19], popupAnchor: [0, -20]
    };
    this.markerIcons["sketch_vertex_bottom"] = {
        url: function (i) { return webgis.css.imgResource('marker_bottom_sketch_vertex.png', 'markers'); },
        size: [25, 41],
        anchor: [12, 0],
        popupAnchor: [0, 2]
    };
    this.markerIcons["sketch_vertex_circle"] = {
        url: function (i) { return webgis.css.imgResource('marker_circle_sketch_vertex_' + i + '.png', 'markers'); },
        size: [21, 21],
        anchor: [11, 11],
        popupAnchor: [0, -11]
    };
    this.markerIcons["sketch_vertex_fixed"] = {
        url: function (i) { return webgis.css.imgResource('sketch_marker_fixed.png', 'markers'); },
        size: [21, 21],
        anchor: [11, 11],
        popupAnchor: [0, -11]
    };
    this.markerIcons["sketch_vertex_selected"] = {
        url: function () { return webgis.css.imgResource('sketch_marker_selected.png', 'markers'); },
        size: [13, 13],
        anchor: [7, 7],
        popupAnchor: [0, -7]
    };
    this.markerIcons["sketch_vertex_square"] = {
        url: function () { return webgis.css.imgResource('sketch_marker_square.png', 'markers'); },
        size: [13, 13],
        anchor: [7, 7],
        popupAnchor: [0, -7]
    };
    this.markerIcons["sketch_vertex_text"] = {
        url: function () { return webgis.css.imgResource('sketch_marker_square_text.png', 'markers'); },
        size: [13, 13],
        anchor: [7, 7],
        popupAnchor: [0, -7]
    };
    this.markerIcons["sketch_vertex"] = this.markerIcons["sketch_vertex_bottom"];
    this.markerIcons["sketch_mover"] = {
        url: function () { return webgis.css.imgResource('sketch_marker_snapper_square.png', 'markers'); },
        size: [13, 13],
        anchor: [7, 7],
        popupAnchor: [0, -7]
    };
    this.markerIcons["query_result"] = [];
    this.markerIcons["query_result"]["default"] = {
        url: function (index, feature) { return webgis.css.imgResource('marker_blue.png', 'markers'); },
        size: function (index, feature) { return [25, 41]; },
        anchor: function (index, feature) { return [12, 42]; },
        popupAnchor: function (index, feature) { return [0, -42]; }
    };
    this.markerIcons["dynamic_content"] = [];
    this.markerIcons["dynamic_content"]["default"] = this.markerIcons["query_result"]["default"];
    this.markerIcons["dynamic_content_extenddependent"] = [];
    this.markerIcons["dynamic_content_extenddependent"]["default"] = this.markerIcons["query_result"]["default"];
    this.markerIcons["watcher"] = {
        url: function (angle) { return webgis.css.imgResource('watcher_' + (isNaN(angle) ? "0" : ((parseInt(angle) + 360) % 360)) + '.png', 'markers'); },
        size: [40, 40], anchor: [20, 20], popupAnchor: [0, -20]
    };
    this.markerIcons["marker_draggable_bottom"] = {
        url: function (i) { return webgis.css.imgResource('marker_draggable_bottom.png', 'markers'); },
        size: [25, 41],
        anchor: [12, 0],
        popupAnchor: [0, 2]
    };
    
    this.hooks = [];
    this.hooks["query_result_feature"] = [];
    this.hooks["query_result_feature"]["default"] = null;
    this.hooks["query_result_feature_table"] = [];
    this.hooks["query_result_feature_table"]["default"] = null;
    this.hooks["dynamic_content_feature_loaded"] = [];
    this.hooks["dynamic_content_feature_loaded"]["default"] = null;
    this.hooks["dynamic_content_loaded"] = [];
    this.hooks["dynamic_content_loaded"]["default"] = null;
    this.sortingAlg = [];
    this.sortingAlg["default"] = function (a, b) {
        if (a === b) {
            return 0;
        }

        if (typeof a === "string" && typeof b === "string") {
            a = a.trim().toLowerCase();
            b = b.trim().toLowerCase();
        }
        return a < b ? -1 : 1;
    };
    this.sortingAlg["number"] = function (a, b) {
        try {
            a = parseFloat(a.toString().replaceAll(',', '.'));
            b = parseFloat(b.toString().replaceAll(',', '.'));

            if (!isNaN(a) && isNaN(b)) return -1;
            if (isNaN(a) && !isNaN(b)) return 1;
        } catch (e) {
            console.log(e);
        }
        return webgis.sortingAlg["default"](a, b);
    };
    this.sortingAlg["gnr"] = function (a, b) {
        try {
            a = a.toString().trim();
            b = b.toString().trim();

            var aIsPktNumber = false, bIsPktNumber = false;
            if (a.indexOf('.') === 0) {
                aIsPktNumber = true;
                a = a.substr(1);
            }
            if (b.indexOf('.') === 0) {
                bIsPktNumber = true;
                b = b.substr(1);
            }

            if (aIsPktNumber && !bIsPktNumber)
                return -1;
            if (!aIsPktNumber && bIsPktNumber)
                return 1;

            var a_ = a.split('/'), b_ = b.split('/');
            var a1 = parseInt(a_[0]), a2 = a_.length > 1 ? parseInt(a_[1]) : 0;
            var b1 = parseInt(b_[0]), b2 = b_.length > 1 ? parseInt(b_[1]) : 0;

            if (a1 === b1) {
                return webgis.sortingAlg["default"](a2, b2);
            }

            a = a1; b = b1;
        } catch (e) {
            console.log(e);
        }

        return webgis.sortingAlg["default"](a, b);
    };
    this.sortingAlg["date_dd_mm_yyyy"] = function (a, b) {
        try {
            // zB: 04.06.2020 11:43:54
            a = a.trim();
            b = b.trim();

            var aPostfix = a.indexOf(' ') > 0 ? a.substr(a.indexOf(' ')) : '';
            var bPostfix = b.indexOf(' ') > 0 ? b.substr(b.indexOf(' ')) : '';

            var aParts = a.split(' ')[0].replaceAll('/', '.').replaceAll('-', '.').split('.');
            var bParts = b.split(' ')[0].replaceAll('/', '.').replaceAll('-', '.').split('.');

            if (aParts.length === 3) {
                a = aParts[2] + '.' + aParts[1] + '.' + aParts[0] + aPostfix;
            }
            if (bParts.length === 3) {
                b = bParts[2] + '.' + bParts[1] + '.' + bParts[0] + bPostfix;
            }
        } catch (e) {
            console.log(e);
        }

        //console.log('dates', a, b);
        return webgis.sortingAlg["default"](a, b);
    };
    this.const = {
        circleMarkerRadii: [100, 250, 500, 1000, 5000, 10000, 50000, 100000, 250000, 500000]
    };
    this.shareOptions = [
        {
            name: "E-Mail",
            img: function () { return webgis.css.imgResource('email-26.png', 'sharebuttons'); },
            share: function (url, subject) {
                url = 'mailto:?subject=Karte&body=' + webgis.encodeURI(url) + '&subject=' + webgis.encodeURI(subject);
                var win = window.open(url);
            },
            available: function () { return true; }
        },
        {
            name: "Kopieren (Zwischenablage)",
            img: function () { return webgis.css.imgResource('copy-26.png', 'sharebuttons'); },
            share: function (url, subject) {
                webgis.copyString(url);
            },
            available: function () { return true; }
        },
        {
            name: "QR-Code",
            img: function () { return webgis.css.imgResource('qrcode-26.png', 'sharebuttons'); },
            share: function (url, subject, qr_base64) {
                $('body').webgis_modal({
                    id: 'qr-code-dialog',
                    width: '330px',
                    height: '400px',
                    title: "Share: QR-Code",
                    onload: function ($content) {
                        $("<img>")
                            .css({ width: '290px', height: '290px' })
                            .attr('src', 'data:image/png;base64, ' + qr_base64)
                            .appendTo($content.css('text-align', 'center'));
                    }
                });
            },
            available: function () { return true; }
        },
        {
            name: "WhatsApp",
            img: function () { return webgis.css.imgResource('whatsapp-26.png', 'sharebuttons'); },
            share: function (url) { window.open('whatsapp://send?text=' + webgis.encodeURI(url)); },
            available: function () { return webgis.usability.socialShare.allowWhatsApp && webgis.isMobileDevice(); }
        },
        {
            name: "Facebook",
            img: function () { return webgis.css.imgResource('facebook-26.png', 'sharebuttons'); },
            share: function (url) { window.open('https://www.facebook.com/sharer/sharer.php?u=' + webgis.encodeURI(url)); },
            available: function () { return webgis.usability.socialShare.allowFacebook; }
        },
        {
            name: "Facebook Messenger",
            img: function () { return webgis.css.imgResource('fb-messenger-26.png', 'sharebuttons'); },
            share: function (url) { window.open('fb-messenger://share?text=' + webgis.encodeURI(url)); },
            available: function () { return webgis.usability.socialShare.allowFacebookMessenger && webgis.isMobileDevice(); }
        },
        {
            name: "Twitter",
            img: function () { return webgis.css.imgResource('twitter-26.png', 'sharebuttons'); },
            share: function (url) { window.open('https://twitter.com/intent/tweet?url=' + webgis.encodeURI(url)); },
            available: function () { return webgis.usability.socialShare.allowTwitter; }
        }
    ];
    this.sketchProperties = [];
    this.getSketchProperties = function (map) {
        var activeTool = map.getActiveTool ? map.getActiveTool() : null;
        if (activeTool != null) {
            var bbox = this.sketchProperties[activeTool.id];
            if (bbox)
                return bbox;
        }
        return this.sketchProperties["default"];
    };
    this._handleMarkerBubbleButtonClick = function (button) {
        var event = this._markerBubbleButtonClickEvents[button.id];
        if (event && event.map && event.marker && event.onclick) {
            event.onclick(event.map, event.marker);
        }
    };
    this._markerBubbleButtonClickEvents = [];
    this._appendMarkerBubbleButtonClickEvent = function (button, map, marker, onclick) {
        this._markerBubbleButtonClickEvents[button.id] = {
            map: map, marker: marker, onclick: onclick
        };
    };
    this._removeMarkerBubbleEvents = function (marker) {
        //console.log('_removeMarkerBubbleEvents')
        //console.log(this._markerBubbleButtonClickEvents);
        var _events = [];
        for (var i in this._markerBubbleButtonClickEvents) {
            var event = this._markerBubbleButtonClickEvents[i];
            if (event.marker != marker) {
                _events.push(this._markerBubbleButtonClickEvents[i]);
            }
        }
        this._markerBubbleButtonClickEvents = _events;
        //console.log(this._markerBubbleButtonClickEvents);
    };

    this.toWGS84 = function (crs, x, y) {
        if (this.mapFramework == "leaflet" && crs && crs.frameworkElement) {
            var latLng = crs.frameworkElement.unproject(L.point(x, y));
            return { lat: latLng.lat, lng: latLng.lng };
        }
        return null;
    };
    this.fromWGS84 = function (crs, lat, lng) {
        if (this.mapFramework == "leaflet" && crs && crs.frameworkElement) {
            var xy = crs.frameworkElement.project(L.latLng(lat, lng));
            return { x: xy.x, y: xy.y };
        }
        return null;
    };
    this.fromWGS84ToProj4 = function (srsId, p4Params, lat, lng) {
        var cacheId = srsId + p4Params;
        var crs = _crsCache[cacheId];
        if (!crs) {
            if (webgis.mapFramework == 'leaflet') {
                crs = new L.Proj.CRS('EPSG:' + srsId, p4Params, null);
                _crsCache[cacheId] = crs;
            }
        }
        if (crs) {
            var xy = crs.project(L.latLng(lat, lng));
            return { x: xy.x, y: xy.y };
        }
        return null;
    };
    this.complementWGS84 = function(crs, coords, worldXProp, worldYProp, lngProp, latProp) {
        worldXProp = worldXProp || 'X';
        worldYProp = worldYProp || 'Y';
        lngProp = lngProp || 'x';
        latProp = latProp || 'y';

        for (var i in coords) {
            var coord = coords[i];

            if (coord.hasOwnProperty(lngProp) && coord.hasOwnProperty(latProp))
                continue;

            var p = webgis.toWGS84(crs, coord[worldXProp], coord[worldYProp]);

            coord[lngProp] = p.lng;
            coord[latProp] = p.lat;

            if (!coord.srs) {
                coord.srs = crs.id;
            }
        }
    }
    this.complementProjected = function (crs, coords, worldXProp, worldYProp, lngProp, latProp) {
        worldXProp = worldXProp || 'X';
        worldYProp = worldYProp || 'Y';
        lngProp = lngProp || 'x';
        latProp = latProp || 'y';

        for (var i in coords) {
            var coord = coords[i];

            if (coord.hasOwnProperty(worldXProp) && coord.hasOwnProperty(worldYProp))
                continue;

            var p = webgis.fromWGS84(crs, coord[latProp], coord[lngProp]);
            coord[worldXProp] = p.x;
            coord[worldYProp] = p.y;

            coord.srs = crs.id;
        }
    };

    this.isTouchDevice = function () {
        return (
            'ontouchstart' in window // works on most browsers 
            || navigator.maxTouchPoints) // works on IE10/11 and Surface
            === true;
    };

    this.isMobileDevice = function () {
        try {
            var isMobile = webgis.is_iOS || webgis.is_Android;
            // device detection
            if (/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|ipad|iris|kindle|Android|Silk|lge |maemo|midp|mmp|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino/i.test(navigator.userAgent)
                || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(navigator.userAgent.substr(0, 4))) {
                isMobile = true;
            }
            return isMobile;
        } catch (e) { return false; }
    };

    this.isSafari = function () {
        var ua = navigator.userAgent.toLowerCase();
        if (ua.indexOf('safari') != -1) {
            if (ua.indexOf('chrome') > -1) {
                return false;
            } else {
                return true;
            }
        }

        return false;
    };

    this.isChrome = function () {
        if (this.isSafari())
            return false;

        return /Chrome/.test(navigator.userAgent) && /Google Inc/.test(navigator.vendor);
    };

    this.useMobileCurrent = function () {
        return this.isMobileDevice() && (Math.max(screen.width, screen.height) < 1024 || Math.min(screen.width, screen.height) < 768); // smaller than iPad
    };

    this.useMobile = this.isMobileDevice();
    this.userAgent = navigator.userAgent || navigator.vendor || window.opera;
    this.is_iOS = /iPad|iPhone|iPod/.test(this.userAgent) && !window.MSStream;   // https://stackoverflow.com/questions/9038625/detect-if-device-is-ios
    this.is_WindowsPhone = /windows phone/i.test(this.userAgent);
    this.is_Android = /android/i.test(this.userAgent) && !this.is_WindowsPhone;


    /*** Ajax & Diagnistic ***/
    this._sleep = function (milliseconds) {
        $.ajax({
            type: 'post',
            url: webgis.baseUrl + '/rest/sleep?milliseconds=100',
            async: false,
            data: webgis.hmac.appendHMACData({ f: 'json' }),
            success: function (result) {
            },
            error: function (err, textStatus, errorThrown) {
            }
        });
    };
    this._ajaxDiagnostic = [];
    this.ajaxSync = function (url, progress, data) {
        var ret = null;
        if (progress)
            webgis.showProgress(progress);
        //var FF = !(window.mozInnerScreenX == null);

        data = data || {};
        data.f = 'json';

        $.ajax({
            type: 'post',
            url: url,
            async: false,
            //xhrFields: (FF && (url.indexOf('https://localhost/') == 0 || url.indexOf('http://localhost/')==0)) ? null : this._ajaxXhrFields,  // Macht Probleme im FF (bei syncronen Calls und bei localhost!?)
            data: webgis.hmac.appendHMACData(data),
            success: function (result) {
                ret = result;
            },
            error: function (err, textStatus, errorThrown) {
                if (err.status == 200 && err.statusText.toLowerCase() == 'ok') { // if response Type is not json...
                    ret = err.responseText;
                }
                else {
                    ret = '';
                    webgis.alert('Error: ' + textStatus + ' ' + errorThrown, 'error');
                }
            }
        });
        //while (true) {
        //    webgis._sleep(100);
        //    if(ret!=null)
        //        break;
        //}
        if (progress) {
            webgis.hideProgress(progress);
            webgis._ajaxDiagnostic[progress] = ret;
        }
        return ret;
    };
    this.ajax = function (options) {
        //if (webgis.options.prefer_fetch_api === true) {
        //    webgis.fetch(options);
        //    return;
        //}

        //console.log('ajax', options);
        var success_function = options.success;
        var error_function = options.error;
        var progress_message = options.progress;
        var cancelTracker = new webgis.cancelTracker();

        if (progress_message) {
            webgis.showProgress(progress_message, null, options.cancelable ? cancelTracker : null);
        }

        $.ajax({
            type: options.type,
            url: options.url,
            data: options.data,
            xhrFields: options.xhrFields,
            success: function (result) {
                if (progress_message) {
                    webgis.hideProgress(progress_message);
                    webgis._ajaxDiagnostic[progress_message] = result;
                }
                if (!cancelTracker.isCanceled()) {

                    if (result.success === false && result.exception_type == "notauthorizedexception") {
                        webgis.handleNotAuthorizedException();
                        return;
                    }

                    if (success_function)
                        success_function(result);
                }
            },
            error: function (err, textStatus, errorThrown) {
                if (progress_message) {
                    webgis.hideProgress(progress_message);
                    webgis._ajaxDiagnostic[progress_message] = errorThrown;
                }
                if (error_function) {
                    error_function(err);
                }
                else {
                    if (err.status == 200 && err.statusText.toLowerCase() == 'ok') // if response Type is not json...
                        var msg = err.responseText;
                    else
                        webgis.alert('Error: ' + textStatus + ' ' + errorThrown, 'error');
                }
            }
        });
    };
    this.fetch = function (options) {
        //console.log('fetch', options);
        var success_function = options.success;
        var error_function = options.error;
        var progress_message = options.progress;
        var cancelTracker = new webgis.cancelTracker();

        if (progress_message) {
            webgis.showProgress(progress_message, null, options.cancelable ? cancelTracker : null);
        }

        // send data as form data
        let formData = undefined;

        // default is GET?
        const type = options.type ? options.type.toUpperCase() : "GET";

        // Nur für GET-Anfragen: Daten in URL-Parameter umwandeln
        let requestUrl = options.url;
        if (type === 'GET' && options.data) {
            const params = new URLSearchParams(options.data);
            requestUrl += (requestUrl.indexOf('?') > 0 ? '&' : '?') + `${params.toString()}`;
        } else {
            formData = new FormData();
            for (const key in options.data) {
                if (options.data.hasOwnProperty(key)
                    && options.data[key] !== null
                    && options.data[key] !== undefined) {
                    formData.append(key, options.data[key]);
                }
            }
        }

        //console.log('fetch', requestUrl, formData);

        fetch(requestUrl, {
            method: type,
            body: formData,
            credentials: options.xhrFields && options.xhrFields.withCredentials ? 'include' : 'same-origin'
        })
            .then(response => response.json())
            .then(result => {
                if (progress_message) {
                    webgis.hideProgress(progress_message);
                    webgis._ajaxDiagnostic[progress_message] = result;
                }
                if (!cancelTracker.isCanceled()) {

                    if (result.success === false && result.exception_type == "notauthorizedexception") {
                        webgis.handleNotAuthorizedException();
                        return;
                    }

                    if (success_function)
                        success_function(result);
                }
            })
            .catch(error => {
                console.log('error', error);
                if (progress_message) {
                    webgis.hideProgress(progress_message);
                    webgis._ajaxDiagnostic[progress_message] = error.message;
                }
                if (error_function) {
                    error_function(error);
                }
                else {
                    webgis.alert('Error: ' + error.message, 'error');
                }
            });
    };
    this.serviceInfo = function (service, customFormParameters) {
        var customParams = '';
        if (webgis.advancedOptions.get_serviceinfo_purpose) {

            if (customParams) {
                customParams += '&';
            }

            customParams += 'purpose=' + webgis.encodeURI(webgis.advancedOptions.get_serviceinfo_purpose);
        }

        var data = customFormParameters || {};
        data.ids = service;  // Post Ids => can get a long string...

        return this.ajaxSync(this.baseUrl + '/rest/serviceinfo?' + customParams, 'Lade Service Information', data);
    };
    this.extentInfo = function (extent) {
        return this.ajaxSync(this.baseUrl + '/rest/extents/' + extent, 'Lade Extent Information');
    };
    this.crsInfo = function (crs) {
        return this.ajaxSync(this.baseUrl + '/rest/srefs/' + crs, 'Lade Projektions Information');
    };
    this.showCrsInfo = function (crsId) {
        var crs = webgis.crsInfo(crsId);
        if (crs) {
            this.alert("Projection Parameters (Json):\n" + JSON.stringify(crs, null, 2), crs.id + ": " + crs.name);
        }
    };
    this.serviceInfos = function (callback, onlyContainerServices) {
        var data = { f: 'json' };
        if (onlyContainerServices)
            data.containerservices = 'true';
        webgis.ajax({
            progress: 'Lade Service Infomationen',
            type: 'post',
            url: this.baseUrl + '/rest/services',
            data: webgis.hmac.appendHMACData(data),
            success: function (result) {
                if (callback)
                    callback(result.services, result.copyright);
            }
        });
    };
    this._toolInfos = [];
    this.toolInfos = function (tools, client, callback) {
        client = client || '';
        if (webgis._toolInfos[client]) {
            if (callback)
                callback(webgis._toolInfos[client]);
        }
        else {
            webgis.ajax({
                progress: 'Lade Werkzeug Informationen',
                type: 'post',
                url: this.baseUrl + '/rest/tools',
                data: webgis.hmac.appendHMACData({ f: 'json', client: client }),
                success: function (result) {
                    webgis._toolInfos[client] = result.tools;

                    // overriede custom properties
                    var overrideProperties = ["name", "container", "tooltip", "image", "cursor", "help_urlpath", "priority"]

                    let tools = result.tools;

                    for (let t in tools) {
                        let tool = tools[t];
                        tool.order = 0;
                        if (webgis.usability.toolProperties[tool.id]) {
                            for (let p in overrideProperties) {
                                let prop = overrideProperties[p];
                                tool[prop] = webgis.usability.toolProperties[tool.id][prop] || tool[prop];
                            }
                        }
                        webgis.l10n.set(tool.id, tool.name);
                        webgis.l10n.set(tool.id + ".tooltip", tool.tooltip);
                    }

                    //console.log(tools);

                    if (callback)
                        callback(tools);
                }
            });
        }
    };
    this.toolInfoLoaded = function (client) {
        return webgis._toolInfos[client || null] ? true : false;
    };
    this.loadTool = function (id, callback) {
        id = webgis.compatiblity.toolId(id);
        var toolInfos = webgis.toolInfos({}, null, function (tools) {
            for (var t in tools) {
                if (tools[t].id == id) {
                    callback(tools[t]);
                    break;
                }
            }
        });
        return null;
    };
    this.getToolInfo = function (id, client) {
        id = webgis.compatiblity.toolId(id);
        client = client || '';
        for (var i in this._toolInfos[client]) {
            if (this._toolInfos[client][i].id === id) {
                return this._toolInfos[client][i];
            }
        }
        return null;
    };
    this.css = {
        img: function (url) {
            return webgis.baseUrl + '/content/api/img/' + url;
        },
        imgResource: function (img, sub) {
            if (!sub)
                sub = '';

            //console.log('imgResource:', img, webgis.baseUrl);

            if (img.indexOf('data:') == 0)
                return img;

            if (img.toLowerCase().indexOf('http://') == 0 || img.toLowerCase().indexOf('https://') == 0) {
                if (img.toLowerCase().indexOf('/rest/toolresource/') < 0)
                    return img;
                else
                    return img + "?company=" + webgis.company + (sub ? "&sub=" + sub : "") + "&colorscheme=" + webgis.colorScheme + "&v=" + webgis.api_version;
            }
            if (img.toLowerCase().indexOf("/toolresource/") >= 0)
                return webgis.baseUrl + '/' + img + "?company=" + webgis.company + (sub ? "&sub=" + sub : "") + "&colorscheme=" + webgis.colorScheme + "&v=" + webgis.api_version;
            else if (img.indexOf('/') >= 0)
                return webgis.baseUrl + '/' + img;
            return webgis.baseUrl + '/rest/imageresource/' + img.replace('.', '~') + "?sub=" + sub + "&company=" + webgis.company + "&colorscheme=" + webgis.colorScheme + "&v=" + webgis.api_version;
        },
        changeColorScheme: function (colorScheme) {
            function replaceSchemeParameter(src) {
                if (src.indexOf('&colorscheme=') > 0) {
                    src = src.replaceAll('&colorscheme=' + webgis.colorScheme, '&colorscheme=' + colorScheme);
                }

                return src;
            };

            $('body')
                .find('img')
                .each(function (i, img) {
                    img.src = replaceSchemeParameter(img.src);
                });
            $('body')
                .find('div')
                .each(function (i, div) {
                    var style = div.style // inline styles only
                    if (style && style.backgroundImage) {
                        style.backgroundImage = replaceSchemeParameter(style.backgroundImage);
                    }
                });

            webgis.colorScheme = colorScheme;
        },
        getColorScheme: function () { return webgis.colorScheme || 'default'; },
        legendImage: function (serviceId, layerId, value) {
            return webgis.baseUrl + '/rest/services/' + serviceId + '/getlegendlayeritem?f=bin&layer=' + layerId + '&value=' + value;
        }
    };
    this.implementEventController = function (obj) {
        obj.events = new webgis.eventController(obj);
    };
    this.tools = {};
    this._handleNotAuthorizedException = true;
    this.handleNotAuthorizedException = function () {
        // damit der Dialog nur einmal aufgehen, auch wen duch Kartenaufbau mehrer Meldungen kommen
        if (!this._handleNotAuthorizedException)
            return;

        this._handleNotAuthorizedException = false;
        webgis.confirm({
            title: 'Error',
            height: '270px',
            iconUrl: webgis.css.imgResource('error-100.png'),
            message: 'Ein Fehler bei der Authentifizierung ist aufgetreten. Die Karte muss neu geöffnet werden, um diesen Fehler zu beheben.',
            okText: 'Karte neu öffnen',
            cancelText: 'Nein danke',
            onOk: function () {
                window.location.reload();
            },
            onCancel: function () {
                webgis._handleNotAuthorizedException = true;
            }
        });
    };
    /***** Projecting ****/
    this._srefs = [];
    this.registerCRS = function (id) {
        if (!this._srefs[id]) {
            console.log('register srs: ' + id);
            var srefInfo = this.ajaxSync(this.baseUrl + '/rest/srefs/' + id, 'Lade CRS Information');
            if (webgis.mapFramework == 'leaflet') {
                var sref = new L.Proj.CRS('EPSG:' + id, srefInfo.p4);
                this._srefs[id] = sref;
            }
        }
    };
    this.project = function (id, lnglat) {
        if (id && this._srefs[id]) {
            if (webgis.mapFramework == 'leaflet') {
                var result = this._srefs[id].projection.project(L.latLng(lnglat[1], lnglat[0]));
                return [result.x, result.y];
            }
        }
        return lnglat;
    };
    this.unproject = function (id, xy) {
        if (id && this._srefs[id]) {
            if (webgis.mapFramework == 'leaflet') {
                var result = this._srefs[id].projection.unproject(L.point(xy[0], xy[1]));
                return [result.lng, result.lat];
            }
        }
        return xy;
    };
    /***** GeoReferencing *****/
    this.geoReference = function (term, callback, services, categories) {
        var data = webgis.hmac.appendHMACData({ term: term });
        if (services)
            data.services = services;
        if (categories)
            data.categories = categories;
        webgis.ajax({
            type: 'get',
            url: webgis.baseUrl + '/rest/georeference',
            //data: { term: term },
            data: data,
            success: callback
        });
    };
    /***** Plugins *****/
    this.plugins = [];
    this.addPlugin = function (plugin) {
        this.plugins.push(plugin);
    };
    this._executeOnInit = function () {
        for (var i in this.plugins) {
            var plugin = this.plugins[i];
            if (plugin.onInit)
                plugin.onInit();
        }
    };
    this._executeOnMapCreated = function (map, container) {
        for (var i in this.plugins) {
            var plugin = this.plugins[i];
            if (plugin.onMapCreated)
                plugin.onMapCreated(map, container);
        }
    };
    this.encodeURI = function (v) {
        if (v && v.indexOf) {
            while (v.indexOf('<') != -1)
                v = v.replace('<', '&lt;');
            while (v.indexOf('>') != -1)
                v = v.replace('>', '&gt;');
        }
        v = encodeURI(v);
        while (v.indexOf('&') != -1)
            v = v.replace('&', '%26');
        while (v.indexOf('+') != -1)
            v = v.replace('+', '%2b');
        while (v.indexOf('#') != -1)
            v = v.replace('#', '%23');
        while (v.indexOf('=') != -1)
            v = v.replace('=', '%3d');
        return v;
    };
    this.encodeXPathString = function (s) {
        s = s
            .replaceAll('\\', '\\\\')
            .replaceAll('"', '\\"')
            .replaceAll("'", "\\'")
            .replaceAll("&", "\u0026");

        //console.log('xpathencoded', s);
        return s;
    };
    this.encodeHtmlString = function (str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    };
    this._autocompleteMapItem = function (map, item) {
        if (map && item.coords) {
            if (item.bbox) {
                map.zoomToBoundsOrScale(item.bbox, webgis.usability.zoom.minFeatureZoom);
            } else {
                map.zoomToBoundsOrScale([item.coords[0] - 0.05, item.coords[1] - 0.05, item.coords[0] + 0.05, item.coords[1] + 0.05], 1000);
            }

            map.removeMarkerGroup('search-temp-marker');
            var marker = map.toMarkerGroup('search-temp-marker', map.addMarker({
                lat: item.coords[1], lng: item.coords[0],
                text: webgis.autocompleteItem2Html(item),
                openPopup: true,
                buttons: [{
                    label: 'Marker entfernen',
                    onclick: function (map, marker) { map.removeMarker(marker); }
                }]
            }));
            // event -> result geoJson with bounds -> same as after_showqueryresult
            map.events.fire('after_autocomplete_mapitem', map, {
                bounds: [item.coords[0], item.coords[1], item.coords[0], item.coords[1]],
                features: [{
                    geometry: { type: 'point', coordinates: item.coords },
                    properties: {
                        autocomplete_label: item.label,
                        autocomplete_subtext: item.subtext
                    }
                }]
            });
        }
    };
    this._appendAutocomplete = function ($parent) {
        if (!$parent)
            return;
        if ($.fn.typeahead) {
            if ($parent.find('.webgis-autocomplete').addBack('.webgis-autocomplete').length == 0) {
                return;
            }
            $parent.find('.webgis-autocomplete').addBack('.webgis-autocomplete').each(function (i, e) {
                if ($(e).hasClass('tt-input')) {
                    return;
                }
                $(e).on({
                    'typeahead:cursorchange': function (e, item) {
                        if ($(this).data('map')) {
                            webgis._autocompleteMapItem($(this).data('map'), item);
                            this._focused = item;
                        }
                        else if (typeof item === 'string') {
                            this._focused = item;
                        }
                    },
                    'typeahead:select': function (e, item) {
                        if ($(this).data('autocomplete-onselect')) {
                            $(this).data('autocomplete-onselect')(this, item);
                        } else if (typeof item === 'string') {
                            $(this).typeahead('val', item);
                        }
                    },
                    'typeahead:open': function (e) {
                        webgis._autocompleteFitMenu(this);
                    },
                    'keyup': function (e) {
                        this._focused = null;
                        if (e.keyCode === 13) {
                            var $this = $(this), dataItems = [];

                            $this.closest('.twitter-typeahead').find('.tt-suggestion').each(function (i, e) {   // run over suggestions
                                var item = $(e).data('item');
                                if (item && item.id && item.id.indexOf('#') !== 0) {   // ignore metadata items (id starts with '#')
                                    dataItems.push(item);
                                }
                            });

                            // on enter: select first... on enter
                            if (dataItems.length > 1 && webgis.usability.quickSearch.selectFirstOnEnter === true) {
                                dataItems = [dataItems[0]];
                            }

                            // if only one left => do like it is selected => get original ...
                            if (dataItems.length === 1 && $this.data('autocomplete-onselect')) {
                                $(this).data('autocomplete-onselect')(this, dataItems[0]);
                            } else if ($this.data('autocomplete-onenter')) {
                                $this.data('autocomplete-onenter').apply(this, e);
                            }

                            $(this).typeahead('close');
                        }
                    },
                    'focus': function (e) {
                        //var $this = $(this);
                        //if (Array.isArray($this.data('depends_on'))) {
                        //    $this.val('~' + $this.val());
                        //    $this.trigger('input');
                        //    $this.val($this.val().substr(1));
                        //    $this.trigger('input');
                        //}
                    }
                })
                    .typeahead({
                        hint: false,
                        highlight: false,
                        minLength: $(e).attr("data-minlength") ? parseInt($(e).attr("data-minlength")) : 0,
                    }, {
                        limit: Number.MAX_VALUE,
                        async: true,
                        displayKey: 'label',
                        source: function (query, processSync, processAsync) {

                            if (query === '~') {
                                console.log('dummy request');
                                return;  // dummy call to refresh empty input boxes with dependencies (see webgis_topbar.js line ~312)
                            }

                            var $element = $(this.$el[0].parentElement.parentElement).children(".webgis-autocomplete").first(); // Ugly!!!
                            //console.log($element);
                            var $ctrl = $element; // $(e); //$(this).find('.webgis-autocomplete');
                            var s = $ctrl.attr('data-source'); //.dataset.source;
                            $ctrl.parent().parent().find('.webgis-input,.webgis-autocomplete-parameter').each(function (j, h) {
                                s += '&' + (h.name || h.id) + '=' + encodeURIComponent($(h).val());
                            });
                            if ($(e).attr('data-search-categories')) {
                                s += '&categories=' + encodeURIComponent($(e).attr('data-search-categories'));
                            }
                            if (webgis.advancedOptions && webgis.advancedOptions.quicksearch_custom_parameters && s.indexOf('/search/')>0) {
                                s += '&' + webgis.advancedOptions.quicksearch_custom_parameters;
                            }
                            s += webgis.hmac.urlParameters() + '&_autocomplete_item_style=label';
                            return webgis.ajax({
                                url: s,
                                type: 'get',
                                data: { term: query },
                                success: function (data) {
                                    //console.log(data);
                                    //data = data.slice(0, 12);  // should be sliced on the server
                                    if ($element.data('data-append-items')) {
                                        for (var i in $element.data('data-append-items')) {
                                            try {
                                                var clone = Object.assign({}, $element.data('data-append-items')[i]);
                                                clone.value = (clone.value || '').replaceAll('{0}', query);
                                                clone.label = (clone.label || '').replaceAll('{0}', query);
                                                data.push(clone);
                                            } catch (e) { }
                                        }
                                    }
                                    processAsync(data);
                                },
                                error: function () {
                                }
                            });
                        },
                        templates: {
                            empty: [
                                // Die Anzeige "Keine Ergebnisse gefunden, nervt bei Projekte laden, neue Darstellungsvariante 
                                // Frage: braucht man das überhaupt irgendwo?
                                //'<div class="tt-suggestion">',
                                //'<div class="tt-content">Keine Ergebnisse gefunden</div>',
                                //'</div>'
                            ].join('\n'),
                            suggestion: function (item) {
                                //console.log(item);
                                var html = webgis.autocompleteItem2Html(item);
                                return $(html).data('item', item);
                                //return "<div><div class='tt-content'><img class='tt-img' src='" + data.thumbnail + "' /><strong>" + data.label + "<strong><br/><span>" + data.subtext + "</span></div>";
                                //return $(html).data('tt-item', item).on('mouseenter', function () {
                                //    var tt = $(this).closest('.twitter-typeahead').find('.tt-input');
                                //    var item = $(this).data('tt-item');
                                //    $(tt).trigger('typeahead:cursorchange', item);
                                //});
                            }
                        }
                    });
                $(e).closest('.twitter-typeahead')
                    .css('display', $(e).data('css-display') || 'block')
                    // Will da Kunde unbedingt haben: Beim Mouseover soll schon gezoomt werden!!
                    .on('mouseenter', '.tt-suggestion', function () {
                        var $input = $(this).closest('.twitter-typeahead').find('.webgis-autocomplete');

                        if ($input.data('map') && $(this).data('item')) {
                            var map = $input.data('map'), item = $(this).data('item');
                            webgis._autocompleteMapItemTimer.Start({ map: map, item: item });
                            $input.get(0)._focused = item;
                        }
                    })
                    .on('mouseleave', '.tt-menu', function () {
                        //console.log('leave');
                        webgis._autocompleteMapItemTimer.Stop();

                        var $input = $(this).closest('.twitter-typeahead').find('.webgis-autocomplete');
                        $input.get(0)._focused = null;
                    });
                //$(e).closest('.twitter-typeahead').find('.tt-menu').css('margin-top', $(e).height()+10);
            });
        }
        else if ($.fn.autocomplete) {
            if ($parent.find('.webgis-autocomplete').addBack('.webgis-autocomplete').length === 0)
                return;
            $parent.find('.webgis-autocomplete').addBack('.webgis-autocomplete').each(function (i, e) {
                $(e).autocomplete({
                    create: function () {
                        $(this).data('ui-autocomplete')._renderItem = function (ul, item) {
                            var $li = $("<li class='ui-menu-item'>").data("item.autocomplte", item).attr('data-value', item.value).appendTo(ul);
                            var $a = $("<a>").appendTo($li);
                            if (item.thumbnail || item.subtext) {
                                //$("<table class='webgis-autocomplete-item'><tr><td class='image-cell'>" +
                                //    (item.thumbnail ? "<img style='max-height:40px' src='" + item.thumbnail + "' /></td><td class='text-cell'>" : "") +
                                //    (item.label ? "<div><strong>" + item.label + "</strong></div>" : "") +
                                //    (item.subtext && item.label ? "<span class='subtext'>" + item.subtext + "</span>" : (item.subtext ? "<span>" + item.subtext + "</span>" : "")) +
                                //    (item.link ? "<div class='webgis-autocomplete-item-details'><a href='" + item.link + "' target='_blank'>Details...</a></div>" : "") +
                                //    "</td></tr></table>").appendTo($a);
                                //item._html = $a.html();
                                $(webgis.autocompleteItem2Html(item)).appendTo($a);
                            }
                            else {
                                $a.html(item.label);
                            }
                            return $li;
                        };
                    },
                    search: function (event, ui) {
                        var s = this.getAttribute('data-source'); //.dataset.source;
                        $(e.parentNode).find('.webgis-input,.webgis-autocomplete-parameter').each(function (j, h) {
                            s += '&' + (h.name || h.id) + '=' + encodeURIComponent($(h).val());
                        });
                        if ($(e).attr('data-search-categories'))
                            s += '&categories=' + encodeURIComponent($(e).attr('data-search-categories'));
                        s += webgis.hmac.urlParameters();
                        $(this).autocomplete("option", "source", s);
                        this._focused = null;
                    },
                    open: function () {
                        $(this).autocomplete('widget').css('z-index', 100);
                        return false;
                    },
                    focus: function (event, ui) {
                        if (ui.item.coords && $(this).data('map')) {
                            webgis._autocompleteMapItem($(this).data('map'), ui.item);
                            this._focused = ui.item;
                        }
                        else {
                            this._focused = null;
                        }
                    },
                    select: function (event, ui) {
                        if ($(this).data('autocomplete-onselect')) {
                            $(this).data('autocomplete-onselect')(this, ui.item);
                        } else if (typeof ui.item === 'string') {
                            $(this).val(ui.item);
                        }
                    },
                    minLength: $(e).attr("data-minlength") ? parseInt($(e).attr("data-minlength")) : 1
                });
            })
                .focus(function () {
                    try { // falls minLength==0 schon beim anklicken suchen!!
                        var minLength = parseInt($(this).attr('data-minlength'));
                        if (minLength <= $(this).val().length)
                            $(this).autocomplete('search', $(this).val());
                    }
                    catch (e) { }
                });
        }
    };
    this._triggerAutocomplete = function (target) {
        if ($.fn.typeahead) {
            $(target).trigger('input');
        }
        else if ($.fn.autocomplete) {
            $(target).autocomplete('search');
        }
    };
    this._autocompleteFitMenu = function (target) {
        if ($.fn.typeahead) {
            new webgis.timer(function (target) {
                var $this = $(target);
                var $tt = $this.closest('.twitter-typeahead');
                var $menu = $tt.find('.tt-menu');
                var $parent = $this.data('control-parent-selector') ? $this.closest($this.data('control-parent-selector')) : $this;
                $menu.css({
                    //display: 'block',
                    top: $parent.offset().top + $parent.outerHeight() - $(document).scrollTop(),
                    left: $parent.offset().left,
                    width: $parent.outerWidth(),
                    position: 'fixed',
                    maxHeight: 'calc(100% - ' + ($parent.offset().top + $parent.outerHeight() - $(document).scrollTop() + 80) + 'px)',
                    overflow: 'auto',
                    zIndex: 99999999
                });
            }, 300, target).Start();
        }
    };
    this.autocompleteItem2Html = function (item) {
        var html = '';

        if (item.id && item.id.indexOf("#metadata") === 0) {
            var metadataType = item.id.length >= 10
                ? item.id.substr(10)
                : 'default';

            if (webgis.usability &&
                webgis.usability.quickSearch &&
                webgis.usability.quickSearch.displayMetadata &&
                webgis.usability.quickSearch.displayMetadata[metadataType] === false) {
                return "<div style='display:none'></div>";
            }
        }

        if (item.thumbnail || item.subtext) {
            html = "<table class='tt-suggestion'><tr><td class='tt-img'>" +
                (item.thumbnail ? "<img style='max-height:40px' src='" + item.thumbnail + "' /></td><td class='tt-content'>" : "") +
                (item.do_you_mean ? "<div>Meinten Sie?</div>" : "") +
                (item.label ? "<div><strong>" + webgis.encodeUntrustedHtml(item.label) + "</strong></div>" : "") +
                (item.subtext && item.label ? "<span class='subtext'>" + webgis.encodeUntrustedHtml(item.subtext).replaceAll('\n', '<br/>') + "</span>" : (item.subtext ? "<span>" + webgis.encodeUntrustedHtml(item.subtext).replaceAll('\n', '<br/>') + "</span>" : "")) +
                (item.link ? "<div class='webgis-autocomplete-item-details'><a href='" + item.link + "' target='_blank'>Details...</a></div>" : "") +
                "</td></tr></table>";
        }
        else {
            html = "<div class='tt-suggestion tt-content' style='text-align:left'>" + webgis.encodeUntrustedHtml(item.label ? item.label : item) + "</div>";
        }
        return html;
    };
    this.removeAutoCompleteMapItem = function (map) {
        webgis._autocompleteMapItemRemoveTimer.Start(map);
    };
    this.scrollTo = function ($parent, $childElement) {
        if ($childElement && $childElement.length > 0) {
            webgis.delayed(function () {
                var $first = $($childElement[0]);

                var $holder = $first.closest('table');
                if ($holder.length === 0) {
                    $holder = $first.closest('.webgis-geojuhu-results');
                }

                if ($holder.length > 0) {
                    var scroll = $first.offset().top - $holder.offset().top - 34;
                    $parent.animate({
                        scrollTop: scroll
                    }, 200);
                }
            }, 500);
        }
    };
    this.bindMarkerPopup = function (marker, content, onPopupOpen) {
        if (webgis.useMobileCurrent()) {
            marker.on('click', function () {
                $('body').webgis_modal({
                    title: '',
                    onload: function ($content) {
                        $content.css('padding', '10px');
                        if (onPopupOpen) {
                            content = onPopupOpen(marker);
                        }
                        $content.html(content);
                    },
                    width: '90%', height: '90%'
                });
            });
        }
        else {
            marker.bindPopup(content, {
                maxWidth: "100%"
            });
            if (onPopupOpen) {
                marker.on('popupopen', function (e) {
                    e.popup.setContent(onPopupOpen(marker));
                });
            }
        }
    };
    this.bindFeatureMarkerPopup = function (map, marker, feature, queryToolId) {
        var max = 12, linebreak = false;
        if (webgis.useMobileCurrent()) {
            max = 3;
            linebreak = true;
        }
        var preview = map.ui.featureResultTable(feature, max, linebreak);
        if (preview.endsWith("..."))
            preview += "<br/><button class='webgis-button' onclick=\"webgis.showFeatureResultModal('" + map.id + "','" + feature.oid + "')\">Mehr...</button>";
        var content = preview != "" ? preview : map.ui.featureResultTable(feature);

        if (webgis.usability.useMarkerPopup === true) {
            marker.bindPopup(content, { autoPanPaddingTopLeft: L.point(40, 38), maxWidth: "100%" });
        }
        else {
            marker.on('click', function (e) {
                var $tabControl = map.ui.getQueryResultTabControl();
                if ($tabControl === null || this._showFeatureResultsModal === true) {
                    webgis.showFeatureResultModal(map.guid, feature.oid, queryToolId);
                } else {
                    var $currentTabContent = $tabControl.webgis_tab_control('currentContent');
                    $currentTabContent.webgis_queryResultsTable('selectRow', { map: map, dataId: feature.oid, scrollTo: e.suppressScrollResultRowTo !== true });
                }
                map.queryResultFeatures.tryHighlightMarker(feature.oid, true); 

                if (webgis.usability.highlightFeatureOnMarkerClick) {
                    map.queryResultFeatures.tryHighlightFeature(feature);
                }

                map.events.fire('onmarkerclick', map, feature);
            });
        }
    };

    this.showFeatureResultDetails = function (mapGuid, url, callback) {
        var map = this.getMapByGuid(mapGuid);
        if (!map || !map.queryResultFeatures) {
            webgis.alert('Details können leider nicht abgefragt werden!', "error");
        }
        webgis.ajax({
            url: webgis.baseUrl + url,
            data: webgis.hmac.appendHMACData({}),
            success: function (result) {
                if (result.features && result.features.length === 1) {
                    if (callback) {
                        var title = '';

                        if (result.metadata &&
                            result.metadata.service_id &&
                            result.metadata.query_id) {
                            var service = map.getService(result.metadata.service_id);
                            if (service) {
                                var query = service.getQuery(result.metadata.query_id);
                                if (query) {
                                    title = query.name;
                                }
                            }
                        }

                        callback(true, result.features[0], title, result.metadata);
                        //callback(false, null);
                    } else {
                        map.queryResultFeatures.showFeatureTable(result.features[0]);
                    }
                } else {
                    if (callback) {
                        callback(false, null);
                    } else {
                        webgis.alert('Keine weiteren Details für dieses Objekt verfügbar', 'info');
                    }
                }
            }
        });
    };
    this.showFeatureResultModal = function (mapGuid, oid, queryToolId) {
        var map = this.getMapByGuid(mapGuid) || this.getMap(mapGuid);
        if (map) {
            if (queryToolId) {
                map.queryResultFeatures.showFeatureTable(oid, queryToolId);
            } else {
                map.queryResultFeatures.showFeatureTable(oid);
            }
        }
    };
    this.showProgress = function (message, elem, cancelTracker) {
        if (!$.fn.webgis_modalprogress)
            return;
        if (typeof elem === "string")
            elem += "#" + elem;
        if (!elem)
            elem = 'body';
        var options = { message: message, animate: false };
        if (cancelTracker) {
            options.cancelable = true;
            options.oncancel = cancelTracker.cancel;
        }
        $(elem).webgis_modalprogress(options);
    };
    this.hideProgress = function (message, elem) {
        if (!$.fn.webgis_modalprogress)
            return;
        if (typeof elem === "string")
            elem += "#" + elem;
        if (!elem)
            elem = 'body';
        $(elem).webgis_modalprogress('close', { message: message });
    };
    this.loadOptions = function (options) {
        if (typeof options == 'string') {
            var x = webgis.ajax({
                url: webgis.baseUrl + '/rest/getjsontemplate?name=' + options,
                async: false,
                dataType: 'json'
            }).responseText;
            options = eval("(" + x + ")");
            if (options.success == false) {
                if (options.exception)
                    webgis.alert('Exception:\n' + options.exception, 'error');
            }
        }
        return options || {};
    };
    this.guid = function () {
        function s4() {
            return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        }
        return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
            s4() + '-' + s4() + s4() + s4();
    };
    this.hash = function (dataString) {
        if (CryptoJS.SHA512) {
            return (webgis.cryptoJS || CryptoJS).SHA512(dataString).toString((webgis.cryptoJS || CryptoJS).enc.hex);
        } else if (CryptoJS.SHA256) {
            return (webgis.cryptoJS || CryptoJS).SHA256(dataString).toString((webgis.cryptoJS || CryptoJS).enc.hex);
        } else if (CryptoJS.SHA1) {
            return (webgis.cryptoJS || CryptoJS).SHA1(dataString).toString((webgis.cryptoJS || CryptoJS).enc.hex);
        }
    };
    this.validClassName = function (id) {
        return id.replace(/\./g, '-');
    };
    this.registerUIEvents = function (element) {
        var refreshAutocomplete = function () {
            //console.log('refresh autocomplete');
            $('input.webgis-autocomplete').each(function () {
                webgis._autocompleteFitMenu(this);
            });
        };
        if (!element) {
            $(window).resize(function () {
                refreshAutocomplete();
            });
            $(document).scroll(function () {
                refreshAutocomplete();
            });
            //$('.webgis-container').click(function () {
            //    // eg webgis-tags-combo-popup
            //    $('.webgis-hide-on-click-outside').css('display', 'none');
            //});
        }
        else {
            $(element).scroll(function () {
                refreshAutocomplete();
            }).find("*").scroll(function () {
                refreshAutocomplete();
            });
        }
    };
    this.copy = function (element, selector) {
        var $content = selector ? $(element).find(selector) : $(element);
        var val = $content.html();
        if (val.indexOf('<a ') == 0 && $content.find('a[href]').length > 0) {
            val = $content.find('a[href]').first().attr('href');
        }
        else {
            // replace whitespaces (&nbsp; -> space, ...)
            val = val
                .replaceAll('&nbsp;', ' ')
        }

        this.copyString(val);

        var css = {
            left: $content.offset().left,
            top: $content.offset().top,
            width: $content.outerWidth(),
            height: $content.outerHeight()
        };

        var $msg = $("<div>")
            .addClass('webgis-copy-message')
            .css(css)
            .text(webgis.l10n.get('copied'))
            .appendTo($('body'));

        webgis.delayed(function(args){
            args.elem.remove();
        },1000, { elem: $msg });
    };
    this.copyAll = function (parent, selector) {
        var vals = '';
        $(parent).find(selector).each(function (i, e) {
            var val = $(e).html();
            if (val.indexOf('<a ') == 0 && $content.find('a[href]').length > 0) {
                val = $content.find('a[href]').first().attr('href');
            }
            else {
                // replace whitespaces (&nbsp; -> space, ...)
                val = val
                    .replaceAll('&nbsp;', ' ')
            }

            if (vals) vals += '\t';  // tab => to insert more cells in ms excel
            vals += val;
        });
      
        this.copyString(vals);
    };
    this.copyString = function (val) {
        if (val) {
            var $temp = $("<input>")
                .css({
                    height: '0px',
                    position: 'absolute',
                    left: '-1000px',
                    top: '-1000px'
                })
                .appendTo('body');

            $temp.val(val).select();
            document.execCommand("copy");
            $temp.remove();
        }
    };
    this.iFrameDialog = function (url, title) {
        $('body').webgis_modal({
            id: 'webgis-iframe-dialog',
            title: title,
            titleButtons: [{
                img: webgis.css.imgResource('open-in-window_26.png',''),
                url: url,
                onClick: function (button) {
                    window.open(button.url, '_blank');
                    $('body').webgis_modal('close', { id: 'webgis-iframe-dialog' });
                }
            }],
            onload: function ($content) {
                $("<iframe>")
                    .addClass('webgis-dialog-iframe')
                    .attr('src', url)
                    .appendTo($content);
            }
        })
    };
    this.modalDialog = function (title, onload, onclose) {
        $('body').webgis_modal({
            id: webgis.guid(),
            title: title,
            onload: function ($content) {
                if (onload) {
                    onload($content);
                }
            },
            onclose: function () {
                if (onclose)
                    onclose();
            }
        });
    };
    this.initialParameters = {};
    this.checkResult = function (result) {
        if (result.success == false && result.exception) {
            webgis.alert(result.exception, 'error');
            return false;
        }
        return true;
    };

    this.triggerEvent = function (selector) {
        $(selector).trigger('click');
    };

    /***** History (Back-Button) **********/
    var _nextHash = 0;
    var _hashDirectory = [];
    var _handleHashChange = function (e) {
        webgis.goBack();
    };
    var _bindHistoryTimer = null;
    this.setHistoryItem = function (button, content) {
        //console.log('setHistoryItem', button, content);
        if (webgis.usability.enableHistoryManagement === false)
            return;
        if (!button || button.length === 0)
            return;

        if (_hashDirectory.length > 0) {
            for (var i in _hashDirectory) {
                if ($(button).get(0) === _hashDirectory[i].button.get(0))
                    return;
            }
        }

        if (_bindHistoryTimer == null) {
            _bindHistoryTimer = new webgis.timer(function () {
                //$(window).unbind('hashchange', _handleHashChange);
                $(window).bind('hashchange', _handleHashChange);
            }, 1000);
        }

        // 
        // Wenn sich im Content ein IFrame befindet, sollte man nach dem Klicken auf den Button keine window.history.back(); ausführen
        // Weil man auch im IFrame navigieren kann, bewirtk das sonst, dass die Viewer nach dem Schließen des Dialogs neu geladen wird!!
        // Darum ist es besser, es bleibt open in de Url in HistoryItem stehen...
        //

        var suppressGoBack = false;
        if (content && $(content).find('iframe').length > 0) {
            console.log('dialog content contains iframe => suppresss window.history.back()');
            suppressGoBack = true;
        }

        var hash = (++_nextHash).toString();
        var $button = $(button)
            .attr('data-hash', hash)
            .attr('data-suppress-goback', suppressGoBack)
        //.click(function () {
        //    if ($(this).attr('data-suppress-goback') !== 'true') {
        //        window.history.back();
        //    }
        //});

        if (_hashDirectory.length === 0) {
            _hashDirectory.push({
                hash: hash,
                button: $button
            });
            $(window).unbind('hashchange', _handleHashChange);
            window.location.hash += (window.location.hash.length > 1 ? ',' : '') + hash;
            _bindHistoryTimer.start(); // Verzögert ausführen
        } else {
            $button.addClass('webgis-display-cleanup')
        }
    };
    this.removeHistoryItem = function (button) {
        // console.log('removeHistoryItem', button);
        if (webgis.usability.enableHistoryManagement === false)
            return;
        if (!button || button.length === 0)
            return;
        if (_hashDirectory.length == 0 || $(button).get(0) !== _hashDirectory[_hashDirectory.length - 1].button.get(0))
            return;

        //var last = _hashDirectory.splice(_hashDirectory.length - 1, 1);
        //$(window).unbind('hashchange', _handleHashChange);
        //window.history.back();
        //_bindHistoryTimer.start(); // Verzögert ausführen
    };
    this.goBack = function () {
        // console.log('goBack');
        if (_hashDirectory.length === 0)
            return;

        var last = _hashDirectory.splice(_hashDirectory.length - 1, 1);

        if (last && last.length === 1 && last[0].button) {
            last[0].button.attr('data-suppress-goback', 'true');
            last[0].button.trigger('click');
        }

        return true;
    };

    /***** Sorting/Features ********/
    this.sortFeatures = function (featuresCollection, cols) {  // Fast
        var features = featuresCollection.features, c, to_c = cols.length, o, x, y, dir;
        var metadata = featuresCollection.metadata;

        if (!cols || cols.length === 0) {
            features.sort(function (a, b) {
                if (a._fIndex === b._fIndex)
                    return 0;
                if (a._fIndex < b._fIndex)
                    return -1;
                return 1;
            });
        } else {
            var colNames = [];
            for (c = 0; c < to_c; c++) {
                colNames.push(cols[c].indexOf("-") === 0 ? cols[c].substr(1) : cols[c]);
            }

            // sort unioned properties (feature.properties is an Array)
            for (var f in features) {
                var feature = features[f];

                if (!Array.isArray(feature.properties))
                    continue;

                for (c = 0; c < to_c; c++) {
                    dir = cols[c].indexOf("-") === 0 ? -1 : 1;
                    var colName = colNames[c];

                    var alg = null;
                    if (metadata && metadata.table_fields) {
                        var fieldMeta = $.grep(metadata.table_fields, function (f) { return f.name === colName });
                        fieldMeta = fieldMeta.length === 1 ? fieldMeta[0] : null;

                        if (fieldMeta && fieldMeta.sorting_alg) {
                            alg = webgis.sortingAlg[fieldMeta.sorting_alg];
                        }
                    }
                    alg = alg || webgis.sortingAlg["default"];

                    feature.properties = feature.properties.sort(function (a, b) {
                        // vorherige Spalten testen
                        var alreadyOrderd = false;

                        if (c > 0) {  // Wenn nicht alles davor gleich ist, dann nicht weiter sortieren
                            for (o = 0; o < c; o++) {
                                x = a[colNames[o]];
                                y = b[colNames[o]];
                                if (x !== y) {
                                    alreadyOrderd = true;
                                }
                            }
                        }
                        if (alreadyOrderd === true)
                            return 0;

                        x = a[colName];
                        y = b[colName];

                        if (x === y)
                            return 0;

                        return alg(x, y) * dir;
                    });
                }
            }

            // sort features
            for (c = 0; c < to_c; c++) {
                dir = cols[c].indexOf("-") === 0 ? -1 : 1;
                var colName = colNames[c];

                var alg = null;
                if (metadata && metadata.table_fields) {
                    var fieldMeta = $.grep(metadata.table_fields, function (f) { return f.name === colName });
                    fieldMeta = fieldMeta.length === 1 ? fieldMeta[0] : null;

                    if (fieldMeta && fieldMeta.sorting_alg) {
                        alg = webgis.sortingAlg[fieldMeta.sorting_alg];
                    }
                }
                alg = alg || webgis.sortingAlg["default"];

                //console.log(cols[c] + " " + dir);
                features.sort(function (a, b) {
                    // vorherige Spalten testen
                    var alreadyOrderd = false;

                    var aProperties = Array.isArray(a.properties) && a.properties.length > 0 ? a.properties[0] : a.properties;
                    var bProperties = Array.isArray(b.properties) && b.properties.length > 0 ? b.properties[0] : b.properties;

                    if (c > 0) {  // Wenn nicht alles davor gleich ist, dann nicht weiter sortieren
                        for (o = 0; o < c; o++) {
                            //var n2 = Math.abs(cols[o]);

                            x = aProperties[colNames[o]];
                            y = bProperties[colNames[o]];
                            if (x !== y) {
                                alreadyOrderd = true;
                            }
                        }
                    }
                    if (alreadyOrderd === true)
                        return 0;

                    x = aProperties[colName];
                    y = bProperties[colName];

                    if (x === y)
                        return 0;

                    //if (x < y)
                    //    return -1 * dir;
                    //return 1 * dir;
                    return alg(x, y) * dir;
                });
            }     
        }

        featuresCollection.features = features;

        return;
    };
    this._userPreferencedMinScale = 0;
    this.setUserPreferenceMinScale = function (scale) {
        webgis._userPreferencedMinScale = Math.max(scale, 0);
    };
    this.getUserPreferencedMinScale = function () { return webgis._userPreferencedMinScale; }
    this.resetUserPreferenceMinScale = function () { webgis._userPreferencedMinScale = 0; };
    this.featuresZoomMinScale = function (features) {
        var minScale = webgis.usability.zoom.minFeatureZoom;

        if (features && features.features) {
            for (var f in features.features) {
                minScale = Math.max(minScale, webgis.featureZoomMinScale(features.features[f]));
            }
        };

        //console.log('featuresZoomMinScale', minScale);
        return minScale;
    };
    this.featureZoomMinScale = function (feature) {
        var minScale =
            this._userPreferencedMinScale > 0 ?
                this._userPreferencedMinScale :
                feature &&
                    feature.properties &&
                    feature.properties._zoomscale &&
                    parseInt(feature.properties._zoomscale) > 1
                    ? parseInt(feature.properties._zoomscale)
                    : webgis.usability.zoom.minFeatureZoom;

        //console.log('minScale', minScale, this._userPreferencedMinScale);
        //if (webgis._userPreferencedMinScale > 0 && webgis._userPreferencedMinScale > minScale) {
        //    return webgis._userPreferencedMinScale;
        //}

        return minScale;
    };
    this.featureHasTableProperties = function (feature) {
        if (feature && feature.properties) {
            for (var p in feature.properties) {
                if (p.indexOf("_") !== 0) {
                    return true;
                }
            }
        };

        return false;
    };
    $.firstOrDefault = function (array) {
        if (!array || array.length === 0)
            return null;

        return array[0];
    };

    this.getUrlParameter = function (parameterName) {
        var urlParams = new URLSearchParams(window.location.search);
        return urlParams.get(parameterName);
    };

    this.addTitleToEllipsis = function (maxWidthFunc) {
        $('.webgis-text-ellipsis-pro.check-for-title').each(function (i, element) {
            var $element = $(element);

            $element.removeClass('check-for-title');

            $(element).hover(function () {
                var $element = $(this);
                if ($element.hasClass('hover-checked')) {
                    return;
                }

                var $element = $(this).addClass('hover-checked');

                var $clone = $element
                    .clone()
                    .css({ display: 'inline', width: 'auto', visibility: 'hidden', fontSize: $element.css('font-size'), fontFamily: $element.css('font-family') })
                    .appendTo('body');

                if ($clone.width() > $element.width()) {
                    //console.log('ellispisis found', $clone.width(), $element.text());
                    $element.attr('title', $element.text());
                }

                $clone.remove();
            });
        });
    };
};

//pads left
String.prototype.lpad = function (padString, length) {
    var str = this;
    while (str.length < length)
        str = padString + str;
    return str;
};
//pads right
String.prototype.rpad = function (padString, length) {
    var str = this;
    while (str.length < length)
        str = str + padString;
    return str;
};
String.prototype.endsWith = function (suffix) {
    return this.indexOf(suffix, this.length - suffix.length) !== -1;
};
String.prototype.replaceAll = function (search, replacement) {
    var target = this;
    try {
        //return target.replace(new RegExp(search, 'g'), replacement);
        return target.replace(new RegExp(search.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&'), 'g'), replacement);
    }
    catch (e) {
        return target;
    }
};
String.prototype.highlightText = function (words) {
    if (typeof (words) === 'string') {
        words = [words];
    }
    var allMatches = [];
    var text = this;

    for (var w in words) {
        var word = words[w];
        if (!word)
            continue;

        var regEx = new RegExp(word/*.replace(/[-\/\\^$*+?.()|[\]{}]/g)*/, 'ig'), match;   // 'ig' => case insensitiv

        while ((match = regEx.exec(text)) !== null) {
            if ($.inArray(match[0], allMatches) < 0) {
                allMatches.push(match[0]);
            }
        }
    }

    for (var i in allMatches) {
        text = text.replaceAll(allMatches[i], "<span class='webgis-highlight-text'>" + allMatches[i] + "</span>");
    }

    return text;
};

String.prototype.countChar = function (char) {
    try {
        return this.split(char).length;
    } catch (e) {
        return 0;
    }
};
String.prototype.numberWithCommas = function () {
    var target = this;
    try {
        return target.replace(/\B(?=(\d{3})+(?!\d))/g, ".");
    }
    catch (e) {
        return target;
    }
};
String.prototype.removeSection = function (sectionName) {
    var description = this.toString();

    var fromIndex;
    if (description && description.indexOf) {
        while ((fromIndex = description.indexOf("@section: " + sectionName)) >= 0) {
            var toIndex = description.indexOf("@endsection", fromIndex);

            if (toIndex > 0) {
                var description1 = description.substr(0, fromIndex);
                var description2 = description.substr(toIndex + "@endsection".length).trim();

                description = description1 + description2;
            }
        }
    }
    return description;
};
String.prototype.removeAllSectionDecoration = function (sectionName) {
    if (this && this.split) {
        var description = '', lines = this.split('\n');

        for (var l in lines) {
            var line = lines[l];

            if (line.trim().indexOf("@section") === 0 || line.trim().indexOf("@endsection") === 0)
                continue;

            description += line + '\n';
        };

        return description;
    }
    return '';
};

webgis.firstOrDefault = function (array, f) {
    if (!array)
        return null;

    var items = webgis.$.grep(array, f);
    if (items.length > 0)
        return items[0];

    return null;
};

webgis.setCurrentKeyCodeFromEvent = function (e) {
    if (!e || webgis.usability.useAdvancedKeyShortcutHandling !== true) {
        webgis.currentKey = null;
    } else {
        let key = '';

        if (e.altKey === true) key += 'alt+';
        if (e.ctrlKey === true) key += 'ctrl+';
        if (e.shiftKey === true) key += 'shift+';

        if (e.key && e.key.length === 1) {
            key += e.key || e.code || '';
        }

        webgis.currentKey = key
    }

    //console.log('webgis.currentKey', webgis.currentKey);
}

if (!Array.prototype.includes) {  // incoude not supported by IE
    Array.prototype.includes = function (searchElement, fromIndex) {
        return this.indexOf(searchElement, fromIndex) !== -1;
    };
}

webgis.refParameter = function (v) {
    this.isRefParameter = true;
    this.value = v;
};

webgis.url = new function () {
    this.encodeString = function (v) {
        if (v && v.indexOf) {
            while (v.indexOf('<') != -1)
                v = v.replace('<', '&lt;');
            while (v.indexOf('>') != -1)
                v = v.replace('>', '&gt;');
        }
        v = encodeURI(v);

        while (v.indexOf('&') != -1)
            v = v.replace('&', '%26');
        while (v.indexOf('+') != -1)
            v = v.replace('+', '%2b');
        while (v.indexOf('#') != -1)
            v = v.replace('#', '%23');
        while (v.indexOf('=') != -1)
            v = v.replace('=', '%3d');

        while (v.indexOf('(') != -1)
            v = v.replace('(', '%28');
        while (v.indexOf(')') != -1)
            v = v.replace(')', '%29');
        while (v.indexOf('[') != -1)
            v = v.replace('[', '%5B');
        while (v.indexOf(']') != -1)
            v = v.replace(']', '%5D');
        while (v.indexOf('{') != -1)
            v = v.replace('{', '%7B');
        while (v.indexOf('}') != -1)
            v = v.replace('}', '%7D');

        return v;
    };
    this.relative = function (url) {
        var loc = document.location.toString().split('?')[0];
        if (loc.endsWith('/'))
            return '../' + url;

        return webgis.url.clean(url);
    };
    this.clean = function (url) {  // Remove double slashes (https://stackoverflow.com/questions/24381480/remove-duplicate-forward-slashes-from-the-url)
        var clean_url = url.replace(/([^:]\/)\/+/g, "$1");
        return clean_url;
    };
};

webgis.cancelTracker = function () {
    var _canceled = false;
    this.cancel = function () {
        _canceled = true;
    };
    this.isCanceled = function () { return _canceled; };
};

webgis.variableContent = new function ($) {
    this.init = function (element, className) {
        $(element).addClass('webgis-vc-' + className).data('initial', $(element).html());
    };
    this.set = function (className, content) {
        $('.webgis-vc-' + className).each(function (i, e) {
            $(e).html(content || $(e).data('initial'));
        });
    };
}(webgis.$);

webgis.compatiblity = new function () {
    this.toolId = function (id) {
        if (id) {
            if (webgis.net === 'standard' && id.indexOf('webmapping.tools.api.') === 0) {
                id = 'webgis.tools.' + id.substring('webmapping.tools.api.'.length);
            }
            else if (webgis.net === 'framework' && id.indexOf('webgis.tools.') === 0) {
                id = 'webmapping.tools.api.' + id.substring('webgis.tools.'.length);
            }
        }
        return id;
    };
};

webgis.help = {};

(window.jExt || window.jQuery)(document).ready(function () {
    webgis.registerUIEvents(null);
    $('webgis-c')
});

if (!webgis.api_version || webgis.api_version.indexOf('{{') === 0) {
    webgis.$.ajax({
        type: 'get',
        url: webgis.baseUrl + '/rest/version',
        async: false,
        success: function (result) {
            webgis.api_version = result;
        },
        error: function (err) {
            alert("WebGIS Initial Error" + err.statusText);
        }
    });
}

console.log('webgis api version:' + webgis.api_version);
