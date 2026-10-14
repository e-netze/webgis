// WebGIS UI Plugins (splitter)
// webgis_splitter
// Splits the children of an element horizontally or vertically with draggable splitter bars
// (data-direction='horizontal|vertical', optional data-minsize on the children)

webgis.ui.definePlugin('webgis_splitter', {
    defaults: {
        map: null,
        direction: null
    },
    init: function () {
        const $parent = this.$el;

        if ($parent.hasClass('webgis-splitter-initialized')) {
            return;
        }

        const direction = this.options.direction || $parent.attr('data-direction');

        if (direction === 'horizontal') {
            this._initDirection(true);
        }
        if (direction === 'vertical') {
            this._initDirection(false);
        }

        $parent.addClass('webgis-splitter-initialized');
    },
    destroy: function () {
        this.$el.off('.webgis_splitter');
    },
    methods: {
        setSize: function (options) {
            const $ = this.$, me = this, $this = this.$el, $children = $this.children();
            let resized = false;

            $children.each(function (i, child) {
                const $child = $(child);
                if ($child.hasClass(options.selectorClass)) {
                    if (options.size > $child.height() || options.absolute === true) {

                        let $next;
                        if (i > 1) {
                            $next = $($children[i - 2]);
                        } else if ($children.length > 2) {
                            $next = $($children[2]);
                        }

                        if ($next) {
                            let nextHeight = $next.height();
                            let delta = options.size - $child.height();

                            if (delta > 0 && delta > nextHeight + 10) {
                                delta = nextHeight - 10;
                            }

                            nextHeight -= delta;

                            $next.css('height', nextHeight);
                            if (i < $children.length - 1) {
                                $child.css('height', $child.height() + delta);
                            }

                            me._recalc(true);
                            resized = true;
                        }
                    }
                }
            });

            if (options.map && resized) {
                options.map.resize();
            }
        },
        // horizontal: children are stacked (top/height), vertical: side by side (left/width)
        _initDirection: function (horizontal) {
            const $ = this.$, me = this, $parent = this.$el, map = this.options.map;
            const posProp = horizontal ? 'top' : 'left', sizeProp = horizontal ? 'height' : 'width';
            const $children = $parent.children();

            let pos = 0;
            $children.each(function (i, child) {
                const $child = $(child), size = horizontal ? $child.height() : $child.width();
                $child.css(horizontal
                    ? { position: 'absolute', top: pos, left: 0, right: 0, height: size }
                    : { position: 'absolute', top: 0, buttom: 0, left: pos, width: size });

                if (i < $children.length - 1) {
                    $child.css(horizontal ? { bottom: 'unset' } : { right: 'unset' });

                    const $splitter = $("<div>")
                        .addClass('webgis-splitter-bar ' + (horizontal ? 'horizontal' : 'vertical'))
                        .css(posProp, pos + size)
                        .insertAfter($child)
                        .on('mousedown.webgis_splitter', function (e) {
                            e.stopPropagation();

                            $parent.data('start-position', me._relPosition(e));
                            $parent.data('current-splitter', $(this));

                            map.events.fire('begin-resize', map);
                        });

                    if (horizontal) {
                        $("<div>")
                            .addClass('webgis-splitter-minimize')
                            .appendTo($splitter)
                            .on('click.webgis_splitter', function (e) {
                                e.stopPropagation();

                                const $next = $(this).parent().next();
                                me.setSize({
                                    selectorClass: $next.attr('class'),
                                    size: $next.attr('data-minsize') || 0,
                                    absolute: true, map: map
                                });
                            });
                    }
                } else {
                    $child.css(horizontal ? { height: 'unset', bottom: 0 } : { width: 'unset', right: 0 });
                }

                pos += size;
            });

            this._recalc(horizontal);

            $parent
                .on('mouseup.webgis_splitter', function (e) {
                    const startPosition = $parent.data('start-position');
                    if (startPosition) {
                        const position = me._relPosition(e);
                        const delta = horizontal ? position.y - startPosition.y : position.x - startPosition.x;

                        const $prev = $parent.data('current-splitter').prev();
                        $prev.css(sizeProp, (horizontal ? $prev.height() : $prev.width()) + delta);

                        $parent.data('start-position', null);
                        $parent.data('current-splitter', null);

                        me._recalc(horizontal);

                        $children.each(function (i, child) {
                            const $child = $(child);
                            if ($child.attr('data-minsize')) {
                                me.setSize({ selectorClass: $child.attr('class'), size: $child.attr('data-minsize'), map: map });
                            }
                        });

                        if (map) {
                            map.resize();
                        }
                    }
                })
                .on('mousemove.webgis_splitter', function (e) {
                    const startPosition = $parent.data('start-position');
                    if (startPosition) {
                        const position = me._relPosition(e);
                        $parent.data('current-splitter').css(posProp, horizontal ? position.y : position.x);
                    }
                });
        },
        _recalc: function (horizontal) {
            const $ = this.$, $children = this.$el.children();

            let pos = 0;
            $children.each(function (i, child) {
                const $child = $(child);

                $child.css(horizontal ? 'top' : 'left', pos);

                if (i < $children.length - 1) {
                    if (!$child.hasClass('webgis-splitter-bar')) {
                        pos += horizontal ? $child.height() : $child.width();
                    }
                }
            });
        },
        _relPosition: function (e) {
            const parentOffset = this.$el.offset();

            return { x: e.pageX - parentOffset.left, y: e.pageY - parentOffset.top };
        }
    }
});
