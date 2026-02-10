#min_zoom_to_scale: Minimum zoom level:

#category_min_zoom_to_scale: General

#allow_empty_search: Allow empty search

Specifies whether the user must enter something to search

#category_allow_empty_search: General

#preview_text_template: Result preview template

If multiple objects are found in a query, a duplicated list of the objects is displayed first. 
A short preview text is generated for each object. 
This text is usually composed of the attribute values ​​of the possible search terms. 
If this is not desired for this query, or if other attributes should be used, a template can be defined here. 
The template can be any text with placeholders for the attributes in square brackets, e.g., House number [HRN] to [STREET]. 
Note: Only attributes that also appear in the result table are translated in the template. For a line break in the preview, you can use \\n.

#category_preview_text_template: Result preview

#category_draggable: General

#draggable: Draggable

WebGIS 5: The result can be dragged from the list into another application (e.g., Datalinq).

#category_show_attachments: General

#show_attachments: Show attachments

#category_distict: Extended properties

#distict: Distinct

If there are objects with identical geometry (e.g., the same point) and the attribute values ​​retrieved in the query are also identical, an object will only be listed once in the results list.

#category_union: Extended properties

#union: Union

Result markers that are located at the same point on the map are combined into a single object. 
The marker in the table view contains all affected records.

#category_apply_zoom_limits: Advanced Properties

#apply_zoom_limits: Apply Layer Zoom Limits

A query (Identify, Dynamic Content in Current View) is only executed if the map is within the zoom limits of the underlying query theme.

#category_max_features: Advanced Properties

#max_features: Maximum Number

Maximum number of features to retrieve in a query. 
A value <= 0 indicates that the maximum number of features that can be returned by the FeatureServer in a request will be retrieved.

#category_max_features: Advanced Properties

#max_features: Maximum Number of Features

Maximum number of features to retrieve in a query. 
A value <= 0 indicates that the maximum number of features that can be returned by the FeatureServer in a request will be retrieved.

#category_max_features: Advanced Properties ... #category_network_tracer: Special

#network_tracer: Network Tracer

#category_gdi_props: Extended Properties (WebGIS 4)

#gdi_props: (Gdi) Properties

#min_scale: Minimum Scale 1:

#category_min_scale: Map Tips (WebGIS 4)

#max_scale: Maximum Scale 1:

#category_max_scale: Map Tips (WebGIS 4)

#map_info_symbol: Symbol

#category_map_info_symbol: Map Tips (WebGIS 4)

#map_info_visible: Visible on Startup

#category_map_info_visible: Map Tips (WebGIS 4)

#is_map_info: Display as Map Tip

#category_is_map_info: Map Tips (WebGIS 4)

#set_visible_with_theme: Set with Theme via TOC Enable with theme

#category_set_visible_with_theme: Map Tips (WebGIS 4)

#feature_table_type: Search result display

#geo_juhu: Query participates in GeoJuhu

#category_geo_juhu: GeoJuhu

#geo_juhu_schema: GeoJuhu Schema

Multiple schemas can be entered here, separated by commas. 
This value is only considered if a GeoJuhu schema is included in the URL. * (asterisk) can be used to query a theme in every schema.

#category_geo_juhu_schema: GeoJuhu

#filter_url: Filter

A query can be linked to a filter. 
A filter icon will then appear in the query results, allowing you to filter by that specific feature.

#category_filter_url: Filter

#query_group_name: Group name