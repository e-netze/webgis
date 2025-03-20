$(document).ready(function () {

    $.ajax({
        url: webgis.url.relative('proxy/toolmethod/webgis-tools-portal-portal/page-content'),
        data: { 'page-id': portal },
        dataType: 'json',
        success: function (result) {

            for (var s in result.sorting) {
                var contentId = result.sorting[s];

                if (contentId === 'page-title' || contentId === 'page-description') {
                    var content = getContent(contentId, result.contentitems);
                    if (content)
                        $('#' + contentId).html(content);
                }
                else if (contentId === 'page-maps') {
                    var $mapContent = $("<li class='page-content-item page-content-item-block' data-content-id='page-maps' style='display:none'><div style='text-align:left'><ul class='webgis-page-category-list' style='display:inline-block;margin:0px;padding:0px;width:100%' id='list-categories'></ul></div></li>")
                        .appendTo('#page-content-list');
                    if (isOwner === true) {
                        $("<input class='edit-style' data-styleitem='.webgis-page-mapm-item,.webgis-page-map-new-item,.webgis-page-map-item-image,.webgis-page-map-new-item-image' data-styleproperty='border-radius' title='Tile border radius' />").prependTo($mapContent);
                    }
                }
                else {
                    var content = getContent(contentId, result.contentitems);
                    $("<li class='page-content-item page-editable-content page-content-item-block' data-content-id='" + contentId + "' data-content-type='html' style='display:none'>" + (content != '' ? content : '<h1>Neuer Inhalt...</h1>') + "</li>").appendTo('#page-content-list');
                }
            }

            if (window.initEditables)
                window.initEditables();
            loadMapCategories();

            beginShowContent($('#page-content-list').children('li:first'))
        }
    });

    $(window).on('resize', refreshTiles);

    $('.webgis-page-content-edit-customjs').click(function () {
        showCustomContentEditor('custom.js');
    });
    $('.webgis-page-content-edit-portalcss').click(function () {
        showCustomContentEditor('portal.css');
    });
    $('.webgis-page-content-edit-defaultcss').click(function () {
        showCustomContentEditor('default.css');
    });
});

function beginShowContent($elem) {
    if ($elem === null || $elem.length === 0) {
        return;
    }

    $elem.slideDown(function(){
        beginShowContent($elem.next());
    })
}

function getContent(contentId, contentItems) {
    for (var i in contentItems) {
        var item = contentItems[i];
        if (item.id === contentId)
            return item.content;
    }
    return '';
}

