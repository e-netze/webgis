# GeoSearch

GeoSearch ist ein Plugin zur Schnellsuche von beliebigen indizierten Datenbeständen.  
Die Ergebnisse werden in einer Listenansicht und in einer Karte angezeigt.
In einer Detailansicht sind Links zu weiteren Prozessierungsschritten und Beauskunftung (bspw. Grundbuchsabfrage) möglich.
Außerdem sind Links ins WebGIS zu vordefinierten Abfragethemen möglich.

##### Oberfläche
  
Die Oberfläche besteht aus drei Bereichen:
* Suchfeld
  * Eingabe der Suchanfrage / Text
* Listenansicht
  * Darstellung der Ergebnisse in einer Liste. Für jedes Element können Details angezeigt werden.
* Kartenansicht
  * Kartendarstellung mit Verortung des Ergebnisses

### Einbindung

Um GeoSearch einzubinden, sind folgende Ressouren nötig:

##### Stylesheets

````html
<link rel="stylesheet" href=".../Content/styles/default.css" />
<link rel="stylesheet" href=".../Content/api/ui.css" />

<link rel="stylesheet" href=".../_templates/WebGIS-GeoSearch/geosearch.css" />
````


##### JavaScript - Bibliotheken

````html
<script type="text/javascript" src=".../scripts/jquery-3.4.1.min.js"></script>
											
<script type="text/javascript" src=".../Scripts/Api/api.min.js" id="webgis-api-script"></script>
<script type="text/javascript" src=".../Scripts/Api/api-ui.min.js"></script>
<script type="text/javascript" src=".../Scripts/Api/ui/webgis_control_upload.js"></script>
    									 
<script type="text/javascript" src=".../Scripts/Api/plugins/geosearch.js"></script>
````
  
##### HTML
Im HTML-Body ist ein `DIV`-Element mit beliebiger ID zur erstellen, das später die Suche beinhaltet:

````html
<div id="geosearch-container">

</div>
````
In JavaScript wird nach dem Laden der Seite (`webgis.init`) das Plugin mit dem o.a. `DIV` - Element über die ID verknüpft:

````js
webgis.init(function () {

    $('#geosearch-container').webgis_geosearch({

        map_options:{
            services: '{{map-services}}',
            extent: '{{map-extent}}'
        },

        search_service: '{{search-service}}',
        map_search_service: '',
        search_placeholder: 'Geben Sie hier Ihren Suchtext ein ...',
        link_collection: '{portal}/compatibility/mapsfromquery?schema={{webgis4-compatibility-schema}}&query={query}&service={service}'
    });

});
````

### Parametrierung im App-Builder

Folgende Einstellungen können im App-Builder konfiguriert werden.

* Karten Dienste
  * bspw. `ortsplan@ccgis_default,estag_basis_ags@ccgis_default`
* Karten Ausdehnung
  * bspw. `stmk_m34@ccgis_default`
* Suchdienst
  * bspw. `elastic_allgemein@ccgis_default,elastic_strom@ccgis_default`
* WebGIS 4 Kompatibilitätsschema
  * zum Anzeigen der Links für WebGIS
  * bspw. `webgis4ccgis`