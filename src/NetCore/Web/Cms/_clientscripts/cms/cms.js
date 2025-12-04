var CMS = new function () {

    this.id = '';
    this.appRootUrl = '';

    var _db = null;
    this.init = function() {
        _db = new CMS.db();
    }

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
                                    CMS.secretsPasswordHash = passwordHash
                                    CMS.api(action, data, onsuccess, onerror);
                                } else {
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
        const formData = new FormData();
        for (const key in data) {
            if (data.hasOwnProperty(key)
                && data[key] !== null
                && data[key] !== undefined) {
                formData.append(key, data[key]);
            }
        }

        //console.log('fetch', 'cms/' + action);
        fetch('cms/' + action, {
                method: 'POST',
                //headers: {
                //    'Content-Type': 'application/json'
                //},
                body: formData //JSON.stringify(data)
            })
            .then(response => response.json())
            .then(result => {
                if (!result.success && result.exception) {
                    if (onerror) {
                        onerror(result.exception);
                    } else {
                        CMS.alert(result.exception);
                    }
                } else {
                    onsuccess(result);
                }
            })
            .catch(error => {
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

                CMS.api('nodeorder', { path: document.currentPath, nodes: JSON.stringify(nodes) },
                    function (result) {
                        updateContent(document.currentPath);
                    },
                    function (error) {
                        CMS.alert(error);
                    }
                );
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
    }

    this.sortAlphabetic = function (list, descending) {
        var items = $(list).children('li[data-path]');
        items.sort(function (a, b) {
            let $a = $(a), $b = $(b);

            //console.log("up", $a.hasClass('up'), $b.hasClass('up'));

            if ($a.hasClass('up')) return -1;
            if ($b.hasClass('up')) return 1;

            let aText = $a.children('.title').text();
            let bText = $b.children('.title').text();
            return aText.localeCompare(bText) * (descending ? -1 : 1);
        });
        $(list).append(items);
    }

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
            } else {
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
        CMS.api('nodesecurity', { path: path, tagName: tagName },
            function (result) {
                $('body').modalDialog({
                    title: result.displayName,
                    onLoad: function ($content) {
                        $content.form(result);
                        if (result.styleSetters)
                            CMS.applyStyleSetters(result.styleSetters);
                    }
                });
            },
            function (error) {
                CMS.alert(error);
            });
    };

    this.alertException = function(result) {
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
        } else {
            alert(message);
        }
    };

    this.message = function (text) {
        if (bootbox) {
            bootbox.alert(text);
        } else {
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
        } else {
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
        } else {
            if (confirm(text)) {
                callback();
            }
        }
    };

    this.showDeployDialog = function () {
        CMS.showModal('deploy',
            function ($content) {
                $("<iframe class='console-iframe'></iframe>")
                    .attr('src', CMS.appRootUrl + '/' + CMS.id + '/deploy?iframe=true')
                    .appendTo($content);
            });
    };

    this.showExportDialog = function () {
        CMS.showModal('Export',
            function ($content) {
                $("<iframe class='console-iframe'></iframe>")
                    .attr('src', CMS.appRootUrl + '/' + CMS.id + '/export')
                    .appendTo($content);
            });
    };

    this.showImportDialog = function () {
        CMS.showModal('Import',
            function ($content) {
                $("<iframe class='console-iframe'></iframe>")
                    .attr('src', CMS.appRootUrl + '/' + CMS.id + '/import')
                    .appendTo($content);
            });
    };

    this.showClearDialog = function () {
        CMS.showModal('Clear',
            function ($content) {
                $("<iframe class='console-iframe'></iframe>")
                    .attr('src', CMS.appRootUrl + '/' + CMS.id + '/clear')
                    .appendTo($content);
            });
    };

    this.showReloadSchemeDialog = function () {
        CMS.showModal('Reload Scheme',
            function ($content) {
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
        } catch (e) {
            return true;
        }
    };

    this.addAutocompleteEvents = function($control) {
        $control.on({
            'typeahead:cursorchange': function (e, item) {
            },
            'typeahead:select': function (e, item) {
                console.log(item);
            },
            'typeahead:open': function (e) {
            },
            'keyup': function (e) {
                if (e.keyCode === 13) {
                    $(this).typeahead('close');
                }
            }
        }).typeahead({
            hint: false,
            highlight: false,
            minLength: 0
        }, {
                limit: Number.MAX_VALUE,
                async: true,
                displayKey: 'value',
                source: function (query, processSync, processAsync) {
                    var $form = this.$el.closest('.cms-form');
                    var data = CMS.formData($form);
                    var name = this.$el.closest('.input-container').attr('data-control-name');

                    CMS.api('formautocomplete', { name: name, path: document.currentPath, id: CMS.id, data: JSON.stringify(data) }, function (data) {
                        data = data.slice(0, 60);
                        processAsync(data);
                    });
                },
                templates: {
                    empty: [
                        '<div class="tt-suggestion">',
                        '<div class="tt-content">Keine Ergebnisse gefunden</div>',
                        '</div>'
                    ].join('\n'),
                    suggestion: function (item) {
                        return "<div><div class='tt-content'>" + (item.thumbnail ? "<img class='tt-img' src='" + item.thumbnail + "' />" : "") + "<strong>" + item.suggested_text + "</strong>"
                            + (item.subtext ? "<br/><div>" + item.subtext + "</div>" : "") +
                            "</div >";
                    }
                }
        });
    }

    this.addPin = function (name, path) {
        if (_db) {
            _db.addPin(name, path, function () {
                console.log('stored...')
            });
        }
    };

    this.removePin = function (path) {
        if (_db) {
            _db.removePin(path, function () {
                console.log('removed...');
            });
        }
    }

    this.allPins = function (onsuccess) {
        if (_db) {
            _db.getAllPins(onsuccess);
        }
    }

    this.loadSecretPlaceholders = function (onsuccess) {
        CMS.api('secretplaceholders', {  },
            function (result) {
                onsuccess(result);
            },
            function (error) {
                CMS.alert(error);
            }
        );
        //onsuccess(["{{secret-sec1}}", "{{secret-sec2}}", "{{secret-sec3}}"]);
    };

    this.encodeHtmlString = function(str) {
        return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }
}();