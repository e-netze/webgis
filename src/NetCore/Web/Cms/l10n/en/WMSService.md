#dynamic_presentations: Provide presentation variations

Presentation variations are no longer parameterized but are dynamically generated from the service's Table of Contents (TOC). 
The level specifies the maximum level at which subgroups are created. 
Layers below the maximum level are grouped into a checkbox presentation variation.

#dynamic_queries: Provide queries

Queries are no longer parameterized but are generated at runtime for all (feature) layers (without search terms, only Identify).

#dynamic_dehavior: Dynamic behavior

Specifies how layers that are not listed under Themes in the CMS upon creation or after a refresh are handled.
AutoAppendNewLayers ... new themes are added to the map when the service is initialized (after a cache/clear) and can be toggled via the TOC.
UseStrict ... only those themes listed under Themes are displayed on the map. 
SealedLayers_UseServiceDefaults ... All layers are always passed.
This option is only relevant for fallback (print) services for VTC services!

#service_type: Service Type

Watermark services are always drawn at the very top and cannot be made transparent or hidden by the user.
Watermark services can also contain polygon cover in addition to watermarks.

#layer_order: Layer Order

This value specifies how the drawing order of the layers in the capabilities should be interpreted.
From top to bottom or vice versa...

#vendor: Server Vendor

Specifying the WMS server vendor allows parameters specific to the respective server to be passed.
For example, the map's DPI value can be passed so that layer scale limits are applied correctly.

#server: Map Server

#category_server: Service

#version: WMS Version

#category_version: Service

#image_format: WMS GetMap Format

#category_image_format: Service

#get_feature_info_format: WMS GetFeatureInfo Format

#category_get_feature_info_format: Service

#get_feature_info_feature_count: WMS GetFeatureInfo Feature Count

#category_get_feature_info_feature_count: Service

#s_l_d_version: (optional) SLD_Version

This parameter is usually optional. 
Only set it if the WMS absolutely requires it. 
It is passed for GetMap and GetLegendGraphics requests.

#category_s_l_d_version: Service

#category_ticket_server: Login

#ticket_server: webGIS instance for ticket service (Optional)

#username: Username

#category_username: Login credentials

#password: Password

#category_password: Login credentials

#token: Token

#category_token: Login token

#category_client_certificate: Login

#client_certificate: Optional: Client certificate