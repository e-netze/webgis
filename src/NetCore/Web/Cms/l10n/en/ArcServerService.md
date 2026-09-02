#dynamic_presentations: Provide presentation variations

Presentation variations are no longer parameterized but are dynamically generated from the service's Table of Contents (TOC). 
The level specifies the maximum level at which subgroups are created. 
Layers below the maximum level are grouped into a checkbox presentation variation.

#dynamic_queries: Provide queries

Queries are no longer parameterized but are generated at runtime for all (feature) layers (without search terms, only Identify).

#dynamic_dehavior: Dynamic behavior

Specifies how layers that are not listed under Themes in the CMS upon creation or after a refresh are handled. 
AutoAppendNewLayers ... new themes are added to the map when the service is initialized (after a cache/clear) and can be toggled via the TOC. 
UseStrict ... only those themes listed under Themes are displayed on the map. SealedLayers_UseServiceDefaults ... this prevents any layer settings from being passed to the service. 
This means that the default settings from the layer are always displayed. This option is only relevant for fallback (print) services for VTC services!

#service_type: Service type

Watermark services are always drawn at the very top and cannot be made transparent or hidden by the user.
Watermark services can also contain polygon markers in addition to watermarks.

#allow_query_builder: Allow QueryBuilder (Display filter from TOC)

The user can set filters from the TOC using your SQL editor.

#server: Map Server

#category_server: Service

#service: Map Service

#category_service: Service

#service_url: Map Service URL

#category_service_url: Service

#export_map_format: Export Map Format

With 'JSON', the result is placed in the output directory of ArcGIS Server and retrieved from there by the client. 
If the client does not have access to this output directory, 'Image' can be selected as the option. 
In this case, ArcGIS Server does not place an image but passes the data directly.

#category_export_map_format: Service

#username: Username

#category_username: Login credentials (optional)

#password: Password

#category_password: Login credentials (Optional)

#token: Token

#category_token: or login token (optional)

#category_ticket_expiration: Login credentials (optional)

#ticket_expiration: Ticket validity period [min]

#category_client_i_d: Remaining information

#client_i_d: Ticket client ID

#get_selection_method: GetSelection method

#category_get_selection_method: Selection (deprecated)