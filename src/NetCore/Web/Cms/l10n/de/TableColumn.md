#column_type: Spalten Typ

Typ der Tabellen-Spalte.

#category_column_type: Allgemein

#data: Definition/Quelle

#category_data: Allgemein

#visible: Sichtbar: wird in der Tabelle angezeigt

Felder die nicht sichtbar sind, werden in der Tabelle nicht angezeigt. Achtung: 
Nicht sichtbare Felder werden trotzdem zum Client übermittelt, um beispielsweise 
für die Markerdefinitionen in der custom.js verwendet zu werden. Ein nicht sichtbar 
schalten ist keine Security Maßnahme, Attribute werden trotzdem an den Client übermittelt, 
nur die Anzeige an der Oberfläche wird unterdrückt.

#category_visible: Allgemein

#category_show_column_name_with_html: Suchergebnis Darstellung (WebGIS 4)

#show_column_name_with_html: Spaltenname in der Html ansicht anzeigen

#category_is_html_header: Suchergebnis Darstellung (WebGIS 4)

#is_html_header: In der Html Kopfzeile anzeigen

#category_show_in_html: Suchergebnis Darstellung (WebGIS 4)

#show_in_html: In der Html ansicht verwenden

#category_sort: Suchergebnis Darstellung (WebGIS 4)

#sort: In der Tabellenansicht sortieren

#field_name: Feld Name

Einfache Übersetzung von Werten. Eingabebeispiel: 0,1,2=ja,nein,vielleicht. Alternativ kann
eine Url zu einem JSON Array mit name,value Werten angegeben werden, beispielsweise eine DataLinq PlainText Query.

#simple_domains: Simple Domains

#raw_html: Raw Html

Der Wert des Feldes werden 1:1 übernommen. Damit können auch HTML Fragmente direkt in die 
Tabelle übernommen werden (standardmäßig werden zB spitze Klammern kodiert, und als solche
auch in der Tabelle dargestellt). Dieser Flag sollte nur falls unbedingt notwendig verwendet 
werden. Handelt es sich bei dem Feld um Ihnalte aus Usereingaben (Editing usw), sollte dieser
Flag unbedingt vermieden werden, da damit eine Cross-Site-Scripting Schwachstelle entsteht!

#sorting_algorithm: Sortier-Algorithmus

Gibt an mit welchen Algorithmus die Spalte in der Tabelle sortiert werden sollte. Standardmäßig wird
die Spalte beim Storieren als Zeichenkette (string) interpretiert. Fix implementierte Algorithmen für
Datum sind: date_dd_mm_yyyy. Über die custom.js können noch weitere Algorithmen definiert werden.

#category_sorting_algorithm: Sortieren

#auto_sort: Automatisch sortieren

Gibt an, ob nach diesem Feld automatisch nach einer Abfrage sortiert werden sollte.

#category_auto_sort: Sortieren

#format_string: Format String (optional)

Für den DisplayType 'normal' kann hier optional der Formatierungsstring angeführt werden. Beispiele:
MM/dd/yyyy, dddd, dd MMMM yyyy HH:mm:ss, MMMM dd. Eine genauere Beschreibung gibt es hier: 
https://docs.microsoft.com/en-us/dotnet/api/system.datetime.tostring?view=net-6.0 oder 
https://www.c-sharpcorner.com/blogs/date-and-time-format-in-c-sharp-programming1

#hotlink_url: Hotlink Url

#hotlink_name: Name/Bezeichnug des Hotlinks

Die Url für den Hotlink mit Platzhaltern, zB http://www.server.com/page?id=[ID_FIELDNAME]&name=[NAME_FIELDNAME]. 
Mit dem prefix 'url-encode:' kann eine kodierung Url Kodierung des Feldes erzwungen werden, falls die 
automatische Kodierung durch den Browser nicht ausreicht, zB [url-encode:FIELDNAME].

#one2_n: 1 : N

#one2_n_seperator: 1 : N Trennzeichen

#browser_window_props: Browser Fenster Attribute

#target: Ziel bei neuem Browserfenster

_blank ... neues Browserfenster\n_self ... Viewerfenster (aktuelles Fenster)\nopener ... Fenster 
von dem webGIS aufgerufen wurde

#image_expression: Bildquelle-Ausdruck

#i_width: Bildquelle-Breite (Pixel)

#i_height: Bildquelle-Höhe (Pixel)

#expression: Ausdruck

#column_data_type: Datentyp des Ergebnisses

Wenn das Ergebnis immer eine Zahl ist, kann hier Number als Typ verwendet werden. Damit kann die 
Splate auch wie ein Zahlenfeld sortiert werden. Achtung: Es muss wirklich jedes Ergebniss 
eine Zahl sein (keine Leerwerte).

