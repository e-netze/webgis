#dynamic_presentations: Provide presentation variations

Presentation variations are no longer parameterized but are dynamically generated from the service's table of contents.
The level specifies the level to which subgroups are created.
Layers below the maximum level are grouped into a checkbox presentation variation.

#dynamic_queries: Provide queries

Queries are no longer parameterized but are generated at runtime for all (feature) layers (without search terms, only Identify).

#service_type: Service type

Watermark services are always drawn at the very top and cannot be made transparent or hidden by the user.
Watermark services can also contain polygon deckers in addition to watermarks.

#server: Map server

#category_server: Service

#service: Card service

#category_service: Service

#service_url: Map service url

#category_service_url: Service

#username: Username

#category_username: Login credentials

#password: Password

#category_password: Login credentials

#token: token

#category_token: Login token

#category_image_format: Image Server Properties

#category_pixel_type: Image Server Properties

#category_no_data: Image Server Properties

#category_no_data_interpretation: Image Server Properties

#category_interpolation: Image Server Properties

#category_compression_quality: Image Server Properties

#category_band_i_ds: Image Server Properties

#category_mosaic_rule: Image Server Properties

#category_rendering_rule: Image Server Properties

#rendering_rule: RenderingRule (ExportImage/Legend)

This rendering rule is used for displaying the service and the legend.

#category_rendering_rule_identify: Image Server Properties

#rendering_rule_identify: RenderingRule (Identify)

This rendering rule is used during the Identify operation.
If a RasterAttributeTable exists for this rendering rule, it is used to determine the displayed value.

#category_pixel_aliasname: Image Server Identify

#pixel_aliasname: Pixel Alias ​​Name

When an Identify operation is performed on the service, this value is displayed in the result table instead of 'Pixel'.
This should be a name that describes the result more precisely, e.g., Height [m]