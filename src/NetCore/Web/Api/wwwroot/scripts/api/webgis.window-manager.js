// WebGIS Window Manager
// webgis.windowManager
// Detaches areas of the viewer into a separate window (Document Picture-in-Picture API, Chromium only).
// Areas are marked in the layout with the class 'webgis-detachable' (nested areas are ignored):
//   data-detachable-id           stable id (storage key), default: element id
//   data-detachable-title        title (l10n key or text) of the area in the detached window
//   data-detachable-order        order of the areas in the (shared) detached window
//   data-detachable-icon-target  selector (inside the area) for the detach button, default: the area itself
//   data-detachable-size         'fill' (area fills its slot) or 'auto' (content height)
// All detached areas share one window. The real DOM nodes are moved (jQuery data/events are kept),
// a placeholder stays at the original position. While something is detached, $(selector), .find()
// and .closest() also include/resolve the detached elements (jQuery hooks), so the other viewer
// scripts don't need to know about the detached window.
// Enabled with webgis.usability.detachableWindows = true (custom.js).

webgis.windowManager = new function () {
    "use strict";

    const wm = this;
    const STORAGE_PREFIX = 'webgis-windowmanager-';
    const MIN_OVERLAY_WIDTH = 400, MIN_OVERLAY_HEIGHT = 300;
    const SLOT_HEADER_HEIGHT = 26;
    const TOAST_TIMEOUT = 15000;

    let _map = null;
    let _initialized = false;
    let _areas = {};
    let _pip = null;
    let _opening = null;
    let _unloading = false;
    let _activeDocument = document;
    let _mounts = [];
    let _state = null;

    const _jq = function () {
        return webgis.$ || window.jQuery;
    };
    const $ = function (selector, context) {
        return _jq()(selector, context);
    };

    /* ---------- public API ---------- */

    this.isSupported = function () {
        return typeof window !== 'undefined' && 'documentPictureInPicture' in window;
    };

    this.isEnabled = function () {
        return wm.isSupported() && !!webgis.usability && webgis.usability.detachableWindows === true;
    };

    this.areas = function () {
        return Object.keys(_areas).map(function (id) {
            const area = _areas[id];
            return { id: area.id, title: area.title, detached: area.detached };
        });
    };

    this.isDetached = function (id) {
        return !!(_areas[id] && _areas[id].detached);
    };

    this.detach = function (id) {
        return wm.detachMany([id]);
    };

    // must be called from a user gesture (click): the window is requested synchronously
    this.detachMany = function (ids) {
        const areas = (ids || []).map(function (id) { return _areas[id]; })
            .filter(function (area) { return area && !area.detached; });

        if (!wm.isEnabled() || areas.length === 0)
            return Promise.resolve(false);

        const size = _initialWindowSize(areas);
        let opened;
        try {
            opened = _openPip(size.width, size.height, areas.length);
        } catch (e) {
            console.error('[windowManager] open window failed:', e);
            return Promise.resolve(false);
        }

        return opened.then(function () {
            areas.sort(_compareAreas).forEach(function (area) { _moveToPip(area); });
            _layoutSlots();
            _afterChange();
            return true;
        }).catch(function (e) {
            console.error('[windowManager] detach failed:', e);
            return false;
        });
    };

    this.attach = function (id) {
        const area = _areas[id];
        if (!area || !area.detached)
            return false;

        _moveBack(area);
        _layoutSlots();
        _afterChange();

        if (_detachedAreas().length === 0)
            _closePip();

        return true;
    };

    this.attachAll = function () {
        _detachedAreas().forEach(function (area) { _moveBack(area); });
        _afterChange();
        _closePip();
    };

    // documents of the viewer (main window + detached window)
    this.documents = function () {
        return _pip ? [document, _pip.document] : [document];
    };

    this.documentOf = function (element) {
        const el = element && element.jquery ? element[0] : element;
        return el && el.ownerDocument ? el.ownerDocument : document;
    };

    // container for overlays (modal dialogs, context menus): in the detached window, if the overlay
    // belongs to it (options.element is inside it) or the user is working there (last input), otherwise $container
    this.overlayContainer = function ($container, options) {
        options = options || {};
        if (!_pip || !$container || !$container.length)
            return $container;

        let usePip;
        if (options.element) {
            const el = options.element.jquery ? options.element[0] : options.element;
            usePip = !!el && el.ownerDocument === _pip.document;
        } else {
            usePip = _activeDocument === _pip.document &&
                _pip.window.innerWidth >= (options.minWidth || MIN_OVERLAY_WIDTH) &&
                _pip.window.innerHeight >= (options.minHeight || MIN_OVERLAY_HEIGHT);
        }

        if (!usePip)
            return $container;

        const host = _overlayHost($container[0]);
        return host ? $(host) : $container;
    };

    /* ---------- init ---------- */

    const _init = function (map) {
        if (_initialized || !wm.isEnabled())
            return;
        _initialized = true;
        _map = map;

        _patchJQuery(window.jQuery);
        _patchJQuery(window.jExt);
        _patchJQuery(webgis.$);

        _state = _loadState();

        if (_map && _map.events)
            _map.events.on('onchangeactivetool', function () { _updateHints(); });

        document.addEventListener('pointerdown', function () { _activeDocument = document; }, true);
        document.addEventListener('keydown', function () { _activeDocument = document; }, true);

        window.addEventListener('beforeunload', function () { _unloading = true; });
        window.addEventListener('pagehide', function () {
            _unloading = true;
            if (_pip) {
                try { _pip.window.close(); } catch (e) { }
            }
        });

        _scan();

        webgis.log('[windowManager] initialized, areas:', Object.keys(_areas));

        _showRestoreToast();
    };

    if (webgis.events && webgis.events.on) {
        webgis.events.on('map-initialized', function (e, sender, map) {
            _init(map);
        });
    }

    /* ---------- areas ---------- */

    const _scan = function () {
        const elements = document.querySelectorAll('.webgis-detachable');
        for (let i = 0; i < elements.length; i++) {
            const el = elements[i];
            if (el.parentElement && el.parentElement.closest('.webgis-detachable'))
                continue;  // nested areas are not supported

            const id = el.getAttribute('data-detachable-id') || el.id;
            if (!id || _areas[id])
                continue;

            const area = {
                id: id,
                root: el,
                title: webgis.l10n.get(el.getAttribute('data-detachable-title') || id),
                order: parseInt(el.getAttribute('data-detachable-order') || '0') || 0,
                size: el.getAttribute('data-detachable-size') === 'fill' ? 'fill' : 'auto',
                iconTarget: el.getAttribute('data-detachable-icon-target'),
                detached: false
            };
            _areas[id] = area;

            _ensureButton(area);

            let timer = null;
            area.observer = new MutationObserver(function () {
                if (timer) return;
                timer = setTimeout(function () {
                    timer = null;
                    _ensureButton(area);
                }, 100);
            });
            area.observer.observe(el, { childList: true, subtree: true });
        }
    };

    const _ensureButton = function (area) {
        const target = area.iconTarget ? area.root.querySelector(area.iconTarget) : area.root;
        if (!target)
            return;
        if (area.button && area.button.parentNode === target)
            return;

        if (area.button && area.button.parentNode)
            area.button.parentNode.removeChild(area.button);

        const button = document.createElement(/^(UL|OL)$/.test(target.tagName) ? 'li' : 'div');
        button.className = 'webgis-detachable-button' + (target === area.root ? '' : ' in-target');
        button.title = webgis.l10n.get('windowmanager-detach');
        button.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            wm.detach(area.id);
        });
        target.appendChild(button);
        area.button = button;
    };

    const _detachedAreas = function () {
        return Object.keys(_areas)
            .map(function (id) { return _areas[id]; })
            .filter(function (area) { return area.detached; })
            .sort(_compareAreas);
    };

    const _compareAreas = function (a, b) {
        return a.order - b.order;
    };

    /* ---------- detached window ---------- */

    const _initialWindowSize = function (areas) {
        if (_pip)
            return { width: _pip.window.innerWidth, height: _pip.window.innerHeight };

        if (_state && _state.width && _state.height)
            return { width: _state.width, height: _state.height };

        let width = 0, height = 0;
        areas.forEach(function (area) {
            const rect = area.root.getBoundingClientRect();
            width = Math.max(width, rect.width);
            height += Math.max(area.size === 'fill' ? 200 : 300, rect.height) + SLOT_HEADER_HEIGHT;
        });

        return {
            width: Math.round(Math.max(360, Math.min(width, screen.availWidth * 0.8))),
            height: Math.round(Math.max(200, Math.min(height, screen.availHeight * 0.8)))
        };
    };

    const _openPip = function (width, height, addCount) {
        if (_pip) {
            if (addCount > 0) {
                try {
                    _pip.window.resizeBy(0, Math.min(300, screen.availHeight - _pip.window.outerHeight));
                } catch (e) {
                    webgis.log('[windowManager] resizeBy not possible:', e);
                }
            }
            return Promise.resolve(_pip);
        }
        if (_opening)
            return _opening;

        _opening = window.documentPictureInPicture.requestWindow({ width: width, height: height })
            .then(function (pipWindow) {
                _opening = null;
                _setupPip(pipWindow);
                return _pip;
            }, function (e) {
                _opening = null;
                throw e;
            });

        return _opening;
    };

    const _setupPip = function (pipWindow) {
        const doc = pipWindow.document;

        const base = doc.createElement('base');
        base.href = document.baseURI;
        doc.head.appendChild(base);
        doc.title = document.title;

        _pip = {
            window: pipWindow,
            document: doc,
            container: null,
            overlays: [],
            observers: [],
            closing: false
        };

        const sheets = document.styleSheets;
        for (let i = 0; i < sheets.length; i++) {
            if (sheets[i].ownerNode)
                _copyStyleNode(sheets[i].ownerNode, doc);
        }

        const headObserver = new MutationObserver(function (mutations) {
            mutations.forEach(function (m) {
                for (let i = 0; i < m.addedNodes.length; i++) {
                    const node = m.addedNodes[i];
                    if (node.nodeName === 'STYLE' || (node.nodeName === 'LINK' && /stylesheet/i.test(node.rel))) {
                        // STYLE: rules are available after insertion
                        setTimeout(function () { if (_pip) _copyStyleNode(node, _pip.document); }, 0);
                    }
                }
            });
        });
        headObserver.observe(document.head, { childList: true });
        _pip.observers.push(headObserver);

        const mirrorRoot = function () {
            _copyAttribute(document.documentElement, doc.documentElement, 'class');
            _copyAttribute(document.documentElement, doc.documentElement, 'style');
            _copyAttribute(document.body, doc.body, 'class');
            _copyAttribute(document.body, doc.body, 'style');
            doc.body.classList.add('webgis-detached');
        };
        mirrorRoot();
        const rootObserver = new MutationObserver(mirrorRoot);
        rootObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['class', 'style'] });
        rootObserver.observe(document.body, { attributes: true, attributeFilter: ['class', 'style'] });
        _pip.observers.push(rootObserver);

        const container = doc.createElement('div');
        container.className = 'webgis-detached-container';
        doc.body.appendChild(container);
        _pip.container = container;

        doc.addEventListener('pointerdown', function () { _activeDocument = doc; }, true);
        doc.addEventListener('keydown', function () { _activeDocument = doc; }, true);
        pipWindow.addEventListener('keydown', _forwardKey);
        pipWindow.addEventListener('keyup', _forwardKey);

        let resizeTimer = null;
        pipWindow.addEventListener('resize', function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                if (!_pip) return;
                _fireResize();
                _saveState();
            }, 150);
        });

        pipWindow.addEventListener('pagehide', _onPipClosed);
    };

    const _onPipClosed = function () {
        if (!_pip)
            return;

        const pip = _pip;

        if (!_unloading) {
            _detachedAreas().forEach(function (area) { _moveBack(area); });
        }

        pip.overlays.forEach(function (overlay) {
            try { $(overlay.host).children().remove(); } catch (e) { }
            overlay.disconnect();
        });
        pip.observers.forEach(function (o) { o.disconnect(); });

        _pip = null;
        _activeDocument = document;
        _updateMounts();

        if (!_unloading) {
            _afterChange();
        }
    };

    const _closePip = function () {
        if (!_pip || _pip.closing)
            return;
        _pip.closing = true;
        const pipWindow = _pip.window;
        try {
            pipWindow.close();
        } catch (e) {
            _onPipClosed();
        }
    };

    const _copyStyleNode = function (node, doc) {
        try {
            if (node.nodeName === 'LINK') {
                const link = doc.createElement('link');
                link.rel = 'stylesheet';
                link.href = node.href;
                if (node.media) link.media = node.media;
                doc.head.appendChild(link);
            } else if (node.nodeName === 'STYLE') {
                const style = doc.createElement('style');
                let css = '';
                try {
                    const rules = node.sheet ? node.sheet.cssRules : [];
                    for (let i = 0; i < rules.length; i++)
                        css += rules[i].cssText + '\n';
                } catch (e) {
                    css = node.textContent;
                }
                style.textContent = css || node.textContent;
                doc.head.appendChild(style);
            }
        } catch (e) {
            webgis.log('[windowManager] copy style failed:', e);
        }
    };

    const _copyAttribute = function (from, to, name) {
        const value = from.getAttribute(name);
        if (value === null) {
            to.removeAttribute(name);
        } else if (to.getAttribute(name) !== value) {
            to.setAttribute(name, value);
        }
    };

    // nested elements in the detached window replicating the classes of the ancestors of 'element' (without ids),
    // so that the CSS of the viewer (variables, themes, parent selectors) also works there
    const _buildReplica = function (element, doc, includeSelf) {
        const originals = [];
        let cur = includeSelf ? element : element.parentElement;
        while (cur && cur !== document.body && cur !== document.documentElement) {
            originals.unshift(cur);
            cur = cur.parentElement;
        }

        const pairs = [];
        let outer = null, inner = null;
        originals.forEach(function (original) {
            const copy = doc.createElement('div');
            copy.setAttribute('data-webgis-replica', '');
            _copyAttribute(original, copy, 'class');
            copy.style.setProperty('display', 'contents', 'important');
            copy.__webgisReplicaOf = original;
            if (inner) inner.appendChild(copy); else outer = copy;
            inner = copy;
            pairs.push({ original: original, copy: copy });
        });

        if (!inner) {
            outer = inner = doc.createElement('div');
            inner.style.setProperty('display', 'contents', 'important');
            inner.__webgisReplicaOf = includeSelf ? element : (element.parentElement || document.body);
        }

        const observer = new MutationObserver(function () {
            pairs.forEach(function (pair) { _copyAttribute(pair.original, pair.copy, 'class'); });
        });
        pairs.forEach(function (pair) {
            observer.observe(pair.original, { attributes: true, attributeFilter: ['class'] });
        });

        return {
            outer: outer,
            inner: inner,
            disconnect: function () { observer.disconnect(); }
        };
    };

    const _overlayHost = function (anchor) {
        if (!_pip || !anchor)
            return null;

        for (let i = 0; i < _pip.overlays.length; i++) {
            if (_pip.overlays[i].anchor === anchor)
                return _pip.overlays[i].host;
        }

        // overlay elements are (logical) children of the anchor
        const replica = _buildReplica(anchor, _pip.document, true);
        _pip.document.body.appendChild(replica.outer);

        _pip.overlays.push({ anchor: anchor, host: replica.inner, disconnect: replica.disconnect });
        _updateMounts();

        return replica.inner;
    };

    /* ---------- slots ---------- */

    const _createSlot = function (area) {
        const doc = _pip.document;

        const slot = doc.createElement('div');
        slot.className = 'webgis-detached-slot size-' + area.size;
        slot.setAttribute('data-detachable-id', area.id);

        const header = doc.createElement('div');
        header.className = 'webgis-detached-slot-header';
        slot.appendChild(header);

        const title = doc.createElement('div');
        title.className = 'webgis-detached-slot-title';
        title.textContent = area.title;
        header.appendChild(title);

        const attachButton = doc.createElement('div');
        attachButton.className = 'webgis-detached-slot-attach';
        attachButton.title = webgis.l10n.get('windowmanager-attach');
        attachButton.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            wm.attach(area.id);
        });
        header.appendChild(attachButton);

        const content = doc.createElement('div');
        content.className = 'webgis-detached-slot-content';
        slot.appendChild(content);

        const bar = doc.createElement('div');
        bar.className = 'webgis-detached-slot-splitter';
        bar.addEventListener('pointerdown', function (e) { _startSlotResize(area, e); });

        return { element: slot, content: content, bar: bar, hint: _createHint(area, doc) };
    };

    const _createHint = function (area, doc) {
        const element = doc.createElement('div');
        element.className = 'webgis-detached-slot-hint';
        element.style.display = 'none';

        const text = doc.createElement('div');
        text.className = 'webgis-detached-slot-hint-text';
        text.textContent = webgis.l10n.get('windowmanager-area-hidden', { area: area.title });
        element.appendChild(text);

        const button = doc.createElement('button');
        button.className = 'webgis-button webgis-detached-slot-hint-button';
        const image = doc.createElement('img');
        button.appendChild(image);
        const label = doc.createElement('span');
        label.textContent = webgis.l10n.get('windowmanager-close-tool');
        button.appendChild(label);
        button.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            _closeActiveTool();
        });
        element.appendChild(button);

        return { element: element, button: button, image: image };
    };

    const _updateHint = function (area) {
        if (!area.detached || !area.slot || !area.slot.hint)
            return;

        const hint = area.slot.hint, win = area.root.ownerDocument.defaultView;
        const hidden = !!win && win.getComputedStyle(area.root).display === 'none';
        hint.element.style.display = hidden ? '' : 'none';
        if (!hidden)
            return;

        const tool = _map && _map.getActiveTool ? _map.getActiveTool() : null;
        const closable = !!tool && !(_map.isDefaultTool && _map.isDefaultTool(tool));
        hint.button.style.display = closable ? '' : 'none';
        if (closable) {
            hint.button.title = tool.name || '';
            hint.image.style.display = tool.image ? '' : 'none';
            hint.image.src = tool.image ? webgis.css.imgResource(tool.image, 'tools') : '';
        }
    };

    const _updateHints = function () {
        _detachedAreas().forEach(_updateHint);
    };

    // same as the close button of the tool dialog (incl. confirmation of unsaved changes)
    const _closeActiveTool = function () {
        if (!_map)
            return;

        const $container = _map.ui && _map.ui.webgisContainer ? $(_map.ui.webgisContainer()) : $(document.body);
        const $header = $container.find('.webgis-tooldialog-header.primary').first();
        if ($header.length === 1 && $header.get(0).onclose) {
            $header.removeClass('collapsed').trigger('click');
        } else if (typeof _map.setParentTool === 'function') {
            _map.setParentTool();
        }
    };

    const _layoutSlots = function () {
        if (!_pip)
            return;

        const container = _pip.container;
        const areas = _detachedAreas();
        const sizes = (_state && _state.sizes) || {};

        while (container.firstChild)
            container.removeChild(container.firstChild);

        areas.forEach(function (area, index) {
            const slot = area.slot, isLast = index === areas.length - 1;
            if (index > 0)
                container.appendChild(areas[index - 1].slot.bar);
            container.appendChild(slot.element);

            if (isLast) {
                slot.element.style.flex = '1 1 0px';
            } else if (sizes[area.id]) {
                slot.element.style.flex = '0 0 ' + sizes[area.id] + 'px';
            } else {
                slot.element.style.flex = area.size === 'fill' ? '1 1 0px' : '0 1 auto';
            }
        });
    };

    const _startSlotResize = function (area, e) {
        if (!_pip)
            return;

        e.preventDefault();

        const doc = _pip.document, slotElement = area.slot.element;
        const startY = e.clientY, startHeight = slotElement.getBoundingClientRect().height;
        const maxHeight = _pip.container.getBoundingClientRect().height - 40;
        let height = startHeight;

        const onMove = function (ev) {
            height = Math.max(SLOT_HEADER_HEIGHT + 10, Math.min(maxHeight, startHeight + ev.clientY - startY));
            slotElement.style.flex = '0 0 ' + Math.round(height) + 'px';
        };
        const onUp = function () {
            doc.removeEventListener('pointermove', onMove);
            doc.removeEventListener('pointerup', onUp);

            _state.sizes = _state.sizes || {};
            _state.sizes[area.id] = Math.round(height);
            _saveState();
            _fireResize();
        };

        doc.addEventListener('pointermove', onMove);
        doc.addEventListener('pointerup', onUp);
    };

    /* ---------- move areas ---------- */

    const _moveToPip = function (area) {
        if (!_pip || area.detached)
            return;

        const root = area.root, parent = root.parentNode;

        area.splitter = null;
        if (parent.classList && parent.classList.contains('webgis-splitter-initialized')) {
            let bar = root.previousElementSibling, prev = bar;
            while (prev && prev.classList.contains('webgis-splitter-bar'))
                prev = prev.previousElementSibling;

            area.splitter = {
                parent: parent,
                prev: prev,
                bar: bar && bar.classList.contains('webgis-splitter-bar') ? bar : null,
                height: root.getBoundingClientRect().height
            };

            if (prev)
                $(prev).css({ height: '', bottom: 0 });
            if (area.splitter.bar)
                area.splitter.bar.style.display = 'none';
        }

        area.placeholder = _createPlaceholder(area);
        parent.insertBefore(area.placeholder, root);

        area.slot = _createSlot(area);
        area.replica = _buildReplica(root, _pip.document, false);
        area.slot.content.appendChild(area.slot.hint.element);
        area.slot.content.appendChild(area.replica.outer);
        area.replica.inner.appendChild(root);

        // tools can hide areas (eg. print => display:none) => show a hint instead of an empty slot
        area.hintObserver = new MutationObserver(function () { _updateHint(area); });
        area.hintObserver.observe(root, { attributes: true, attributeFilter: ['style', 'class'] });

        area.detached = true;
        _updateHint(area);
        _updateMounts();

        webgis.events.fire('window-detached', wm, { id: area.id });
    };

    const _moveBack = function (area) {
        if (!area.detached)
            return;

        const placeholder = area.placeholder, parent = placeholder.parentNode;
        const splitter = area.splitter;

        if (splitter && splitter.prev && parent === splitter.parent) {
            const parentHeight = $(parent).height();
            $(splitter.prev).css({
                height: Math.max(0, parentHeight - (splitter.height || 0)),
                bottom: 'unset'
            });
        }
        if (splitter && splitter.bar)
            splitter.bar.style.display = '';

        parent.insertBefore(area.root, placeholder);
        parent.removeChild(placeholder);

        if (splitter && _jq().fn.webgis_splitter && $(parent).data('webgis_splitter')) {
            $(parent).webgis_splitter('_recalc', true);
        }

        if (area.replica)
            area.replica.disconnect();
        if (area.hintObserver)
            area.hintObserver.disconnect();
        if (area.slot) {
            [area.slot.element, area.slot.bar].forEach(function (el) {
                if (el.parentNode) el.parentNode.removeChild(el);
            });
        }

        area.detached = false;
        area.placeholder = area.slot = area.replica = area.splitter = area.hintObserver = null;
        _updateMounts();

        // query results: the height before detaching may be outdated (e.g. restored after a reload while
        // still collapsed, new results while detached) => let the viewer size the frame for its content
        if (splitter && _map && _map.ui && typeof _map.ui._setResultFrameSize === 'function' &&
            typeof _map.ui.getQueryResultTabControl === 'function') {
            try {
                const $tabControl = _map.ui.getQueryResultTabControl();
                if ($tabControl && $tabControl[0] === area.root)
                    _map.ui._setResultFrameSize($tabControl);
            } catch (e) { }
        }

        _ensureButton(area);

        webgis.events.fire('window-attached', wm, { id: area.id });
    };

    const _createPlaceholder = function (area) {
        const placeholder = document.createElement('div');
        placeholder.className = 'webgis-detachable-placeholder';
        placeholder.setAttribute('data-detachable-id', area.id);

        if (area.splitter) {
            placeholder.classList.add('in-splitter');
        } else {
            const style = getComputedStyle(area.root);
            if (style.position === 'absolute' || style.position === 'fixed') {
                placeholder.classList.add('positioned');
                placeholder.style.position = style.position;
                placeholder.style.left = style.left;
                placeholder.style.top = style.top;
            }
        }

        const title = document.createElement('span');
        title.className = 'webgis-detachable-placeholder-title';
        title.textContent = area.title;
        placeholder.appendChild(title);

        const button = document.createElement('div');
        button.className = 'webgis-detachable-placeholder-attach';
        button.title = webgis.l10n.get('windowmanager-attach');
        placeholder.appendChild(button);

        placeholder.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            wm.attach(area.id);
        });

        return placeholder;
    };

    const _afterChange = function () {
        if (_map && typeof _map.resize === 'function') {
            try { _map.resize(); } catch (e) { }
        }
        _fireResize();
        _saveState();
    };

    const _fireResize = function () {
        if (_map && _map.events) {
            _map.events.fire('resize-live', _map, {});
        }
    };

    /* ---------- keyboard ---------- */

    // keys pressed in the detached window (outside of input fields) are passed to the main window (shortcuts)
    const _forwardKey = function (e) {
        const target = e.target;
        if (target && (target.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName)))
            return;

        const init = {
            key: e.key, code: e.code,
            ctrlKey: e.ctrlKey, shiftKey: e.shiftKey, altKey: e.altKey, metaKey: e.metaKey,
            bubbles: true, cancelable: true
        };
        // handlers in the detached window (e.g. context menu) go first
        setTimeout(function () {
            if (!e.defaultPrevented)
                document.dispatchEvent(new KeyboardEvent(e.type, init));
        }, 0);
    };

    /* ---------- jQuery hooks ---------- */

    const _updateMounts = function () {
        const mounts = [];
        if (_pip) {
            _detachedAreas().forEach(function (area) {
                mounts.push({ anchor: area.placeholder, root: area.root, includeRoot: true });
            });
            _pip.overlays.forEach(function (overlay) {
                mounts.push({ anchor: overlay.anchor, root: overlay.host, includeRoot: false });
            });
        }
        _mounts = mounts;
    };

    const _query = function (jq, selector, mounts) {
        const result = [];
        for (let i = 0; i < mounts.length; i++) {
            const mount = mounts[i];
            try {
                if (mount.includeRoot && jq.find.matchesSelector(mount.root, selector))
                    result.push(mount.root);
                jq.find(selector, mount.root, result);
            } catch (e) {
                // invalid or unsupported selector: only the original result
            }
        }
        return result;
    };

    const _patchJQuery = function (jq) {
        if (!jq || !jq.fn || !jq.find || jq.fn.__webgisWindowManager === true)
            return;
        jq.fn.__webgisWindowManager = true;

        const origInit = jq.fn.init, origFind = jq.fn.find, origClosest = jq.fn.closest;
        const rId = /^#[\w-]+$/;

        // $('#id') doesn't use .find() (getElementById)
        const init = function (selector, context, root) {
            const result = origInit.call(this, selector, context, root);
            if (_mounts.length > 0 && !context && typeof selector === 'string' &&
                result && result.length === 0 && rId.test(selector)) {
                const extra = _query(jq, selector, _mounts);
                for (let i = 0; i < extra.length; i++)
                    result[i] = extra[i];
                result.length = extra.length;
            }
            return result;
        };
        init.prototype = jq.fn;
        jq.fn.init = init;

        // $(selector), $(element).find(selector): elements containing a placeholder (overlay anchor)
        // also find the elements of the detached area (overlay)
        jq.fn.find = function (selector) {
            const result = origFind.apply(this, arguments);
            if (_mounts.length === 0 || typeof selector !== 'string' || this.length === 0)
                return result;

            const relevant = [];
            for (let m = 0; m < _mounts.length; m++) {
                const anchor = _mounts[m].anchor;
                for (let i = 0; i < this.length; i++) {
                    const el = this[i];
                    if (el && (el === anchor || (el.contains && el.contains(anchor)))) {
                        relevant.push(_mounts[m]);
                        break;
                    }
                }
            }
            if (relevant.length === 0)
                return result;

            const extra = _query(jq, selector, relevant);
            if (extra.length === 0)
                return result;

            return this.pushStack(jq.uniqueSort(result.get().concat(extra)));
        };

        // elements in the detached window: continue with the original ancestors (main window) instead of the replicas
        jq.fn.closest = function (selectors, context) {
            if (!_pip || typeof selectors !== 'string' || context)
                return origClosest.apply(this, arguments);

            let inPip = false;
            for (let i = 0; i < this.length; i++) {
                if (this[i] && this[i].ownerDocument === _pip.document) {
                    inPip = true;
                    break;
                }
            }
            if (!inPip)
                return origClosest.apply(this, arguments);

            const matched = [];
            try {
                for (let i = 0; i < this.length; i++) {
                    let cur = this[i];
                    while (cur && cur.nodeType === 1) {
                        if (cur.__webgisReplicaOf) {
                            cur = cur.__webgisReplicaOf;
                            continue;
                        }
                        if (jq.find.matchesSelector(cur, selectors)) {
                            if (matched.indexOf(cur) < 0)
                                matched.push(cur);
                            break;
                        }
                        cur = cur.parentElement;
                    }
                }
            } catch (e) {
                return origClosest.apply(this, arguments);
            }

            return this.pushStack(matched.length > 1 ? jq.uniqueSort(matched) : matched);
        };
    };

    /* ---------- storage ---------- */

    const _storageKey = function () {
        const pageId = webgis.globals && webgis.globals.portal && webgis.globals.portal.pageId;
        return STORAGE_PREFIX + (pageId || 'default');
    };

    const _loadState = function () {
        let state = null;
        try {
            if (webgis.localStorage && webgis.localStorage.usable())
                state = JSON.parse(webgis.localStorage.get(_storageKey()) || 'null');
        } catch (e) { }

        state = state || {};
        state.detached = Array.isArray(state.detached) ? state.detached : [];
        state.sizes = state.sizes || {};
        return state;
    };

    const _saveState = function () {
        if (_unloading || !_state)
            return;

        _state.detached = _detachedAreas().map(function (area) { return area.id; });
        if (_pip) {
            _state.width = _pip.window.innerWidth;
            _state.height = _pip.window.innerHeight;
        }

        try {
            if (webgis.localStorage && webgis.localStorage.usable())
                webgis.localStorage.set(_storageKey(), JSON.stringify(_state));
        } catch (e) { }
    };

    /* ---------- restore ---------- */

    // the window can only be opened with a user gesture => ask the user (toast in the main window)
    const _showRestoreToast = function () {
        if (!_state || _state.dontAsk === true)
            return;

        const ids = _state.detached.filter(function (id) { return !!_areas[id]; });
        if (ids.length === 0)
            return;

        const titles = ids.map(function (id) { return _areas[id].title; }).join(', ');

        const toast = document.createElement('div');
        toast.className = 'webgis-windowmanager-toast';

        const text = document.createElement('div');
        text.className = 'webgis-windowmanager-toast-text';
        text.textContent = webgis.l10n.get('windowmanager-restore-question', { areas: titles });
        toast.appendChild(text);

        const buttons = document.createElement('div');
        buttons.className = 'webgis-windowmanager-toast-buttons';
        toast.appendChild(buttons);

        let timer = null;
        const close = function () {
            clearTimeout(timer);
            if (toast.parentNode)
                toast.parentNode.removeChild(toast);
        };
        const addButton = function (l10nKey, cssClass, onClick) {
            const button = document.createElement('button');
            button.className = 'webgis-button ' + cssClass;
            button.textContent = webgis.l10n.get(l10nKey);
            button.addEventListener('click', function (e) {
                e.preventDefault();
                close();
                onClick();
            });
            buttons.appendChild(button);
        };

        addButton('windowmanager-restore', '', function () { wm.detachMany(ids); });
        addButton('windowmanager-restore-no', 'light', function () { });
        addButton('windowmanager-restore-dont-ask', 'light', function () {
            _state.dontAsk = true;
            _saveState();
        });

        document.body.appendChild(toast);
        timer = setTimeout(close, TOAST_TIMEOUT);
    };
};
