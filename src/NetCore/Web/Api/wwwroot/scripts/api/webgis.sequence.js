webgis.globalSequence = (function () {
    return {
        sequence: 0,
        next: function () {
            this.sequence++;
            return sequence;
        }
    };
})();