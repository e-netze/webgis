function updateContent(path) {
    $('childnodes').empty();
    $('#cms-search').focus().val('');

    var data = {
        path: path,
        copyNodePath: JSON.stringify(document.copyNodePath)
    };

    CMS.api('childnodes', data, function (result) {
        document.currentPath = path;

        if (CMS.hasUINavtree()) {
            $('#main-navtree').pinlist({
                items: null
            });
            $("#main-navtree").navtree({
                items: result.navItems,
                nodes: result.nodes,
                path: path
            }).find('.text').each(function (i, e) {
                var $item = $(e);
                if (!$item.hasClass('event-added')) {
                    $item.click(function () {
                        updateContent($(this).closest('.cms-treenode').attr('data-path'));
                    });
                    $item.addClass('event-added');
                }
            });
        }

        $('.navbar-header .tool.order')
            .removeClass('asc refresh').addClass('desc');

        $('#main-navbar').navbar({
            items: result.navItems
        }).find('.item').each(function (i, e) {
            var $item = $(e);
            $item.click(function () {
                updateContent($(this).attr('data-path'));
            })
        });

        $('#main-toolbar').toolbar({
            tools: result.nodeTools,
            orderable: result.orderable
        });

        var hasClipboard = false;
        $('#main-toolbar').find('.tool').each(function (i, e) {
            let $tool = $(e);

            hasClipboard |= ($tool.attr('data-action') === 'paste' || $tool.attr('data-action') === 'cut');

            $tool.click(function () {
                var $tool = $(this);

                CMS.api('toolclick', { action: $tool.attr('data-action'), path: $tool.attr('data-path'), name: $tool.attr('data-name') }, function (result) {
                    if (result.controls) {
                        $('body').modalDialog({
                            title: result.displayName,
                            onLoad: function ($content) {
                                $content.form(result);
                                $content.secretSelectorPanel();
                            },
                            onCommit: function ($sender, $modal) {
                                $sender.addClass('loading');
                                var data = {};
                                switch ($tool.attr('data-action')) {
                                    case 'new':
                                    case 'paste':
                                    case 'cut':
                                        var $form = $modal.find('.cms-form');
                                        data = CMS.formData($form);
                                        break;
                                    case 'link':
                                        var $tree = $modal.find('.cms-lazy-navtree');
                                        data = CMS.navTreeData($tree);
                                        break;
                                }

                                if (CMS.hasFormErrors($form) === true) {
                                    CMS.alert("Dialog ist nicht vollst?ig ausgefüllt");
                                    $sender.removeClass('loading');
                                    return;
                                }

                                if ($tool.attr('data-action') === 'cut') {
                                    document.copyNodePath = jQuery.grep(document.copyNodePath, function (value) { return value != '-' + $tool.attr('data-name') });
                                    console.log(document.copyNodePath);
                                }

                                CMS.api('toolcommit', { data: JSON.stringify(data), action: $tool.attr('data-action'), path: $tool.attr('data-path'), name: $tool.attr('data-name') },
                                    function (result) {
                                        $sender.removeClass('loading');
                                        CMS.closeModal($modal);

                                        updateContent(document.currentPath);
                                    },
                                    function (error) {
                                        $sender.removeClass('loading');
                                        CMS.alert(error);
                                    });
                            }
                        })
                    }
                });
            })
        });
        if (hasClipboard) {
            $("<div><div class='text'>X</div></div>")
                .addClass('tool empty-clipboard')
                .appendTo('#main-toolbar')
                .click(function (e) {
                    e.stopPropagation();
                    document.copyNodePath = [];
                    updateContent(document.currentPath);
                });
        }

        $('#main-content').contentNodes(result);

        $('#main-content').find('.node').each(function (i, e) {
            var $node = $(e).addClass('match');
            if ($node.attr('data-haschildren') === 'true') {
                $node.click(function () {
                    updateContent($(this).attr('data-path'));
                });
            } else {
                $node.click(function () {
                    editNode($(this).attr('data-path'));
                });
            }
            $node.find('.node-properties').click(function (e) {
                e.stopPropagation();
                editNode($(this).closest('.node').attr('data-path'));
            });
            $node.find('.node-target-properties').click(function (e) {
                e.stopPropagation();
                editNode($(this).closest('.node').attr('data-target'));
            });
            $node.find('.node-delete').click(function (e) {
                e.stopPropagation();
                deleteNode($(this).closest('.node').attr('data-path'));
            });
            $node.find('.node-security').click(function (e) {
                e.stopPropagation();
                CMS.secureNode($(this).closest('.node').attr('data-path'), '');
            });
            $node.find('.node-refresh').click(function (e) {
                e.stopPropagation();
                refreshNode($(this).closest('.node').attr('data-path'), 0);
            });
            $node.find('.node-copy').click(function (e) {
                e.stopPropagation();
                copyNode($(this).closest('.node').attr('data-path'), 'nodecopy');
            });
            $node.find('.node-cut').click(function (e) {
                e.stopPropagation();
                copyNode($(this).closest('.node').attr('data-path'), 'nodecut');
            });
        });

        CMS.removeAllDescriptions($('#main-container'));
        if (result.description) {
            var converter = new showdown.Converter(),
                html = converter.makeHtml(result.description);
            $('#main-container').descriptionPanel({ id: path, text: html });
        }
    });
}

