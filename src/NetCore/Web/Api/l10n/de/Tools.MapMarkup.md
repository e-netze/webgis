# name: Zeichnen (Map Markup)

Einfaches Zeichnen in der Karte.

# container: Werkzeuge

# tools:
## select: Auswählen
## symbol: Symbol
## text: Text
## point: Punkt
## freehand: Freihand
## line: Linie
## polygon: Fläche
## rectangle: Rechteck
## circle: Kreis
## distance_circle: Umgebungs Kreis
## compass_rose: Kompass Rose
## dimline: Bemaßung
## dimpolygon: Bemaßungsfläche
## hectoline: Hektometrierungslinine

## save: Zeichnung speichern
## open: Zeichnung laden

## share: Teilen
## upload: Hochladen (GPX, ...)
## download: Herunterladen (GPX, Shape)

# symbology:
## set: Symbolik ändern

## symbol: Symbol

## point-color: Punktfarbe
## point-size: Punktgröße

## font-size: Schriftgröße
## font-style: Schriftstil
## font-color: Schriftfarbe

## line-color: Linienfarbe
## line-weight: Linienstärke
## line-style: Linienart

## fill-color: Füllfarbe
## fill-opacity: Deckkraft

## properties: Eigenschaften
## num-circles: Anzahl Kreise
## radius: Radius (m)
## apply-radius: Radius übernehmen
## num-angle-segments: Anzahl Winkel Segmente
## unit: Einheit
## segment-unit: Abstand [Einheit]
## apply-segment: Abstand übernehmen
## area-unit: Flächen Einheit
## label-edges: Kanten beschriften
## label-total-length: Gesamt-Länge beschriften

# draw-symbol: Symbol setzen
# draw-point: Punkt setzen
# draw-text: Text setzen
# draw-freehand: Freihand zeichnen
# draw-line: Linie zeichnen
# draw-polygon: Fläche zeichnen
# draw-distance-circle: Umgebungs Kreis zeichnen
# draw-compass-rose: Kompass Rose zeichnen
# draw-dimline: Bemaßung zeichnen
# draw-dimpolygon: Bemaßungsfläche zeichnen
# draw-hectoline: Hektometrierungslinie zeichnen

# apply-line: Linie übernehmen
# apply-polygon: Fläche übernehmen
# apply-symbol: Symbol übernehmen

# text:
## symbols-from-selection: Symbole aus Auswahl {0} übernehmen
## points-from-selection: Punkte aus Auswahl {0} übernehmen
## text-from-selection: Texte aus Auswahl {0} übernehmen
## line-from-selection: Linien aus Auswahl {0} übernehmen
## polygon-from-selection: Flächen aus Auswahl {0} übernehmen

# selection: Auswahl
## take-from: Aus Auswahl übernehmen
## symbology: Symbolik
## label1:

Die ausgewählten Objekte aus {0} können ins Map-Markup übernommen werden.
|Die Darstellung (Farben) werden aus den aktuellen Map-Markup Einstellungen übernommen und 
können nachher für jedes Objekt wieder einzeln geändert werden.
|Um die Map-Markup Elemente
besser zu unterscheiden, können sie später über das hier angegeben Feld identifiziert werden:

## exception-to-many-objects: Es dürfen maximal {0} ins Map-Markup übernommen werden
## exception-to-many-vertices: Eine Auswahl von Geo-Objekten mit mehr als {0} Stützpunkten dürfen nicht ins Map-Markup übernommen werden!

# io:
## exception-invalid-char: Ungültiges Zeichen im Namen. Vermeinden Sie folgende Zeichen: {0}
## exception-no-projects-found: Unter ihrem Benutzer sind bisher noch keine Map-Markup Projete gespeichert worden. Speichern sie ein Map-Markup Projekt, bevor sie dieses Werkzeug verwenden.
## confirm-delete-project: Soll das Map-Markup Projekt '{mapmarkup-io-load-name}' wirklich gelöscht werden?

## extend-current-session: Bestehendes Map-Markup erweitern
## replace-current-session: Bestehendes Map-Markup ersetzen

## upload-label1:

Hier können Map-Markup Objekte hochgeladen werden. Gültige Dateiendungen sind hier *.gpx

## mapmarkup-project: Map-Markup Projekt (Geo-Json)
## gpx-file: GPX Datei
## shape-file: ESRI Shape Datei

## download-label1:

Hier können Map-Markup Objekte herunter geladen werden.

## download-label-gpx:

Bei GPX werden nur die gezeichneten Linien als 'Tracks'
und die Texte bzw. Symbole als 'Waypoints' exportiert.

## download-label-shape:

Für ESRI Shape Dateien muss noch zusätzlich die Zielprojektion angegeben werden.
Für jeden Geometrietyp wird ein Shapefile angelegt und in ein ZIP File verpackt.

## download-label-json:

Bei Map-Markup Projekten werden alle Objekte (plus Darstellung)
als GeoJSON herunter geladen und können später wieder hochgeladen werden.
