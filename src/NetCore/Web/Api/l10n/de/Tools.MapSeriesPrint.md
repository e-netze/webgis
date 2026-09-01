# name: Karten Serie Drucken

Karten Serie im PDF Format drucken.

# container: Karte

# layout-quality: Layout/Qualität

# layout: Layout

# format: Papierformat

# print-scale: Druckmaßstab

# print-quality: Druckqualität

# print-jobs: Druckaufträge

# start-print-job: Druckauftrag starten

# tools:
## save: Serie speichern
## open: Serie laden
## upload: Serie Hochladen
## download: Serie Herunterladen
## remove-series: Serie entfernen

# create:
## method: Methode
### bbox-grid: Bounding Box Raster

Es wird ein regelmäßiger Raster über die gesamte
Boundingbox der selektierten Objekte gelegt.

### intersection-grid: Schnitt Raster

Es wird ein regelmäßiger Raster gelegt,
aber nur die Seiten werden erzeugt, die mindestens ein Objekt schneiden.

### along-polyline: Entlang einer Linie

Bei Liniengeometrien wird der Raster entlang der Linie erzeugt.
Die Seiten sind entsprechend der Linienrichtung gedreht.

### one-per-feature: Eine Seite pro (Geo)Objekt

Es wird genau eine Seite je Objekt erzeugt.
Der Mittelpunkt liegt im Zentrum des Objekts. 
Jedes einzelne Objekt muss vollständig auf eine Seite passen!

## overlapping-percent: Überlappung (Prozent)
## start: Serie erstellen

# io:
## extend-current-session: Bestehende Serie erweitern
## replace-current-session: Bestehende Serie ersetzen
## exception-no-sketch-defined: 

In der Karte ist noch keine Serie definiert. Bitte erstellen Sie
zuerst eine Serie. Die einzelnen Seiten der Serie können Sie 
Beispielsweise durch klicken in die Karte definieren.

## exception-shape-not-contains-vertices:

Ups, etwas ist schief gelaufen.
Die Geometrie der Serie enthält keine Stützpunkte.

## exception-too-many-pages:

Das Laden der Serien kann nicht korrekt durchgeführt werden, da die Datei
zu viele Seiten ({0} Seiten) enthält.
Es können maximal {1} Seiten in einer Serie definiert werden. Daher werden nicht
alle Seite geladen.

## upload-label1:

Hier können Serien hochgeladen werden. Gültige Dateiendungen sind hier *.json.
Es können nur Serien hochgeladen werden, die mit diesem Werkzeug erstellt wurden.

# create-series-from-features: Serie aus Geo-Objekten
## exception-too-many-pages:
 
Das Erzeugen der Serie kann nicht korrekt durchgeführt werden, weil zu viele Seiten 
erstellt werden müssten ({0} Seiten).
Es können maximal {1} Seiten in einer Serie definiert werden. Verändern sie den Maßstab
oder das Papierformat, um weniger Seiten zu erstellen.

## exception-to-many-iterations:

Das Berechnen der Serie übersteigt die maximale Anzahl der erlaubten Iterationen >{0}
und kann nicht durchgeführt werden. Verändern sie den Maßstab
oder das Papierformat, um weniger Seiten zu erstellen.

## exception-not-all-features-fit:

Nicht alle Objekte passen im gewählten Maßstab auf eine Druckseite. Bitte erhöhen sie den 
Druckmaßstab.