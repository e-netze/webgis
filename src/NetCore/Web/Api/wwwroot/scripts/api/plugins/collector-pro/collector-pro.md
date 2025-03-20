# Collector Pro

Collector-Pro ist ein Plugin zur Eingabe von Daten sowie deren Verortung.
Die nötigen Eingabefelden / Attribute sind in einem XML-Dokument spezifiziert. 
Sie werden vom Plugin ausgelesen und dargestellt.

##### Oberfläche
  
Die Oberfläche besteht aus drei Bereichen:
* Attributieren
  * Eingabe der Daten
* Verorten
  * Kartendarstellung mit Tool zum Einzeichnen
* Speichern
  * Übersicht der eingegebenen Daten sowie Möglichkeit zum Abschicken des Formulars bzw. Speichern

##### Parametrierung der Eingabefelder

In den XML-Dokumenten kann jedem Eingabefeld ein Typ (Text, Dropdown, Checkbox, Radio, File und hidden), 
eine Kategorie (nur für Darstellung) und bei Bedarf die Eigenschaft "required" (verpflichtend) zugewiesen werden.
Verpflichtende Felder müssen ausgefüllt werden und werden vor dem Absenden geprüft.


### Einbindung

Um Collector-Pro einzubinden, sind folgende Ressouren nötig:

##### Stylesheets

````html
<link rel="stylesheet" href=".../Content/styles/default.css" />
<link rel="stylesheet" href=".../Content/api/ui.css" />
<link rel="stylesheet" href=".../Scripts/flatpickr/flatpickr.css" />

<link rel="stylesheet" href=".../_templates/WebGIS-Collector-pro/collector_pro.css" />
<link rel="stylesheet" href=".../_templates/WebGIS-Collector-pro/select2.custom.css" />
````


##### JavaScript - Bibliotheken

````html
<script type="text/javascript" src=".../scripts/jquery-3.4.1.min.js"></script>
											
<script type="text/javascript" src=".../Scripts/Api/api.min.js" id="webgis-api-script"></script>
<script type="text/javascript" src=".../Scripts/Api/api-ui.min.js"></script>
<script type="text/javascript" src=".../Scripts/flatpickr/flatpickr.js"></script>
<script type="text/javascript" src=".../Scripts/flatpickr/de.js"></script>
<script type="text/javascript" src=".../Scripts/Api/ui/webgis_control_upload.js"></script>
<script type="text/javascript" src=".../Scripts/select2/js/select2.min.js"></script>
    									 
<script type="text/javascript" src=".../Scripts/Api/plugins/collector-pro.js"></script>
````
  
##### HTML
Im HTML-Body ist ein `DIV`-Element mit beliebiger ID zur erstellen, das später den Collector beinhaltet:

````html
<div id="collector-container">

</div>
````
In JavaScript kann bei Bedarf ein eigenes GDI-Schema für die Anwendung definiert werden.  
````js
webgis.gdiScheme = 'gdi_erfassung';
````

Nach dem Laden der Seite (`webgis.init`) wird das Plugin mit dem o.a. `DIV` - Element über die ID verknüpft:

````js
webgis.init(function () {

    $('#collector-container').webgis_collector_pro({
        map_options:{
            services:'{{map-services}}',
            extent:'{{map-extent}}'
        },

        map_search_service: '{{map-search-service}}',
        tabs: ["Erfassen", "Verorten", "Speichern"],
        edit_service: '{{edit-service}}',
        edit_themeid: '{{edit-themes}}',
        quick_tools: 'webgis.tools.navigation.zoomToSketch,webgis.tools.navigation.currentPos',
        allow_multipart: '{{allow-multipart}}',
        on_init: function () {
            // Element zu Beginn unsichtbar
            $('#collector-container').webgis_collector_pro('hideElement', 'ERF_KAT');
        }
        }).data('eventHandlers')....
        ....
});
````

### Parametrierung im App-Builder

Folgende Einstellungen können im App-Builder konfiguriert werden, um die Werte in "{{...}}" zu ersetzen:

* Karten Dienste
  * bspw. `ortsplan@ccgis_default,estag_basis_ags@ccgis_default`
* Karten Ausdehnung
  * bspw. `stmk_m34@ccgis_default`
