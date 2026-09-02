#visible: Visible

#enable_edit_server: Available via Edit Server

If the edit topic should be available not only via the WebGIS map viewer's editing tools but also via the Collector (App Builder), this option must be set.

#srs: Spatial Reference System (EPSG Code)

Specify the coordinate system in which the data is stored in the database here! 
If no reference system is specified, the edit topic cannot be selected in the viewer.

#tags: Tags (optional)

Tags used to classify an edit topic. List them using commas.

#category_allow_insert: Permissions

#allow_insert: Allow INSERT (create new items)

#category_allow_update: Permissions

#allow_update: Allow UPDATE (edit existing items)

#category_allow_delete: Permissions

#allow_delete: Allow DELETE (delete existing items)

#category_allow_edit_geometry: Permissions

#allow_edit_geometry: Allow editing of geometry

#category_allow_multipart_geometries: Permissions

#allow_multipart_geometries: Allow creating multiparts of geometry

#category_allow_mass_attributation: Permissions

#allow_mass_attributation: Allow mass attribution

#category_show_save_button: Actions (Insert)

#show_save_button: Show save button

Specifies whether the 'Save' button is displayed in the creation dialog.

#category_show_save_and_select_button: Actions (Insert)

#show_save_and_select_button: Show Save and Select (Select) Button

Specifies whether the 'Save and Select' button is offered in the creation dialog.

#category_insert_action1: Actions (Insert)

#insert_action1: 1. Extended Save Action (optional)

For additional buttons offered when saving. 
For a corresponding button to be displayed, an action must be selected here and text assigned to the button. 
The first two options (Save and SaveAndSelect) allow you to override the predefined actions listed above and display different button text.

#category_insert_action_text1: Actions (Insert)

#insert_action_text1: 1. Extended Save Action (Text)

Text displayed in the button for this action.

#category_insert_action2: Actions (Insert)

#insert_action2: 2nd Extended Save Action (optional)

Same as 'Extended Save Action 1'

#category_insert_action_text2: Actions (Insert)

#insert_action_text2: 2nd Extended Save Action (Text)

Same as 'Extended Save Action 1'

#category_insert_action3: Actions (Insert)

#insert_action3: 3rd Extended Save Action (optional)

Same as 'Extended Save Action 1'

#category_insert_action_text3: Actions (Insert)

#insert_action_text3: 3rd Extended Save Action (Text)

Same as 'Extended Save Action 1'

#category_insert_action4: Actions (Insert)

#insert_action4: 4th Extended Save Action (optional)

Same as 'Extended Save Action 1'

#category_insert_action_text4: Actions (Insert)

#insert_action_text4: 4. Extended Save Action (Text)

Same as 'Extended Save Action 1'

#category_insert_action5: Actions (Insert)

#insert_action5: 5. Extended Save Action (Optional)

Same as 'Extended Save Action 1'

#category_insert_action_text5: Actions (Insert)

#insert_action_text5: 5. Extended Save Action (Text)

Same as 'Extended Save Action 1'

#category_auto_explode_multipart_featuers: Actions (Insert)

#auto_explode_multipart_featuers: Auto Explode Multipart Features

If the user draws multipart (also known as fan geometry) features, these are automatically exploded when saving. 
Multiple objects are split.

#category_theme_id: Extended Properties

#theme_id: Internal ThemeId

The ThemeId must be unique for each edit theme and should not be changed once the theme is deployed. 
A unique ID is automatically assigned when a theme is created. For certain tasks, it makes sense to assign a descriptive name to this ID (e.g., if the edit theme is used via a collector app outside the map viewer). 
However, it is crucial to ensure that this value remains unique for all themes. This value should only be changed by experienced administrators!