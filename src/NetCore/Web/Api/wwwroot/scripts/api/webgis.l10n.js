webgis.l10n = new function () {
    this.language = 'de';
    this.supportedLanguage = {};
    this.literals = [];

    this.get = function (id) {
        if (this.literals[this.language] && this.literals[this.language][id]) {
            return this.literals[this.language][id];
        }

        return id;
    }
};