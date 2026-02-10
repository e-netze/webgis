#opacity: Initial Opacity

Specifies whether the service should be displayed transparently after the map is launched. 
The value must be between 0 (100% transparent) and 100 (not transparent).

#category_opacity: General

#opacity_factor: Transparency Factor

A factor by which the user-defined transparency is always multiplied. 
For example, if the service should always be displayed semi-transparently, a value of 0.5 can be set here. 
If the user sets the service to 100% opacity, the service will still remain 50% transparent. 
A value of 1 means that the service has no transparency at 100% opacity. 
A value of 0 cannot be entered here, as the service would then not be displayed at all!

#category_opacity_factor: General

#timeout: Timeout

If the service does not return a result after 'x' seconds, the request is aborted.

#category_timeout: General

#image_format: Image Format

Specifies the format in which the map image is retrieved from the service. 
This property is only relevant for ArcIMS and AGS services!

#image_format: Image Format

Specifies the format in which the map image is retrieved from the service. 

#category_image_format: General

#meta_data: Link to metadata

#category_meta_data: General

#visible: Visible by default

Specifies whether the service is visible when the map is accessed...

#category_visible: TOC

#category_toc_display_name: TOC

#category_toc_name: TOC

#collapsed: Extended

#category_collapsed: TOC

#show_in_toc: Show in TOC

ESRI date transformations (array) to be used for on-the-fly projection for this map. 
Only for REST services from AGS 10.5 onwards. An array must always be specified, e.g., [1618, ...] or [1618]. 
Only one transformation can be passed when querying; the first transformation listed here is always used.

#category_show_in_toc: TOC

#category_projection_method: Map projection

#category_projection_id: Map projection

#show_in_legend: Service participates in the legend

Specifies whether the service appears in the legend display

#category_show_in_legend: Legend

#legend_opt_method: Optimization level

Specifies how the legend is optimized.

#category_legend_opt_method: Legend

#legend_opt_symbol_scale: Symbol optimization from a scale of 1:

Specifies the scale at which the symbols in the legend are optimized (only if optimization level = Symbols).

#category_legend_opt_symbol_scale: Legend

#legend_url: URL for fixed legend

If a fixed legend is specified, only this legend will be displayed. 
All other legend properties are ignored.

#category_legend_url: Legend

#use_fix_ref_scale: Use fixed reference scale

Applies only to ArcIMS (AXL) services. 
If this property is set to 'true', a fixed reference scale is always applied to this map service. 
This cannot be overridden by the map user!

#category_use_fix_ref_scale: Reference scale

#fix_ref_scale: Fixed reference scale 1:

Applies only to ArcIMS (AXL) services. 
Specifies a fixed reference scale that is applied to this service. 
This cannot be overridden by the map user!

#category_fix_ref_scale: Reference scale

#min_scale: MinScale: Service is visible down to 1:

If 0 (default) is entered, this value is ignored!

#category_min_scale: Scale limits

#max_scale: MaxScale: Service is visible from 1:

If 0 is entered (default), this value is ignored!

#category_max_scale: Scale limits

#use_with_spatial_constraint_service: Use spatial constraint

Query the service only if a spatial constraint applies to this service.

#category_use_with_spatial_constraint_service: Spatial constraint

#is_basemap: (Background) basemap

Indicates whether this service is a background map.

#category_is_basemap: Basemap

#basemap_type: (Background) basemap type

The URL to an image that is displayed as a preview image for the tile in the viewer. Necessary, for example... For WMS Services

#category_basemap_type: Basemap

#category_basemap_preview_image_url: Basemap

#basemap_preview_image_url: Preview Image URL (Optional)

#export_w_m_s: Service exportable as WMS

#category_export_w_m_s: OGC Export

#map_extent_url: Map Extent Name

#category_map_extent_url: OGC Export

#warning_level: Warning Level

Specifies the level at which errors are displayed on the map

#category_warning_level: Diagnostics

#copyright_info: Copyright Info

Specifies the copyright information assigned to this service. 
This information must be defined under Miscellaneous/Copyright.

#category_copyright_info: General

#category_display_name: General

#query_layer_id: ID of the query layer

#category_query_layer_id: General

#service_url_field_name: Layer field containing service URL

#category_service_url_field_name: General

#layer_visibility: Layer visibility

Specifies whether layers are visible by default.

#category_layer_visibility: General