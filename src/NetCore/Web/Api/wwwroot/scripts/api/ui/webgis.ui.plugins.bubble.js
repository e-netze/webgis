// WebGIS UI Plugins (bubble)
// webgis_bubble
// Draggable map bubble (click bubble, context menu bubble, tool bubble, quick access buttons) with trash/park positions

webgis.ui.definePlugin('webgis_bubble', {
    defaults: {
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
    },

    init: function () {
        const $ = this.$;
        const self = this;
        const options = this.options;
        const ctor = this.constructor;

        var $bubble = this.$el.data('map', options.map).data('options', options).addClass('webgis-bubble');

        if (options.id) {
            if (options.rememberDisabled === true && webgis.localStorage.get('_bubble-' + options.id + '-disabled') === '1') {
                options.disabled = true;
            }
        }

        if (options.onClick) {
            $bubble.data('onclick', options.onClick)
                .on('click.webgis_bubble', function (e) {
                    if ($(this).hasClass('is-longclick'))
                        return;
                    var options = $(this).data('options');
                    var map = $(this).data('map');
                    if ($(this).data('isdragging') !== true) {
                        if ($(this).hasClass('disabled')) {
                            if (options.helpName)
                                map.ui.showHelp(options.helpName);
                            else
                                webgis.alert('Zum Aktivieren des Werkzeugs, die Kugel ziehen...', 'info');
                        }
                        else if ($(this).data('onclick')) {
                            $(this).data('onclick').apply(this, [e]);
                        }
                    }
                });
        }

        // per-instance handler: events.off(channel, fn) has no context parameter
        this._onMapResize = function (e, sender) {
            var parentWidth = $(this).parent().width();
            $(this).css('left', parentWidth - $(this).outerWidth(true));
            ctor.setDisableFlag($(this));
        };
        options.map.events.on('resize', this._onMapResize, $bubble);

        if (options.bgImage)
            $bubble.css({ backgroundImage: 'url(' + options.bgImage + ')' });
        if (options.text) {
            var $text = $("<div>" + options.text + "</div>").addClass('text').appendTo($bubble.addClass('text'));
            if (options.textClass) {
                webgis.variableContent.init($text, options.textClass);
            }
        }
        var top = Math.max(ctor.trashTopPosition($bubble), options.top) +
            $bubble.outerHeight(true) * (options.disabled === true ? 0 : 1.1) * (options.trashPosition === 'bottom' ? -1 : 1);
        if (options.trashPosition === 'none')
            top = Math.max(ctor.parkTopPosition($bubble), options.top);
        $bubble.css({
            left: $bubble.parent().width() - $bubble.outerWidth(true),
            top: top
        });
        if (options.parkable)
            $bubble.addClass('return-to-position');

        this._pointerDownTimer = new webgis.timer(function ($bubble) {
            if ($bubble.data('isdragging') !== true)
                $bubble.addClass('is-longclick');
        }, 1000, $bubble);
        $bubble.data('pointer-down-timer', this._pointerDownTimer);

        if ($.fn.draggabilly) {
            this._addDraggabilly();
        }
        else {
            webgis.loadScript(webgis.baseUrl + '/scripts/draggabilly/draggabilly.pkgd.min.js', '', function () {
                if (self._destroyed)
                    return;
                self._addDraggabilly();
            });
        }
        ctor.setDisableFlag($bubble);
    },

    destroy: function () {
        if (this.options.map && this.options.map.events && this._onMapResize) {
            this.options.map.events.off('resize', this._onMapResize);
        }
        if (this._pointerDownTimer) {
            this._pointerDownTimer.stop();
        }
        this.$el.find('*').addBack().off('.webgis_bubble');
    },

    methods: {
        _addDraggabilly: function () {
            const $ = this.$;
            const self = this;
            const ctor = this.constructor;
            var $bubble = this.$el;

            var $draggy = $bubble.draggabilly();
            $draggy.on('dragEnd.webgis_bubble', function (e, pointer) {
                $(this).data('moveOffsetXY', null);
                var options = $(this).data('options');

                if ($(this).data('isdragging') !== true) {
                    if (!$(this).hasClass('is-longclick'))
                        $(this).trigger('click');
                    return;
                }

                if (!$(this).hasClass('disabled') && !$(this).hasClass('parked') && options.onDragEnd) {
                    var relOffset = ctor.relativeOffsetTo(this, pointer.target);
                    ctor.applyOffsetXY(pointer);

                    options.onDragEnd.apply(this, [{
                        clientX: pointer.clientX - pointer.offsetX + relOffset.x,
                        clientY: pointer.clientY - pointer.offsetY + relOffset.y
                    }]);
                }
                webgis.delayed(function (e) {
                    $(e).data('isdragging', false);
                    self.hideTrash();
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
                            var top = ctor.trashTopPosition($bubble);
                            if (status)
                                top = ctor.parkTopPosition($bubble);
                            $(e).css({
                                left: $(e).parent().width() - $bubble.outerWidth(true),
                                top: top
                            });
                            webgis.delayed(function (e) {
                                ctor.setDisableFlag($(e).removeClass('returning'));
                            }, 500, e);
                            if ($(e).hasClass('parked') && options.onParked)
                                options.onParked.apply(e);
                        }, 10, e);
                    }
                }, 10, this);
            });
            $draggy.on('dragMove.webgis_bubble', function (e, pointer, moveVector) {
                var $this = $(this);

                if (webgis.is_iOS && !$this.data('moveOffsetXY')) {
                    ctor.applyOffsetXY(pointer);  // offset beim Start merken. Ist nicht sehr genau, aber besser gehts leider nicht fürs erste
                    $this.data('moveOffsetXY', [pointer.offsetX, pointer.offsetY]);
                }

                var options = $this.data('options');
                if ($this.data('isdragging') !== true &&
                    (moveVector.x * moveVector.x + moveVector.y * moveVector.y) >= options.dragTolerance * options.dragTolerance) { // Toleranz: wenn kleiner, dann klick...
                    $this.data('isdragging', true)
                        .data('dragStartStatus', $(this).hasClass('disabled') === false);
                    self.showTrash();
                }
                ctor.setDisableFlag($(this), pointer.clientX, pointer.clientY);

                if (!$this.hasClass('disabled') && !$this.hasClass('parked') && options.onDragMove) {
                    var relOffset = ctor.relativeOffsetTo(this, pointer.target);

                    if (webgis.is_iOS === true) {
                        if ($this.data('moveOffsetXY')) {
                            try {
                                pointer.offsetX = $this.data('moveOffsetXY')[0];
                                pointer.offsetY = $this.data('moveOffsetXY')[1];
                            } catch (ex) { }
                        }
                    }

                    options.onDragMove.apply(this, [{
                        clientX: pointer.clientX - pointer.offsetX + relOffset.x,
                        clientY: pointer.clientY - pointer.offsetY + relOffset.y
                    }]);
                }
            });
            $draggy.on('pointerDown.webgis_bubble', function (e, pointer) {
                $(this).data('pointer-down-timer').start();
            });
            $draggy.on('pointerUp.webgis_bubble', function (e, pointer) {
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
            });
        },

        remove: function () {
            var options = this.$el.data('options');
            if (options && options.onDisable)
                options.onDisable.apply(this.$el);
            this.$el.remove();
        },

        showTrash: function () {
            const $ = this.$;
            var $bubble = this.$el;
            var options = $bubble.data('options');
            if (options.trashPosition !== 'none') {
                var $trash = $("<div>").addClass('webgis-bubble-trash').appendTo($bubble.parent());
                $trash.css({
                    left: $bubble.parent().width() - $trash.outerWidth(true),
                    top: this.constructor.trashTopPosition($bubble)
                });
            }
            if (options.parkable) {
                var $park = $("<div>").addClass('webgis-bubble-park').appendTo($bubble.parent());
                $park.css({
                    left: $bubble.parent().width() - $park.outerWidth(true),
                    top: this.constructor.parkTopPosition($bubble)
                });
            }
        },

        hideTrash: function () {
            this.$el.parent().find('.webgis-bubble-trash').remove();
            this.$el.parent().find('.webgis-bubble-park').remove();
        },

        isEnabled: function () {
            return !this.$el.hasClass('disabled');
        },

        moveTo: function (options) {
            const $ = this.$;
            var x = options.x, y = options.y;
            if (options.target) {
                var targetOffset = $(options.target).offset();
                x += targetOffset.left;
                y += targetOffset.top;
            }
            this.$el.css({
                left: x,
                top: y
            });
        },

        park: function (options) {
            const $ = this.$;
            const ctor = this.constructor;
            webgis.delayed(function (e) {
                var $bubble = $(e);
                var top = ctor.parkTopPosition($bubble);
                $bubble.css({
                    left: $bubble.parent().width() - $bubble.outerWidth(true),
                    top: top
                });
                webgis.delayed(function (e) {
                    ctor.setDisableFlag($(e).removeClass('returning'));
                }, 500, e);
                if ($(e).hasClass('parked') && options && options.onParked)
                    options.onParked.apply(e);
            }, 10, this.$el);
        }
    },

    staticMethods: {
        trashTopPosition: function ($bubble) {
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
        },

        parkTopPosition: function ($bubble) {
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
        },

        setDisableFlag: function ($bubble, x, y) {
            var options = $bubble.data('options');
            var width = $bubble.outerWidth(true);
            x = x ? x : parseInt($bubble.css('left')) + $bubble.outerWidth(true) / 2;
            y = y ? y : parseInt($bubble.css('top')) + $bubble.outerHeight(true) / 2;
            var trashX = $bubble.parent().width() - 40, trashY = (webgis.$ || jQuery).fn.webgis_bubble.plugin.trashTopPosition($bubble) + $bubble.outerHeight(true) / 2;
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
                (webgis.$ || jQuery).fn.webgis_bubble.plugin.setParkedFlag($bubble, x, y);
        },

        setParkedFlag: function ($bubble, x, y) {
            var width = $bubble.outerWidth(true);
            x = x ? x : parseInt($bubble.css('left')) + $bubble.outerWidth(true) / 2;
            y = y ? y : parseInt($bubble.css('top')) + $bubble.outerHeight(true) / 2;
            var parkX = $bubble.parent().width() - 40, parkY = (webgis.$ || jQuery).fn.webgis_bubble.plugin.parkTopPosition($bubble) + $bubble.outerHeight(true) / 2;
            var dist2 = (x - parkX) * (x - parkX) + (y - parkY) * (y - parkY);
            if (dist2 < width * width)
                $bubble.addClass('parked');
            else
                $bubble.removeClass('parked');
        },

        relativeOffsetTo: function (bubble, element) {
            const $ = webgis.$ || jQuery;
            if (bubble === element)
                return { x: 0, y: 0 };
            var bubblePos = $(bubble).offset();
            var elementPos = $(element).offset();
            return {
                x: parseInt(bubblePos.left) - parseInt(elementPos.left),
                y: parseInt(bubblePos.top) - parseInt(elementPos.top)
            };
        },

        applyOffsetXY: function (e) {
            const $ = webgis.$ || jQuery;
            if (webgis.is_iOS) {
                var target = e.target, dx = 0, dy = 0;
                if (!$(e.target).hasClass('webgis-bubble')) {
                    target = $(e.target).closest('.webgis-bubble').get(0);
                    var dOffset = (webgis.$ || jQuery).fn.webgis_bubble.plugin.relativeOffsetTo(target, e.target);
                    dx = dOffset.x;
                    dy = dOffset.y;
                }
                try {
                    e.offsetX = e.pageX - target.offsetLeft + dx;
                    e.offsetY = e.pageY - target.offsetTop + dy;
                } catch (ex) { }
            }
        }
    }
});
