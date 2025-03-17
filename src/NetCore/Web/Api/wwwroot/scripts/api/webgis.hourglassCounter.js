webgis.hourglassCounter = function () {
    "use strict";
    webgis.implementEventController(this);
    //this._counter = 0;
    this._names = {};
    this.inc = function (name) {
        //this._counter++;
        this._names[name] = name;
        this.events.fire('showhourglass', this);
    };
    this.dec = function (name) {
        //console.log("dec", name);
        //this._counter--;
        this._names[name] = null;
        if (this._counter() > 0)
            this.events.fire('showhourglass', this);
        else
            this.reset();

        //console.log(this.names(), this._counter());
    };
    this.reset = function () {
        //this._counter = 0;
        this._names = {};
        this.events.fire('hidehourglass', this);
    };
    this.names = function () {
        var ret = [];
        for (var n in this._names) {
            if (this._names[n] != null)
                ret.push(this._names[n]);
        }
        return ret;
    };
    this._counter = function () {
        var counter = 0;
        for (var n in this._names) {
            if (this._names[n] != null) {
                counter++;
            }
        }
        return counter;
    }
};