function loadMapCategories() {
    var $sidebarList = $('.webportal-layout-sidebar-items.center')
        .addClass('webgis-portal-sortable')
        .attr('data-sorting-method','categories')

    $.ajax({
        url: webgis.url.relative('proxy/toolmethod/webgis-tools-portal-publish/map-categories'),
        data: { 'page-id': portal },
        dataType: 'json',
        success: function (result) {
            var count = result.categories.length + 1;

            for (var c = 0; c < result.categories.length; c++) {
                if (!isHiddenCategory(parseCategoryName(result.categories[c]))) {
                    var $sidebarItem = $("<li class='webportal-layout-sidebar-item'>")
                        .addClass('webgis-portal-sortable-item')
                        .attr('data-sortable', result.categories[c])
                        .appendTo($sidebarList);
                    $("<div class='webportal-layout-sidebar-item-firstletter' />").appendTo($sidebarItem);
                    $("<a href='' onclick=\"showCategory($(this).attr('data-category'));return false;\" data-category='" + result.categories[c] + "'>" + parseCategoryName(result.categories[c]) + "</a>").appendTo($sidebarItem);
                }
            }

            // My projects
            if (!isAnonymous) {
                var $sidebarItem = $("<li class='webportal-layout-sidebar-item'>").appendTo($sidebarList);
                $("<div class='webportal-layout-sidebar-item-firstletter' />").appendTo($sidebarItem);
                $("<a href='' onclick=\"showMyProjects();return false;\" >Meine Projekte</a>").appendTo($sidebarItem);
            }

            webPortalLayout.refreshSidebar();

            $("<li><div id='category-content'></div></li>").appendTo('#list-categories');
            var $categoryContent = $('#category-content')
                .addClass('webgis-page-category-item')
                .data('categories', result.categories);

            if (showOptimizationFilter) {
                if (webgis.useMobileCurrent() === true) {
                    $("#category-content").addClass('filter filter-mobile');
                } else if (webgis.isTouchDevice() === false) {
                    $("#category-content").addClass('filter filter-desktop');
                }
            }

            ////// Search Maps
            var searchPlaceholder = 'Karten suchen (alle Karten) ...';

            var $searchHolder = $("<div></div>")
                .addClass('webgis-maps-search-holder webgis-content-search-area active')
                .insertBefore($categoryContent)
                .click(function () {
                    $this = $(this);

                    var categories = $this.next('#category-content').data('categories');

                    $('.webgis-tile-container').empty();
                    var $allMaps = $this.find('#all-maps').find('.webgis-tile-container').empty();

                    for (var c in categories) {
                        if (!_categoryMaps[categories[c]]) {
                            $.ajax({
                                url: webgis.url.relative('proxy/toolmethod/webgis-tools-portal-publish/category-maps'),
                                data: { page: portal, category: categories[c] },
                                dataType: 'json',
                                success: function (result) {
                                    console.log(result.category);
                                    _categoryMaps[result.category] = result.maps;
                                    rendererCategoryMapTiles($allMaps, result.category, true, true);
                                }
                            });
                        } else {
                            rendererCategoryMapTiles($allMaps, categories[c], true, true);
                        }
                    }
                });

            $("<div>").appendTo($searchHolder).webgis_contentsearch({
                placeholder: searchPlaceholder,
                addInputElementClass: 'webgis-input',
                container_selectors: [".webgis-page-map-item.tile"],
                onChanged: function (sender, val) {
                    refreshTiles();
                },
                onMatch: function (sender, $item, val) {
                    var $text = $item.children('.webgis-page-map-item-text');
                    var title = $text.children('.title').html();
                    var description = $text.children('.description').html();

                    $item.children('.info').html('<h3>' + title.highlightText(val.split(' ')) + '</h3>' + description.highlightText(val.split(' ')));
                }
            });

            $("<div id='all-maps'><div class='webgis-tile-container' data-tilewidth='140'></div></div>").appendTo($searchHolder);

            if (isOwner) {
                $("<button>Edit Sorting</button>")
                    .addClass('webgis-button uibutton uibutton-cancel webgis-portal-edit-ordering-button')
                    .insertBefore($searchHolder)
                    .click(function () {
                        toggleEditOrdering();
                    });
            }

            //////////////////////////////////////////////////////////////////////////

            var firstVisibleCategory = null;
            for (var c in result.categories) {
                if (!isHiddenCategory(result.categories[c])) {
                    firstVisibleCategory = result.categories[c];
                    break;
                }
            }

            if (firstVisibleCategory) {
                showCategory(firstVisibleCategory);
            } else if (isAuthor) {
                $("<li class='webgis-page-category-item' style='width:100%'><div class='webgis-page-category-item-title'>MapBuilder...<div></li>").appendTo('#list-categories')
                    .click(function () {
                        document.location = './' + portal + '/mapbuilder/';
                    });
            }
        }
    });
}

function toggleEditOrdering() {
    if ($('body').hasClass('webgis-portal-edit-ordering')) {
        stopEditOrdering();
    } else {
        startEditOrdering();
    }
}

