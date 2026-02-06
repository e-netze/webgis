#opacity: Initiale Deckkraft

Gibt an, ob der Dienst nach den Starten der Karte transparent dargestellt werden soll. Der Wert muss zwischen 0 (100% transparent) und 100 (nicht transparent) liegen.

#category_opacity: Allgemein

#opacity_factor: Transparenz Faktor

Ein Factor, mit den die vom Anwender eingestellte Transparenz immer multipliziert wird. Sollte der Dienst beispielsweise immer hab durchlässig dargestellt werden, kann hier ein Wert von 0.5 eingestellt werden. Stellt der Anwender den Dienst auf 100% Deckkraft, bleibt der Dienst immer noch zu 50% durchlässig. Ein Wert von 1 bedeutet, dass der dienst bei 100% Deckkraft keine Transparenz aufweißt. 0 kann hier nicht eingegeben werden, da der Dienst dann überhaupt nicht angezeigt werden könnte!

#category_opacity_factor: Allgemein

#timeout: Zeitverhalten: Timeout

Liefert der Dienst nach 'x' Sekunden kein Ergebnis, wird der Request abgebrochen...

#category_timeout: Allgemein

#image_format: Image Format

Gibt an, in welchen Format das Kartenbild von Dienst abgeholt wird. Diese Eigenschaft wird nur für ArcIMS- und AGS Dienste berücksichtigt!

#category_image_format: Allgemein

#meta_data: Link auf Metadaten

#category_meta_data: Allgemein

#visible: Standardmäßig sichtbar

Gibt an, ob der Dienst beim Kartenaufruf sichtbar ist...

#category_visible: TOC

#category_toc_display_name: TOC

#category_toc_name: TOC

#collapsed: Erweitert

#category_collapsed: TOC

#show_in_toc: Im Toc anzeigen

ESRI Datumstransformationen (Array), die bei Projection-On-The-Fly für diese Karte verwendet werden sollen. Nur für REST Services ab AGS 10.5. Es muss immer ein Array angegeben werden, zB [1618, ...] oder [1618]. Beim Abfragen (Query) kann nur eine Transformation übergeben werden; hier wird immer die erste hier angeführte Transformation verwendet.

#category_show_in_toc: TOC

#category_projection_methode: Kartenprojektion

#category_projection_id: Kartenprojektion

#show_in_legend: Service nimmt an der Legende teil

Gibt an, ob der Dienst in der Legendendarstellung erscheint

#category_show_in_legend: Legende

#legend_opt_method: Optimierungsgrad

Gibt an, wie die Lengende optimiert wird.

#category_legend_opt_method: Legende

#legend_opt_symbol_scale: Symboloptimierung ab einem Maßstab von 1:

Gibt an, ab welchem Maßstab die Symbole in der Legende optimiert werden (nur wenn Optimierungsgrad=Symbols).

#category_legend_opt_symbol_scale: Legende

#legend_url: Url für fixe Legende

Bei Angabe einer fixen Legende, wird immer nur diese angezeigt. Alle anderen Legendeneigenschaften werden ignoriert

#category_legend_url: Legende

#use_fix_ref_scale: Fixen Referenzmaßstab verwenden

Gilt nur für ArcIMS (AXL) Dienste. Ist diese Eigenschaft auf 'true' gesetzt, wird auf diesen Kartendienst immer ein fester Referenzmaßstab angewendet. Dieser kann auch von Kartenbenutzer nicht überschreiben werden!

#category_use_fix_ref_scale: Referenzmaßstab

#fix_ref_scale: Fixer Referenzmaßstab 1:

Gilt nur für ArcIMS (AXL) Dienste. Gibt einen fixen Referenzmaßstab an, der auf diensen Dienst angewendet wird. Dieser kann auch von Kartenbenutzer nicht überschreiben werden!

#category_fix_ref_scale: Referenzmaßstab

#min_scale: MinScale: Dienst ist sichtbar bis 1:

Bei Eingabe von 0 (default) wird dieser Wert ignoriert!

#category_min_scale: Maßstabsgrenzen

#max_scale: MaxScale: Dienst ist sichtbar ab 1:

Bei Eingabe von 0 (default) wird dieser Wert ignoriert!

#category_max_scale: Maßstabsgrenzen

#use_with_spatial_constraint_service: Räumliche Einschränkung verwenden

Dienst nur abfragen, wenn räumliche Einschränkung für diesen Dienst zutrifft

#category_use_with_spatial_constraint_service: Räumliche Einschränkung

#is_basemap: (Hinter)Grundkarte

Gibt an, ob dieser Dienst eine Hintergrundkarte ist

#category_is_basemap: Basemap

#basemap_type: (Hinter)Grundkarten-Typ

Die Url zu einem Bild, dass für die Kachel im Viewer als Vorschaubild angezeigt wird. Notwendig zB. für WMS Dienste

#category_basemap_type: Basemap

#category_basemap_preview_image_url: Basemap

#basemap_preview_image_url: Preview Image Url (Optional)

#export_w_m_s: Dienst als WMS exportierbar

#category_export_w_m_s: OGC Export

#map_extent_url: Name der Kartenausdehnung

#category_map_extent_url: OGC Export

#warning_level: Warning Level

Gibt an, ab wann Fehler in der Karte angezeigt werden

#category_warning_level: Diagnostics

#copyright_info: Copyright Info

Gibt die Copyright Info an, die für diesen Dienst hinterlegt ist. Die Info muss unnter Sonstiges/Copyright definiert sein.

#category_copyright_info: Allgemein

#category_display_name: Allgemein

#query_layer_id: ID des Abfrage-Layers

#category_query_layer_id: Allgemein

#service_url_field_name: Layer-Feld mit Service Url

#category_service_url_field_name: Allgemein

#layer_visibility: Layer Sichtbarkeit

Gibt an, ob Layer per default sichtbar sind.

#category_layer_visibility: Allgemein

