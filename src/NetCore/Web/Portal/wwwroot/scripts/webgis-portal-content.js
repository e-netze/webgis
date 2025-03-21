$(document).ready(function () {
    $('#image-upload-form').attr('action', webgis.url.relative(portal + '/uploadcontentimage'));
});

function initEditables() {
    $('.page-editable-content').portal_contenteditor();

    webgis.require('sortable', function () {
        Sortable.create($('#page-content-list').get(0), {
            animation: 150,
            ghostClass: 'webgis-sorting',
            onSort: function (event) {
                $.ajax({
                    url: webgis.url.relative(portal + '/sortcontent'),
                    data: { sorting: $.webgis_portal_contentItemSorting() },
                    type: 'post',
                    success: function (result) {

                    }
                });
            }
        });
    });

    $('#page-content-new-item').click(function () {
        var $newLi = $("<li class='page-content-item page-editable-content page-content-item-block' data-content-id='" + guid() + "' data-content-type='html'><h1>Neuer Inhalt...</h1></li>").appendTo('#page-content-list').portal_contenteditor();
    });
}

(function ($) {

    $.fn.portal_contenteditor = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on jQuery.portal_contenteditor');
        }
    };

    var defaults = {
    };

    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);

            return this.each(function () {
                new initUI(this, options);
            });
        }
    };

    var initUI = function (parent, options) {
        var $this = $(parent);

        $this.addClass('editable-content');
        $this.html("<span class='content-edit-content'>" + $this.html() + "</span>");

        var $btn = $("<div class='content-edit-button'></div>").prependTo($this)
            .click(function () {
                var contentElement = this._contentElement;
                var content = $(contentElement).html();

                var type=$(contentElement).closest('.editable-content').attr('data-content-type');

                $('body').webgis_modal({
                    title: type=='input' ? 'Titel bearbeiten' : type=='text' ? 'Beschreibung bearbeiten' : 'Inhalt bearbeiten',
                    height: type=='input' ? '130px' : type=='text' ? '230px' : '90%',
                    onload: function ($content) {
                        $content.css('padding','8px');
                        switch (type) {
                            case 'input':
                                $("<input class='content-editor' id='content-editor' style='width:98.7%;' value='"+content + "' />").appendTo($content);
                                break;
                            case 'text':
                                $("<textarea class='content-editor' id='content-editor' rows='5' style='width:98.7%;'>" + content + "</textarea>").appendTo($content);
                                break;
                            case 'html':
                                $("<textarea class='content-editor' id='content-editor' style='width:98%;height:95%'>" + content + "</textarea>").appendTo($content);
                                tinymce.init({
                                    selector: '#content-editor',
                                    plugins: "textcolor colorpicker emoticons fullscreen image imagetools autolink link",
                                    menubar: "view insert tools",
                                    height: $content.height() - 120,
                                    statusbar:false,
                                    //toolbar: "forecolor emoticons fullscreen image link",
                                    file_browser_callback: function (field_name, url, type, win) {
                                        if (type == 'image') $('#image-upload-form input').click();
                                    }
                                });
                                break;
                        }

                        

                        $("<button style='float:right;margin:2px 0px 0px 5px'>Inhalt Übernehmen</button>").appendTo($content)
                            .click(function () {
                                var mce = tinymce.get('content-editor');
                                var newContent = '';
                                switch ($(contentElement).closest('.editable-content').attr('data-content-type')) {
                                    case 'input':
                                        newContent = $('#content-editor').val();
                                        break;
                                    case 'text':
                                        newContent = $('#content-editor').val();
                                        break;
                                    case 'html':
                                        newContent = mce.getContent();
                                        break;
                                }
                                if (newContent.indexOf('<p>') == 0)
                                    newContent = newContent.substr(3, newContent.length - 7);


                                $.ajax({
                                    url: webgis.url.relative(portal+'/editcontent'),
                                    data: { contentId: $(contentElement).closest('.editable-content').attr('data-content-id'), content: newContent, sorting: $.webgis_portal_contentItemSorting() },
                                    type: 'post',
                                    success: function (result) {
                                        $(contentElement).html(newContent);
                                        if (mce)
                                            mce.remove();
                                        $('.content-editor').remove();
                                        $('body').webgis_modal('close');
                                    }
                                });

                            });

                        if (type == 'html') {
                            $("<button style='float:right;margin:2px 0px 0px 5px'>Inhalt entfernen</button>").appendTo($content)
                                .click(function () {
                                    if (!confirm("Den Inhalt dauerhaft löschen?"))
                                        return;

                                    var mce = tinymce.get('content-editor');
                                    var contentId=$(contentElement).closest('.editable-content').attr('data-content-id');
                                    $.ajax({
                                        url: webgis.url.relative(portal + '/removecontent'),
                                        data: { contentId: contentId },
                                        type: 'post',
                                        success: function (result) {
                                            if (mce)
                                                mce.remove();
                                            $('.content-editor').remove();
                                            $('body').webgis_modal('close');
                                            $(".page-content-item[data-content-id='"+contentId+"']").remove();
                                        }
                                    });
                                });
                        }
                       
                        $('#currentEditContentId').val($(contentElement).closest('.editable-content').attr('data-content-id'));
                    },
                    onclose: function () {
                        var mce = tinymce.get('content-editor');
                        if (mce)
                            mce.remove();
                        $('.content-editor').remove();
                    }
                });
            });

        $btn.get(0)._contentElement = $this.find('.content-edit-content').get(0);

    };

    $.webgis_portal_contentItemSorting = function () {
        var ret = '';
        $('.page-content-item').each(function (i, e) {
            if (ret != '') ret += ",";
            ret += $(e).attr('data-content-id');
        });
        return ret;
    };

})(webgis.$ || jQuery);

function guid() {
    function s4() {
        return Math.floor((1 + Math.random()) * 0x10000)
          .toString(16)
          .substring(1);
    }
    return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
      s4() + '-' + s4() + s4() + s4();
}