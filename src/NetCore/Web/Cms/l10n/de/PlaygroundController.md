#playground_title: CMS Playground

#playground_description: Teste CMS-Ausdrücke mit GeoJSON, ohne das CMS zu veröffentlichen.

#expressions_tile: Expressions

#editform_tile: Editformular & AutoValues

#editform_title: Editformular- und AutoValue-Tester

#editform_description: Wähle eine konfigurierte Editiermaske, gib Beispieldaten und einen simulierten Bearbeitungskontext ein und prüfe die AutoValue-Ergebnisse.

#editform_simulation_warning: Die Simulation läuft vollständig im CMS und ruft weder die WebGIS API noch Quelldienste auf. Expressions, GeoJSON-Geometrie und einfache Kontextwerte werden lokal ausgewertet. Geometriefunktionen verwenden das CRS im GeoJSON (ohne CRS EPSG:4326), nicht automatisch das Karten- oder Editierthema-CRS. Einfache Datenbank-AutoValues (db_select, db_select_on_insert; SELECT oder DataLinq-Endpunkt) werden direkt gegen die konfigurierte Datenbank bzw. den DataLinq-Endpunkt ausgeführt. Räumliche Service-AutoValues werden nicht ausgeführt; sie erscheinen als nicht simuliert.

#editform_cms: CMS-Konfiguration

#editform_deployment: Deployment (Secrets)

#editform_deployment_default: Standard-Secrets (kein Deployment)

#editform_deployment_help: Secret-Platzhalter ({{secret-...}}) und Ersetzungsdatei werden wie beim Deploy für die Umgebung dieses Deployments aufgelöst.

#editform_select_cms: CMS auswählen

#editform_theme: Editiermaske

#editform_select_theme: Editiermaske auswählen

#editform_fields: Formularfelder

#editform_fields_help: Wähle bis zu 20 Felder aus. Pflicht-, Sichtbarkeits- und Readonly-Merkmale stammen aus der CMS-Konfiguration; Bedingungen und Layout werden nicht als vollständige WebGIS-Maske gerendert.

#editform_operation: Vorgang

#editform_insert: Neu anlegen (Insert)

#editform_update: Ändern (Update)

#editform_username: Simulierter Benutzername

#editform_database_username: Simulierter Datenbankbenutzer

#editform_map_scale: Kartenmaßstab (Nenner)

#editform_map_sref: Karten-SRefId

#editform_context_values: Kontextwerte als JSON

#editform_context_values_help: Werte für role-parameter:name, url-parameter:name und bei Bedarf edit_service_id oder edit_theme_id als JSON-Objekt, z. B. {"department":"West"}.

#editform_geojson_help: Ein Feature oder eine FeatureCollection mit den Beispielattributen und der Geometrie. Beim Auswählen eines Editthemas werden die Felder ohne AutoValue als Attribute übernommen; vorhandene Werte und die Geometrie bleiben erhalten. Maximal 5 MB und 1.000 Features.

#editform_apply: AutoValues simulieren

#editform_results: Simulationsergebnisse

#editform_field: Feld

#editform_value: Simulierter Wert

#editform_details: Hinweis / Fehler

#editform_required: Pflicht

#editform_hidden: ausgeblendet

#editform_readonly: schreibgeschützt

#editform_no_autovalue: kein AutoValue

#editform_select_field: Bitte mindestens ein Feld auswählen.

#editform_too_many_fields: Es können höchstens 20 Felder gleichzeitig simuliert werden.

#editform_no_themes: In dieser CMS-Konfiguration wurden keine Editiermasken gefunden.

#editform_no_fields: Für diese Editiermaske wurden keine Formularfelder in der CMS-Konfiguration gefunden.

#editform_evaluation_failed: Die Simulation konnte nicht ausgeführt werden.

#expressions_title: Expressions Playground

#expressions_description: Füge GeoJSON und einen Ausdruck ein und wende ihn direkt auf jedes Feature an.

#back_to_playground: Zurück zum Playground

#expression_type: Ausdruckstyp

#autovalue: AutoValue

#table_column: Tabellenspalte

#expression_type_help: Das führende = wird nur bei AutoValues benötigt und kann bei Tabellenspalten weggelassen werden.

#geojson_label: GeoJSON

#geojson_help: Unterstützt ein einzelnes Feature oder eine FeatureCollection. Bei einer FeatureCollection wird jedes Feature separat ausgewertet. Maximal 5 MB und 1.000 Features.

#expression_label: Ausdruck

#expression_help: Structured Expressions und bestehende Legacy-Templates/$-Ausdrücke werden entsprechend dem gewählten Ausdruckstyp ausgewertet. Geometriefunktionen verwenden das CRS der FeatureCollection (ohne CRS EPSG:4326); shape_*-Funktionen können in eine verfügbare EPSG-Ziel-SRefId transformieren.

#expression_syntax: Erkannte Syntax

#structured_expression: Structured Expression

#legacy_expression: Legacy-Template / Simple Expression

#expression_docs: Dokumentation

#expression_docs_link: Ausdrücke in WebGIS

#expression_docs_url: https://docs.webgiscloud.com/de/webgis/annex/expressions.html

#expression_reference: Felder und Funktionen

#expression_fields: GeoJSON-Felder

#expression_fields_empty: Keine GeoJSON-Felder gefunden.

#expression_fields_invalid: GeoJSON ist ungültig; Feldnamen können nicht erkannt werden.

#structured_functions: Structured-Funktionen: concat, upper, lower, trim, substring, replace, length, is_null, is_empty, null_if_empty, round, abs, min, max, format_date, year, month, day, if, coalesce.

#geometry_functions: Geometrie-Funktionen: shape_len, shape_area, shape_perimeter, shape_centroid_x, shape_centroid_y. Optional kann eine EPSG-Ziel-SRefId angegeben werden.

#format_geojson: GeoJSON formatieren

#apply: Anwenden

#results: Ergebnisse

#feature_number: Feature

#result: Ergebnis
