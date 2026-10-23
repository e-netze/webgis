// WebGIS UI Plugins (autocomplete)
// webgis_autocomplete: own autocomplete component for text inputs (replaces twitter typeahead / jQuery UI autocomplete)
//
// DOM: span.webgis-autocomplete-wrapper > input + div.webgis-autocomplete-menu(.open) > .webgis-autocomplete-suggestion(.webgis-autocomplete-cursor)
// The menu is positioned fixed below the input (or options.positionTarget) while it is open.

webgis.ui.definePlugin('webgis_autocomplete', {
    defaults: {
        source: null,            // url string (remote: GET <url>&term=<query>) or function (query, done(items))
        minLength: 0,
        debounceDelay: 0,        // ms, only used while typing
        parameterSelector: '.webgis-input,.webgis-autocomplete-parameter', // remote: values of these fields (inside the wrapper's parent) are appended to the url
        categories: null,        // remote: appended as &categories=
        appendItems: null,       // remote: items appended to every result, '{0}' in value/label is replaced by the query
        displayKey: 'label',     // property written into the input for object items
        renderItem: null,        // function (item) => html string or element; default: webgis.autocompleteItem2Html
        emptyHtml: null,         // html shown, if there are no results (null: the menu is closed)
        positionTarget: null,    // selector (closest) or function ($input) => $element the menu is aligned to; default: the input
        cssDisplay: 'block',     // css display of the wrapper
        onSelect: null,          // function (input, item)
        onCursorChange: null,    // function (input, item|null)  keyboard cursor
        onEnter: null,           // function (input, items)      Enter without a cursor item
        onItemHover: null,       // function (input, item)
        onMenuLeave: null,       // function (input)
        onOpen: null,            // function (input)
        onClose: null            // function (input)
    },
    init: function () {
        const $ = this.$, me = this;
        const input = this.$el.get(0);

        this._items = [];
        this._lastQuery = null;
        this._userQuery = this.$el.val();
        this._cursor = -1;
        this._isOpen = false;
        this._requestId = 0;
        this._debounceTimer = null;
        this._fitTimer = null;
        this._currentRequest = null;
        this._menuId = 'webgis-autocomplete-menu-' + webgis.guid();

        this._$wrapper = $("<span>")
            .addClass('webgis-autocomplete-wrapper')
            .css('display', this.options.cssDisplay || 'block')
            .insertBefore(this.$el);
        this.$el.appendTo(this._$wrapper);

        this._$menu = $("<div>")
            .addClass('webgis-autocomplete-menu')
            .attr({ id: this._menuId, role: 'listbox' })
            .appendTo(this._$wrapper)
            // keep the focus in the input, when clicking into the menu
            .on('mousedown.webgis_autocomplete', function (e) { e.preventDefault(); })
            .on('click.webgis_autocomplete', '.webgis-autocomplete-suggestion', function () {
                const index = parseInt($(this).attr('data-index'));
                if (!isNaN(index)) me._select(me._items[index]);
            })
            .on('mouseenter.webgis_autocomplete', '.webgis-autocomplete-suggestion', function () {
                const index = parseInt($(this).attr('data-index'));
                if (!isNaN(index) && me.options.onItemHover) me.options.onItemHover(input, me._items[index]);
            })
            .on('mouseleave.webgis_autocomplete', function () {
                if (me.options.onMenuLeave) me.options.onMenuLeave(input);
            });

        this.$el
            .attr({
                autocomplete: 'off',
                spellcheck: 'false',
                role: 'combobox',
                'aria-autocomplete': 'list',
                'aria-expanded': 'false',
                'aria-controls': this._menuId
            })
            .on('input.webgis_autocomplete', function () {
                me._userQuery = me.$el.val();
                me._setCursor(-1, true);
                me._update(true);
            })
            .on('focus.webgis_autocomplete', function () {
                me._userQuery = me.$el.val();
                if (me._minLengthMet(me._userQuery) && me._userQuery === me._lastQuery) {
                    me._render();
                    me._openIfFocused();
                } else {
                    me._update(false);
                }
            })
            .on('blur.webgis_autocomplete', function () {
                me.close();
            })
            .on('keydown.webgis_autocomplete', function (e) {
                me._onKeyDown(e);
            });

        this._onWindowChange = function () { if (me._isOpen) me.fitMenu(); };
    },
    update: function (key, value) {
        if (key === 'cssDisplay' && this._$wrapper) {
            this._$wrapper.css('display', value || 'block');
        }
    },
    destroy: function () {
        clearTimeout(this._debounceTimer);
        clearTimeout(this._fitTimer);
        this._abortRequest();
        this._requestId++;
        this._removeWindowEvents();

        this.$el
            .off('.webgis_autocomplete')
            .removeAttr('role aria-autocomplete aria-expanded aria-controls aria-activedescendant');

        // unwrap the input (native DOM: the wrapper contains no other plugins)
        if (this._$menu) this._$menu.remove();   // frees the jQuery data of the suggestions
        const wrapper = this._$wrapper ? this._$wrapper.get(0) : null, input = this.$el.get(0);
        if (wrapper && wrapper.parentNode) {
            wrapper.parentNode.insertBefore(input, wrapper);
            wrapper.parentNode.removeChild(wrapper);
        }
        this._$wrapper = this._$menu = null;
        this._items = [];
    },
    methods: {
        val: function (value) {
            if (value === undefined) return this.$el.val();
            this.$el.val(value);
            this._userQuery = value;
        },
        search: function (query) {
            if (query !== undefined && query !== null) {
                this.$el.val(query);
            }
            this._userQuery = this.$el.val();
            this._lastQuery = null;   // force a new query
            this._update(false);
        },
        open: function () {
            if (this._isOpen || !this._$menu) return;
            if (this._items.length === 0 && !this.options.emptyHtml) return;
            if (!this.$el.is(':visible')) return;

            const me = this;
            this._isOpen = true;
            this._$menu.addClass('open');
            this.$el.attr('aria-expanded', 'true');
            this.fitMenu();

            // the layout of the input container may still change (css transitions)
            clearTimeout(this._fitTimer);
            this._fitTimer = setTimeout(function () { if (me._isOpen) me.fitMenu(); }, 300);

            window.addEventListener('resize', this._onWindowChange);
            window.addEventListener('scroll', this._onWindowChange, true);

            if (this.options.onOpen) this.options.onOpen(this.$el.get(0));
        },
        close: function () {
            if (!this._isOpen || !this._$menu) return;

            this._setCursor(-1, false);
            this._isOpen = false;
            clearTimeout(this._fitTimer);
            this._$menu.removeClass('open');
            this.$el.attr('aria-expanded', 'false');
            this._removeWindowEvents();

            if (this.options.onClose) this.options.onClose(this.$el.get(0));
        },
        items: function () {
            return this._items.slice();
        },
        focusedItem: function () {
            return this._cursor >= 0 ? this._items[this._cursor] : null;
        },
        fitMenu: function () {
            if (!this._$menu) return;

            const $ = this.$, o = this.options;
            let $target = this.$el;
            if (typeof o.positionTarget === 'function') {
                $target = $(o.positionTarget(this.$el));
            } else if (o.positionTarget) {
                $target = this.$el.closest(o.positionTarget);
            }
            if (!$target || $target.length === 0) {
                $target = this.$el;
            }

            const rect = $target.get(0).getBoundingClientRect();
            this._$menu.css({
                top: rect.bottom,
                left: rect.left,
                width: rect.width,
                maxHeight: 'calc(100% - ' + (rect.bottom + 80) + 'px)'
            });
        },

        // private

        _minLengthMet: function (query) {
            return (query || '').length >= (parseInt(this.options.minLength) || 0);
        },
        _update: function (typing) {
            const me = this, query = this._userQuery || '';

            clearTimeout(this._debounceTimer);

            if (!this._minLengthMet(query)) {
                this._requestId++;
                this._abortRequest();
                this._items = [];
                this._lastQuery = null;
                this._render();
                this.close();
                return;
            }

            const delay = typing ? parseInt(this.options.debounceDelay) || 0 : 0;
            if (delay > 0) {
                this._abortRequest();
                this._debounceTimer = setTimeout(function () { me._fetch(query); }, delay);
            } else {
                this._fetch(query);
            }
        },
        _fetch: function (query) {
            const me = this, requestId = ++this._requestId;
            const done = function (items) {
                if (me._destroyed || requestId !== me._requestId) return;   // outdated response
                me._lastQuery = query;
                me._items = Array.isArray(items) ? items : [];
                me._render();
                if (me._items.length > 0 || me.options.emptyHtml) {
                    me._openIfFocused();
                } else {
                    me.close();
                }
            };

            this._abortRequest();

            if (typeof this.options.source === 'function') {
                this.options.source.call(this, query, done);
            } else if (typeof this.options.source === 'string' && this.options.source) {
                this._fetchRemote(query, done);
            }
        },
        _fetchRemote: function (query, done) {
            const $ = this.$, o = this.options;
            let url = o.source;

            const $scope = this._$wrapper ? this._$wrapper.parent() : $();
            if (o.parameterSelector) {
                $scope.find(o.parameterSelector).each(function (i, field) {
                    url += '&' + (field.name || field.id) + '=' + encodeURIComponent($(field).val());
                });
            }
            if (o.categories) {
                url += '&categories=' + encodeURIComponent(o.categories);
            }
            if (webgis.advancedOptions &&
                webgis.advancedOptions.quicksearch_custom_parameters &&
                url.indexOf('/search/') > 0) {
                url += '&' + webgis.advancedOptions.quicksearch_custom_parameters;
            }
            url += webgis.hmac.urlParameters() + '&_autocomplete_item_style=label';

            const me = this;
            this._currentRequest = webgis.ajax({
                url: url,
                type: 'get',
                data: { term: query },
                success: function (data) {
                    me._currentRequest = null;
                    const items = Array.isArray(data) ? data.slice() : [];
                    if (Array.isArray(o.appendItems)) {
                        o.appendItems.forEach(function (appendItem) {
                            const clone = Object.assign({}, appendItem);
                            clone.value = (clone.value || '').replaceAll('{0}', query);
                            clone.label = (clone.label || '').replaceAll('{0}', query);
                            items.push(clone);
                        });
                    }
                    done(items);
                },
                error: function (xhr, status) {
                    me._currentRequest = null;
                    if (status !== 'abort') {
                        done([]);
                    }
                }
            });
        },
        _abortRequest: function () {
            if (this._currentRequest && this._currentRequest.readyState !== 4 && this._currentRequest.abort) {
                this._currentRequest.abort();
            }
            this._currentRequest = null;
        },
        _render: function () {
            if (!this._$menu) return;

            const $ = this.$, o = this.options, me = this;
            const renderItem = o.renderItem || webgis.autocompleteItem2Html;

            this._cursor = -1;
            this.$el.removeAttr('aria-activedescendant');
            this._$menu.empty();

            if (this._items.length === 0) {
                if (o.emptyHtml) {
                    $("<div>").addClass('webgis-autocomplete-empty').html(o.emptyHtml).appendTo(this._$menu);
                }
                return;
            }

            this._items.forEach(function (item, index) {
                const $item = $(renderItem(item)).first();
                $item
                    .addClass('webgis-autocomplete-suggestion')
                    .attr({ id: me._menuId + '-' + index, role: 'option', 'data-index': index })
                    .data('item', item)
                    .appendTo(me._$menu);
            });
        },
        _openIfFocused: function () {
            if (document.activeElement === this.$el.get(0)) {
                this.open();
            }
        },
        _selectableIndices: function () {
            const indices = [];
            this._$menu.children('.webgis-autocomplete-suggestion').each(function (i, e) {
                if (e.style.display !== 'none') indices.push(parseInt(e.getAttribute('data-index')));
            });
            return indices;
        },
        _moveCursor: function (delta) {
            const indices = this._selectableIndices();
            if (indices.length === 0) return;

            // position -1 is the input itself (original query)
            let pos = indices.indexOf(this._cursor);
            if (pos < 0) {
                pos = delta > 0 ? 0 : indices.length - 1;
            } else {
                pos += delta;
                if (pos < 0 || pos >= indices.length) pos = -1;
            }
            this._setCursor(pos < 0 ? -1 : indices[pos], true);
        },
        _setCursor: function (index, updateInput) {
            if (!this._$menu) return;
            if (index === this._cursor) return;

            const $ = this.$, input = this.$el.get(0);
            this._cursor = index;
            this._$menu.children('.webgis-autocomplete-cursor').removeClass('webgis-autocomplete-cursor');

            const item = index >= 0 ? this._items[index] : null;
            if (item !== null && item !== undefined) {
                const $item = this._$menu.children("[data-index='" + index + "']").addClass('webgis-autocomplete-cursor');
                this.$el.attr('aria-activedescendant', $item.attr('id'));
                this._scrollIntoView($item);
            } else {
                this.$el.removeAttr('aria-activedescendant');
            }

            if (updateInput) {
                this.$el.val(item !== null && item !== undefined ? this._display(item) : this._userQuery);
                if (this.options.onCursorChange) this.options.onCursorChange(input, item);
            }
        },
        _scrollIntoView: function ($item) {
            const menu = this._$menu.get(0), el = $item.get(0);
            if (!menu || !el) return;
            if (el.offsetTop < menu.scrollTop) {
                menu.scrollTop = el.offsetTop;
            } else if (el.offsetTop + el.offsetHeight > menu.scrollTop + menu.clientHeight) {
                menu.scrollTop = el.offsetTop + el.offsetHeight - menu.clientHeight;
            }
        },
        _display: function (item) {
            if (item === null || item === undefined) return '';
            if (typeof item !== 'object') return String(item);
            const v = item[this.options.displayKey || 'label'];
            return v !== undefined && v !== null ? String(v) : (item.value !== undefined ? String(item.value) : '');
        },
        _select: function (item) {
            if (item === null || item === undefined) return;

            const input = this.$el.get(0);
            const display = this._display(item);

            this.$el.val(display);
            this._userQuery = display;
            this._lastQuery = display;   // no new query on focus, the items belong to the selected value
            this.close();

            if (this.options.onSelect) this.options.onSelect(input, item);
            this.$el.trigger('autocomplete-selected', [item]);
        },
        _onKeyDown: function (e) {
            switch (e.keyCode) {
                case 40: // down
                    e.preventDefault();
                    if (!this._isOpen) {
                        this.open();
                    } else {
                        this._moveCursor(1);
                    }
                    break;
                case 38: // up
                    if (this._isOpen) {
                        e.preventDefault();
                        this._moveCursor(-1);
                    }
                    break;
                case 27: // esc
                    if (this._isOpen) {
                        this._setCursor(-1, true);
                        this.close();
                    }
                    break;
                case 13: // enter
                    if (this._isOpen && this._cursor >= 0) {
                        e.preventDefault();
                        this._select(this._items[this._cursor]);
                    } else {
                        const items = this._items.slice();
                        this.close();
                        if (this.options.onEnter) this.options.onEnter(this.$el.get(0), items);
                    }
                    break;
            }
        },
        _removeWindowEvents: function () {
            if (this._onWindowChange) {
                window.removeEventListener('resize', this._onWindowChange);
                window.removeEventListener('scroll', this._onWindowChange, true);
            }
        }
    }
});
