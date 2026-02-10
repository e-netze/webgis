#field_name: Field Name

#field_type: Input Type

In addition to text input fields, other input types can be defined here: drop-down list (domain), date, file upload, etc.

#visible: Visible

Specifies whether the field is visible to the user. 
Invisible fields can be useful if they are calculated later via an AutoValue or are passed via URL and can be modified.

#locked: Locked (not editable)

Same as 'not visible'. However, the field is displayed here but cannot be changed by the user. 
Locked fields are also written to the database when saving. 
This can be useful if a value (ID) is already passed via URL and the user should no longer be able to change it.

#legend_field: Field determines the legend

If the map's editing theme has a legend with different symbols and the symbol depends on the value of this field, this option can be set. 
The user can then select the (legendary) symbol in addition to the table value from the drop-down list.

#resistant: Persistent (Resistant)

The field value is retained after saving and does not need to be re-entered by the user each time. 
This also applies if the same field exists in different themes. 
For example, a project number that is saved with every object only needs to be entered once and remains 'persistent' in the form until the user assigns a different project number.

#mass_attributable: Field for mass attribution

If mass attribution (changing all selected objects) is enabled, you can specify here whether the field can be set via mass attribution.

#readonly: Read-only (Readonly)

Here, the field is displayed but cannot be changed by the user. 
Readonly fields are NOT written to the database when saving and are for informational purposes only. 
Exception: Readonly fields for which an AutoValue is specified are also written to the database when saving.

#mass_attributable #category_clientside_validation: Validation

#clientside_validation: Client-side validation

Validation takes place on the client side during input or at the latest when the save button is clicked. 
This generally results in a better user experience.

#category_required: Validation

#required: Required

Indicates that user input is required for this field.

#category_min_length: Validation

#min_length: Minimum input length

Specifies the minimum number of characters a user must enter.

#category_regex_pattern: Validation

#regex_pattern: Regular expression (Regex)

A regular expression can be specified here. 
An object can only be created if the user's input for this field matches the regular expression.

#category_validation_error_message: Validation

#validation_error_message: Validation error message

If an error occurs during field validation, this text is displayed to the user. 
Examples of correct input can/should also be provided here.

#category_validation_error_message: Validation error message

#validation_error_message: Validation error message

If an error occurs during field validation, this text is displayed to the user. 

#category_auto_value: Autovalue

#auto_value: Auto Value

#category_custom_auto_value: Autovalue

#custom_auto_value: User-defined Auto Value (custom, db_select=ConnectionString)

#category_custom_auto_value2: Autovalue

#custom_auto_value2: User-defined Auto Value 2 (e.g., db_select=SqlStatement)

Some autovalues ​​require additional parameters. 
For example, with 'db_select', an SQL statement must be entered to retrieve the corresponding value. 
The statement must be formulated so that exactly one result (one value, one record) is generated. {{..}} must be used as a placeholder for existing fields, e.g., select gnr from grst objectid={{id}}. 
Caution: To prevent SQL injection, the placeholders in the statement are converted into parameters. 
Therefore, single quotes around the placeholders are not permitted. The statement can be used even if the corresponding field is a string!

#category_db_domain_connection_string: optional: Database Domain

#db_domain_connection_string: Connection String

#category_db_domain_table: optional: Database Domain

#db_domain_table: Database Table

#category_db_domain_field: optional: Database Domain

#db_domain_field: Database Field

#category_db_domain_alias: optional: Database Domain

#db_domain_alias: Database Display Field (Alias)

#category_db_domain_where: optional: Database Domain

#db_domain_where: Database Where Clause (WHERE)

The selection list can be further restricted here. 
This can be done using a static expression (if the same table is used for different selection lists) or a dynamic expression. 
(XYZ='{{role-parameter:...}}', for example, to restrict a selection list to a specific user group.

#category_db_order_by: optional: Database Domain

#db_order_b