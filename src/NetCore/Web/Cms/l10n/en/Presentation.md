#layer_names: Visible Layers

#category_layer_names: General

#thumb_nail: Preview Image

#category_thumb_nail: General

#description: Description

Description of the display variant. Enter '#' in this field to automatically list the affected layers as a description.

#category_description: General

#category_use_for_basemap: (WebGIS 4) Only when using a service with GDI

#use_for_basemap: Use with basemap

Specifies what should be affected by the display variant: the respective service or the entire map. 
This value is not relevant for display variants with checkboxes, as only the listed themes are activated.

#category_use_for_basemap: Use with basemap #category_gdi_group_name: Group

#category_gdi_group_display_style: Group

#category_visible_with_service: Group

#visible_with_service: Visible when this service is on the map

Displaying a display variant wasn't always useful. 
For example, if you want to disable topics from another service (e.g., cadastral map) when activating a display variant (e.g., natural resources), it makes no sense for the container to be displayed if only the cadastral services are present on the map. 
In this case, you can disable this option here. The actual group will then only be displayed if the service (e.g., natural resources) is also integrated into the map.

#category_gdi_group_display_style: Group

#category_visible_with_service: Group

#category_visible_with_service: Visible when this service is on the map

#category_visible_with_service: Visible when this service is on the map

#category_gdi_group_display_style: Group

#category_visible_with_service: Visible when this service is on the map ... integrated 

#category_visible_with_one_of_services: Group

#visible_with_one_of_services: Visible when one of these services is present on the map

List of service URLs separated by commas

#category_is_container_default: Container

#is_container_default: Default for containers

#category_container_url: Container

#container_url: Container URL

Specifies what should be affected by the display variant: the respective service or the entire map. 
This value is not relevant for display variants with checkboxes, as only the listed topics are activated there.

#visible: Visible

Display variant is visible/switchable for the user

#category_visible: General

#metadata_link: Metadata Link

Displayed as an [i] button in the viewer and points to the specified link. 
The link can use placeholders for the map, similar to those used in custom tools: {map.bbox}, {map.centerx}, {map.centery}, {map.scale}

#category_metadata_link: Metadata

#metadata_target: Metadata Target

Specifies how the link opens (tab => new tab, dialog => in a dialog box in the viewer).

#category_metadata_target: Metadata

#metadata_title: Metadata Title

A title for the metadata button can be specified here.

#category_metadata_title: Metadata

#metadata_link_button_style: Metadata Button Style

Specifies how the button is displayed: [i] Button or prominent link button with a title.

#category_metadata_link_button_style: Metadata

#client_visibility: Visible if client

Here you can restrict whether a display variant is shown only on a specific device.

#category_client_visibility: Visibility

#u_i_group_name: Grouping

The display variant tree consists of a container (parent element) and the actual display variants, which in turn can be located in a (collapsible) group. 
Multiple levels are not offered by default to prevent the user from having to click through too many levels. 
Therefore, no further level is offered here in the interface. However, there are exceptions where an additional level can make the user elements in the viewer leaner and simpler. 
For these exceptions, it is possible to specify a further grouping here. 
The name specified here corresponds to the name of another collapsible group that is displayed in the display variant tree. 
Multiple display variants in the current level can share the same group name and will be displayed under this group. 
Note: The value entered here should generally be empty, unless further grouping offers usability advantages. 
The value entered here will only be considered for display variants that are already in the expandable group. 
If the display variant is located in the top level of the container, this value will be ignored. 
The solution is to create a group and place the display variant there! 
Multiple levels can be specified. The separator is a forward slash (/). If a '/' is present in the text, it must be encoded using '\\/'.

#category_u_i_group_name: User Interface

#checked: Visible

Theme is enabled on startup

#category_checked: General

#name: Name

Name of the theme for the display variants

#category_name: General