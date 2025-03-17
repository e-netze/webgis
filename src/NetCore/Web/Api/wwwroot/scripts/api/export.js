jQuery.fn.webgis_table2CSV = function (options) {
    options = jQuery.extend({
            separator: ';',
            skipCols: 0,
            header: []
        },
        options);

    var csvData = [];
    var headerArr = [];
    var el = this;

    //header
    var numCols = options.header.length;
    var tmpRow = []; // construct header avalible array

    if (numCols > 0) {
        for (var i = 0; i < numCols; i++) {
            tmpRow[tmpRow.length] = formatData(options.header[i]);
        }
    } else {
        $(el).filter(':visible').find('th').each(function (i, e) {
            if (i >= options.skipCols) {
                if ($(this).css('display') !== 'none') {
                    tmpRow[tmpRow.length] = formatData($(this).html());
                }
            }
        });
    }

    row2CSV(tmpRow);

    // actual data
    $(el).find('tr').each(function () {
        var tmpRow = [];
        $(this).filter(':visible').find('td').each(function (i, e) {
            if (i >= options.skipCols) {
                if ($(this).css('display') !== 'none') tmpRow[tmpRow.length] = formatData($(this).html());
            }
        });

        row2CSV(tmpRow);
    });
    var mydata = csvData.join('\n');
    return mydata;

    function row2CSV(tmpRow) {
        var tmp = tmpRow.join('') // to remove any blank rows
       
        if (tmpRow.length > 0 && tmp !== '') {
            var mystr = tmpRow.join(options.separator);
            csvData[csvData.length] = mystr;
        }
    }
    function formatData(input) {
        // replace " with “
        var regexp = new RegExp(/["]/g);
        var output = input.replace(regexp, "“");
        //HTML
        var regexp = new RegExp(/\<[^\<]+\>/g);
        var output = output.replace(regexp, "");
        if (output == "") return '';
        return '"' + output + '"';
    }
};