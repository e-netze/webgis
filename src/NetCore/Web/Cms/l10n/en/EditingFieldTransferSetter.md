#field: Field

Name of the field in the target

#category_field: General

#value_expression: Value Expression

Value to be written

#category_value_expression: General

#is_default_value: Is Default Value

The value specified here is a default value and is only used if the field in the source feature class is empty. 
If the field is set in the source feature class, that value is used. 
If this value is set to 'false', the value listed here is always set => The value from the source feature class is overwritten.

#category_is_default_value: General

#is_required: Is required in the target

The result of the value expression must be a value; the result cannot be empty. 
Furthermore, the field must exist in the target feature class; otherwise, an error is returned.

#category_is_required: General