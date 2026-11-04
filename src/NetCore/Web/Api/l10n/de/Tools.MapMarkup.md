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
## view_angle: Sichtfeld (Winkel)
## intersection_angle: Schnittwinkel
## circle_arc: Kreisbogen
## perpendicular: Normalabstand
## buffer_zone: Pufferzone
## object_distance: Objektabstand
## measure_grid: Messgitter

## save: Zeichnung speichern
## open: Zeichnung laden

## share: Teilen
## upload: Hochladen (GPX, ...)
## download: Herunterladen (GPX, Shape)

# toolgroups:
## draw: Zeichnen
## measure: Messen
## io: Import/Export

# symbology:
## set: Symbolik ändern
## panel-title: Symbolik: {0}
## selection: Auswahl

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
## angle: Öffnungswinkel
## apply-angle: Winkel übernehmen
## angle-unit-deg: Grad
## angle-unit-gon: Gon
## angle-unit-both: Grad und Gon
## angle-unit-none: Keine
## angle-type: Winkelart
## angle-type-inner: Innenwinkel
## angle-type-deflection: Ablenkwinkel (L/R)
## angle-type-azimuth: Richtungswinkel (Azimut)
## label-axis-length: Achsenlänge beschriften
## show-angles: Winkel anzeigen
## show-angles-intersection: Schnittwinkel
## show-angles-adjacent: Nebenwinkel
## show-angles-both: Beide
## show-angles-all: Alle
## label-line-length: Länge beschriften
## length-unit: Längeneinheit
## station-format: Stationierung
## station-format-m: Meter (156,24m)
## station-format-km: Kilometrierung (0+156,24)
## station-format-none: Keine
## buffer-source-type: Quelltyp
## buffer-source-point: Punkt
## buffer-source-multipoint: Mehrere Punkte
## buffer-source-line: Linie
## buffer-source-polygon: Fläche
## buffer-width: Pufferbreite (bei Flächen auch negativ)
## apply-buffer-width: Pufferbreite übernehmen
## show-labels: Beschriftung
## show-labels-all: Alle
## show-labels-radius: Radius
## show-labels-arc-length: Bogenlänge
## show-labels-central-angle: Zentriwinkel
## show-number: Nummer anzeigen
## grid-cell-width: Maschenweite X / Breite [m]
## grid-cell-height: Maschenweite Y / Höhe [m] (leer = wie Breite)
## apply-grid-cell-size: Maschenweite übernehmen
## grid-labels: Beschriftung
## grid-labels-cells: Felder (A1, B1, ...)
## grid-labels-border: Rand (Spalten/Zeilen)
## grid-labels-coords: Koordinaten (nur ausgerichtet)
## grid-labels-none: Keine
## grid-align: Am Koordinatenraster ausrichten
## grid-origin-x: Ursprung X (Ecke A1)
## grid-origin-y: Ursprung Y (Ecke A1)
## apply-grid-origin: Ursprung übernehmen

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
# draw-view-angle: Sichtfeld (Winkel) zeichnen
# draw-intersection-angle: Schnittwinkel zeichnen
# draw-circle-arc: Kreisbogen zeichnen
# draw-perpendicular: Normalabstand zeichnen
# draw-buffer-zone: Pufferzone zeichnen
# draw-object-distance: Objektabstand messen (Objekte in der Karte anklicken)
# draw-measure-grid: Messgitter zeichnen

# apply-line: Linie übernehmen
# finish-reference-line: Bezugslinie beenden
# apply-polygon: Fläche übernehmen
# apply-symbol: Symbol übernehmen

# text:
## symbols-from-selection: Symbole aus Auswahl {0} übernehmen
## points-from-selection: Punkte aus Auswahl {0} übernehmen
## text-from-selection: Texte aus Auswahl {0} übernehmen
## line-from-selection: Linien aus Auswahl {0} übernehmen
## polygon-from-selection: Flächen aus Auswahl {0} übernehmen
## dimline-from-selection: Bemaßungen aus Auswahl {0} übernehmen
## dimpolygon-from-selection: Bemaßungsflächen aus Auswahl {0} übernehmen
## hectoline-from-selection: Hektometrierungen aus Auswahl {0} übernehmen
## buffer_zone-from-selection: Pufferzonen aus Auswahl {0} übernehmen
## object_distance-from-selection: Auswahl {0} als Objekte B übernehmen
## perpendicular-from-selection: Punkte aus Auswahl {0} übernehmen

# selection: Auswahl
## take-from: Aus Auswahl übernehmen
## symbology: Symbolik
## label1:

Die ausgewählten Objekte aus {0} können ins Map-Markup übernommen werden.
|Die Darstellung (Farben) werden aus den aktuellen Map-Markup Einstellungen übernommen und 
können nachher für jedes Objekt wieder einzeln geändert werden.
|Um die Map-Markup Elemente
besser zu unterscheiden, können sie später über das hier angegeben Feld identifiziert werden:

## merge: Objekte zusammenführen
## merge-no: Nein, ein Element je Objekt
## merge-yes: Ja, zu einem Element vereinigen
## info-parts-split: Hinweis: Mehrteilige Objekte wurden in einzelne Elemente aufgeteilt.
## info-holes-removed: Hinweis: Löcher (Innenringe) von Flächen wurden nicht übernommen.
## exception-invalid-buffer-width: Ungültige Pufferbreite: muss größer 0 sein (bei Flächen ungleich 0, negativ = Innenpuffer)
## exception-to-many-objects: Es dürfen maximal {0} ins Map-Markup übernommen werden
## exception-to-many-vertices: Eine Auswahl von Geo-Objekten mit mehr als {0} Stützpunkten dürfen nicht ins Map-Markup übernommen werden!

# io:
## exception-invalid-char: Ungültiges Zeichen im Namen. Vermeinden Sie folgende Zeichen: {0}
## exception-no-projects-found: Unter ihrem Benutzer sind bisher noch keine Map-Markup Projete gespeichert worden. Speichern sie ein Map-Markup Projekt, bevor sie dieses Werkzeug verwenden.
## confirm-delete-project: Soll das Map-Markup Projekt '{mapmarkup-io-load-name}' wirklich gelöscht werden?
## exception-name-is-empty: Der Name darf nicht leer sein
## exception-name-to-long: Der Name ist zu lang. Verwenden sie maximal {0} Zeichen.

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
