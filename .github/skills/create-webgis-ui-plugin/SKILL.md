---
name: create-webgis-ui-plugin
description: How to write a viewer UI plugin with webgis.ui.definePlugin (structure, defaults/options, init/update/destroy, instance methods, static methods, DOM and webgis event subscription and their release), and how to convert a legacy $.fn jQuery plugin to it. Use when creating a new UI plugin or converting/changing an existing one.
---

# Skill: Create a WebGIS UI Plugin (`webgis.ui.definePlugin`)

`webgis.ui.definePlugin(name, spec)` (`src/NetCore/Web/Api/wwwroot/scripts/api/ui/webgis.ui.plugins.js`)
is the **current** plugin format. Legacy plugins written as
`$.fn.webgis_x = function (method) { ... methods ... }` (files `webgis_*.js`) are converted to it
step by step - never write new legacy plugins. (Sometimes called "registerPlugin" in discussions;
the function is `definePlugin`, `webgis.ui.__registerPlugin` is only the internal instance registry.)

Reference implementations: `webgis.ui.plugins.taskprogress.js` (small, methods),
`webgis.ui.plugins.sketch_info_container.js` (static methods, webgis events),
`webgis.ui.plugins.timefilter.js` (map events, DOM events).

## 1. Structure

```js
// WebGIS UI Plugins (<topic>)
// webgis_myThing
// Element builders: webgis.ui.builder.<topic>.js   (if any)

webgis.ui.definePlugin('webgis_myThing', {
    defaults: {               // merged deep: $.extend(true, {}, defaults, options)
        map: null,
        readonly: false,
        onChange: null         // outgoing "events" are callback options
    },
    init: function () {        // called once, when the plugin is created on an element
        const $ = this.$;      // always use this.$ (webgis.$ || jQuery), not a global $
        const o = this.options;
        this.$el.addClass('webgis-my-thing');
        // build DOM, subscribe events (see 4.)
    },
    update: function (key, value) { /* optional, called by $el.webgis_myThing('option', key, value) */ },
    destroy: function () {     // release everything init subscribed (see 5.)
        this.$el.off('.webgis_my_thing');
    },
    methods: { /* instance methods, see 2. */ },
    staticMethods: { /* see 3. */ }
});
```

- Name: `webgis_<camelCase>`; it becomes `$.fn.webgis_<name>` and the `$el.data('<name>')` key.
- File: `webgis.ui.plugins.<topic>.js`, registered per the `organize-api-javascript-files` skill.
- Instance fields available everywhere: `this.$`, `this.$el`, `this.options`, `this._pluginName`,
  `this._destroyed`. Keep per-instance state on `this` (e.g. `this._value`), not in closures or
  module variables shared by all instances.
- User-facing text only via `webgis.l10n.get('key')` (keys in `webgis.l10n.de.js` and `.en.js`).

## 2. Creating and calling methods

```js
$div.webgis_myThing({ map: map });              // create (calls init)
$div.webgis_myThing('addItem', { id: 1 });      // call instance method
var v = $div.webgis_myThing('getValue');        // method with return value
$div.webgis_myThing('option', 'readonly', true);// setter -> spec.update('readonly', true)
$div.webgis_myThing('option', 'readonly');      // getter
$div.webgis_myThing('destroy');                 // explicit destroy
var inst = $div.data('webgis_myThing');         // instance (if needed)
```

- `methods` are copied to the prototype; inside them `this` is the instance.
- A method returning `undefined` keeps jQuery chaining; any other return value ends the chain and is
  returned (for multi-element selections: the value of the last element).
- Calling a method on an element without instance -> `$.error('Method ... does not exist')`.
- Calling the plugin **again with options** on an element that already has an instance does
  **nothing** (no re-init, options are ignored). To re-initialize: `'destroy'` first, or use
  `'option'` + `update`. Plugins that are re-initialized by design (e.g. the toolbox, rebuilt after a
  tool is released) set `reinit: true` in the spec: a second call with options destroys the old
  instance and creates a new one (`destroy` must then remove what `init` added to `$el`).
- The instance is stored in `$el.data(name)` **before** `init` runs, so code triggered during `init`
  (e.g. `map.ui.refreshUIElements()`) can already call its methods.
- Don't name methods `option` or `destroy` (built-in).

## 3. Static methods

- `staticMethods` are attached to the plugin constructor, i.e. shared by all instances and not
  bound to one. Inside instance code reach them via `this.constructor.<name>`; from outside via
  `$.fn.webgis_myThing.plugin.<name>(...)` (no instance needed, e.g.
  `$.fn.webgis_toolbar.plugin.applyToolDependencies(...)`). Inside a static method `this` is the
  constructor when called this way.
