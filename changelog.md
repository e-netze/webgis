﻿# Change Log

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## Unreleased

### Added

## Fixed

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
