$(document).ready(function () {
    var $sidebarList = $('.webportal-layout-sidebar-items.center');
    if ($sidebarList.length > 0) {
        $('.webgis-ui-sidebar-item-target').each(function (i, e) {
            var $e = $(e);
            var img = $e.attr('data-sidebaritem-img'), label = $e.attr('data-sidebar-label');

            var $item = $("<li class='webportal-layout-sidebar-item'></li>").appendTo($sidebarList);
            $("<img>").attr('src', img).appendTo($item);
            $("<a href=''>").html(label).appendTo($item)
                .data('targetElement', $e)
                .click(function (e) {
                    e.stopPropagation();
                    var $targetElement = $(this).data('targetElement');
                    
                    if ($targetElement.attr('href')) {
                        document.location.href = $targetElement.attr('href');
                    } else {
                        $targetElement.trigger('click');
                    }

                    return false;
                });
        });
    }
});

function generateRandomToken(length) {
    var chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';

    var token = '';
    for (var i = 0; i < length; i++) {
        token += chars[parseInt((Math.random()) * 0x10000) % chars.length];
    }

    return token;
}