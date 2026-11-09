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
                linkPlaceholders[placeholder] = linkHtml('dialog-button', linkText, url);
                placeholderIndex++;
                return placeholder;
            });

            text = text.replace(/\{([^}]+)\}\(([^)]+)\)/g, function (match, linkText, url) {
                const placeholder = `###LINK_${placeholderIndex}###`;
                linkPlaceholders[placeholder] = linkHtml('dialog-link', linkText, url);
                placeholderIndex++;
                return placeholder;
            });

            text = text.replace(/\[\[([^\]]+)\]\]\(([^)]+)\)/g, function (match, linkText, url) {
                const placeholder = `###LINK_${placeholderIndex}###`;
                linkPlaceholders[placeholder] = linkHtml('blank-button', linkText, url);
                placeholderIndex++;
                return placeholder;
            });

            text = text.replace(/\[([^\]]+)\]\(([^)]+)\)/g, function (match, linkText, url) {
                const placeholder = `###LINK_${placeholderIndex}###`;
                linkPlaceholders[placeholder] = linkHtml('blank-link', linkText, url);
                placeholderIndex++;
                return placeholder;
            });

            for (var r in rules) {
                var rule = rules[r];
                text = text.replace(rule.regex, rule.replacement);
            }

            // replace links back 
            for (const placeholder in linkPlaceholders) {
                text = text.replace(new RegExp(placeholder, "g"), function () { return linkPlaceholders[placeholder]; });
            }

            text = text.replaceAll('\n', '<br/>').trim();
        }

        return text;
    }

    function escapeHtml(s) {
        return String(s)
            .replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;').replaceAll("'", '&#39;');
    }

    function isSafeUrl(url) {
        try {
            const protocol = new URL(url, window.location.href).protocol;
            return protocol === 'http:' || protocol === 'https:';
        } catch (e) {
            return false;
        }
    }

    // kind: 'dialog-button' | 'dialog-link' | 'blank-button' | 'blank-link'
    // linkText/url are escaped, urls with other schemes than http(s) are rendered as plain text.
    // Clicks are handled by the delegated listener below (no inline handlers).
    function linkHtml(kind, linkText, url) {
        const text = escapeHtml(linkText);
        if (!isSafeUrl(url)) {
            return text;
        }

        const u = escapeHtml(url);
        switch (kind) {
            case 'dialog-button':
                return `<button data-webgis-md-dialog="${u}" data-webgis-md-title="${text}" class="webgis-button">${text}</button>`;
            case 'dialog-link':
                return `<a data-webgis-md-dialog="${u}" data-webgis-md-title="${text}" href="${u}">${text}</a>`;
            case 'blank-button':
                return `<button data-webgis-md-open="${u}" class="webgis-button">${text}</button>`;
            default:
                return `<a target="_blank" rel="noopener noreferrer" href="${u}">${text}</a>`;
        }
    }

    document.addEventListener('click', function (e) {
        const dialogElement = e.target.closest && e.target.closest('[data-webgis-md-dialog]');
        if (dialogElement) {
            e.preventDefault();
            webgis.iFrameDialog(dialogElement.getAttribute('data-webgis-md-dialog'), dialogElement.getAttribute('data-webgis-md-title'));
            return;
        }

        const openElement = e.target.closest && e.target.closest('[data-webgis-md-open]');
        if (openElement) {
            window.open(openElement.getAttribute('data-webgis-md-open'), '_blank', 'noopener');
        }
    });

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