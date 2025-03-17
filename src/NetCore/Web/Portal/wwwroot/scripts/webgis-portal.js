$(document).ready(function () {

    /*
    webgis.init(function () {


        
    });
    */

    $.ajax({
        url: webgis.url.relative('proxy/toolmethod/webgis-tools-portal-portal/page-content'),
        data: { 'page-id': portal },
        dataType: 'json',
        success: function (result) {

            for (var s in result.sorting) {
                var contentId = result.sorting[s];

                if (contentId == 'page-title' || contentId == 'page-description') {
                    var content = getContent(contentId, result.contentitems);
                    if (content)
                        $('#' + contentId).html(content);
                }
                else if (contentId == 'page-maps') {
                    var $mapContent=$("<li class='page-content-item page-content-item-block' data-content-id='page-maps' style='display:none'><div style='text-align:left'><h1>Karten</h1><br/><ul class='webgis-page-category-list' style='display:inline-block;margin:0px;padding:0px;width:100%' id='list-categories'></ul></div></li>").appendTo('#page-content-list');
                    if (isOwner == true) {
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
});

function beginShowContent($elem) {
    if ($elem == null || $elem.length == 0) {
        return;
    }

    $elem.slideDown(function(){
        beginShowContent($elem.next());
    })
}

function getContent(contentId, contentItems) {
    for (var i in contentItems) {
        var item = contentItems[i];
        if (item.id == contentId)
            return item.content;
    }
    return '';
}

function loadMapCategories() {
    $.ajax({
        url: webgis.url.relative('proxy/toolmethod/webgis-tools-portal-publish/map-categories'),
        data: { 'page-id': portal },
        dataType: 'json',
        success: function (result) {
            var count = result.categories.length + 1;

            $("<li><div id='category-content'></div></li>").appendTo('#list-categories');
            $('#category-content').addClass('webgis-page-category-item').data('categories', result.categories);

            var firstVisibleCategory = null;
            for (var c in result.categories) {
                if (!isHiddenCategory(result.categories[c])) {
                    firstVisibleCategory = result.categories[c];
                    break;
                }
            }

            if (firstVisibleCategory) {
                webgis.delayed(function () {
                    showCategory(firstVisibleCategory);
                }, 700);
               
            } else if(isAuthor) {
                $("<li class='webgis-page-category-item' style='width:100%'><div class='webgis-page-category-item-title'>MapBuilder...<div></li>").appendTo('#list-categories')
                    .click(function () {
                        document.location = './' + portal + '/mapbuilder/';
                    });
            }

            return;

            for (var c in result.categories) {

                var category = result.categories[c];

                var $li = $("<li style='' class='webgis-page-category-item' data-category='" + category + "'></li>").appendTo('#list-categories');
                var $title = $("<div class='webgis-page-category-item-title style-target'>" + category + "</div>").appendTo($li)
                    .click(function (event) {
                        event.stopPropagation();
                        if ($(this).parent().hasClass('webgis-page-category-item-selected')) {
                            $(this).parent().removeClass('webgis-page-category-item-selected');
                            $(this).parent().find('.webgis-page-category-item-content').slideUp();
                            return;
                        }

                        $(this).closest('ul').find('.webgis-page-category-item-selected').removeClass('webgis-page-category-item-selected');
                        $(this).parent().addClass('webgis-page-category-item-selected');
                        var cat = $(this).parent().attr('data-category');
                        loadMaps(cat, $(this).parent().find('.webgis-page-category-item-content'));
                    });
                var $div = $("<div class='webgis-page-category-item-content'></div>").appendTo($li);
                if (uaEditable == true) {
                    $("<div class='edit-access'></div>").appendTo($title).data('access-name',category)
                        .click(function (event) {
                            event.stopPropagation();
                            changeUserAccess($(this).data('access-name'));
                        });
                }
            }

            if (isAnonymous == false) {
                var $li = $("<li style='' class='webgis-page-category-item' data-category='my-projects'></li>").appendTo('#list-categories');
                var $title = $("<div class='webgis-page-category-item-title'>Meine Projekte</div>").appendTo($li)
                        .click(function (event) {
                            event.stopPropagation();
                            if ($(this).parent().hasClass('webgis-page-category-item-selected')) {
                                $(this).parent().removeClass('webgis-page-category-item-selected');
                                $(this).parent().find('.webgis-page-category-item-content').slideUp();
                                return;
                            }

                            $(this).closest('ul').find('.webgis-page-category-item-selected').removeClass('webgis-page-category-item-selected');
                            $(this).parent().addClass('webgis-page-category-item-selected');
                            var cat = $(this).parent().attr('data-category');
                            loadProjects($(this).parent().find('.webgis-page-category-item-content'));
                        });
                var $div = $("<div class='webgis-page-category-item-content'></div>").appendTo($li);
            }
            $('#list-categories').find('li:first').find('div:first').trigger('click');

            if (count == 1 && isAuthor) {
                $("<li class='webgis-page-category-item' style='width:100%'><div class='webgis-page-category-item-title'>MapBuilder...<div></li>").appendTo('#list-categories')
                    .click(function () {
                        document.location = './' + portal + '/mapbuilder/';
                    });
            }
        }
    });
}

function parseCategoryName(category) {
    if (category == "_Favoriten")
        return "♥ Favoriten";
    return category;
}

function isHiddenCategory(category) {
    if (isAuthor || isOwner)
        return false;

    return category && category.indexOf('@@') === 0;
}

function showCategory(category) {
    var $div = $('#category-content').empty();

    var $title = $("<table>").addClass('webgis-page-category-header').appendTo($div);
    var $row = $("<tr>").appendTo($title);

    var $category = $("<div>" + parseCategoryName(category) + "</div>").addClass('webgis-page-category-item-title').appendTo(
        $("<td>").addClass('webgis-page-category-item-selected').css({ width: '100%' }).appendTo($row))
        .click(function () {
            showCategories();
        });
    if (uaEditable == true) {
        $("<div class='edit-access'></div>").appendTo($category).data('access-name', category)
            .click(function (event) {
                event.stopPropagation();
                changeUserAccess($(this).data('access-name'));
            });
    }
    var $more = $("<div>...</div>").addClass('webgis-page-category-item-title more').appendTo(
        $("<td>").appendTo($row))
        .click(function () {
            showCategories();
        });

    var $maps = $("<div>").appendTo($div);
    laodMaps(category, $maps);

    var categories = $div.data('categories');
    for (var c in categories) {
        var cat = categories[c];
        if (cat === category || isHiddenCategory(cat))
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
                    if ($("#category-content").offset().top < $([document.documentElement, document.body]).scrollTop()) {
                        console.log('autoscroll');
                        $([document.documentElement, document.body]).animate({
                            scrollTop: $("#category-content").offset().top
                        }, 100);
                    }
                    $(element)
                        .addClass('changing')
                        .css({
                            top: '-' + $(element).position().top + 'px'
                        });
                },1,this);
                
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

    //var $div = $('#category-content')
    //    .addClass('webgis-page-category-item').
    //    empty();    
    //var categories = $div.data('categories');

    //var $container = $('<div>').addClass('webgis-tile-container').attr('data-tilewidth',140).appendTo($div);
    //for (var c in categories) {
    //    var category = categories[c];

    //    var $tile = $("<div class='webgis-page-map-item catetory tile' data-category='" + category + "'></div>").appendTo($container)
    //        .click(function () {
    //            var cat = $(this).attr('data-category');
    //            showCategory(cat);
    //        });

    //    var $title = $("<div class='webgis-page-map-item-title'>"+category+"</div>").appendTo($tile);
    //}
    //refreshTiles();
};

function laodMaps(category, $target) {
    //$target.closest('ul').find('.webgis-page-category-item-content').slideUp();
    //$target.empty();
    //$("<img src='" + webgis.url.relative('content/img/hourglass/loader1.gif') + "' />").appendTo($target);
    //$target.slideDown();

    $target.addClass('webgis-tile-container').attr('data-tilewidth', 140);
    $.ajax({
        url: webgis.url.relative('proxy/toolmethod/webgis-tools-portal-publish/category-maps'),
        data: { page: portal, category: category },
        dataType:'json',
        success: function (result) {
            $target.slideUp(1,function () {
                $target.empty();

                for (var i in result.maps) {
                    var map = result.maps[i];
                    var $tile = $("<div class='webgis-page-map-item tile' data-category='" + (map.category || category) + "' data-map='" + map.name + "'></div>")
                        .css('position','absolute')
                        .appendTo($target)
                        .click(function () {
                            var cat = $(this).attr('data-category');
                            var map = $(this).attr('data-map');

                            loadMap(cat, map);
                        });
                    //$tile.css('background-image', "url(./mapimage?category=" + category + "&map=" + map.urlname + ")");

                    var $img = $("<div class='webgis-page-map-item-image' style=\"background:url(" + webgis.url.relative(portal + "/mapimage?category=" + webgis.url.encodeString(map.category || category) + "&map=" + webgis.url.encodeString(map.name)) + ") no-repeat center center\"></div>").appendTo($tile);
                    var $title = $("<div class='webgis-page-map-item-text'></div>").appendTo($tile);
                    $("<div style='height:50px'>" + (map.displayname || map.name) + "</div>").appendTo($title);

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
                }
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
                            document.location = './' + portal + '/mapbuilder';
                        });
                    var $img = $("<div class='webgis-page-map-new-item-image'></div>").appendTo($tile);
                    var $title = $("<div class='webgis-page-map-item-text'></div>").appendTo($tile);
                    $("<div style='height:50px'>Neue Karte erstellen</div>").appendTo($title);
                }

                refreshTiles();
                //$target.slideDown();
            });
            
        }
    });
}

function refreshTiles() {
    $('.webgis-tile-container').each(function (i, e) {
        var $container = $(e).css({ display: 'block', position: 'relative' });
        var tileWidth = parseInt($container.attr('data-tilewidth')),
            tileHeight = 140,
            containerWidth = $container.width(),
            gutter = 4;

        var maxTilesPerRow = Math.floor((containerWidth + gutter) / (tileWidth + gutter));
        var currentTileWidth = (containerWidth - (maxTilesPerRow + 1) * gutter) / maxTilesPerRow;

        var items = $container.children('.tile').css({ position: 'absolute', left: 0, top: 0 });
        var rows = items.length / maxTilesPerRow;
        rows = Math.floor(rows) != rows ? parseInt(rows) + 1 : parseInt(rows);

        var height = rows * tileHeight + (rows + 1) * gutter + 5;
        $container.css('height', '0px').data('height', height);

        var x = 0, y = 0;
        items.each(function (j, item) {
            var $item = $(item);

            if (j > 0 && j % maxTilesPerRow === 0) {
                y += tileHeight + gutter;
                x = 0;
            }

            $item.css({
                width: currentTileWidth,
                left: x, top: y
            });

            x += currentTileWidth + gutter;
        });

        webgis.delayed(function () {
            $container.css('height', $container.data('height') + 'px');
        }, 1);
    });
}

function loadProjects($target) {
    $target.closest('ul').find('.webgis-page-category-item-content').slideUp();
    $target.empty();

    $.ajax({
        url: webgis.url.relative('proxy/toolmethod/webgis-tools-serialization-loadmap/list-user-projects'),
        dataType: 'json',
        success: function (result) {
            for (var p in result.projects) {
                var project = result.projects[p];

                var $li = $("<li class='webgis-page-map-item' data-category='my-projects' data-project='" + project.urlname + "'>" + project.name + "</li>").appendTo($target)
                        .click(function () {
                            //var cat = $(this).attr('data-category');
                            var project = $(this).attr('data-project');

                            loadProject(project);
                        });

                $target.slideDown();
            }
        }
    });
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