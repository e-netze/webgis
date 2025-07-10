# name: Bearbeiten (Edit)

Geo-Objekte in der Karte bearbeiten

# container: Werkzeuge

# error-on-insert: Fehler beim INSERT

# error-on-update: Fehler beim UPDATE

# error-on-delete: Fehler beim DELETE

# desktop:

## point-selection: Punkt Selektion
## rectangle-selection: Rechtecks Selektion
## rectangle-selection-querybuilder: Rechtecks Selection/QueryBuilder
## new-feature: Neues Objekt anlegen
## undo: Rückgängig: Bearbeitungsschritt

## querybuilder: Query Builder

## exception-select-edit-theme: Zum Erstellen eines neuen Objektes zuerst ein Thema aus der Liste auswählen und erneut auf den Button 'Neues Objekt anlegen' klicken.



# mobile:

## new-feature: Neues Objekt anlegen
## edit-feature: Bestehendes Objekt bearbeiten
## delete-feature: Bestehendes Objekt löschen
## attributes_and_save: Stachdaten & Speichern...

## selected-features: Ausgewählte Objekte
## edit-selected-feature: Objekt bearbeiten
## delete-selected-feature: Objekt löschen

## explode-multipart-feature: Multipart auftrennen (explode)
## cut-feature: Objekt teilen (cut)
## clip-feature: Objekt Ausschneiden (clip)
## merge-features: Zusammenführen (merge)
## massattribution: Massenattributierung

## selection-label1:

Zur Zeit sind keine Objekte ausgewählt. Für ausgwählte Objekte stehen noch weitere
Bearbeitunsfunktionen zur Verfügung (merge, explode, cut)

## use-selection-tool: mit Auswahlwerkzeug auswählen...

## selection-label2:

Oder einfach in die Karte klicken, um ein Objekt auszuwählen

## undo: Rückgangig: Arbeitsschritt

# mask:

## edit-attributes: Attribute bearbeiten
## label-geometry: Geometrie
## button-geometry: bearbeiten

## apply-attributes: Attribute übernehmen
## save-and-select: Speichern & Auswählen
## stop-editing: Beenden
## explode: Auftrennen
## merge: Zusammenführen
## cut: Teilen
## clip: Ausschneiden

## warning-massattribution1: Alle ausgewähten Attribute werden übernommen. Für diese Operation ist KEIN Undo möglich!

# warning-affected-layer1: Achtung: Der betroffene Layer wird im aktuellen Kartenmaßstab nicht dargestellt.
# warning-affected-layer2: Achtung: Der betroffene Layer ist momentan nicht sichtbar geschalten. Bereits bestehende Objekte werden nicht angezeigt.
# button-affected-layer-visible: Betroffenen Layer sichtbar schalten!

# confirm-delete-object: Soll das Objekt wirklich gelöscht werden?

# update-in-layer: {0} bearbeiten
# delete-in-layer: {0} löschen

# shortcuts: Tastenkürzel
 
md:
Für das **Bearbeiten-Selektionswerkzeug** stehen folgende Tastenkürzel zur Verfügung:

- **Leertaste**: Nur ein Objekt selektieren. Es wird das Objekt ausgeählt, dass dem geklickten Punkt am nächsten ist.
- **E**: Wie oben, nur dass sofort die Bearbeiten Maske geöffnet wird. 
- **D**: Wie oben, nur dass sofort die Löschen Maske geöffnet wird.

**Voraussetzung**: das Selektionswerkzeug muss aktiv sein (Punkt Selektion) und ein Thema aus der Liste muss ausgeählt sein.

**Vorgehensweise**: Die entsprechende Taste drücken, dann in die Karte klicken, um das Objekt auszuwählen. 
Taste so lange gedrückt halten, bis das Objekt ausgewählt ist. Danach die Taste loslassen.

Weitere wichtige Tastenkürzel:

- **ESC**:
  Bearbeitung abbrechen. Das aktuelle Werkzeug (z.B. Insert/Update/Delete Maske) wird geschlossen. Wurden in der Maske Änderungen vorgenommen, muss das Abbrechen mit **Änderugen verwerfen** bestätigt werden.
- 