- Use them for things that must be **one function reference** for every instance, typically event
  handlers that are subscribed with an explicit context object (see `onSketchCurrentStateChanged`
  in `webgis.ui.plugins.sketch_info_container.js`). Inside such a handler `this` is the context
  passed to `events.on(...)`, not the plugin instance.

## 4. Subscribing events

**DOM events (jQuery):** always use a plugin-specific namespace
(`'click.webgis_my_thing'`, `'change.webgis_my_thing'`), so `destroy` can remove exactly these.

**WebGIS events** (`webgis.eventController`, e.g. `map.events`, `sketch.events`,
`webgis.continuousPosition.events`):

- Signature `events.on(channel, fn, context)`; the callback gets `(e, ...args)` with
  `e.channel`, and `this` = `context` (**without a context `this` is the event controller, not the
  plugin** - `this.$`/`this.options` are then undefined).
- `events.off(channel, fn)` removes **every** subscription with that function reference - it has no
  context parameter. Prototype methods (`this.addService`) and static methods are the same reference
  for all instances, so `off` in one instance's `destroy` also unsubscribes all other instances.
  For plugins that can exist more than once, create a per-instance handler in `init` and keep it:

  ```js
  init: function () {
      this._onAddService = this.addService.bind(this);
      this.options.map.events.on('onaddservice', this._onAddService);
  },
  destroy: function () {
      this.options.map.events.off('onaddservice', this._onAddService);
  }
  ```

  Shared (static) handlers are only fine for singletons, or when "off for all" is intended
  (sketch info container: `off` before `on` to avoid double subscriptions).
- If the context is a DOM element that has been removed from the document, the event controller
  drops the subscription on the next `fire` - a safety net, not a replacement for `off`.

**Outgoing events:** expose callback options in `defaults` (`onChange`, `callback`, ...) and call
them guarded: `if (o.onChange) o.onChange(start, end);`. Triggering standard DOM events on own
inputs (`.trigger('change')`) is fine where other code listens to them.

## 5. Releasing events (destroy)

- `destroy` must undo everything from `init` that outlives the element: `this.$el.off('.<ns>')`,
  `events.off(...)` for every webgis event subscription (same function reference as in `on`),
  timers (`clearTimeout`/`clearInterval`), global/document handlers (`$(document).off('.<ns>')`).
- `this.$el.off('.<ns>')` only removes handlers bound on `$el` itself. Handlers on child elements are
  cleaned by jQuery when the children are removed; if the element survives an explicit `'destroy'`,
  use `this.$el.find('*').addBack().off('.<ns>')`.
- When is `destroy` called automatically? `definePlugin` wraps `$.fn.remove` and `$.fn.empty`; both
  call `webgis.ui.destroyPluginsDeep` (children first, then parent). **Not** covered: `.html(...)`,
  `.replaceWith(...)`, `.detach()`, native DOM removal - call
  `webgis.ui.destroyPluginsDeep($container)` before replacing content that way.
- `destroy` runs at most once per instance (`_destroyed`), then the instance is unregistered and
  `$el.data(name)` removed. Don't throw from `destroy` (errors are swallowed by the deep destroy).

## 6. Converting a legacy `$.fn` plugin

1. `defaults` <- the legacy `$.extend({...}, options)` defaults; `init` <- `methods.init` body
   (iterate-`this.each` wrapper is not needed, `definePlugin` handles every element).
2. Other `methods.*` -> `methods` (replace `$(this)` / passed `options` with `this.$el` / `this.options`).
3. Private helper functions inside the IIFE -> instance methods or `staticMethods` (if no state).
4. Add `destroy` releasing all subscriptions (section 5).
5. Callers stay the same (`$el.webgis_x(...)`, `$el.webgis_x('method', ...)`), but check for
   re-init calls on existing elements (section 2) and for methods returning values.
6. Move the code from `webgis_<name>.js` into `webgis.ui.plugins.<topic>.js`, delete the old file and
   update `bundleconfig.json` + Portal views (`organize-api-javascript-files` skill).

## 7. Verify

- `node --check` the file.
- If the plugin is created by a UI builder element, run the UI characterization tests
  (`dotnet test` in `src/NetCore/Tests/WebGIS.UI.Tests`) - snapshots must stay unchanged for
  pure refactorings/conversions; add tests for new elements.
- In the viewer: create the plugin, call its methods, remove the element (e.g. close the tool) and
  check (breakpoint/`console.log` in `destroy`) that it is destroyed and that map/sketch events no
  longer reach it - and that a second instance still works after the first one was destroyed.
