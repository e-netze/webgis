# name: Koordinaten / Höhe

Koordinaten und Höhenwerte abfragen

# container: Abfragen

# enter-coordinates: Koordinaten eingeben
# upload-coordinates: Koordinaten hochladen (CSV)
# download-coordinates: Koordinaten herunterladen (CSV)

# coordinate-system: Koordinatensystem
# easting: Rechtswert
# northing: Hochwert
# apply-coordinates: Koordinaten übernehmen
# show-coordinates: Koordinaten anzeigen

# exception-no-points-found: Keine Punkte zum herunterladen gefunden

# upload:
## label1:

Hier können Koordinaten hochgeladen werden. Die Koordinaten müssen als CSV Dateien 
vorliegen mit einem Strichpunkt als Trennzeichen. Die Spalten der CSV Datei sollten 
Punktname/nummer, Rechtswert und Hochwert entsprechen. Die erste Zeile wird als Tabellenüberschrift 
interpretiert.

## label2:

Hier muss das Koordinatensystem angegeben werden, in dem die Koordinaten der CSV Datei vorliegen:

## exception-too-many-points: Es dürfen maximal {0} Koordinatenzeilen hochgeladen werden
## exception-invalid-row: Ungültige Zeile: {0}

## sketch:

### label1: Hier kann GPX oder GeoJson File hochgeladen werden, das als Sketch übernommen wird.
### exception-no-geometry-candidates: In der Datei wurden keine passenden Sketch Kanditaten für die Geometrie {0} gefunden.

# download:

## sketch:

### label1: Hier kann der aktuelle Sketch herunter geladen werden.

# tip-label: Eingabe Tipp
# tip:

Es gibt projizierte Koordinaten (GK-M34, Web Mercator, ...) und geographische Koordinaten (WGS 84, GPS).
Bei der Eingabe sollte daher immer zuerst das Koordinatensystem ausgewählt werden.
<br/>
Bei projizierten Koordinaten werden die Rechts- und Hochwerte üblicherweise in Metern angegeben.
<strong>GK-M34</strong>
Rechtswert: -67772,43 
Hochwert: 215837,13
<br/>
Bei geographischen Koordinaten entspricht der Rechtswert der geographischen Länge (Werte westlich des Nullmeridians müssen ein negatives Vorzeichen aufweisen).
Der Hochwert entspricht der geographischen Breite (Werte südlich des Äquators müssen ein negatives Vorzeichen aufweisen).
<br/>
Folgende Schreibweisen sind möglich:
<br/>
Rechtswert: 15,439833 
Hochwert: 47,078167
<br/>
<strong>Grad/Minuten:</strong>
Rechtswert: 15°26,39' 
Hochwert: 47°04,69'
<br/>
<i>vereinfacht mit Leerzeichen:</i>
Rechtswert: 15 26,39 
Hochwert: 47 04,69
<br/>
<strong>Grad/Minuten/Sekunden:</strong>
Rechtswert: 15°26'23,4'' 
Hochwert: 47°04'41,4''
<br/>
<i>vereinfacht mit Leerzeichen:</i>
Rechtswert: 15 26 23,4 
Hochwert: 47 04 41,4
<br/>
Für alle Koordinaten gilt: Beistrich und Punkt werden immer als Komma interpretiert.