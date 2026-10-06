#playground_title: CMS Playground

#playground_description: Test CMS expressions with GeoJSON without publishing the CMS.

#expressions_tile: Expressions

#editform_tile: Edit form & AutoValues

#regex_tile: RegEx

#regex_title: RegEx Playground

#regex_description: Test input validation using regular expressions. Select a template and check valid and invalid inputs. Uses .NET like the CMS regex editor; client-side JavaScript checks may differ for special syntax.

#regex_example: Select example

#regex_custom: Custom expression

#regex_example_username: Username (3–32 characters, starts with a letter)

#regex_example_email: Email address (basic format check)

#regex_example_number: Number (sign, decimal point or comma)

#regex_example_kg: Austrian cadastral municipality number (5 digits)

#regex_example_kg_styria: Styrian cadastral municipality number (starts with 6)

#regex_example_parcel: Parcel number (optional building-area dot and subdivision)

#regex_example_postcode: Austrian postcode (4 digits)

#regex_examples_help: Templates are editable format checks, not existence checks. Cadastral and parcel numbers are not checked against registers; email delivery is not verified. Other federal states have no presets. Selecting an example replaces the pattern and test inputs.

#regex_validation_mode: Validate inputs (one input per line)

#regex_validation_help: Like the CMS editor, any regex match means valid. Use ^ and $ to validate entire inputs. Whitespace is preserved; blank lines test empty inputs. All templates are anchored.

#regex_inputs_help: Validation mode: up to 100 inputs, one per line (up to 10,000 characters in total). Without validation mode: test matches and replacements across the entire text.

#regex_valid: Valid

#regex_invalid: Invalid

#regex_pattern: Regular expression

#regex_test_text: Test text

#regex_ignore_case: Ignore case

#regex_multiline: Multiline mode (^ and $ match each line)

#regex_singleline: Singleline mode (. also matches line breaks)

#regex_replacement: Replacement

#regex_replacement_help: Supports .NET replacement expressions such as $1, ${name}, $&, $` and $'.

#regex_show_replacement: Show replacement preview

#regex_matches: Matches

#regex_number: #

#regex_position: Position (length)

#regex_match: Match text

#regex_groups: Groups

#regex_groups_truncated: (only the first 100 groups shown)

#regex_no_groups: No groups in this expression.

#regex_unmatched: (no match)

#regex_match_count: matches shown

#regex_truncated: Only the first 100 matches are shown.

#regex_no_matches: No matches.

#regex_replacement_preview: Replacement preview

#regex_replacement_truncated: Preview truncated.

#regex_failed: The expression could not be evaluated.

#regex_timeout: Evaluation exceeded the time limit.

#editform_title: Edit form and AutoValue tester

#editform_description: Select a configured edit form, provide sample data and a simulated editing context, then inspect AutoValue results.

#editform_simulation_warning: This simulation runs entirely in the CMS and does not call the WebGIS API or source services. Expressions, GeoJSON geometry, and simple context values are evaluated locally. Geometry functions use the GeoJSON CRS (EPSG:4326 when omitted), not the map or editing theme CRS automatically. Simple database AutoValues (db_select, db_select_on_insert; SELECT or DataLinq endpoint) are executed directly against the configured database or DataLinq endpoint. Spatial service AutoValues are not executed; they are reported as not simulated.

#editform_cms: CMS configuration

#editform_deployment: Deployment (secrets)

#editform_deployment_default: Default secrets (no deployment)

#editform_deployment_help: Secret placeholders ({{secret-...}}) and the replacement file are resolved like a deploy for this deployment's environment.

#editform_select_cms: Select a CMS

#editform_theme: Editing form

#editform_select_theme: Select an editing form

#editform_fields: Form fields

#editform_fields_help: Select up to 20 fields. Required, visibility, and read-only flags come from CMS configuration; conditional rules and layout are not rendered as a complete WebGIS form.

#editform_operation: Operation

#editform_insert: Create new (Insert)

#editform_update: Update

#editform_username: Simulated username

#editform_database_username: Simulated database username

#editform_map_scale: Map scale denominator

#editform_map_sref: Map SRefId

#editform_context_values: Context values as JSON

#editform_context_values_help: Provide values for role-parameter:name, url-parameter:name, and optionally edit_service_id or edit_theme_id as a JSON object, e.g. {"department":"West"}.

#editform_geojson_help: A Feature or FeatureCollection with sample attributes and geometry. Selecting an edit theme fills in its fields without AutoValue as attributes; existing values and the geometry are kept. Maximum 5 MB and 1,000 features.

#editform_apply: Simulate AutoValues

#editform_results: Simulation results

#editform_field: Field

#editform_value: Simulated value

#editform_details: Note / error

#editform_required: required

#editform_hidden: hidden

#editform_readonly: read-only

#editform_no_autovalue: no AutoValue

#editform_select_field: Select at least one field.

#editform_too_many_fields: No more than 20 fields can be simulated at once.

#editform_no_themes: No editing forms were found in this CMS configuration.

#editform_no_fields: No form fields were found for this editing form in the CMS configuration.

#editform_evaluation_failed: The simulation could not be completed.

#expressions_title: Expressions Playground

#expressions_description: Paste GeoJSON and an expression, then apply it directly to each feature.

#back_to_playground: Back to Playground

#expression_type: Expression type

#autovalue: AutoValue

#table_column: Table column

#expression_type_help: The leading = is only required for AutoValues and can be omitted for table columns.

#geojson_label: GeoJSON

#geojson_help: Supports a single Feature or a FeatureCollection. Each Feature in a FeatureCollection is evaluated separately. Maximum 5 MB and 1,000 Features.

#expression_label: Expression

#expression_help: Structured Expressions and existing Legacy templates/$ expressions are evaluated according to the selected expression type. Geometry functions use the FeatureCollection CRS (EPSG:4326 when omitted); shape_* functions can transform to a target SRefId available in the EPSG reference list.

#expression_syntax: Detected syntax

#structured_expression: Structured Expression

#legacy_expression: Legacy template / Simple Expression

#expression_docs: Documentation

#expression_docs_link: WebGIS expressions

#expression_docs_url: https://docs.webgiscloud.com/en/webgis/annex/expressions.html

#expression_reference: Fields and functions

#expression_fields: GeoJSON fields

#expression_fields_empty: No GeoJSON fields found.

#expression_fields_invalid: GeoJSON is invalid; field names cannot be detected.

#structured_functions: Structured functions: concat, upper, lower, trim, substring, replace, length, is_null, is_empty, null_if_empty, round, abs, min, max, format_date, year, month, day, if, coalesce.

#geometry_functions: Geometry functions: shape_len, shape_area, shape_perimeter, shape_centroid_x, shape_centroid_y. An optional target EPSG SRefId can be supplied.

#format_geojson: Format GeoJSON

#apply: Apply

#results: Results

#feature_number: Feature

#result: Result
