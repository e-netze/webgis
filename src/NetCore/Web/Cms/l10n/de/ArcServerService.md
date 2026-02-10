#dynamic_presentations: Darstellungsvariaten bereitstellen

Darastellungsvarianten werden nicht mehr parametriert, sondern werden dynamisch aus dem TOC des Dienstes erstellt.
Das Level gibt an, bis zu welcher Ebene Untergruppen erstellt werden. 
Layer unterhalb des maximalen Levels werden zu einer Checkbox-Darstellungsvariante zusammengeasst.

#dynamic_queries: Abfragen bereitstellen

Abfragen werden nicht mehr parametriert, sondern werden zur Laufzeit für alle (Feature) Layer eine Abfrage erstellt (ohne Suchbegriffe, nur Identify)

#dynamic_dehavior: Dynamisches Verhalten

Gibt an, wie mit Layern umgegangen wird, die nicht beim erstellen oder nach einem Refresh im CMS unter Themen gelistet werden. 
AutoAppendNewLayers ... neue Themen werden beim Initialisieren des Dienstes (nach einem cache/clear) der Karte hinzugefügt und können über den TOC geschalten werden. 
UseStrict ... nur jene Themen, die unter Themen aufgelistet sind, kommen auch in der Karte vor. 
SealedLayers_UseServiceDefaults ... des wird keine Layerschaltung an den Dienst übergeben. 
Das bewirkt, dass immer die Defaultschaltung aus dem Layer angezeigt wird. Diese Options macht nur beim Fallback(druck)services für VTC Dienste Sinn!

#service_type: Service-Typ

Watermark Services werden immer ganz oben gezeichnet und können vom Anwender nicht transparent geschalten oder ausgelendet werden. 
Watermark Services können neben Wasserzeichen auch Polygondecker enthalten.

#allow_query_builder: Allow QueryBuilder (Darstellungsfilter aus TOC

Der Anwender kann aus dem TOC Filter als mit deinen SQL Edititor setzen.

#server: Karten Server

#category_server: Service (Dienst)

#service: Karten Dienst

#category_service: Service (Dienst)

#service_url: Karten Dienst Url

#category_service_url: Service (Dienst)

#export_map_format: Export Map Format

Bei 'Json' wird das Ergebnis ins Outputverzeichnis von ArcGIS Server gelegt und dort vom Client abgeholt. 
Hat der Client keinen Zugriff auf dieses Output Verzeichnis, kann als Option 'Image' gewählt werden. 
Es wird dann vom ArcGIS Server keine Bild abgelegt sondern direkt übergeben.

#category_export_map_format: Service (Dienst)

#username: Username

#category_username: Anmeldeinformationen (optional)

#password: Password

#category_password: Anmeldeinformationen (optional)

#token: Token

#category_token: oder Anmeldetoken (optional)

#category_ticket_expiration: Anmeldeinformationen (optional)

#ticket_expiration: Ticket-Gültigkeit [min]

#category_client_i_d: Rest

#client_i_d: Ticket-ClientId

#get_selection_method: GetSelection Methode

#category_get_selection_method: Auswahl/Selektion (veraltet)