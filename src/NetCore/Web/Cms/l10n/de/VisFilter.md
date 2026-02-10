#layer_names: Betroffene Layer

#category_layer_names: Filter

#filter: SQL Filter

#category_filter: Filter

#type: Anzeigetyp

Gibt an, ob der Filter im Werkzeugdialog angezeigt wird. Gesperrte Filter (locked) werden nicht
angezeigt und sind automatisch immer gesetzt (der Benutzer kann den Filter nicht zurücksetzen). 
Unsichtbare (invisible) Filter werde nicht im Werkzeugdialog angezeigt und sind auch nicht automisch
gesetzt. Diese Option ist nur sinnvoll, wenn der Filter z.B. nur als Abfragefilter verwendet werden soll.

#category_type: Filter

#set_layer_visibility: Betroffene Layer sichtbar schalten

Hier kann für jeden Suchbegriff eine Auswahlliste definiert werden. Liefert das SQL Statement als 
Spalten 'VALUE' und 'NAME' zurück (SELECT f1 as NAME, f2 as VALUE FROM ....) werden diese für die
Auswahlliste verwendet. Ansonsten wird die erste Spalte, sowohl für 'VALUE' und 'NAME' verwendet. 
Die Werte für VALUE aus der angeführten Abfrage müssen für Auswahllisten eindeutig sein.

#category_set_layer_visibility: Filter

#sql_injection_white_list: Sql Injektion Whitelist

Hier kann ein String mit Zeichen angegeben werden, die von der SQL-Injektion überprüfung ignoriert
werden. zB: ><&'\"

#category_sql_injection_white_list: Sicherheit

#lookup_layer: Optional: Lookup Layer

Werden die Lookup-Werte nicht über eine Datenbank oder eine DataLinq Query abgeohlt, sondern
direkt von einem Layer des Dienstes, dann dieser Layer hier eingestellt werden. Bei der
Auswahlliste muss dann nur noch ein '#' als Connection String eingestellt werden. Die 
Angabe dieses Layers ist nur notwendig, wenn die betroffenen Layer für diesesn Filter sich 
auf mehrere Themen beziehen. Wird über diesen Filter nur ein Layer gefilter, ist dieser auch
automatisch der Layer für die Lookup Tabelle, wenn hier nichts anderes angeführt wurde.

#category_lookup_layer: Auswahlliste

#key: Key-Feld

#look_up: Auswahlliste

Auswahlliste für dieses Key-Feld

