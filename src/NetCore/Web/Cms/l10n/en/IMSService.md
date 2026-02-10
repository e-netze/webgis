#server: Map Server

#service: Map Service

#dynamic_presentations: Dynamic Presentations

Presentation variations are no longer parameterized but are dynamically generated from the service's Table of Contents (TOC). 
The level specifies the level to which subgroups are created. 
Layers below the maximum level are grouped into a checkbox presentation variation.

#dynamic_queries: Dynamic Queries

Queries are no longer parameterized but are generated at runtime for all (feature) layers (without search terms, only Identify).

#dynamic_dehavior: Dynamic Behavior

Specifies how layers are handled that are not listed under Themes in the CMS upon creation or after a refresh. 
AutoAppendNewLayers ... new themes are added to the map when the service is initialized (after a cache/clear) and can be toggled via the TOC. 
UseStrict ... only those themes listed under Themes are displayed on the map.

#service_type: Service Type

Watermark services are always drawn at the very top and cannot be made transparent or hidden by the user. 
Watermark services can also contain polygonal cover in addition to watermarks.

#username: Username

#category_username: Login Credentials

#password: Password

#category_password: Login Credentials

#token: Token

#category_token: Login Token

#category_override_local: Localization

#override_local: Override IMS Service LOCALE

Here you can specify how a comma should be interpreted for services.
No value ... Localization is taken from the LOCALE tag of GET_SERVICE_INFO\nde-AT ... Comma as comma, en-US ... Period as comma