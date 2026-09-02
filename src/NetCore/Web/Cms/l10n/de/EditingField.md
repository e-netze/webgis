#field_name: Feld Name

#field_type: Eingabe Typ

Neben Text-Eingabefeldern können hier noch weitere Eingabetypen definiert werden: Auswahlliste (Domain), Datum, Fileupload, ...

#visible: Sichtbar

Gibt an, ob das Feld für den Anwender sichtbar ist. 
Unsichtbare Felder können praktisch sein, wenn diese eventuell erst später über einen AutoValue berechnet 
werden oder bereits über Url übergeben werde und verändert werden dürfen.

#locked: Gesperrt (nicht veränderbar)

Wie bei 'nicht Sichtbar'. Hier wird das Feld allergings angezeigt, kann vom Anwender aber nicht geändert werden. 
Locked Felder werden beim Speichern auch in die Datenbank geschreiben. 
Kann nützlich sein, wenn ein Wert (ID) schon über Url Aufruf übergeben wird und der Anwender diesen nicht mehr ändern sollte.

#legend_field: Feld bestimmt die Legende

Besitzt das Editierthema in der Karte eine Legende mit unterschiedlichen Symbolen und ist das Symbol 
abhängig vom Wert dieses Feldes, kann diese Option gesetzt werden. Der Anwender hat dann über die 
Auswahlliste die Möglichkeit, neben dem Tabellenwert auch das (Legenden) Symbol auszuwählen.

#resistant: Beständig (Resistant)

Der Wert des Feldes bleibt nach dem Speichern erhalten und muss nicht jedes Mal vom Anwender neu eingeben werden. 
Das gilt auch, wenn es das gleiche Feld in unterschiedlichen Themen gibt. ZB muss so eine Projektnummer, die auf jedem 
Objekt mitverspeichert wird nur einmal eingeben werden und bleibt im Formular 'beständig', bis der Anwender eine andere Projektnummer vergibt.

#mass_attributable: Feld für Massen-Attributierung

Wird Massen-Attributierung (alle ausgewählten Objekte ändern) erlaubt, kann hier angegeben werden, ob das Feld über Massen-Attributierung gesetzt werden darf.

#readonly: Schreibgeschützt (Readonly)

Hier wird das Feld zwar angezeigt, kann vom Anwender aber nicht geändert werden. Readonly Felder werden 
beim Speichern NICHT in die Datenbank geschrieben und dienen nur informativen Zwecken. 
Ausnahme: Readonly Felder, für die ein AutoValue angegeben wird, werden beim Speichern auch in die Datenbank geschrieben.

#category_clientside_validation: Validierung

#clientside_validation: Clientseitige Validierung

Die Validierung erfolgt bereichts am Client bei der Eingabe bzw. spätestens beim Klick auf den Speichern Button. 
Dadurch ergibt sich in der Regel eine besser 'User Experience'

#category_required: Validierung

#required: Erforderlich (requried)

Gibt an, dass für dieses Feld eine Eingabe vom Anwender erfolgen muss

#category_min_length: Validierung

#min_length: Minimale Eingabe-Länge

Gibt die minimale Eingabe von Zeichen an, die ein Anwender eingeben muss

#category_regex_pattern: Validierung

#regex_pattern: Regulärer Ausdruch (Regex)

Hier kann ein regulärer Ausdruck angegeben werden. Ein Objekt kann nur erstellt werden, wenn die Eingabe 
des Benutzers für dieses Feld, dem regulären Ausdruck entspricht

#category_validation_error_message: Validierung

#validation_error_message: Validierungsfehlermeldung

Kommt es bei der Validierung eines Feldes zu einem Fehler, wird dem Anwender dieser Text angezeigt. 
Hier können/sollten auch Beispiele für korrekte Eingaben angeführt werden.

#category_auto_value: Autovalue

#auto_value: Auto Value

#category_custom_auto_value: Autovalue

#custom_auto_value: Benuterdefinierter Auto Value (custom, db_select=ConnectionString)

#category_custom_auto_value2: Autovalue

#custom_auto_value2: Benutzerdefinerter Auto Value 2 (zb: db_select=SqlStatement

Manche Autovalues benötigen weitere Parameter. zB. bei 'db_select' muss hier ein SQL Statement 
eingetragen werden, über das der entsprechende Wert ermittelt wird. Das Statement muss dabei so formuliert 
werden, dass exakt ein Ergebniss (ein Wert, ein Record) entsteht. 
Als Platzhalter für bestehende Felder muss {{..}} verwendet werden, zB select gnr from grst objectid={{id}}. 
Achtung: Um Sql Injection vorzubeugen, werden die Platzhalter im Statement in Parameter umgewandelt. 
Daher dürfen hier keine Hochkomma rund um die Platzhalter im Statement verwendet werden, auch wenn das entsprechende Feld ein String ist!

#category_db_domain_connection_string: optional: Database Domain

#db_domain_connection_string: Connection String

#category_db_domain_table: optional: Database Domain

#db_domain_table: Db-Tabelle (Table)

#category_db_domain_field: optional: Database Domain

#db_domain_field: Db-Feld (Field)

#category_db_domain_alias: optional: Database Domain

#db_domain_alias: Db-Anzeige Feld (Alias)

#category_db_domain_where: optional: Database Domain

#db_domain_where: Db-Where Klausel (WHERE)

Hier kann die Auswahlliste weiter eingeschränkt werden. 
Das kann über einen statischer Ausdruck erfolten (wenn die gleiche Tabelle für unterschiedliche Auswahlisten verwendet wird) 
oder über einen dynamische Ausdruck (XYZ='{{role-parameter:...}}', um beispielsweise eine Auswahlliste für eine 
Bestimmte Benutzergruppe einzuschränken.

#category_db_order_by: optional: Database Domain

#db_order_by: Db-Orderby (Field)

#category_domain_list: optional: Domain List

#domain_list: Db-Anzeige Feld (Alias)

#category_domain_pro_behaviour: optional: Domain Behaviour (experimental)

#domain_pro_behaviour: Pro Behaviour

Gibt an, ob die Auswahlliste ein erweitertes Verhalten haben soll. 
Das erweiterte Verhalten ist von der WebGIS Instanz Konfiguration abhängig. 
(setzt man in der custom.js: webgis.usability.select_pro_behaviour = \"select2\"; ist für diese Auswahlliste die Suche nach 
items möglich. Das macht bei Auswahllisten mit vielen Items die Eingabe leichter.

#category_attribute_picker_query: optional: Attribute Picker

#attribute_picker_query: Attribute Picker Abfrage

Die Abfrage, von der ein Attribute geholt werden soll. Format service-id@query-id

#category_attribute_picker_field: optional: Attribute Picker

#attribute_picker_field: Attribute Picker Feld

Das Feld aus der Abfrage, das beim Attribute Picking übernommen werden soll.