function startEditOrdering() {
    if (isOwner) {
        $('body').addClass('webgis-portal-edit-ordering');
        $('button.webgis-portal-edit-ordering-button')
            .addClass('uibutton-danger')
            .removeClass('uibutton-cancel');

        webgis.require('sortable', function () {
            $('.webgis-portal-sortable').each(function (i, e) {
                //console.log('Sortable.create', e);
                $(e).data('sortable', Sortable.create(e, {
                    onSort: function (event) {
                        var $sender = $(event.target);
                        var sortedItems = [];
                        $sender
                            .children('.webgis-portal-sortable-item[data-sortable]')
                            .each(function (i, e) {
                                sortedItems.push($(e).attr('data-sortable'));
                            });

                        console.log('sort', $sender.attr('data-sorting-method'), sortedItems);

                        $.ajax({
                            url: webgis.url.relative(portal + '/sortitems'),
                            data: { sortingMethod: $sender.attr('data-sorting-method'), items: JSON.stringify(sortedItems), currentCategory: $sender.attr('data-sorting-currentcategory') || '' },
                            type: 'post',
                            success: function (result) {

                            }
                        });
                    }
                }));
            });            
        });
        
        $('.webgis-portal-sortable').children().each(function (i, e) {
            if (!$(e).hasClass('webgis-portal-sortable-item'))
                $(e).addClass('webgis-portal-sortable-hidden-item');
        });

        $('.webgis-portal-sortable .webgis-portal-sortable-item')
            .css({});
        $('.webgis-portal-sortable .webgis-page-map-item.webgis-portal-sortable-item')
            .css({ position: 'relative', left:'', top:'', display: 'block', margin:'' /*, width:'calc(100% - 10px)'*/ });
    }
}

function stopEditOrdering() {
    if (isOwner) {
        $('body').removeClass('webgis-portal-edit-ordering');
        $('button.webgis-portal-edit-ordering-button')
            .removeClass('uibutton-danger')
            .addClass('uibutton-cancel');

        $('.webgis-portal-sortable .webgis-portal-sortable-hidden-item')
            .removeClass('webgis-portal-sortable-hidden-item')

        try {
            webgis.require('sortable', function () {
                $('.webgis-portal-sortable').each(function (i, e) {
                    var sortable = $(e).data('sortable');
                    if (sortable) {
                        //console.log('Sortable.destroy', e, sortable);
                        sortable.destroy();
                        $(e).data('sortable', null);
                    }
                });
            });
            //$('.webgis-portal-sortable').sortable("destroy");
        } catch (e) { console.log(e); }
    }
}

function parseCategoryName(category) {
    if (category === "_Favoriten")
        return "♥ Favoriten";
    if (category === '__myProjects')
        return 'Meine Projekte';

    return category;
}

function isHiddenCategory(category) {
    if (isAuthor || isOwner)
        return false;

    return category && category.indexOf('@@') === 0;
}

