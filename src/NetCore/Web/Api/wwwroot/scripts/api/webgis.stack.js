webgis.fixedStack = function (size) {
    "use strict";
    var _size = size || 10;
    var _stack = [];
    for (var i = 0; i < size; i++) {
        _stack[i] = null;
    }
    this.push = function (obj) {
        for (var i = 1; i < _size; i++) {
            _stack[i - 1] = _stack[i];
        }
        _stack[_size - 1] = obj;
    };
    this.pull = function () {
        var ret = _stack[_size - 1];
        for (var i = size - 1; i > 0; i--) {
            _stack[i] = _stack[i - 1];
        }
        _stack[0] = null;
        return ret;
    };
};
