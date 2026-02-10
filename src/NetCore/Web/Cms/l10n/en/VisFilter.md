#layer_names: Affected Layers

#category_layer_names: Filter

#filter: SQL Filter

#category_filter: Filter

#type: Display Type

Specifies whether the filter is displayed in the tool dialog. 
Locked filters are not displayed and are always automatically set (the user cannot reset the filter).
Invisible filters are not displayed in the tool dialog and are not automatically set.
This option is only useful if the filter is intended to be used, for example, only as a query filter.

#category_type: Filter

#set_layer_visibility: Make Affected Layers Visible

Here, a selection list can be defined for each search term. 
If the SQL statement returns 'VALUE' and 'NAME' as columns (SELECT f1 as NAME, f2 as VALUE FROM ...), these are used for the selection list. 
Otherwise, the first column is used for both 'VALUE' and 'NAME'. 
The values ​​for VALUE in the specified query must be unique for selection lists.

#category_set_layer_visibility: Filter

#sql_injection_white_list: SQL Injection Whitelist

Here you can specify a string containing characters that will be ignored by the SQL injection check. e.g.: ><&'\"

#category_sql_injection_white_list: Security

#lookup_layer: Optional: Lookup Layer

If the lookup values ​​are not retrieved via a database or a DataLinq query, but directly from a layer of the service, then this layer must be specified here. 
In the dropdown list, only a '#' needs to be set as the connection string.
Specifying this layer is only necessary if the layers affected by this filter relate to multiple topics.
If only one layer is filtered using this filter, this layer is automatically used for the lookup table unless otherwise specified.

#category_lookup_layer: Dropdown list

#key: Key field

#look_up: Dropdown list

Dropdown list for this key field