function showCategory(category) {
    stopEditOrdering();
    if (category === '__myProjects') {
        showMyProjects();
    } else {

        $('#all-maps').find('.webgis-tile-container').empty();

        var $div = $('#category-content').empty();

        var $title = $("<table>").addClass('webgis-page-category-header').appendTo($div);
        var $row = $("<tr>").appendTo($title);

        var $category = $("<div>" + parseCategoryName(category) + "</div>")
            .addClass('webgis-page-category-item-title')
            .data('category', category)
            .appendTo($("<td>").addClass('webgis-page-category-item-selected').css({ width: '100%' }).appendTo($row))
            .click(function () {
                var $tileContainer = $('#category-content').find('.webgis-tile-container');
                if ($tileContainer.children().length == '0') {
                    showCategory($(this).data('category'));
                } else {
                    showCategories();
                }
            });

        if (uaEditable === true) {
            $("<div class='edit-access'></div>").appendTo($category.addClass('has-edit-access')).data('access-name', category)
                .click(function (event) {
                    event.stopPropagation();
                    changeUserAccess($(this).data('access-name'));
                });
        } 

        if (showOptimizationFilter) {
            var $optButtons = $("<div>")
                .addClass('optimization-buttons')
                .appendTo($category);

            var clickOptFilter = function (e, sender, filter) {
                e.stopPropagation();
                var $content = $(sender).closest('#category-content');
                var hasFilter = filter && $content.hasClass('filter-' + filter);

                $content.removeClass('filter filter-desktop filter-mobile');

                if (filter && !hasFilter) {
                    $content.addClass('filter filter-' + filter);
                }

                refreshTiles();
            }

            $("<div>")
                .addClass('optimization-button desktop')
                .attr('title', 'Optimiert für Desktop (große Displays)')
                .appendTo($optButtons)
                .click(function (e) {
                    clickOptFilter(e, this, 'desktop');
                });
            $("<div>")
                .addClass('optimization-button mobile')
                .attr('title', 'Optimiert fürs Handy (kleine Displays)')
                .appendTo($optButtons)
                .click(function (e) {
                    clickOptFilter(e, this, 'mobile');
                });
            $("<div>")
                .addClass('optimization-button allplatforms')
                .attr('title', 'Alle Ziel-Plattformen anzeigen')
                .appendTo($optButtons)
                .click(function (e) {
                    clickOptFilter(e, this, null);
                });
        }
        
            
        //var $more = $("<div>...</div>").addClass('webgis-page-category-item-title more').appendTo(
        //    $("<td>").appendTo($row))
        //    .click(function () {
        //        showCategories();
        //    });

        var $maps = $("<div>")
            .addClass('webgis-portal-sortable')
            .attr('data-sorting-method', 'maps')
            .attr('data-sorting-currentcategory', category)
            .appendTo($div);
        loadMaps(category, $maps);

        appendAllCategoriesTabs($div, category, true);
    }

    // Autoscroll
    if ($("#category-content").offset().top < $([document.documentElement, document.body]).scrollTop()) {
        console.log('autoscroll', $("#category-content").offset().top, $([document.documentElement, document.body]).scrollTop());
        try {
            document.getElementById('category-content').scrollIntoView({ behavior: 'smooth' });
        } catch (ex) {
            $([document.documentElement, document.body]).animate({
                scrollTop: $("#category-content").offset().top
            }, 2000);
        }
    }
};

function showMyProjects() {
    if (isAnonymous)
        return;

    $('#all-maps').find('.webgis-tile-container').empty().css('height', '');
    var $div = $('#category-content').empty();

    var category = '__myProjects';
    var $title = $("<table>").addClass('webgis-page-category-header').appendTo($div);
    var $row = $("<tr>").appendTo($title);

    var $category = $("<div>" + parseCategoryName(category) + "</div>")
        .addClass('webgis-page-category-item-title')
        .data('category', category)
        .appendTo($("<td>").addClass('webgis-page-category-item-selected').css({ width: '100%' }).appendTo($row))

    var $target = $("<div>")
        .css('white-space', 'normal')
        .appendTo($div);

    $.ajax({
        url: webgis.url.relative('proxy/toolmethod/webgis-tools-serialization-loadmap/list-user-projects'),
        dataType: 'json',
        success: function (result) {
            for (var p in result.projects) {
                var project = result.projects[p];

                var $li = $("<div class='webgis-page-map-item project' data-project='" + project.urlname + "'></div>")
                    .appendTo($target)
                    .click(function () {
                        //var cat = $(this).attr('data-category');
                        var project = $(this).attr('data-project');

                        loadProject(project);
                    });
                $("<div>")
                    .addClass('webgis-page-map-item-image')
                    .appendTo($li)

                $("<div>")
                    .addClass("title")
                    .text(project.name)
                    .appendTo(
                        $("<div>")
                            .addClass("webgis-page-map-item-text")
                            .appendTo($li)
                        );
            }

            appendAllCategoriesTabs($div, '__myProjects');
        }
    });
}

