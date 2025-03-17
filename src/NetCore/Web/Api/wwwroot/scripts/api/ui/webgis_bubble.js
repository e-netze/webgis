(function ($) {
    "use strict";
    $.fn.webgis_bubble = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_bubble');
        }
    };
    var defaults = {
        id: null,
        map: null,
        onclick: null,
        bgImage: null,
        text: null,
        textClass: null,
        onDisable: null,
        onEnable: null,
        onParked: null,
        onDragEnd: null,
        onDragMove: null,
        trashPosition: 'middle',
        parkPosition: null,
        disabled: true,
        rememberDisabled: false,
        parkable: false,
        top: 0,
        helpName: null,
        dragTolerance: 1
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {

                var $bubble = $(this).data('map', options.map).data('options', options).addClass('webgis-bubble');

                if (options.id) {
                    if (options.rememberDisabled === true && webgis.localStorage.get('_bubble-' + options.id + '-disabled') === '1') {
                        options.disabled = true;
                    }
                }

                if (options.onClick) {
                    $bubble.data('onclick', options.onClick)
                        .click(function (e) {
                            //console.log('onclick');
                            if ($(this).hasClass('is-longclick'))
                                return;
                            var options = $(this).data('options');
                            var map = $(this).data('map');
                            if ($(this).data('isdragging') !== true) {
                                if ($(this).hasClass('disabled')) {
                                    if (options.helpName)
                                        map.ui.showHelp(options.helpName);
                                    else
                                        webgis.alert('Zum Aktivieren des Werkzeugs, die Kugel ziehen...', 'Hinweis');
                                }
                                else if ($(this).data('onclick')) {
                                    $(this).data('onclick').apply(this, [e]);
                                }
                            }
                        });
                }
                options.map.events.on('resize', function (e, sender) {
                    var parentWidth = $(this).parent().width();
                    $(this).css('left', parentWidth - $(this).outerWidth(true));
                    setDisableFlag($(this));
                }, $bubble);
                if (options.bgImage)
                    $bubble.css({ backgroundImage: 'url(' + options.bgImage + ')' });
                if (options.text) {
                    var $text = $("<div>" + options.text + "</div>").addClass('text').appendTo($bubble.addClass('text'));
                    if (options.textClass) {
                        webgis.variableContent.init($text, options.textClass);
                    }
                }
                var top = Math.max(trashTopPosition($bubble), options.top) +
                    $bubble.outerHeight(true) * (options.disabled === true ? 0 : 1.1) * (options.trashPosition === 'bottom' ? -1 : 1);
                if (options.trashPosition === 'none')
                    top = Math.max(parkTopPosition($bubble), options.top);
                $bubble.css({
                    left: $(this).parent().width() - $bubble.outerWidth(true),
                    top: top
                });
                if (options.parkable)
                    $bubble.addClass('return-to-position');
                var pointerDownTimer = new webgis.timer(function ($bubble) {
                    //console.log('timer');
                    if ($bubble.data('isdragging') !== true)
                        $bubble.addClass('is-longclick');
                }, 1000, $bubble);
                $bubble.data('pointer-down-timer', pointerDownTimer);
                function addDraggabilly($bubble) {
                    //console.log('add draggabilly');
                    var $draggy = $bubble.draggabilly();
                    $draggy.on('dragStart', function (e, pointer) {
                        //console.log('dragstart');
                        //$(this).data('isdragging', true)
                        //    .data('dragStartStatus', $(this).hasClass('disabled') === false)
                        //    .webgis_bubble('showTrash');
                    });
                    $draggy.on('dragEnd', function (e, pointer) {
                        $(this).data('moveOffsetXY', null);
                        var options = $(this).data('options');

                        if ($(this).data('isdragging') !== true) {
                            if (!$(this).hasClass('is-longclick'))
                                $(this).trigger('click');
                            return;
                        }

                        if (!$(this).hasClass('disabled') && !$(this).hasClass('parked') && options.onDragEnd) {
                            var relOffset = relativeOffsetTo(this, pointer.target);
                            applyOffsetXY(pointer);

                            //console.log(pointer.clientX, pointer.offsetX, relOffset.x);
                            //console.log(pointer.clientY, pointer.offsetY, relOffset.y);

                            options.onDragEnd.apply(this, [{
                                clientX: pointer.clientX - pointer.offsetX + relOffset.x,
                                clientY: pointer.clientY - pointer.offsetY + relOffset.y
                            }]);
                        }
                        webgis.delayed(function (e) {
                            $(e).data('isdragging', false).webgis_bubble('hideTrash');
                            var options = $(e).data('options');
                            var status = $(e).data('dragStartStatus'), changedStatus = false;
                            if (status === true && $(e).hasClass('disabled')) {
                                if (options.onDisable)
                                    options.onDisable.apply(e);
                                changedStatus = true;
                            }
                            else if (status === false && !$(e).hasClass('disabled')) {
                                if (options.onEnable)
                                    options.onEnable.apply(e);
                                changedStatus = true;
                            }
                            if (options.parkable) {
                                if ($(e).hasClass('disabled')) {
                                    status = false;
                                }
                                else {
                                    status = true;
                                }
                                $(e).addClass('returning');
                                webgis.delayed(function (e) {
                                    var top = trashTopPosition($bubble);
                                    if (status)
                                        top = parkTopPosition($bubble);
                                    $(e).css({
                                        left: $(e).parent().width() - $bubble.outerWidth(true),
                                        top: top
                                    });
                                    webgis.delayed(function (e) {
                                        setDisableFlag($(e).removeClass('returning'));
                                    }, 500, e);
                                    if ($(e).hasClass('parked') && options.onParked)
                                        options.onParked.apply(e);
                                }, 10, e);
                            }
                        }, 10, this);
                    });
                    $draggy.on('dragMove', function (e, pointer, moveVector) {
                        var $this = $(this);

                        if (webgis.is_iOS && !$this.data('moveOffsetXY')) {
                            applyOffsetXY(pointer);  // offset beim Start merken. Ist nicht sehr genau, aber besser gehts leider nicht fürs erste
                            $this.data('moveOffsetXY', [pointer.offsetX, pointer.offsetY]);
                        }

                        var options = $this.data('options');
                        if ($this.data('isdragging') !== true &&
                            (moveVector.x * moveVector.x + moveVector.y * moveVector.y) >= options.dragTolerance * options.dragTolerance) { // Toleranz: wenn kleiner, dann klick...
                            $this.data('isdragging', true)
                                .data('dragStartStatus', $(this).hasClass('disabled') === false)
                                .webgis_bubble('showTrash');
                        }
                        setDisableFlag($(this), pointer.clientX, pointer.clientY);
                        //console.log(pointer);

                        if (!$this.hasClass('disabled') && !$this.hasClass('parked') && options.onDragMove) {
                            var relOffset = relativeOffsetTo(this, pointer.target);

                            if (webgis.is_iOS === true) {
                                //applyOffsetXY(pointer);
                                if ($this.data('moveOffsetXY')) {
                                    try {
                                        pointer.offsetX = $this.data('moveOffsetXY')[0];
                                        pointer.offsetY = $this.data('moveOffsetXY')[1];
                                    } catch (ex) { }
                                }
                            }

                            //console.log('relOffset', [relOffset.x, relOffset.y]);
                            //console.log('offsetXY', [pointer.offsetX, pointer.offsetY]);
                            //console.log('clientXY', [pointer.clientX, pointer.clientY]);
                            //console.log('result', [pointer.clientX - pointer.offsetX + relOffset.x, pointer.clientY - pointer.offsetY + relOffset.y]);
                            //pointer.offsetX = pointer.offsetY = 0;

                            options.onDragMove.apply(this, [{
                                clientX: pointer.clientX - pointer.offsetX + relOffset.x,
                                clientY: pointer.clientY - pointer.offsetY + relOffset.y
                            }]);
                        }
                    });
                    $draggy.on('pointerDown', function (e, pointer) {
                        //console.log('pointer down...');
                        $(this).data('pointer-down-timer').start();
                    });
                    $draggy.on('pointerUp', function (e, pointer) {
                        if ($(this).hasClass('is-longclick')) {
                            var options = $(this).data('options');
                            (new webgis.timer(function () {
                                $bubble.removeClass('is-longclick');
                            }, 100, $(this))).start();
                            if (options.onLongClick) {
                                options.onLongClick.apply(this);
                            }
                        }
                        else {
                            $(this).data('pointer-down-timer').stop();
                            if (webgis.is_iOS) {   // click down work with iOS
                                $(this).trigger('click');
                            }
                        }
                        //console.log('pointer up...');
                    });
                }

                //rebuildTimer = new webgis.timer(function (args) {
                //    webgis.events.off('on-ios-swipe', funcOn_iOS_swipe);

                //    try {
                //        console.log('renew-bubble');
                //        var options = args.options;
                //        var classNames = args.classNames;

                //        var $newBubble = $("<div>").appendTo(args.parent);

                //        $.each(classNames, function (i, className) {
                //            if (className === 'webgis-bubble' || className.indexOf('webgis-') !== 0)
                //                return;

                //            console.log('addClass to new bubble', className);
                //            $newBubble.addClass(className);
                //        });

                //        $newBubble.webgis_bubble(options);
                //    } catch (e) {
                //        console.log('error');
                //        console.log(e.error);
                //        console.trace(e); 
                //    }
                //},
                //500,
                //{ classNames: $bubble.attr('class').split(' '), options: options, parent: $bubble.parent() });

                //webgis.events.on('on-ios-swipe', funcOn_iOS_swipe, $bubble); 
                
                if ($.fn.draggabilly) {
                    addDraggabilly($bubble);
                }
                else {
                    webgis.loadScript(webgis.baseUrl + '/scripts/draggabilly/draggabilly.pkgd.min.js', '', function () {
                        addDraggabilly($bubble);
                    });
                }
                setDisableFlag($bubble);
            });
        },
        remove: function () {
            var options = $(this).data('options');
            if (options && options.onDisable)
                options.onDisable.apply(this);
            $(this).remove();
        },
        showTrash: function () {
            var $bubble = $(this);
            var options = $bubble.data('options');
            if (options.trashPosition !== 'none') {
                var $trash = $("<div>").addClass('webgis-bubble-trash').appendTo($bubble.parent());
                $trash.css({
                    left: $(this).parent().width() - $trash.outerWidth(true),
                    top: trashTopPosition($bubble)
                });
            }
            if (options.parkable) {
                var $park = $("<div>").addClass('webgis-bubble-park').appendTo($bubble.parent());
                $park.css({
                    left: $(this).parent().width() - $park.outerWidth(true),
                    top: parkTopPosition($bubble)
                });
            }
        },
        hideTrash: function () {
            $(this).parent().find('.webgis-bubble-trash').remove();
            $(this).parent().find('.webgis-bubble-park').remove();
        },
        isEnabled: function () {
            return !$(this).hasClass('disabled');
        },
        moveTo: function (options) {
            var x = options.x, y = options.y;
            if (options.target) {
                var targetOffset = $(options.target).offset();
                x += targetOffset.left;
                y += targetOffset.top;
            }
            $(this).css({
                left: x,
                top: y
            });
        },
        park: function (options) {
            webgis.delayed(function (e) {
                var $bubble = $(e);
                var top = parkTopPosition($bubble);
                $bubble.css({
                    left: $bubble.parent().width() - $bubble.outerWidth(true),
                    top: top
                });
                webgis.delayed(function (e) {
                    setDisableFlag($(e).removeClass('returning'));
                }, 500, e);
                if ($(e).hasClass('parked') && options.onParked)
                    options.onParked.apply(e);
            }, 10, this);
        }
    };
    var trashTopPosition = function ($bubble) {
        var options = $bubble.data('options');
        var $trash = $bubble.parent().find('.webgis-bubble-trash');
        var top = 0;
        if (options && options.trashPosition) {
            switch (options.trashPosition) {
                case 'top':
                    top = 40;
                    break;
                case 'bottom':
                    top = $bubble.parent().height() - 65 - ($trash.length > 0 ? $trash.outerHeight(true) : 80);
                    break;
                case 'middle':
                    top = $bubble.parent().height() / 2 - ($trash.length > 0 ? $trash.outerHeight(true) : 80) / 2;
                    break;
                case 'none':
                    top = -100;
                    break;
                default:
                    top = options.trashPosition;
                    break;
            }
        }
        return top;
    };
    var parkTopPosition = function ($bubble) {
        var options = $bubble.data('options');
        var $trash = $bubble.parent().find('.webgis-bubble-trash');
        var top = 0;
        switch (options.parkPosition || options.trashPosition) {
            case 'top':
                top = 40 + (options.trashPosition === 'none' ? 0 : ($trash.length > 0 ? $trash.outerHeight(true) : 80));
                break;
            case 'bottom':
                top = $bubble.parent().height() - 65 - (options.trashPosition === 'none' ? 0 : ($trash.length > 0 ? $trash.outerHeight(true) : 80)) * 2;
                break;
            case 'middle':
                top = $bubble.parent().height() / 2 - ($trash.length > 0 ? $trash.outerHeight(true) : 80) / 2 + ($trash.length > 0 ? $trash.outerHeight(true) : 80);
                break;
            case 'none':
                top = options.parkable === true ? (options.top | 0) : -100;
                break;
            default:
                top = parseInt(options.trashPosition) + 80;
                break;
        }
        return top;
    };
    var setDisableFlag = function ($bubble, x, y) {
        var options = $bubble.data('options');
        var width = $bubble.outerWidth(true);
        x = x ? x : parseInt($bubble.css('left')) + $bubble.outerWidth(true) / 2;
        y = y ? y : parseInt($bubble.css('top')) + $bubble.outerHeight(true) / 2;
        var trashX = $bubble.parent().width() - 40, trashY = trashTopPosition($bubble) + $bubble.outerHeight(true) / 2;
        var dist2 = (x - trashX) * (x - trashX) + (y - trashY) * (y - trashY);
        if (dist2 < width * width) {
            $bubble.addClass('disabled');
            if (options && options.id && options.rememberDisabled === true) {
                webgis.localStorage.set('_bubble-' + options.id + '-disabled', '1');
            }
        } else {
            $bubble.removeClass('disabled');
            if (options && options.id && options.rememberDisabled === true) {
                webgis.localStorage.set('_bubble-' + options.id + '-disabled', '0');
            }
        }
        if (options && options.parkable)
            setParkedFlag($bubble, x, y);
    };
    var setParkedFlag = function ($bubble, x, y) {
        var width = $bubble.outerWidth(true);
        x = x ? x : parseInt($bubble.css('left')) + $bubble.outerWidth(true) / 2;
        y = y ? y : parseInt($bubble.css('top')) + $bubble.outerHeight(true) / 2;
        var parkX = $bubble.parent().width() - 40, parkY = parkTopPosition($bubble) + $bubble.outerHeight(true) / 2;
        var dist2 = (x - parkX) * (x - parkX) + (y - parkY) * (y - parkY);
        if (dist2 < width * width)
            $bubble.addClass('parked');
        else
            $bubble.removeClass('parked');
    };
    var relativeOffsetTo = function (bubble, element) {
        if (bubble === element)
            return { x: 0, y: 0 };
        var bubblePos = $(bubble).offset();
        var elementPos = $(element).offset();
        return {
            x: parseInt(bubblePos.left) - parseInt(elementPos.left),
            y: parseInt(bubblePos.top) - parseInt(elementPos.top)
        };
    };
    var applyOffsetXY = function (e) {
        if (webgis.is_iOS) {
            //console.log('isBubble', $(e.target).hasClass('webgis-bubble'));

            var target = e.target, dx=0, dy=0;
            if (!$(e.target).hasClass('webgis-bubble')) {
                target = $(e.target).closest('.webgis-bubble').get(0);
                var dOffset = relativeOffsetTo(target, e.target);
                dx = dOffset.x; 
                dy = dOffset.y;
                //console.log('dOffset', [dx, dy]);
            }
            try {
                //if (!e.offsetX && e.offsetX !== 0) {  // for iOS Devices
                e.offsetX = e.pageX - target.offsetLeft + dx;
                e.offsetY = e.pageY - target.offsetTop + dy;
                //}
            } catch (ex) { }
        }
    };
    var getTransform = function (element) {
        var cssTransform = $(element).css('transform');
        if (!cssTransform)
            return { x: 0, y: 0 };
        var matrix = cssTransform.replace(/[^0-9\-.,]/g, '').split(',');
        return {
            x: parseInt(matrix[4]),
            y: parseInt(matrix[5])
        };
    };

    //var rebuildTimer = null;
    //var funcOn_iOS_swipe = function (channel, sender, args) {
    //    var $bubble = this;
    //    if (rebuildTimer != null) {
    //        console.log('swipe', args.swipe);
    //        if ($bubble) {
    //            $bubble.remove();
    //            $bubble = null;
    //        }

    //        rebuildTimer.start();
    //    }
    //};
})(webgis.$ || jQuery);
