# merge: Zusammenführen

# merge-object: Objekte zusammenführen

# merged-bject: Zusammengeführtes Objekt

# polyline-merge-method: Merge Methode (Polylinien)

# merge-has-no-result: Beim Zusammenführen, wurde keine Lösung gefunden, in der alle Ausgangsobjekte enthalten sind.

# merge-origin-feature: Attribute aus diesem Objekt übernehmen

Durch das Zusammenführen entsteht ein neues Objekte. Die Sachdaten für das neue Objekt werden aus dem hier 
angeführten Objekt übernommen (entsprechende Objekt-ID wählen). Das gewählte Objekt wird in der Karte 
farblich hervorgehoben. Die Sachendaten für das Ausgewählte Element, werden die der (readonly) Editmaske unten angezeigt.

# create-multipart: Multipart Objekt erzeugen

Die Ursprünglichen Objekte werden 1:1 übernommen und ein Multipart Objekt erzeugt.
Jeder Teil des neuen Objekts entspricht einen Teil der Ausgangsobjekte.

# create-singlepart: Singlepart Objekt erzeugen

Es wird versucht, aus den Objekten ein Single Part Feature zu erzeugen. Daruch werden die Ausgangsobjekte
zuschnitten und neue zusammengesetzt. Durch den Schnitt und das neue Zusammensetzen, gehen eventuelle einige 
Teile aus den Ausgangsobjekten verloren (Artifakte). 
Es gibt in der Regel mehrere Lösungen, die im nächsten Schritt angeboten werden. 
Die Methode ist nur erfolgreich, wen ein neuens Objekte zusammengesetzt wird, in dem mindesten ein Teil aus jedem
Ausgangsobjekt enthalten ist.