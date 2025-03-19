# clip: Zuschneiden

Zeichnen sie ein Polygon in die Karte, mit dem geschnitten werden soll.

# clip-objects:	Objekte zuschneiden (Clip)

# clip-has-no-result: Beim Zerschneiden (Clippen) wurde ein Ergebnis gefunden

# choose-clip-method: Clip Methode auswählen

# clip-intersected-and-difference: Schnittmenge + Differenzmenge

# clip-intersected:	Schnittmenge

# clip-difference: Differenzmenge

# clip-xor:	Symmetrische Differenzmenge

# draw-clip-polygon-first: Bitte zuerst eine Verschnittfläche zeichnen

# apply-clip-to-intersected: Nur auf geschnittene Objekte anwenden

Sind mehrere Objekte ausgewählt und nicht alle werden von der Schnittfläche
getroffen, bleiben diese durch das Zuschneiden unverändert.

# apply-clip-to-all: Auf alle Objekte anwenden

Sind mehrere Objekte ausgewählt und nicht alle werden von der Schnittfläche
getroffen, werden auch diese für das Zuschneiden berücksichtigt. Objekte außerhalb
der Schnittfläche, kommen damit in die Differenzschnittmenge. Wird im nächsten
Schritt nur die Schnittmenge als Ergebenis ausgewählt, werden alle Objekte Objekte
aus der Differenzmenge (optional) gelöscht.

# disolve-multipart-features: Multiparts auflösen

Wird durch das Zuschneiden ein Objekt in mehrere Teile geteilt, wird aus den 
einzelnen Teilen je ein neues Objekt erstellt. Somit entstehen keine Multipart Objekte.
Durch das Zerschneiden können so mehr Objekte entstehen, als ürsprunglich ausgwählt waren.

# clipped-features-stay-multiparts:	Zugeschnittene Features als Multiparts

Wird durch das Zuschneiden ein Objekt in mehrere Teile geteilt, bleibt es ein Objekt,
das aus mehreren Teilen besteht. In diesen Fall enstehent ein Multipart-Features.
Nach dem Verschnitt gibt es wieder gleich viele Objekte, wie vor dem Verschnitt.