function appendAllCategoriesTabs($div, currentCategory, appendMyProjects) {
    var categories = $div.data('categories');

    if (appendMyProjects && isAnonymous === false) {
        categories = categories.slice();  // clone
        categories.push('__myProjects');
    }

    for (var c in categories) {
        var cat = categories[c];
        if (cat === currentCategory || isHiddenCategory(cat))
            continue;

        var $catDiv = $("<div>")
            .addClass('webgis-page-category-header change')
            .data('category', cat)
            .appendTo($div)
            .click(function () {
                webgis.delayed(function (element) {
                    showCategory($(element).data('category'));
                }, 350, this);

                webgis.delayed(function (element) {
                    //if ($("#category-content").offset().top < $([document.documentElement, document.body]).scrollTop()) {
                    //    console.log('autoscroll', $("#category-content").offset().top, $([document.documentElement, document.body]).scrollTop());
                    //    try {
                    //        document.getElementById('category-content').scrollIntoView({ behavior: 'smooth' });
                    //    } catch (ex) {
                    //        $([document.documentElement, document.body]).animate({
                    //            scrollTop: $("#category-content").offset().top
                    //        }, 2000);
                    //    }
                    //}
                    $(element)
                        .addClass('changing')
                        .css({
                            top: '-' + $(element).position().top + 'px'
                        });
                }, 1, this);

            });


        var $catTitle = $("<div>" + parseCategoryName(cat) + "</div>").addClass('webgis-page-category-item-title').appendTo($catDiv);
    }
};

function showCategories() {
    var $tileContainer = $('#category-content').find('.webgis-tile-container');
    if ($tileContainer.height() > 0) {
        $tileContainer.css('height', '0px');
    } else {
        $tileContainer.css('height', $tileContainer.data('height') + 'px');
    }
};

var _categoryMaps = [];
function loadMaps(category, $target) {
    $target.addClass('webgis-tile-container').attr('data-tilewidth', 140);
    $.ajax({
        url: webgis.url.relative('proxy/toolmethod/webgis-tools-portal-publish/category-maps'),
        data: { page: portal, category: category },
        dataType:'json',
        success: function (result) {
            $target.slideUp(1,function () {
                $target.empty();

                _categoryMaps[category] = result.maps;
                rendererCategoryMapTiles($target, category); 

                if (isAuthor == true && result.maps.length == 0) {
                    var $tile = $("<div class='webgis-page-map-new-item tile'></div>").appendTo($target)
                        .click(function () {
                            $.ajax({
                                url: webgis.url.relative('proxy/toolmethod/webgis-tools-portal-publish/remove-category'),
                                data: { 'page-id': portal, category: category },
                                dataType: 'json',
                                success: function (result) {
                                    if (result.success == true)
                                        document.location = './' + portal;
                                    else
                                        alert(result.exception);
                                }
                            });
                        });
                    var $img = $("<div class='webgis-page-map-delete-item-image'></div>").appendTo($tile);
                    var $title = $("<div class='webgis-page-map-item-text'></div>").appendTo($tile);
                    $("<div style='height:50px'>Kategorie entfernen</div>").appendTo($title);
                }
                if (isAuthor == true) {
                    var $tile = $("<div class='webgis-page-map-new-item tile'></div>").appendTo($target)
                        .click(function () {
                            document.location = './' + portal + '/mapbuilder?category=' + webgis.url.encodeString(category);
                        });
                    var $img = $("<div class='webgis-page-map-new-item-image'></div>").appendTo($tile);
                    var $title = $("<div class='webgis-page-map-item-text'></div>").appendTo($tile);
                    $("<div style='height:50px'>Neue Karte erstellen</div>").appendTo($title);
                }

                //refreshTiles();
            });
            
        }
    });
}

