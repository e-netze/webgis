#visible: Sichtbar

#enable_edit_server: Über Edit-Server verfügbar

Wenn das Editthema nicht nur die Editwerkzeuge des WebGIS-Kartenviewers verfügbar sein sollten, 
sondern auch über den Collector (App-Builder), muss diese Option gesetzt werden.

#srs: Räumliches Bezugssystem (EPSG-Code)

Hier muss das Koordinatensystem angeben werden, in dem die Daten in der Datenbank vorliegen! 
Wenn kein Bezugssystem angegeben wird, kann das Editthema nicht im Viewer ausgewählt werden.

#tags: Tags (optional)

Tags, über die ein Editthema klassifiziert werden kann. Mit Beistrich getrennte Liste anführen.

#category_allow_insert: Rechte

#allow_insert: INSERT (neu anlegen) erlauben

#category_allow_update: Rechte

#allow_update: UPDATE (bestehendes bearbeiten) erlauben

#category_allow_delete: Rechte

#allow_delete: DELETE (bestehendes löschen) erlauben

#category_allow_edit_geometry: Rechte

#allow_edit_geometry: Geometrie: bearbeiten erlauben

#category_allow_multipart_geometries: Rechte

#allow_multipart_geometries: Geometrie: Multiparts erstellen erlauben

#category_allow_mass_attributation: Rechte

#allow_mass_attributation: Massenattributierung erlauben

#category_show_save_button: Aktionen (Insert)

#show_save_button: Speichern Button anzeigen

Gibt an, ober der 'Speichern' Button in der Erstellungsmaske angeboten wird.

#category_show_save_and_select_button: Aktionen (Insert)

#show_save_and_select_button: Speichern und Selektieren (Auswählen) Button anzeigen

Gibt an, ober der 'Speichern und Auswählen' Button in der Erstellungsmaske angeboten wird.

#category_insert_action1: Aktionen (Insert)

#insert_action1: 1. Erweiterte Speicheraktion (optional)

Für zusätzliche Buttons, die beim Speichern angeboten werden. Damit ein entsprechender Button 
angezeigt wird, muss hier eine Aktion gewählt und eine Text für den Button vergeben werden. 
Durch die ersten beiden Optionen (Save und SaveAndSelect) können die hier oben angeführten 
vordefinerten Aktionen überschreiben und mit einem anderen Button Text dargestellt werden.

#category_insert_action_text1: Aktionen (Insert)

#insert_action_text1: 1. Erweiterte Speicheraktion (Text)

Text, der für diese Akton im Button angezeigt wird.

#category_insert_action2: Aktionen (Insert)

#insert_action2: 2. Erweiterte Speicheraktion (optional)

Wie 'Erweiterte Speicheraktion 1'

#category_insert_action_text2: Aktionen (Insert)

#insert_action_text2: 2. Erweiterte Speicheraktion (Text)

Wie 'Erweiterte Speicheraktion 1'

#category_insert_action3: Aktionen (Insert)

#insert_action3: 3. Erweiterte Speicheraktion (optional)

Wie 'Erweiterte Speicheraktion 1'

#category_insert_action_text3: Aktionen (Insert)

#insert_action_text3: 3. Erweiterte Speicheraktion (Text)

Wie 'Erweiterte Speicheraktion 1'

#category_insert_action4: Aktionen (Insert)

#insert_action4: 4. Erweiterte Speicheraktion (optional)

Wie 'Erweiterte Speicheraktion 1'

#category_insert_action_text4: Aktionen (Insert)

#insert_action_text4: 4. Erweiterte Speicheraktion (Text)

Wie 'Erweiterte Speicheraktion 1'

#category_insert_action5: Aktionen (Insert)

#insert_action5: 5. Erweiterte Speicheraktion (optional)

Wie 'Erweiterte Speicheraktion 1'

#category_insert_action_text5: Aktionen (Insert)

#insert_action_text5: 5. Erweiterte Speicheraktion (Text)

Wie 'Erweiterte Speicheraktion 1'

#category_auto_explode_multipart_featuers: Aktionen (Insert)

#auto_explode_multipart_featuers: Auto Explode Multipart Features

Zeichnet der Anwender Multipart (auch Fan-Geometrie) Features, werden diese beim Speichern automatisch auf mehere Objekte aufgeteilt.

#category_theme_id: Erweiterte Eigenschaften

#theme_id: Interne ThemeId

Die ThemeId muss für eine Editthema eindeutig sein und sollte nicht mehr geändert werden, 
wenn ein Thema produktiv eingebunden wird. Die Vergabe einer eindeutigen Id wird beim erstellen 
eines Themas automatisch vergeben. Für bestimmte Aufgaben macht es Sinn, für diese Id einen 
sprechenden Namen zu vergeben (z.B. wenn das Editthema über eine Collector App außerhalb des 
Kartenviewers verwendet wird). Hier muss allerdings immer darauf geachtet werden, dass dieser 
Wert für alle Themen eindeutig bleibt. Dieser Wert sollte nur von versierten Administratoren geändert werden!!!

