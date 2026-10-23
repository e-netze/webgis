var CMS = new function () {
    this.id = '';
    this.appRootUrl = '';
    var _languageStorageKey = 'cms-language';
    this.language = '';
    this.defaultLanguage = 'en';
    this.supportedLanguages = ['de', 'en'];
    // Mirrors the language into a cookie so server-rendered pages (e.g. the
    // playgrounds) can localize themselves. Returns true if the cookie changed.
    var _syncLanguageCookie = function (lang) {
        var previous = (document.cookie.match(/(?:^|;\s*)cms-language=([^;]*)/) || [])[1];
        document.cookie = 'cms-language=' + encodeURIComponent(lang) + '; path=/; max-age=31536000; SameSite=Lax';
        return previous !== lang && document.cookie.indexOf('cms-language=') !== -1;
    };
    // Reads the last used language from localStorage (falling back to the
    // server-side default) so the UI keeps showing the user's preferred
    // language on the next visit.
    this.initLanguage = function (defaultLanguage, supportedLanguages) {
        if (defaultLanguage) {
            this.defaultLanguage = defaultLanguage;
        }
        if (supportedLanguages && supportedLanguages.length) {
            this.supportedLanguages = supportedLanguages;
        }
        var stored = null;
        try {
            stored = window.localStorage.getItem(_languageStorageKey);
        }
        catch (e) { }
        this.language = (stored && this.supportedLanguages.indexOf(stored) !== -1)
            ? stored
            : this.defaultLanguage;
        var cookieChanged = _syncLanguageCookie(this.language);
        this.updateLanguageSwitcherUI();
        if (cookieChanged && document.querySelector('[data-cms-localized-page]')) {
            document.location.reload();
        }
    };
    // Switches the CMS UI language, persists the choice in localStorage and
    // reloads the page so all node/property/tree data is re-fetched translated.
    // A full reload is required here (not an in-place refresh): the tree and
    // pin-list renderers are incremental and skip already-rendered nodes by
    // path, so they never redraw stale (old-language) node text in place.
    // To avoid losing the user's position, the current tree path is carried
    // along via a transient "_cp" query parameter and restored after reload.
    this.setLanguage = function (lang) {
        if (!lang || this.supportedLanguages.indexOf(lang) === -1 || lang === this.language) {
            return;
        }
        this.language = lang;
        try {
            window.localStorage.setItem(_languageStorageKey, lang);
        }
        catch (e) { }
        _syncLanguageCookie(lang);
        var url = new URL(document.location.href);
        if (typeof document.currentPath === 'string' && document.currentPath) {
            url.searchParams.set('_cp', document.currentPath);
        }
        else {
            url.searchParams.delete('_cp');
        }
        var newUrl = url.toString();
        if (newUrl === document.location.href) {
            document.location.reload();
        }
        else {
            document.location.href = newUrl;
        }
    };
    this.updateLanguageSwitcherUI = function () {
        $('#navbar-language-switcher').val(CMS.language);
    };
    var _db = null;
    this.init = function () {
        _db = new CMS.db();
    };
    this.api = function (action, data, onsuccess, onerror) {
        //data = {} || data;
        //data.id = this.id;
        if (data && data.path) {
            if (data.path.toLowerCase().indexOf('/__secrets') === 0 ||
                data.path.toLowerCase().indexOf('__secrets') === 0) {
                if (!CMS.secretsPasswordHash) {
                    CMS.password("Enter secrets password", function (password) {
                        if (password !== null) {
                            var passwordHash = CryptoJS.SHA512(("secrets:" + password).toString(CryptoJS.enc.Base64)).toString(CryptoJS.enc.Base64);
                            CMS.api('verifysecretspassword', { id: data.id, pw: passwordHash }, function (result) {
                                if (result.success === true) {
                                    CMS.secretsPasswordHash = passwordHash;
                                    CMS.api(action, data, onsuccess, onerror);
                                }
                                else {
                                    CMS.alert("Error:" + result.errorMessage);
                                }
                            });
                        }
                    });
                    return;
                }
                data.pw = CMS.secretsPasswordHash;
            }
        }
        // send data as form data
        var formData = new FormData();
        for (var key in data) {
            if (data.hasOwnProperty(key)
                && data[key] !== null
                && data[key] !== undefined) {
                formData.append(key, data[key]);
            }
        }
        //console.log('fetch', 'cms/' + action);
        fetch('cms/' + action + '?_ul=' + encodeURIComponent(CMS.language || ''), {
            method: 'POST',
            //headers: {
            //    'Content-Type': 'application/json'
            //},
            body: formData //JSON.stringify(data)
        })
            .then(function (response) { return response.json(); })
            .then(function (result) {
            if (!result.success && result.exception) {
                if (onerror) {
                    onerror(result.exception);
                }
                else {
                    CMS.alert(result.exception);
                }
            }
            else {
                onsuccess(result);
            }
        })
            .catch(function (error) {
            if (onerror) {
                onerror(error);
            }
        });
        //$.ajax({
        //    url: 'cms/' + action,
        //    type: 'post',
        //    data: data,
        //    success: function (result) {
        //        if (result.success === false && result.exception) {
        //            if (onerror) {
        //                onerror(result.exception);
        //            } else {
        //                CMS.alert(result.exception);
        //            }
        //        } else {
        //            onsuccess(result);
        //        }
        //    },
        //    error: function (error) {
        //        if (onerror) {
        //            onerror();
        //        }
        //    }
        //});
    };
    this.toAbsUrl = function (path) {
        if (!path)
            return '';
        while (path.indexOf('/') === 0) {
            path = path.substr(1);
        }
        var appUrl = this.appRootUrl;
        if (appUrl) {
            while (appUrl.lastIndexOf('/') === appUrl.length - 1) {
                appUrl = appUrl.substr(0, appUrl.length - 1);
            }
        }
        return appUrl + '/' + path;
    };
    this.showModal = function (title, onLoad, onCommit, onClose) {
        $('body').modalDialog({
            title: title,
            onLoad: onLoad,
            onCommit: onCommit,
            onClose: onClose
        });
    };
    this._consoleCloseButton;
    this.openConsole = function (cmd, reloadOnClose) {
        $.ajax({
            url: CMS.appRootUrl + '/' + cmd,
            type: 'get',
            success: function (result) {
                CMS.showConsole(result, reloadOnClose);
            },
            error: function () {
                alert("Unbekannter Fehler ist aufgetreten");
            }
        });
    };
    this.showConsole = function (proc, reloadOnClose) {
        $('body').modalDialog({
            title: proc.title,
            onLoad: function ($content) {
                CMS._consoleCloseButton = $content.closest('.modaldialog-modal').find('.modaldialog-close').css('display', 'none');
                $("<iframe class='console-iframe'></iframe>").attr('src', CMS.appRootUrl + '/console?procId=' + proc.procId + "&cmsId=" + proc.cmsId).appendTo($content);
            },
            onClose: reloadOnClose === true ? function () {
                document.location.reload();
            } : null
        });
    };
    this.consoleFinished = function () {
        this._consoleCloseButton.css('display', '');
    };
    this.closeModal = function (sender) {
        $(sender).modalDialog('Close');
    };
    this.commitModal = function (sender) {
        $(sender).modalDialog('Commit');
    };
    this.makeSortable = function (list) {
        list = $(list).addClass("sortable").get(0);
        var editableList = Sortable.create(list, {
            animation: 150,
            ghostClass: 'sorting',
            filter: '.list-remove',
            onFilter: function (evt) {
                var el = editableList.closest(evt.item); // get dragged item
                el && el.parentNode.removeChild(el);
            },
            onSort: function (e) {
                var nodes = [];
                $(e.target).children('li[data-path]').each(function (i, li) {
                    if (!$(li).hasClass('current') && !$(li).hasClass('up')) {
                        var path = $(li).attr('data-path');
                        nodes.push(path);
                    }
                });
                CMS.api('nodeorder', { path: document.currentPath, nodes: JSON.stringify(nodes) }, function (result) {
                    updateContent(document.currentPath);
                }, function (error) {
                    CMS.alert(error);
                });
            }
        });
        $(list).data('sortable', editableList);
    };
    this.destroySortable = function (list) {
        var sortable = $(list).data('sortable');
        if (sortable) {
            sortable.destroy();
            $(list).data('sortable', null).removeClass("sortable");
        }
    };
    this.sortAlphabetic = function (list, descending) {
        var items = $(list).children('li[data-path]');
        items.sort(function (a, b) {
            var $a = $(a), $b = $(b);
            //console.log("up", $a.hasClass('up'), $b.hasClass('up'));
            if ($a.hasClass('up'))
                return -1;
            if ($b.hasClass('up'))
                return 1;
            var aText = $a.children('.title').text();
            var bText = $b.children('.title').text();
            return aText.localeCompare(bText) * (descending ? -1 : 1);
        });
        $(list).append(items);
    };
    this.updateContent = function (path) {
        updateContent(path);
    };
    this.formData = function (form) {
        return $(form).form('RefreshData').data('form-data');
    };
    this.hasFormErrors = function (form) {
        return $(form).form('HasFormErrors').data('form-haserros');
    };
    this.refreshForm = function (form, controls) {
        return $(form).form('RefreshForm', { controls: controls });
    };
    this.applyStyleSetters = function (styleSetters) {
        for (var i in styleSetters) {
            var styleSetter = styleSetters[i];
            console.log('stylesetter', styleSetter.selector, $(styleSetter.selector).length);
            if (styleSetter.append === true) {
                $(styleSetter.selector).addClass(styleSetter.className);
            }
            else {
                $(styleSetter.selector).removeClass(styleSetter.className);
            }
        }
    };
    this.navTreeData = function (tree) {
        return $(tree).lazyNavTree('RefreshData').data('navtree-data');
    };
    this.propertyGridData = function (propertyGrid) {
        return $(propertyGrid).propertyGrid('RefreshData').data('propertygrid-data');
    };
    this.toLazyNavTree = function (element, path, name, singleSelect) {
        $(element).lazyNavTree({ path: path, name: name, singleSelect: singleSelect });
    };
    this.secureNode = function (path, tagName) {
        CMS.api('nodesecurity', { path: path, tagName: tagName }, function (result) {
            $('body').modalDialog({
                title: result.displayName,
                onLoad: function ($content) {
                    $content.form(result);
                    if (result.styleSetters)
                        CMS.applyStyleSetters(result.styleSetters);
                }
            });
        }, function (error) {
            CMS.alert(error);
        });
    };
    this.alertException = function (result) {
        CMS.alert("Fehler: " + (result.exception || "Unbekannter Fehler"), true);
    };
    this.showHourglass = function (title) {
        return $('body').hourglass({ title: title });
    };
    this.hideHourglass = function () {
        return $('body').hourglass('Close');
    };
    this.removeAllDescriptions = function ($parent) {
        $parent.descriptionPanel('RemoveAll');
    };
    this.hasUINavtree = function () {
        return $('#main-navtree').width() > 0;
    };
    this.alert = function (message) {
        message = message || "Leider ist ein unbekannter Fehler aufgetreten";
        if (bootbox) {
            message = message.replace(/\n/g, '<br/>');
            bootbox.alert(message);
        }
        else {
            alert(message);
        }
    };
    this.message = function (text) {
        if (bootbox) {
            bootbox.alert(text);
        }
        else {
            alert(text);
        }
    };
    this.confirm = function (text, callback) {
        if (bootbox) {
            bootbox.confirm(text, function (result) {
                if (result === true) {
                    callback();
                }
            });
        }
        else {
            if (confirm(text)) {
                callback();
            }
        }
    };
    this.password = function (text, callback) {
        if (bootbox) {
            bootbox.prompt({
                title: text || "Enter password",
                inputType: 'password',
                callback: callback
            });
        }
        else {
            if (confirm(text)) {
                callback();
            }
        }
    };
    this.showDeployDialog = function () {
        CMS.showModal('deploy', function ($content) {
            $("<iframe class='console-iframe'></iframe>")
                .attr('src', CMS.appRootUrl + '/' + CMS.id + '/deploy?iframe=true')
                .appendTo($content);
        });
    };
    this.showExportDialog = function () {
        CMS.showModal('Export', function ($content) {
            $("<iframe class='console-iframe'></iframe>")
                .attr('src', CMS.appRootUrl + '/' + CMS.id + '/export')
                .appendTo($content);
        });
    };
    this.showImportDialog = function () {
        CMS.showModal('Import', function ($content) {
            $("<iframe class='console-iframe'></iframe>")
                .attr('src', CMS.appRootUrl + '/' + CMS.id + '/import')
                .appendTo($content);
        });
    };
    this.showClearDialog = function () {
        CMS.showModal('Clear', function ($content) {
            $("<iframe class='console-iframe'></iframe>")
                .attr('src', CMS.appRootUrl + '/' + CMS.id + '/clear')
                .appendTo($content);
        });
    };
    this.showReloadSchemeDialog = function () {
        CMS.showModal('Reload Scheme', function ($content) {
            $("<iframe class='console-iframe'></iframe>")
                .attr('src', CMS.appRootUrl + '/' + CMS.id + '/reloadscheme')
                .appendTo($content);
        });
    };
    this.downloadFile = function (url) {
        console.log('download file', CMS.appRootUrl + '/' + url);
        $("<iframe>").attr('src', CMS.appRootUrl + '/' + url).appendTo('body');
    };
    this.inIFrame = function () {
        try {
            return window.self !== window.top;
        }
        catch (e) {
            return true;
        }
    };
    // suggestion menu for 'inputautocomplete' form controls (no external autocomplete library).
    // One shared menu element (appended to body, fixed) is reused by all inputs, so nothing
    // is left behind when a form/dialog is removed.
    var _acMenu = null, _acInput = null, _acCursor = -1, _acRequest = 0, _acTimer = null;
    var _acItems = function () { return _acMenu ? _acMenu.querySelectorAll('.cms-autocomplete-item[data-value]') : []; };
    var _acIsOpen = function () { return _acMenu !== null && _acMenu.style.display === 'block'; };
    var _acClose = function () {
        clearTimeout(_acTimer);
        _acRequest++;
        if (_acMenu) {
            _acMenu.style.display = 'none';
        }
        _acInput = null;
        _acCursor = -1;
    };
    var _acSetCursor = function (index) {
        var all = _acItems();
        if (all.length === 0) {
            return;
        }
        _acCursor = (index + all.length) % all.length;
        for (var i = 0; i < all.length; i++) {
            all[i].classList.toggle('cms-autocomplete-cursor', i === _acCursor);
        }
        all[_acCursor].scrollIntoView({ block: 'nearest' });
    };
    var _acSelect = function (item) {
        var input = _acInput;
        _acClose();
        if (input) {
            input.value = item.getAttribute('data-value');
            $(input).trigger('change');
        }
    };
    var _acEnsureMenu = function () {
        if (_acMenu) {
            return;
        }
        _acMenu = document.createElement('div');
        _acMenu.className = 'cms-autocomplete-menu';
        _acMenu.style.display = 'none';
        document.body.appendChild(_acMenu);
        // mousedown instead of click: fires before the input's blur closes the menu
        _acMenu.addEventListener('mousedown', function (e) {
            e.preventDefault();
            var item = e.target.closest('.cms-autocomplete-item[data-value]');
            if (item) {
                _acSelect(item);
            }
        });
        window.addEventListener('resize', _acClose);
        // fixed menu: close when the page or a dialog scrolls (but not when the menu itself scrolls)
        window.addEventListener('scroll', function (e) {
            if (_acIsOpen() && e.target !== _acMenu) {
                _acClose();
            }
        }, true);
    };
    var _acRender = function (input, data) {
        _acMenu.innerHTML = '';
        _acCursor = -1;
        if (data.length === 0) {
            var empty = document.createElement('div');
            empty.className = 'cms-autocomplete-item cms-autocomplete-empty';
            empty.textContent = 'Keine Ergebnisse gefunden';
            _acMenu.appendChild(empty);
        }
        data.forEach(function (entry) {
            var div = document.createElement('div');
            div.className = 'cms-autocomplete-item';
            div.setAttribute('data-value', entry.value || entry.suggested_text || '');
            if (entry.thumbnail) {
                var img = document.createElement('img');
                img.className = 'cms-autocomplete-img';
                img.src = entry.thumbnail;
                div.appendChild(img);
            }
            var strong = document.createElement('strong');
            strong.textContent = entry.suggested_text || entry.value || '';
            div.appendChild(strong);
            if (entry.subtext) {
                var sub = document.createElement('div');
                sub.className = 'cms-autocomplete-subtext';
                sub.textContent = entry.subtext;
                div.appendChild(sub);
            }
            _acMenu.appendChild(div);
        });
        var rect = input.getBoundingClientRect();
        _acMenu.style.left = rect.left + 'px';
        _acMenu.style.top = rect.bottom + 'px';
        _acMenu.style.width = rect.width + 'px';
        _acMenu.style.display = 'block';
        _acInput = input;
    };
    var _acQuery = function (input) {
        var $input = $(input);
        var data = CMS.formData($input.closest('.cms-form'));
        var name = $input.closest('.input-container').attr('data-control-name');
        var request = ++_acRequest;
        CMS.api('formautocomplete', { name: name, path: document.currentPath, id: CMS.id, data: JSON.stringify(data) }, function (result) {
            // ignore outdated responses and responses for an input that lost the focus
            if (request !== _acRequest || document.activeElement !== input) {
                return;
            }
            _acRender(input, (result || []).slice(0, 60));
        });
    };
    this.addAutocompleteEvents = function ($control) {
        _acEnsureMenu();
        $control.attr('autocomplete', 'off').each(function () {
            var input = this;
            input.addEventListener('focus', function () { _acQuery(input); });
            input.addEventListener('input', function () {
                clearTimeout(_acTimer);
                _acTimer = setTimeout(function () { _acQuery(input); }, 250);
            });
            input.addEventListener('blur', function () {
                if (_acInput === input || _acInput === null) {
                    _acClose();
                }
            });
            input.addEventListener('keydown', function (e) {
                if (!_acIsOpen() || _acInput !== input) {
                    return;
                }
                if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    _acSetCursor(_acCursor + 1);
                }
                else if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    _acSetCursor(_acCursor - 1);
                }
                else if (e.key === 'Enter') {
                    var all = _acItems();
                    if (_acCursor >= 0 && all[_acCursor]) {
                        e.preventDefault();
                        _acSelect(all[_acCursor]);
                    }
                    else {
                        _acClose();
                    }
                }
                else if (e.key === 'Escape') {
                    _acClose();
                }
            });
        });
    };
    this.addPin = function (name, path) {
        if (_db) {
            _db.addPin(name, path, function () {
                console.log('stored...');
            });
        }
    };
    this.removePin = function (path) {
        if (_db) {
            _db.removePin(path, function () {
                console.log('removed...');
            });
        }
    };
    this.allPins = function (onsuccess) {
        if (_db) {
            _db.getAllPins(onsuccess);
        }
    };
    this.loadSecretPlaceholders = function (onsuccess) {
        CMS.api('secretplaceholders', {}, function (result) {
            onsuccess(result);
        }, function (error) {
            CMS.alert(error);
        });
        //onsuccess(["{{secret-sec1}}", "{{secret-sec2}}", "{{secret-sec3}}"]);
    };
    this.encodeHtmlString = function (str) {
        return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    };
}();
