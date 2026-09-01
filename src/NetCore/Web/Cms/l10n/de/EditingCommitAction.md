#timing: Timing

Gibt an, ob die Action vor oder nach dem Speichern des Objektes passiert.

#protocol: Protocol

Gibt an, über welches Protokoll die Action aufgerufen wird.

#target: Target

Gibt das Ziel der Action an. Bei HTTP ist das Ziel die Url der Action.
Im Target können Platzhalter [FELDNAME] angeführt werden, um Werte an das
Ziel zu übergeben.

#headers: Headers

Hier können Header angegeben werden, die beim HTTP Request beispielsweise zur Authentifizierung oder zur Angabe eines Content-Types verwendet werden können.

#payload: Payload

Die Daten, die an die Action übergeben werden. Bei HTTP_GET können das Url-Parameter sein,
bei HTTP_POST ist der Payload der Request Body.
Im Text können Platzhalter [FELDNAME] angeführt werden, um Werte aus dem aktuellen Feature
an das Ziel (Target) zu übergeben.

#success_message: Success Message

Eine Message, die im Viewer ausgegeben wird, wenn diese Commit Action erfolgreich
ausgeführt wurde (unabhängig davon, ob das Timing Before oder After ist). Im Text
können Platzhalter [FELDNAME] angeführt werden, um Werte aus dem aktuellen Feature
in die Message einzufügen.
Die Message wird per default als Toast Message angezeigt und verschwindet nach einigen
Sekunden wieder automatisch. Sollte die Nachricht in einem Dialog angezeigt werden, muss
sie mit "dialog:" beginnen.