* Karten Suchdienst
  * bspw. `elastic_allgemein@ccgis_default`
* Edit-Service
  * laut CMS
  * bspw. `st_stoer_test@ccgis_default`
* Edit-Thema
  * Konfigurationsname im XML-Dokument
  * bspw. `test_stoerung_strom_sdet`
* Multipart-Features
  * Sind Multipart-Geometrien erlaubt?
  * `true` oder `false` 


### Interaktion

##### Zugriff auf Elemente

Auf die Formularfelder kann mithilfe ihres Namens (aus XML-Datei) zugegriffen werden.  
Folgende Methoden sind für alle Formularfelder verfügbar:
* ***getValue*** Wert auslesen
````js 
	$('#collector-container').webgis_collector_pro('getValue', 'FELDNAME');
````
* ***setValue*** Wert setzen
````js 
	$('#...').webgis_collector_pro('setValue', {fieldname: 'FELDNAME', value: 'NEUER WERT'})
````
* ***hideElement*** Feld ausblenden
````js 
	$('#...').webgis_collector_pro('hideElement', 'FELDNAME');
````
* ***showElement*** Feld einblenden
````js 
	$('#...').webgis_collector_pro('showElement', 'FELDNAME');
````
* ***refreshSketch*** Sketch aktualisieren
````js 
	$('#...').webgis_collector_pro('refreshSketch');
````
* ***getMap*** Kartenobjekt auslesen
````js 
	$('#...').webgis_collector_pro('getMap');
````

### Ereignisse

##### Initialisierung

In den Optionen sind u.a. das Editierservice und -thema (aus CMS bzw. XML) sowie Kartendienste, Kartenauschnitt etc. zu definieren.
Über `on_init` können Befehle nach dem erfolgreichen Laden des Plugins definiert werden.

##### Änderung von Feldern (`change`)

Bei jeder Änderung von Feldwerten (nach Texteingabe, bei Änderung von Dropdown-Auswahl) wird das Ereignis `change` gefeuert.
Auf dieses kann über das Data-Attribut `eventHandlers` zugegriffen werden.

````js
webgis.init(function () {

    $('#collector-container').webgis_collector_pro({
	    // ... OPTIONEN ...
        }
    }).data('eventHandlers').events

    .on('change', function (channel, args) {
		
        if (args.field == "FELDNAME1" && args.value == "123") {
	        $('#...').webgis_collector_pro('hideElement', 'FELDNAME1');			
        }

    });
});
````

Das `args`-Objekt beim beinhaltet zwei Eigenschaften
* `field`: den Feldnamen (laut XML-Definition)
* `value`: der neuen (veränderten) Wert

Im o.g. Beispiel wird bei einer Änderung von FELDNAME1 (und wenn dieses den neuen Wert 123 erhält), das Feld ausgeblendet.


##### Abschicken des Formulars (`save`)

Beim Abschicken des Formulars wird das Ereignis `save` gefeuert.
Auf dieses kann über das Data-Attribut `eventHandlers` zugegriffen werden.

````js
webgis.init(function () {

    $('#collector-container').webgis_collector_pro({
            // ... OPTIONEN ...
        }
    }).data('eventHandlers').events

    .on('change', function (channel, args) {
		
        // ... Code bei Änderungen (siehe oben)

    })
    .on('save', function (channel, args) {
        if (args.success == true) {
            alert("Success");
        } else {
            alert(args.result);
        }
    });
});
````

Das `args`-Objekt beim beinhaltet zwei Eigenschaften
* `success`: true oder false
* `result`: 
  * bei Erfolg: übergebene Werte
  * sonst: Fehlermeldung aus Exception

Im o.g. Beispiel wird nach dem erfolgreichen Abschicken der Text "Success" ausgegben und bei Fehler die Fehlerbeschreibung.


### Diverses

#### Benutzerdefiniertes Marker-Symbol

Bei der Initialisierung kann ein eigenes Marker-Symbol definiert werden, das bei Klick in die Karte gesetzt wird:

````js
webgis.init(function () {

    webgis.markerIcons["sketch_vertex"].url = function () {
        return webgis.css.imgResource('marker_red_flag.png', 'markers')
    };

    $('#collector-container').webgis_collector_pro({
        // ...
````