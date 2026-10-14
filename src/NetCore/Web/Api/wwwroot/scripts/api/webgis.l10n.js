webgis.l10n = new function () {
    this.language = 'de';
    this.defaultLanguage = 'de';
    this.supportedLanguages = {};
    this.literals = [];

    // args (optional): object with values for named placeholders, e.g. get('key', { count: 5 }) replaces all "{count}"
    this.get = function (id, args) {
        var text = id;

        if (this.literals[this.language] && this.literals[this.language][id]) {
            text = this.literals[this.language][id];
        } else if (this.literals[this.defaultLanguage] && this.literals[this.defaultLanguage][id]) {
            text = this.literals[this.defaultLanguage][id];
        }

        if (args) {
            for (var name in args) {
                text = text.split('{' + name + '}').join(args[name] === undefined || args[name] === null ? '' : String(args[name]));
            }
        }

        return text;
    }

    this.set = function (id, value) {
        if (!this.literals[this.language]) {
            this.literals[this.language] = {};
        }
        this.literals[this.language][id] = value;
    };
};