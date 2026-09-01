#timing: Timing

Specifies whether the action is executed before or after the object is saved.

#protocol: Protocol

Specifies which protocol is used to call the action.

#target: Target

Specifies the target of the action. For HTTP, the target is the URL of the action.
Placeholders [FIELDNAME] can be used in the target to pass values to the target.

#headers: Headers

Headers can be specified here that are used in the HTTP request, for example for authentication or to specify a content type.

#payload: Payload

The data that is passed to the action. For HTTP_GET these can be URL parameters,
for HTTP_POST the payload is the request body.
Placeholders [FIELDNAME] can be used in the text to pass values from the current feature
to the target.

#success_message: Success Message

A message that is displayed in the viewer when this commit action was executed
successfully (regardless of whether the timing is Before or After). Placeholders
[FIELDNAME] can be used in the text to insert values from the current feature into
the message.
By default the message is displayed as a toast message and disappears automatically
after a few seconds. If the message should be displayed in a dialog, it must start
with "dialog:".
