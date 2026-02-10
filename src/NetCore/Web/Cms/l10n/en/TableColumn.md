#column_type: Column Type

Type of table column.

#category_column_type: General

#data: Definition/Source

#category_data: General

#visible: Visible: Displayed in the table

Fields that are not visible are not displayed in the table. 
Note: Hidden fields are still transmitted to the client, for example, for marker definitions in custom.js. 
Making fields invisible is not a security measure; attributes are still transmitted to the client, only their display in the user interface is suppressed.

#data: Definition/Source

#category_data: General

#visible: Visible: Displayed in the table

Fields that are not visible are still transmitted to the client, for example, for marker definitions in custom.js. 

#category_visible: General

#category_show_column_name_with_html: Search result display (WebGIS 4)

#show_column_name_with_html: Show column name in HTML view

#category_is_html_header: Search result display (WebGIS 4)

#is_html_header: Show in HTML header

#category_show_in_html: Search result display (WebGIS 4)

#show_in_html: Use in HTML view

#category_sort: Search result display (WebGIS 4)

#sort: Sort in table view

#field_name: Field name

Simple translation of values. Input example: 0,1,2=yes,no,maybe. Alternatively, a URL to a JSON array with name,value values ​​can be specified, for example, a DataLinq PlainText query.

#simple_domains: Simple Domains

#raw_html: Raw HTML

The field value is copied verbatim. 
This allows HTML fragments to be directly inserted into the table (by default, angle brackets, for example, are encoded and displayed as such in the table). 
This flag should only be used if absolutely necessary. 
If the field contains user input (editing, etc.), this flag should be avoided at all costs, as it creates a cross-site scripting vulnerability!

#sorting_algorithm: Sorting Algorithm

Specifies the algorithm used to sort the column in the table. 
By default, the column is interpreted as a string when sorting. 
The built-in algorithms for dates are: date_dd_mm_yyyy. 
Additional algorithms can be defined in custom.js.

#sorting_algorithm: Sorting Algorithm #category_sorting_algorithm: Sort

#auto_sort: Auto-sort

Specifies whether this field should be automatically sorted after a query.

#category_auto_sort: Sort

#format_string: Format string (optional)

For the DisplayType 'normal', the formatting string can optionally be specified here. 
Examples: MM/dd/yyyy, dddd, dd MMMM yyyy HH:mm:ss, MMMM dd. 
A more detailed description can be found here: https://docs.microsoft.com/en-us/dotnet/api/system.datetime.tostring?view=net-6.0 or https://www.c-sharpcorner.com/blogs/date-and-time-format-in-c-sharp-programming1

#hotlink_url: Hotlink URL

#hotlink_name: Name/Label of the hotlink

The URL for the hotlink with wildcards, e.g., http://www.server.com/page?id=[ID_FIELDNAME]&name=[NAME_FIELDNAME]. 
The prefix 'url-encode:' can be used to force URL encoding of the field if the browser's automatic encoding is insufficient, e.g., [url-encode:FIELDNAME].

#hotlink_name: Name/Label of the hotlink

The URL for the hotlink with wildcards, e.g., http://www.server.com/page?id=[ID_FIELDNAME]&name=[NAME_FIELDNAME]. 

#one2_n: 1 : N

#one2_n_seperator: 1 : N separator

#browser_window_props: Browser window attributes

#target: Target for new browser window

_blank ... new browser window\n_self ... viewer window (current window)\nopener ... window from which webGIS was accessed

#image_expression: Image source expression

#i_width: Image source width (pixels)

#i_height: Image source height (pixels)

#expression: Expression

#column_data_type: Data type of the result

If the result is always a number, Number can be used as the type here. 
This allows the column to be sorted like a numeric array. 
Important: Every result must be a number (no empty values).