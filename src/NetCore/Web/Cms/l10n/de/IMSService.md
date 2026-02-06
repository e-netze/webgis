#server: Karten Server

#service: Karten Dienst

#dynamic_presentations: Dynamische Darstellungsvariaten

Darastellungsvarianten werden nicht mehr parametriert, sondern werden dynamisch aus dem TOC des Dienstes erstellt. Das Level gibt an, bis zu welcher Ebene Untergruppen erstellt werden. Layer unterhalb des maximalen Levels werden zu einer Checkbox-Darstellungsvariante zusammengeasst.

#dynamic_queries: Dynamische Abfragen

Abfragen werden nicht mehr parametriert, sondern werden zur Laufzeit für alle (Feature) Layer eine Abfrage erstellt (ohne Suchbegriffe, nur Identify)

#dynamic_dehavior: Dynamisches Verhalten

Gibt an, wie mit Layern umgegangen wird, die nicht beim erstellen oder nach einem Refresh im CMS unter Themen gelistet werden. AutoAppendNewLayers ... neue Themen werden beim Initialisieren des Dienstes (nach einem cache/clear) der Karte hinzugefügt und können über den TOC geschalten werden. UseStrict ... nur jene Themen, die unter Themen aufgelistet sind, kommen auch in der Karte vor.

#service_type: Service-Typ

Watermark Services werden immer ganz oben gezeichnet und können vom Anwender nicht transparent geschalten oder ausgelendet werden. Watermark Services können neben Wasserzeichen auch Polygondecker enthalten.

#username: Username

#category_username: Anmeldungs-Credentials

#password: Password

#category_password: Anmeldungs-Credentials

#token: Token

#category_token: Anmeldungs-Token

#category_override_local: Lokalisierung

#override_local: IMS Service LOCALE überschreiben

Hier kann gegeben werden, wie ein Komma für Dienste interpretiert werden soll.\nKein Wert ... Lokalisierung wird aus dem LOCALE Tag von GET_SERVICE_INFO übernommen\nde-AT ... Beistrich als Komma, en-US ... Punkt als Komma

