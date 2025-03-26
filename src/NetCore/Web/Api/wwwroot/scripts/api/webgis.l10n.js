webgis.l10n = new function () {
    this.language = 'de';
    this.defaultLanguage = 'de';
    this.supportedLanguage = {};
    this.literals = [];

    this.get = function (id) {
        if (this.literals[this.language] && this.literals[this.language][id]) {
            return this.literals[this.language][id];
        }

        if (this.literals[this.defaultLanguage] && this.literals[this.defaultLanguage][id]) {
            return this.literals[this.defaultLanguage][id];
        }
        
        return id;
    }
};