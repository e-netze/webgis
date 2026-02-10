#method: Query Method

#category_method: General

#visible: Visible

Specifies whether the field is displayed in the search form. 
Hidden fields are useful, for example, if they are only intended for parameterized calls and should not be visible to the user (e.g., address code, object ID, etc.).

#category_visible: General

#required: Input Required

Specifies whether input in this field is required to execute the query.

#category_required: General

#examples: Input Examples

This text is displayed below the input field and provides the user with example values ​​for input.

#category_examples: Input

#regular_expression: Regular Expression

This expression is used for input validation.

#category_regular_expression: Input

#format_expression: Format Expression

This expression formats the input. {0} is the placeholder for user input, e.g., DATE '{0}' => becomes DATE '2015-5-3'. 
Note: When specifying an expression, the complete expression must be included, including any (single) quotation marks at the beginning or end! e.g., 'fix_prefix_{0}_fix_postfix'.

#category_format_expression: Input

#look_up: Drop-down list

Drop-down list for this search field.

#category_look_up: Drop-down list

#use_look_up: Use drop-down list

Apply a drop-down list for this search field.

#category_use_look_up: Drop-down list

#min_input_length: Minimum character input

The drop-down list is created after entering 'x' characters.

#category_min_input_length: Selection list

#sql_injection_white_list: SQL injection whitelist

Here you can specify a string containing characters that will be ignored by the SQL injection check. e.g.: ><&'\"

#category_sql_injection_white_list: Security

#ignore_in_preview_text: Ignore in result preview

If multiple objects are found in a query, a duplicated list of the objects is displayed first. 
A short preview text is generated for each object. This text is usually composed of the attribute values ​​of the possible search terms. 
If a search term should not be used for the preview text, it can be disabled here.

#category_ignore_in_preview_text: Result preview

#use_upper: Use SQL upper (Oracle)

To avoid case-sensitive searches in Oracle databases, the SQL upper can be set to 'true' for string fields. 
If an SQL Server database is used, the value should always be set to 'false'.

#category_use_upper: SQL