var rendererCategoryMapTiles = function ($target, category, addCategoryToName, orderByName) {
    if (_categoryMaps[category]) {
        var maps = _categoryMaps[category];

        for (var i in maps) {
            var map = maps[i];
            var $tile = $("<div class='webgis-page-map-item tile' data-category='" + (map.category || category) + "' data-map='" + map.name + "'></div>")
                .addClass('webgis-search-content active')
                .addClass('webgis-portal-sortable-item')
                .attr('data-sortable', map.name)
                .css('position', 'absolute')
                .click(function () {
                    var cat = $(this).attr('data-category');
                    var map = $(this).attr('data-map');

                    loadMap(cat, map);
                });

            var mapName = map.displayname || map.name;
            $tile.attr('data-map-displayname', mapName);

            var $insertBefore = null;
            if (orderByName === true) {
                $target
                    .find('.webgis-page-map-item')
                    .each(function (i, e) {
                        if ($(e).attr('data-map-displayname').toLowerCase() > mapName.toLowerCase()) {
                            $insertBefore = $(e);
                            return false;
                        }
                    });
            }

            if ($insertBefore) {
                $tile.insertBefore($insertBefore);
            } else {
                $tile.appendTo($target);
            }

            var $img = $("<div class='webgis-page-map-item-image' style=\"background:url(" + webgis.url.relative(portal + "/mapimage?category=" + webgis.url.encodeString(map.category || category) + "&map=" + webgis.url.encodeString(map.name)) + ") no-repeat center center;\"></div>").appendTo($tile);
            var $title = $("<div class='webgis-page-map-item-text'></div>").appendTo($tile);

            
            if (addCategoryToName === true) {
                mapName += " (" + category + ")";
            }

            $("<div class='title' style='min-height:30px'>" + mapName + "</div>").appendTo($title);
            $("<div class='description'>").html(webgis.simpleMarkdown.render(map.description)).appendTo($title);
            $("<div class='more'>").text('Details...')
                .appendTo($title)
                .click(function (e) {
                    e.stopPropagation();
                    var $text = $(this).closest('.webgis-page-map-item-text');

                    webgis.modalDialog(
                        $text.children('.title').text(),
                        function ($content) {
                            var searchValue = $('.webgis-content-search-holder').find('input').val();

                            $content.css('font-size', '1.35em').html(
                                ($text.children('.description').html() || 'Für diese Kart ist keine Beschreibung vorhanden...').highlightText($('.webgis-content-search-holder').find('input').val().split(' '))
                            );
                        }
                    )
                });

            $("<div class='info'>").appendTo($tile);

            if (uaEditable == true) {
                //$tile.css('position', 'relative');
                $("<div class='edit-access' style='position:absolute;right:0px;top:0px;'></div>").appendTo($tile).data('access-name', category + "/" + map.name)
                    .click(function (event) {
                        event.stopPropagation();
                        changeUserAccess($(this).data('access-name'));
                    });
            }

            if (map.hidden == true) {
                $tile.addClass('hidden');
            }

            if (showOptimizationFilter && map.optimized_for) {
                $tile.addClass(map.optimized_for);
            }
        }

        refreshTiles();
    }
};

function refreshTiles() {
    var sumContainerWidth = 0;
    $('.webgis-tile-container').each(function (i, e) {
        sumContainerWidth += $(e).width();
    });

    //console.trace("refreshTiles");
    //console.log('sumContainerWidth', sumContainerWidth);
    webgis.delayed(function () {
        $('.webgis-tile-container').each(function (i, e) {
            var $container = $(e).css({ display: 'block', position: 'relative' });
            var tileWidth = parseInt($container.attr('data-tilewidth')),
                tileHeight = 140,
                containerWidth = $container.outerWidth(),
                gutter = 4;

            var maxTilesPerRow = Math.floor((containerWidth + gutter) / (tileWidth + gutter));
            console.log(containerWidth, maxTilesPerRow);
            var currentTileWidth = (containerWidth - (maxTilesPerRow + 1) * gutter) / maxTilesPerRow;

            //if (sumContainerWidth < 640) {
            //    maxTilesPerRow = 1;
            //    currentTileWidth = '100%';
            //    tileHeight = 100;
            //    $container.addClass('list');
            //} else {
            //    $container.removeClass('list');
            //}

            var items = [];

            $container
                .children('.tile')
                .css({ position: 'absolute', left: 0, top: 0 })
                .each(function (i, e) {
                    if ($(e).css('display') !== 'none') {
                        items.push($(e));
                    }
                });

            var rows = items.length / maxTilesPerRow;
            rows = Math.floor(rows) != rows ? parseInt(rows) + 1 : parseInt(rows);

            var height = rows * tileHeight + (rows + 1) * gutter;
            $container.css('height', '0px').data('height', height);

            var x = 0, y = 0;
            $.each(items, function (j, item) {
                var $item = $(item);
                if ($item.css('display') === 'none')
                    return;

                if ($item.hasClass('webgis-content-search-match')) {
                    $item.
                        css({ position: 'relative', width: '', height: 'auto' });
                } else {

                    if (j > 0 && j % maxTilesPerRow === 0) {
                        y += tileHeight + gutter;
                        x = 0;
                    }

                    $item.css({
                        width: currentTileWidth,
                        left: x, top: y
                    });

                    x += currentTileWidth + gutter;
                }
            });

            webgis.delayed(function () {
                $container.css('height', $container.data('height') + 'px');
            }, 1);
        });
    }, sumContainerWidth == 0 ? 1000 : 500);  // Waren Container schon bereit? Sonst kurz warten
}

