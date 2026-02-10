#unit: Längeneinheit

#category_unit: Darstellung

#expression: Ausdruck

Hier kann ein Ausdruck angegeben werden, der die Stationierung berechnet. 
Der Ausdruck muss einen Parameter enthalten, der die Länge in Metern angibt. 
Beispiel: {0} m, {json-property1} {json-property2} ... wenn eine API abgefragt wird, können hier die JSON Parameter des Responses als Platzhalter eingesetzt werden. 
Zeilenumbrüche können mit \n erzwungen werden.

#category_expression: Darstellung

#point_line_relation: Punkt-Linien Beziehung (SQL)

#category_point_line_relation: Verknüfung mit Punkt-Linienthema

#point_stat_field: Stationierungsfeld des Punktthemas

#category_point_stat_field: Verknüfung mit Punkt-Linienthema

#service_url: Service URL

Hier kann eine URL zu einem Service angegeben werden, der die Stationierung berechnet. 
Wird hier kein Wert angegeben, wird die Stationierung aus den Punkt- und Linienthemen berechnet. 
Folgende Patzhalter sind möglich: {x}, {y} ... x,y in WGS84, {x:espgCode}, {y:epsgCode} ... x,y konvertiert nach EPSG-Code, {mapscale} ... current map scale

#category_service_url: Oder Abfrage-Service-API

#category_calc_sref_id: Berechnung

#calc_sref_id: Koordinatensystem, in dem gerechnet werden soll (EPSG-Code)

Um die Genauigkeit der Ergebnisse zu gewährleisten, sollte in einer Abbildungsebene mit möglichst geringer Längenverzerrung gerechnet werden. 
Wird hier kein Wert oder 0 angeführt, wird in der Kartenprojektion rechnert. 
Das kann bei WebMercator oder geographischen Projektionen zu Verzerrungen führen. 
Hier ist beispielsweise ein Projeziertes Koordinatensystem wie Gauß-Krüger ideal.