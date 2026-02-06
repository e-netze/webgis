#min_zoom_to_scale: Minimaler Maßstab beim Zoom To 1:

#category_min_zoom_to_scale: Allgemein

#allow_empty_search: (Leere) Suche erlauben

Gibt an, ob der Anwender etwas eingeben muss um zu suchen

#category_allow_empty_search: Allgemein

#preview_text_template: Ergebnisvorschau Vorlage (Template)

Werden mehrere Objekte bei einer Abfrage gefunden, wird zuerst eine verfachte Liste der Objekte angezeigt. Dazu wird für jedes Objekte ein kurzer Vorschau-Text erstellt. Dieser Text setzt sich in der Regel aus den Attributwerten der möglichen Suchbegriffe zusammen. Ist dies für diese Abfrage nicht erwünscht oder sollten andrere Attribute verwendet werden, kann hier eine Vorlage defineirt werden. Die Vorlage kann ein beliegibter Text mit Platzhaltern für die Attribute in eckigen Klammern sein, zB Hausnummer [HRN] in [STRASSE]. Hinweis: Es werden nur Attribute im Template übersetzt, die auch in der Ergebnisstabelle vorkommen. Für einen Zeilenumbruch in der Vorschau kann \\n geschreiben werden.

#category_preview_text_template: Ergebnisvorschau

#category_draggable: Allgemein

#draggable: Draggable (Ziehbar)

WebGIS 5: Das Ergebnis kann aus der Liste in eine andere Anwendung (zB Datalinq) gezogen werden.

#category_show_attachments: Allgemein

#show_attachments: Show Attachments

#category_distict: Erweiterte Eigenschaften

#distict: Distinct

Gibt es Objekte mit idententer Geometie (zB gleicher Punkt) und sind ebenso die in der Abfrage abgeholten Attributewerte ident, wird ein Objekt in der Erebnisliste nur einmal angeführt.

#category_union: Erweiterte Eigenschaften

#union: Union

Ergebnismarker, die in der Karte am gleiche Ort liegen (identer Punkt) werden zu einem Objekt zusammengefasst. Der Marker enthält in der Tabellenansicht alle betroffenen 'Records'

#category_apply_zoom_limits: Erweiterte Eigenschaften

#apply_zoom_limits: Layer Zoomgrenzen anwenden

Eine Abfrage (Identify, Dynamischer Inhalt im aktuellen Auscchnit) wird nur durchgeführt, wenn sich die Karte inhalb der Zoomgrenzen des zugrunde liegenden Abfragethemas befinden.

#category_max_features: Erweiterte Eigenschaften

#max_features: Maximale Anzahl

Maximale Anzahl an Features, die bei eine Abfrage abgeholt werden sollten. Ein Wert <= 0 gibt an, dass die maximale Anzahl von Features abgeholt wird, die vom FeatureServer bei einem Request zurück gegeben werden können.

#category_network_tracer: Sonder

#network_tracer: Netzwerk Tracer

#category_gdi_props: Erweiterte Eigenschaften (WebGIS 4)

#gdi_props: (Gdi) Properties

#min_scale: Minimaler Maßstab 1:

#category_min_scale: Karten Tipps (WebGIS 4)

#max_scale: Maximaler Maßstab 1:

#category_max_scale: Karten Tipps (WebGIS 4)

#map_info_symbol: Symbol

#category_map_info_symbol: Karten Tipps (WebGIS 4)

#map_info_visible: beim Start sichtbar

#category_map_info_visible: Karten Tipps (WebGIS 4)

#is_map_info: Als Karten Tipp darstellen

#category_is_map_info: Karten Tipps (WebGIS 4)

#set_visible_with_theme: Mit dem Thema über TOC mitschalten

#category_set_visible_with_theme: Karten Tipps (WebGIS 4)

#feature_table_type: Suchergebnis Darstellung

#geo_juhu: Abfrage nimmt an GeoJuhu teil

#category_geo_juhu: GeoJuhu

#geo_juhu_schema: GeoJuhu Schema

Hier können mehrere Schematas mit Beistrich getrennt eingeben werden. Der Wert wird nur berücksichtigt, wenn ein GeoJuhu Schema in der Aufruf-Url übergeben wird. * (Stern) kann angeben werden, wenn eine Thema in jedem Schema abgefragt werden soll.

#category_geo_juhu_schema: GeoJuhu

#filter_url: Filter

Eine Abfrage kann mit einem Filter verbunden werden. Bei den Abfrageergebnissen erscheint dann ein Filter-Symbol mit dem man genau dieses Feature filtern kann.

#category_filter_url: Filter

#query_group_name: Gruppenname

