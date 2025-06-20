﻿# Change Log

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## Unreleased

### Added

### Fixed

## 7.25.2501

### Added

- Identify: Show **Hover Hightlight** in map (on hover table row) only if every features in response
  have less then 1000 Vertices. [discussion #293](https://github.com/e-netze/webgis-community/discussions/293)
  Workaround for User: Click on a table row to show yellow highlight in map. 
  
  The maximum number of vertices can be set in the `api.config` file (``max-vertices-for-hover-highlighting``:
  [docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#werkzeug-identify)

### Fixed

- Print: if output has multiple pages, webgis looses path to output path and stores images temporarily in default folder for AppPool user.
  [discussion #305](https://github.com/e-netze/webgis-community/discussions/305)

- Print: Marker (Queryresults, Coordinates) numbers are alway **1**
- 
## 7.25.2403

### Added

- Editing 
  - AutoValues: Introduced ``guid_v7`` and ``guid_v7_sql`` [Docs](https://docs.webgiscloud.com/de/webgis/apps/cms/editing/fields_autovalues.html)
  - FeatureTransfer: Allow empty values for "mass attribution fields".

- Cache Db: If KeyValue Cache is a database, the webgis_cache table name depends on the current crypto keys
  to avoid errors, when instances with different crypto keys accessing same table.
  
- !!
  !! Important: if you already use DB as KeyValue Cache:
  !! -  API: Login as admin => run setup 
  !!
- 
### Fixed

- Image Georeferencing: Transparency not worked with PNG Images (alway 100% transparent, becaus opacity_factor was 0.0)

## 7.25.2302

### Added

- Tool Properties: custom.js [Docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/usability.html#toolbox)
  [discussion #279](https://github.com/e-netze/webgis-community/discussions/279)

- Chainage: a api service can be used to calculate the chainage of a point on a line
  [discussion #261](https://github.com/e-netze/webgis-community/discussions/261)

- Usability Identify (alpha): Show current visible querythemes in Identify-Combobox on top:
  [discussion #278](https://github.com/e-netze/webgis-community/discussions/278)
  Must be enabled in ``custom.js``: ``webgis.usability.listVisibleQueriesInQueryCombo = true;``   

- Feature: 1:n links from results table will be openend in dialog, if target is ``dialog``. Until now, this only worked for single result links. 

- Feature: Feature Results with (AGS) Attachments. The attachments are shown in a separate
  dialog after click on the icon in the table.
  In CMS, Attachment must be allowed and authorized for the user.
  [discussion #272](https://github.com/e-netze/webgis-community/discussions/272)

- Feature: HttpClient Default-Timeout-Seconds for all requests. This can be set in the api.config file.
  [docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#httpclient)

- Usability Result Table: 
  - Click row in result table don't fire an autoscroll to this row. Autoscroll is only fired, if a result-map-marker is clicked.
  - Enhance result-table scrolling. Tables remembers scroll position on switch between tabs.
  - Enhance result-table loading state
  - Improved selection symbology (smaller transparent selection markers for points, thinner lines with polygons, more transparent lines for line selections)
  - User Preferences: User can set some settings like: Show Markers on new query results, Select new query results

- Metadata: Enhance metadata behavior
  - nicer Button
  - Metadata for hole service as button in TOC. 
  - Presentation type ``Info`` shows list item with i-button and opens metadata in dialog.
  - Shows service metadata link in service copyright & description dialog

## Fixed

- Bug: Can't save edit features if calc-CRS differs from map-CRS
- 
- Metadata:
  - Bug: Metadata button not shown in TOC for services with dynamic TOC sometimes 

## 7.25.2001

### Added

- Improved Block out text printed maps (Labels for markers, Coordinates, etc.) [discussion #277](https://github.com/e-netze/webgis-community/discussions/277)

- Bookmarks: Pan to center point and scale instead of zoom to extent [discussion #276](https://github.com/e-netze/webgis-community/discussions/276)

## 7.25.1904

### Added

- Filebased caching for datalinq view-compilations

## 7.25.1902

### Added

- Deployment: using NUKE to create deployment zip files and docker images

## Fixed

- Bug: AGS FeatureService Editing - Can't transfer line and polygon features because STLength() and STArea() Fields
  => Fields will be ignored and not set on INSERT/UPDATE.

- Bug: Editing Autocomplete not worked with System.Text.Json

- Bug: Massatributation (Can't determine Shape field name)

## 7.25.1503

### Added

- Show more than one Overlay Basemap [discussion #264](https://github.com/e-netze/webgis-community/discussions/264)
  
- Setting an opacity facor in CMS. Make services always transparent, even if the user sets them to 100% opacity.
  [discussion #268](https://github.com/e-netze/webgis-community/discussions/268)

## 7.25.1502

### Added

- Editing: Field Type "Locked": Shown as readonly field in the edit mask, an will be written to the database
           Useful for fields that are not editable in the frontend, which are passed via a URL parameter.
           (Readonly fields are not written to the database, only if they are also "AutoValues")

### Fixed

- Editing: Handling nullable ``esriFieldTypeDate`` Fields
  [discussion #254](https://github.com/e-netze/webgis-community/discussions/254)

- CMS: List AGS services without user/password if any ``folder`` has restricted access.

- Reverser Proxy Access: Fixed some hard coded (relative) Urls for load script and stylesheeds in portal pages and MapBuilder

## 7.25.1402

### Fixed

-   Javascript object `webgis.defaults` was removed by the JS-Minimizer

## 7.25.1401

### Added

-   Order TOC Containers by Service Order [docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/usability.html#inhaltsverzeichnis) (`orderPresentationTocContainsByServiceOrder`)
-   custom.js: Defaults [docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/defaults.html)
-   Sort CMS Items aplhabetically [discussion #252](https://github.com/e-netze/webgis-community/discussions/252)
-   Editing: Allow NULL values with numbers if edit field is nullable [discussion #254](https://github.com/e-netze/webgis-community/discussions/254)

### Fixed

-   AGS Images Services with authentication not displayed in viewer
-   Viewer Url Parameter `srs` => NullReferenceException [discussion #250](https://github.com/e-netze/webgis-community/discussions/250)
-   Group Layers ordering in TOC not the same as in (AGS) services [discussion #251](https://github.com/e-netze/webgis-community/discussions/251)
