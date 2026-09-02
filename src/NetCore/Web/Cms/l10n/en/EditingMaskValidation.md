#field_name: Field Name

Field that should be validated before saving

#operator: Operator

Method used to check the field value against the validator: e.g., Ident (exactly the same), Equals (equal but case-insensitive), in (field value must be in the list of validator values), inside (one of the field values ​​must be in one of the validator values), IN/INSIDE (like in/inside, but case-sensitive). 
If the field value or the validator contains commas or semicolons, these are used as separators, and the value is interpreted as a list.

#validator: Validator

Value to be checked. A list with separators (, or ;) can also be specified here. 
Placeholders for user roles are also possible: role-parameter:GEMNR

#message: Message

Message displayed if validation fails