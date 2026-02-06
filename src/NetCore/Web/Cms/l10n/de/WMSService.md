#dynamic_presentations: Darstellungsvariaten bereitstellen

Darastellungsvarianten werden nicht mehr parametriert, sondern werden dynamisch aus dem TOC des Dienstes erstellt. Das Level gibt an, bis zu welcher Ebene Untergruppen erstellt werden. Layer unterhalb des maximalen Levels werden zu einer Checkbox-Darstellungsvariante zusammengeasst.

#dynamic_queries: Abfragen bereitstellen

Abfragen werden nicht mehr parametriert, sondern werden zur Laufzeit für alle (Feature) Layer eine Abfrage erstellt (ohne Suchbegriffe, nur Identify)

#dynamic_dehavior: Dynamisches Verhalten

Gibt an, wie mit Layern umgegangen wird, die nicht beim erstellen oder nach einem Refresh im CMS unter Themen gelistet werden. AutoAppendNewLayers ... neue Themen werden beim Initialisieren des Dienstes (nach einem cache/clear) der Karte hinzugefügt und können über den TOC geschalten werden. UseStrict ... nur jene Themen, die unter Themen aufgelistet sind, kommen auch in der Karte vor. SealedLayers_UseServiceDefaults ... Es werden immer alle Layer übergeben. Diese Options macht nur beim Fallback(druck)services für VTC Dienste Sinn!

#service_type: Service-Typ

Watermark Services werden immer ganz oben gezeichnet und können vom Anwender nicht transparent geschalten oder ausgelendet werden. Watermark Services können neben Wasserzeichen auch Polygondecker enthalten.

#layer_order: Layer Reihenfolge

Dieser Wert gibt an, wie die Zeichenreihenfolge der Layer in den Capabilities interpretiert werden sollte. Von oben nach unten oder umgekehrt...

#vendor: Server Vendor (Anbieter)

Durch die Angabe des WMS Server Vendors können für den jeweiligen Server spezifische Parameter übergeben werden. Zum Beispiel der kann der DPI Wert der Karte übergeben werden, damit Maßstabsgrenzen von Layern richtig angewendet werden.

#server: Karten Server

#category_server: Service

#version: WMS Version

#category_version: Service

#image_format: WMS GetMap Format

#category_image_format: Service

#get_feature_info_format: WMS GetFeatureInfo Format

#category_get_feature_info_format: Service

#get_feature_info_feature_count: WMS GetFeatureInfo Feature Count

#category_get_feature_info_feature_count: Service

#s_l_d_version: (optional) SLD_Version

In der Regel ist dieser Parameter optional. Nur setzen, wenn der WMS diesen Parameter unbedingt braucht. Wird für GetMap- und GetLegendGraphics-Requests übergeben.

#category_s_l_d_version: Service

#category_ticket_server: Anmeldung

#ticket_server: webGIS Instanz für Ticket Service (Optional)

#username: Username

#category_username: Anmeldungs-Credentials

#password: Password

#category_password: Anmeldungs-Credentials

#token: Token

#category_token: Anmeldungs-Token

#category_client_certificate: Anmeldung

#client_certificate: Optional: Client Certificate