function loadMap(category, map) {
    //document.location = portal+'/map?category=' + category + '&map=' + map;
    document.location = webgis.url.relative(portal + '/map/' + webgis.url.encodeString(category) + '/' + webgis.url.encodeString(map));
}

function loadProject(project) {
    document.location = webgis.url.relative(portal + '/map?project=' + project);
}

if (!window.webgis) {
    webgis = {};
    webgis.url = new function () {

        this.encodeString = function (v) {

            if (v && v.indexOf) {
                while (v.indexOf('<') != -1) v = v.replace('<', '&lt;');
                while (v.indexOf('>') != -1) v = v.replace('>', '&gt;');
            }

            v = encodeURI(v);

            while (v.indexOf('&') != -1) v = v.replace('&', '%26');
            while (v.indexOf('+') != -1) v = v.replace('+', '%2b');
            while (v.indexOf('#') != -1) v = v.replace('#', '%23');
            while (v.indexOf('=') != -1) v = v.replace('=', '%3d');

            return v;
        };

        this.relative = function (url) {
            var loc = document.location.toString().split('?')[0];
            if (loc.endsWith('/'))
                return '../' + url;
            return url;
        };
    };
}

String.prototype.endsWith = function (suffix) {
    return this.indexOf(suffix, this.length - suffix.length) !== -1;
};

function showCustomContentEditor(c) {
    var editor = null;
    var portalUrl = window.portalEnv && window.portalEnv.portalUrl ?
        window.portalEnv.portalUrl :
        '/';

    $.ajax({
        url: portalUrl + 'customcontent/' + portal + '/load?c=' + c + '&f=json',
        success: function (result) {
            if (result.success === false) {
                alert(result.exception);
            } else {
                $('body').webgis_modal({
                    id: 'custom-editor-dialog',
                    title: c,
                    onload: function ($content) {
                        var $editor = $('<div style="position:absolute;left:0px;top:0px;right:0px;bottom:0px;"></div>').appendTo($content);
                        var $bottom = $('<div style="position:absolute;left:0px;height:35px;right:0px;bottom:0px;text-align:right;padding:5px"></div>').appendTo($editor);

                        $("<button>Save</button>")
                            .addClass('webgis-button')
                            .appendTo($bottom)
                            .click(function () {
                                $.ajax({
                                    url: portalUrl + 'customcontent/' + portal + '/save',
                                    type:'post',
                                    data: { c: c, content: editor.getValue() },
                                    success: function (saveResult) {
                                        if (saveResult.success === false) {
                                            alert(saveResult.exception);
                                        } else {
                                            $(null).webgis_modal('close', { id: 'custom-editor-dialog' });
                                        }
                                    }
                                });
                            });

                        $('<div id="code-editor" style="position:absolute;left:0px;top:0px;right:0px;bottom:45px;"></div>').appendTo($editor);
                        webgis.require('monaco-editor', function (result) {

                            var language = 'text';
                            if (c.endsWith('.js')) {
                                language = 'javascript';
                            } else if (c.endsWith('.css')) {
                                language = 'css';
                            }

                            editor = monaco.editor.create(document.getElementById('code-editor'), {
                                language: language,
                                automaticLayout: true,
                                theme: 'vs'
                            });

                            editor.setValue(result.content);
                        }, result);
                    }
                });
            }
        }
    });
};