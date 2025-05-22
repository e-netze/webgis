# Change Log

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## Unreleased

### Added

- Tool Properties: custom.js [Docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/usability.html#toolbox)
  [discussion #279](https://github.com/e-netze/webgis-community/discussions/279)

- Chainage: a api service can be used to calculate the chainage of a point on a line
  [discussion #261](https://github.com/e-netze/webgis-community/discussions/261)

- Feature: 1:n links from results table will be openend in dialog, if target is ``dialog``. Until now, this only worked for single result links. 

- Feature: Feature Results with (AGS) Attachments. The attachments are shown in a separate
  dialog after click on the icon in the table.
  In CMS, Attachment must be allowed and authorized for the user.
  [discussion #272](https://github.com/e-netze/webgis-community/discussions/272)

- Feature: HttpClient Default-Timeout-Seconds for all requests. This can be set in the api.config file.
  [docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#httpclient)

- Usability: Click row in result table don't fire an autoscroll to this row. Autoscroll is only fired, if a result-map-marker is clicked.

- Usability: Enhance result table scrolling. Tables remembers scroll position on switch between tabs.

## Fixed

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