function editNode(path) {
    CMS.api('nodeproperties', { path: path },
        function (result) {
            $('body').modalDialog({
                title: result.displayName,
                onLoad: function ($content) {
                    $content.propertyGrid({ properties: result.properties, path: result.path });
                    $content.secretSelectorPanel();
                },
                onCommit: result.readonly === true ?
                    null :
                    function ($sender, $modal) {
                        $sender.addClass('loading');
                        var $grid = $modal.find('.cms-propertygrid');
                        var data = CMS.propertyGridData($grid);

                        CMS.api('NodePropertiesCommit',
                            { path: path, data: JSON.stringify(data) },
                            function (result) {
                                $sender.removeClass('loading');
                                CMS.closeModal($modal);
                                updateContent(document.currentPath);  // Falls nich name ge?ert hat
                            },
                            function (error) {
                                $sender.removeClass('loading');
                                CMS.alert(error);
                            });
                    }
            });
        },
        function (error) {
            CMS.alert(error);
        });
};

function deleteNode(path) {
    CMS.confirm("Diesen Knoten löschen?", function () {
        CMS.api('nodedelete', { path: path },
            function (result) {
                if (result.success === false) {
                    CMS.alertException(result);
                }
                $(".cms-treenode[data-path='/" + path + "']").remove();
                updateContent(document.currentPath);
            },
            function (error) {
                CMS.alert(error);
            });
    });
};

function refreshNode(path, level) {
    CMS.showHourglass('Refresh');
    CMS.api('noderefresh', { path: path, level: level },
        function (result) {
            CMS.hideHourglass();
            if (result.success === false && result.confirm) {
                if (confirm(result.confirm)) {
                    refreshNode(path, result.level + 1);
                }
            }
            else if (result.success === false) {
                CMS.alertException(result);
            }
            else if (result.success === true) {
                CMS.message('Refresh erfolgreich...');
            }
        },
        function (error) {
            CMS.hideHourglass();
            CMS.alert(error);
        });
};

function copyNode(path, action) {

    document.copyNodePath = jQuery.grep(document.copyNodePath, function (value) { return value != path });
    document.copyNodePath = jQuery.grep(document.copyNodePath, function (value) { return value != '+' + path });
    document.copyNodePath = jQuery.grep(document.copyNodePath, function (value) { return value != '-' + path });

    CMS.api(action, { path: path },
        function (result) {
            if (result.success === true) {
                document.copyNodePath.push(result.copyNodePath);
                updateContent(document.currentPath);
            }
        });
};

function checkSearchItem(item, terms) {
    for (var t in terms) {
        if (item.indexOf(terms[t]) < 0)
            return false;
    }
    return true;
}

$(document).ready(function () {
    if (hasElastic) {
        $('.cms-search')
            .css('display', '')
            .on({
                'typeahead:cursorchange': function (e, item) {
                },
                'typeahead:select': function (e, item) {
                    //console.log(item);
                    if (item.cmd && !item.path) {
                        CMS.openConsole(item.cmd);
                    } else {
                        updateContent(item.path);
                    }
                    $(this).typeahead('val', '');
                },
                'typeahead:open': function (e) {
                },
                'keyup': function (e) {
                    if (e.keyCode == 13) {
                        $(this).typeahead('close');
                    }
                }
            }).typeahead({
                hint: false,
                highlight: false,
                minLength: 1
            }, {
                limit: Number.MAX_VALUE,
                async: true,
                displayKey: 'suggested_text',
                source: function (query, processSync, processAsync) {
                    CMS.api('search', { term: query, id: CMS.id, path: document.currentPath }, function (data) {
                        data = data.slice(0, 12);
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
                        return "<div><div class='tt-content'>" + (item.thumbnail ? "<img class='tt-img' src='" + item.thumbnail + "' />" : "") + "<strong>" + item.suggested_text + "</strong><br/><div>" + item.subtext + "</div><div style='color:#ccc'>" + item.path + "</div></div>";
                    }
                }
            });
    } else {
        $('.cms-search')
            .css('display', '')
            .attr('autocomplete', 'off')
            .on('keydown', function (e) {
                //console.log(e.keyCode);
                if (e.keyCode === 13) {
                    var items = $('.match.selected');

                    if (items.length != 1) {
                        items = $('.match[data-searchitem]');
                    }

                    if (items.length === 1) {
                        items.trigger('click');
                    }

                }
                else if (e.keyCode === 40) { // Arrow Down
                    var items = $('.match');

                    var found = false;
                    for (var i = 0; i < items.length - 1; i++) {
                        if ($(items[i]).hasClass('selected')) {
                            $(items[i]).removeClass('selected');
                            $(items[i + 1]).addClass('selected');
                            found = true;
                            break;
                        }
                    }

                    if (found === false && items.length > 0) {
                        $(items[0]).addClass('selected');
                    }
                }
                else if (e.keyCode === 38) { // Arrow Up
                    var items = $('.match');

                    for (var i = 1; i < items.length; i++) {
                        if ($(items[i]).hasClass('selected')) {
                            $(items[i]).removeClass('selected');
                            $(items[i - 1]).addClass('selected');
                        }
                    }
                }
            })
            .on('keyup', function (e) {
                var val = $(this).val();
                var terms = val.split(' ');

                if (val === '') {
                    $('[data-searchitem]').css('display', '');
                } else {
                    if (e.keyCode !== 40 && e.keyCode !== 38) {
                        $('.match.selected').removeClass('selected');
                    }
                    $('[data-searchitem]').each(function () {
                        var $this = $(this);
                        var match = checkSearchItem($this.attr('data-searchitem'), terms);

                        if (match) {
                            $this
                                .addClass('match')
                                .css('display', '')
                        } else {
                            $this
                                .removeClass('match')
                                .css('display', 'none');
                        }
                    });
                }
            });
    }

    document.copyNodePath = [];
    updateContent(document.currentPath = '');

    $('.navbar-search-button').trigger('click');
});
