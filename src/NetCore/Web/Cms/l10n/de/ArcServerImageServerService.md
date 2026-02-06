#dynamic_presentations: Darstellungsvariaten bereitstellen

Darastellungsvarianten werden nicht mehr parametriert, sondern werden dynamisch aus dem TOC des Dienstes erstellt. Das Level gibt an, bis zu welcher Ebene Untergruppen erstellt werden. Layer unterhalb des maximalen Levels werden zu einer Checkbox-Darstellungsvariante zusammengeasst.

#dynamic_queries: Abfragen bereitstellen

Abfragen werden nicht mehr parametriert, sondern werden zur Laufzeit für alle (Feature) Layer eine Abfrage erstellt (ohne Suchbegriffe, nur Identify)

#service_type: Service-Typ

Watermark Services werden immer ganz oben gezeichnet und können vom Anwender nicht transparent geschalten oder ausgelendet werden. Watermark Services können neben Wasserzeichen auch Polygondecker enthalten.

#server: Karten Server

#category_server: Service

#service: Karten Dienst

#category_service: Service

#service_url: Karten Dienst Url

#category_service_url: Service

#username: Username

#category_username: Anmeldungs-Credentials

#password: Password

#category_password: Anmeldungs-Credentials

#token: Token

#category_token: Anmeldungs-Token

#category_image_format: Image Server Properties

#category_pixel_type: Image Server Properties

#category_no_data: Image Server Properties

#category_no_data_interpretation: Image Server Properties

#category_interpolation: Image Server Properties

#category_compression_quality: Image Server Properties

#category_band_i_ds: Image Server Properties

#category_mosaic_rule: Image Server Properties

#category_rendering_rule: Image Server Properties

#rendering_rule: RenderingRule (ExportImage/Legend)

Diese RenderingRule wird für die Darstellung des Dienstes und der Legende verwendet

#category_rendering_rule_identify: Image Server Properties

#rendering_rule_identify: RenderingRule (Identify)

Diese RenderingRule wird beim Identify verwendet. Gibt es für diese RenderingRule auch einen RasterAttributeTable, wird dieser zum bestimmen des angezeigten Wertes verwendet.

#category_pixel_aliasname: Image Server Identify

#pixel_aliasname: Pixel Aliasname

Wird ein Identify auf den Dienst ausgeführt wird beim Ergebnis statt 'Pixel' dieser Wert in der Ergbnisstabelle angezeigt. Dies sollte ein Name sein, der das Ergebnis genauer beschreibt, zB Höhe [m]

