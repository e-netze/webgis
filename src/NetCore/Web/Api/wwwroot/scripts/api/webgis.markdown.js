webgis.simpleMarkdown = new function() {
    var rules = [
        {
            regex: /(\*\*)(.*.*)\1/g,
            replacement: '<strong>$2</strong>'
        },
        {
            regex: /(\*)(.*)\1/g,
            replacement: '<i>$2</i>'
        },
        {
            regex: /(_)(.*)\1/g,
            replacement: '<em>$2</em>'
        },
        {
            regex: /(\^)(.*)\1/g,
            replacement: '<sup>$2</sup>'
        },
        {
            regex: /(~)(.*)\1/g,
            replacement: '<sub>$2</sub>'
        },
        {
            regex: /(\+)(.*)\1/g,
            replacement: '<ins>$2</ins>'
        },
        {
            regex: /(\?\?)(.*)\1/g,
            replacement: '<cite>&mdash; $2</cite>'
        },
        {
            regex: /(bq\.\s*)(.*)/g,
            replacement: '<blockquote>$2</blockquote>'
        },
        {
            regex: /{{(.*)}}/g,
            replacement: '<code>$1</code>'
        },
        {
            regex: /{quote}((.|\n)*){quote}/g,
            replacement: blockquote
        },
        {
            regex: /{color:(.*)}((.|\n)*?){color}/g,
            replacement: changeColor
        }
    ];

    this.render = function (text) {
        if (text) {

            // placeholders for Links
            const linkPlaceholders = {};
            let placeholderIndex = 0;

            // extract all link and replace it with placheholders => reason: placeholders should not be touched by roles

            text = text.replace(/\{\[([^}]+)\]\}\(([^)]+)\)/g, function (match, linkText, url) {
                const placeholder = `###LINK_${placeholderIndex}###`;
                linkPlaceholders[placeholder] = `<button onclick="webgis.iFrameDialog('${url}','${linkText}')" class="webgis-button">${linkText}</button>`;
                placeholderIndex++;
                return placeholder;
            });

            text = text.replace(/\{([^}]+)\}\(([^)]+)\)/g, function (match, linkText, url) {
                const placeholder = `###LINK_${placeholderIndex}###`;
                linkPlaceholders[placeholder] = `<a onclick="webgis.iFrameDialog('${url}','${linkText}');return false;"" href="${url}">${linkText}</a>`;
                placeholderIndex++;
                return placeholder;
            });

            text = text.replace(/\[\[([^\]]+)\]\]\(([^)]+)\)/g, function (match, linkText, url) {
                const placeholder = `###LINK_${placeholderIndex}###`;
                linkPlaceholders[placeholder] = `<button onclick="window.open('${url}')" class="webgis-button">${linkText}</button>`;
                placeholderIndex++;
                return placeholder;
            });

            text = text.replace(/\[([^\]]+)\]\(([^)]+)\)/g, function (match, linkText, url) {
                const placeholder = `###LINK_${placeholderIndex}###`;
                linkPlaceholders[placeholder] = `<a target="_blank" href="${url}">${linkText}</a>`;
                placeholderIndex++;
                return placeholder;
            });

            for (var r in rules) {
                var rule = rules[r];
                text = text.replace(rule.regex, rule.replacement);
            }

            // replace links back 
            for (const placeholder in linkPlaceholders) {
                text = text.replace(new RegExp(placeholder, "g"), linkPlaceholders[placeholder]);
            }

            text = text.replaceAll('\n', '<br/>').trim();
        }

        return text;
    }

    function blockquote(match, p1) {
        var lines = p1.split('\n');
        var result = '<blockquote>';
        for (var l in lines) {
            var line = lines[l];
            if (line !== '') {
                result += '  <p>' + line.trim() + '</p>';
            }
        };

        result += '</blockquote>';

        return result;
    }

    function changeColor(match, p1, p2) {
        var color = p1;
        var lines = p2.split('\n');
        var result = '';
        for (var l in lines) {
            var line = lines[l];
            if (line !== '') {
                result += '<p style="color:' + color + '">' + line.trim() + '</p>';
            }
        };

        return result;
    }
}