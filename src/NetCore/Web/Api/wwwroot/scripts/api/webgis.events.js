webgis.eventController = function (baseObj) {
    this._baseObj = baseObj;
    this._events = [];
    this._suppressed = [];

    this.on = function (channel, fn, context) {
        if (typeof channel === 'string') {
            if (!this._events[channel])
                this._events[channel] = [];
            this._events[channel].push({ context: context || this, callback: fn });
        }
        else {
            for (var i = 0; i < channel.length; i++) {
                this.on(channel[i], fn, context);
            }
        }
        return this;
    };

    this.off = function (channel, fn) {
        if (typeof channel === 'string') {
            if (!this._events[channel])
                return;

            var events = [];

            for (var e in this._events[channel]) {
                var event = this._events[channel][e];

                if (event.callback === fn) {
                    //console.log('off event', event);
                    continue;
                }

                events.push(event);
            }

            this._events[channel] = events;
        } else {
            for (var i = 0; i < channel.length; i++) {
                this.off(channel[i], fn);
            }
        }
    };

    this.fire = function (channel) {
        if (!this._events[channel])
            return false;

        if (this._suppressed.length > 0) {
            if (typeof channel === 'string') {
                if (this.isSuppressed(channel)) return;
            } else {
                for (let c of channel) {
                    if (this.isSuppressed(c)) return;
                }
            }
        }

        var args = Array.prototype.slice.call(arguments, 1);
        var eventArgs = [];

        eventArgs.push({ channel: channel });

        for (var i = 0; i < args.length; i++) {
            eventArgs.push(args[i]);
        }

        var invalidSubscriptions = [];
        for (var i = 0, l = this._events[channel].length; i < l; i++) {
            var subscription = this._events[channel][i];

            var context = subscription.context;
            if (channel === 'onaddservice') {
                
            }

            if (_isDomElement(subscription.context) && _isRemovedDomElement(subscription.context)) {
                invalidSubscriptions.push(i);
                //console.log('invalid channel (' + channel + ') subscription', subscription);
                continue;
            }

            subscription.callback.apply(context, eventArgs);
        }

        if (invalidSubscriptions.length > 0 && invalidSubscriptions.includes) {
            var channelSubscriptions = [];
            for (var i = 0; i < this._events[channel].length; i++) {
                if (!invalidSubscriptions.includes(i)) {
                    //console.log(invalidSubscriptions, invalidSubscriptions.includes(i), i);
                    channelSubscriptions.push(this._events[channel][i]);
                }
            }
            this._events[channel] = channelSubscriptions;
            console.log('new valid channel (' + channel + ') subscription: ', this._events[channel]);
        }

        return this;
    };

    this.registered = function (channel) {
        return this._events[channel] ? true : false;
    }

    this.isSuppressed = function (channel) {
        return this._suppressed.indexOf(channel) >= 0;
    }

    this.suppress = function (channel) {
        if (typeof channel === 'string') {
            if (!this.isSuppressed(channel)) this._suppressed.push(channel);
        } else {
            for (let c of channel) {
                if (!this.isSuppressed(c)) this._suppressed.push(c);
            }
        }
    }
    this.enable = function (channel) {
        let suppressed = [];

        for (let suppressedChannel of this._suppressed) {
            let enable = false;
            if (typeof channel === 'string') {
                if (channel === suppressedChannel) enable = true;
            } else {
                for (let c of channel) {
                    if (c === suppressedChannel) enable = true;
                }
            }

            if (enable === false) {
                suppressed.push(c);
            }
        }

        this._suppressed = suppressed;
    }

    var _isDomElement = function (context) {
        return (
            typeof HTMLElement === "object" ? context instanceof HTMLElement : //DOM2
                context && typeof context === "object" && context !== null && context.nodeType === 1 && typeof context.nodeName === "string"
        );
    };

    var _isRemovedDomElement = function (context) {
        if (context.closest && !context.closest('body')) {
            //console.log('removed dom element: ', context, context.closest('body'));
            return true;
        }
        return false;
    };
};

new webgis.implementEventController(webgis);