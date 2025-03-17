(function ($) {
    "use strict";
    $.fn.webgis_splitter = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_splitter');
        }
    };
    var defaults = {
        map: null,
        direction: null
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        setSize: function (options) {
            var $this = $(this), $children = $this.children(), resized = false;

            $children.each(function (i, child) {
                var $child = $(child);
                if ($child.hasClass(options.selectorClass)) {
                    var height = $child.height();

                    if (options.size > height || options.absolute === true) {

                        var $next;
                        if (i > 1) {
                            $next = $($children[i - 2]);
                        } else if ($children.length > 2) {
                            $next = $($children[2]);
                        }

                        if ($next) {
                            var nextHeight = $next.height();
                            var delta = options.size - $child.height();

                            if (delta > 0 && delta > nextHeight + 10) {
                                delta = nextHeight - 10;
                            }

                            nextHeight -= delta;

                            $next.css('height', nextHeight);
                            if (i < $children.length - 1) {
                                $child.css('height', $child.height() + delta);
                            }

                            recalcHorizontal($this);
                            resized = true;
                        }
                    }
                }
            });

            if (options.map && resized) {
                options.map.resize();
            }
        }
    };

    var initUI = function (parent, options) {
        var $parent = $(parent);

        if ($parent.hasClass('webgis-splitter-initialized')) {
            return;
        }

        var direction = options.direction || $parent.attr('data-direction');

        //console.log('splitter.direction', direction);

        if (direction === 'horizontal') {
            var parentHeight = $parent.height();
            var $children = $parent.children();

            var top = 0;
            $children.each(function (i, child) {
                var $child = $(child), height = $child.height();
                $child.css({
                    position: 'absolute',
                    top: top,
                    left: 0, right: 0,
                    height: height
                });

                if (i < $children.length - 1) {
                    $child.css({ bottom: 'unset' });

                    var $splitter = $("<div>")
                        .addClass('webgis-splitter-bar horizontal')
                        .css({ top: top + height })
                        .insertAfter($child)
                        .on('mousedown', function (e) {
                            e.stopPropagation();

                            $parent.data('start-position', relPosition($parent, e));
                            $parent.data('current-splitter', $(this));

                            options.map.events.fire('begin-resize', options.map);

                        });
                    $("<div>")
                        .addClass('webgis-splitter-minimize')
                        .appendTo($splitter)
                        .click(function (e) {
                            e.stopPropagation();

                            var $this = $(this);
                            $parent.webgis_splitter('setSize', {
                                selectorClass: $this.parent().next().attr('class'),
                                size: $this.parent().next().attr('data-minsize') || 0,
                                absolute: true, map: options.map
                            });
                        });

                } else {
                    $child.css({ height: 'unset', bottom: 0 });
                }

                top += height;
            });

            recalcHorizontal($parent);

            $parent
                .on('mouseup', function (e) {
                    var startPosition = $parent.data('start-position');
                    if (startPosition) {
                        //console.log('splitter-mouseup');

                        var position = relPosition($parent, e);
                        var deltaY = position.y - startPosition.y;
                        //console.log('splitter-mousemove', deltaY);

                        var $prev = $parent.data('current-splitter').prev();
                        $prev.css({ height: $prev.height() + deltaY });

                        $parent.data('start-position', null);
                        $parent.data('current-splitter', null);

                        recalcHorizontal($parent);

                        $children.each(function (i, child) {
                            var $child = $(child);
                            if ($child.attr('data-minsize')) {
                                $parent.webgis_splitter('setSize', { selectorClass: $child.attr('class'), size: $child.attr('data-minsize'), map: options.map });
                            }
                        });

                        if (options.map) {
                            options.map.resize();
						}
                    }
                })
                .on('mousemove', function (e) {
                    var startPosition = $parent.data('start-position');
                    if (startPosition) {
                        var position = relPosition($parent, e);
                        //console.log('splitter-mousemove', position.y - startPosition.y);

                        $parent.data('current-splitter').css({ top: position.y });
                    }
                });
        }

        if (direction === 'vertical') {
            var parentWidth = $parent.width();
            var $children = $parent.children();

            var left = 0;
            $children.each(function (i, child) {
                var $child = $(child), width = $child.width();
                $child.css({
                    position: 'absolute',
                    top: 0, buttom:0,
                    left: left,
                    width: width
                });

                if (i < $children.length - 1) {
                    $child.css({ right: 'unset' });

                    var $splitter = $("<div>")
                        .addClass('webgis-splitter-bar vertical')
                        .css({ left: left + width })
                        .insertAfter($child)
                        .on('mousedown', function (e) {
                            e.stopPropagation();

                            $parent.data('start-position', relPosition($parent, e));
                            $parent.data('current-splitter', $(this));

                            options.map.events.fire('begin-resize', options.map);

                        });
                    //$("<div>")
                    //    .addClass('webgis-splitter-minimize')
                    //    .appendTo($splitter)
                    //    .click(function (e) {
                    //        e.stopPropagation();

                    //        var $this = $(this);
                    //        $parent.webgis_splitter('setSize', {
                    //            selectorClass: $this.parent().prev().attr('class'),
                    //            size: $this.parent().next().attr('data-minsize') || 0,
                    //            absolute: true, map: options.map
                    //        });
                    //    });

                } else {
                    $child.css({ width: 'unset', right: 0 });
                }

                left += width;
            });

            recalcVertical($parent);

            $parent
                .on('mouseup', function (e) {
                    var startPosition = $parent.data('start-position');
                    if (startPosition) {
                        //console.log('splitter-mouseup');

                        var position = relPosition($parent, e);
                        var deltaX = position.x - startPosition.x;
                        //console.log('splitter-mousemove', deltaX);

                        var $prev = $parent.data('current-splitter').prev();
                        $prev.css({ width: $prev.width() + deltaX });

                        //console.log('$prev', $prev, $prev.width());

                        $parent.data('start-position', null);
                        $parent.data('current-splitter', null);

                        recalcVertical($parent);

                        $children.each(function (i, child) {
                            var $child = $(child);
                            if ($child.attr('data-minsize')) {
                                $parent.webgis_splitter('setSize', { selectorClass: $child.attr('class'), size: $child.attr('data-minsize'), map: options.map });
                            }
                        });

                        if (options.map) {
                            options.map.resize();
                        }
                    }
                })
                .on('mousemove', function (e) {
                    var startPosition = $parent.data('start-position');
                    if (startPosition) {
                        var position = relPosition($parent, e);
                        //console.log('splitter-mousemove', position.y - startPosition.y);

                        $parent.data('current-splitter').css({ left: position.x });
                    }
                });
        }

        $parent.addClass('webgis-splitter-initialized');
    };

    var recalcHorizontal = function ($parent) {
        var $children = $parent.children();

        var top = 0;
        $children.each(function (i, child) {
            var $child = $(child);

            $child.css({ top: top });

            if (i < $children.length - 1) {
                if (!$child.hasClass('webgis-splitter-bar')) {
                    top += $child.height();
				}
            } 
        });
    };

    var recalcVertical = function ($parent) {
        var $children = $parent.children();

        var left = 0;
        $children.each(function (i, child) {
            var $child = $(child);

            $child.css({ left: left });

            if (i < $children.length - 1) {
                if (!$child.hasClass('webgis-splitter-bar')) {
                    left += $child.width();
                }
            }
        });
    }

    var relPosition = function ($parent, e) {
        var parentOffset = $parent.offset();
        
        var relX = e.pageX - parentOffset.left;
        var relY = e.pageY - parentOffset.top;

        return { x: relX, y: relY };
	}
})(webgis.$ || jQuery);