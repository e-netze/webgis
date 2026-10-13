/*
 * WebGIS service layers for Leaflet
 *
 * This file contains no code. It only groups the webgis-servicelayer.*.js files
 * in Visual Studio (file nesting).
 *
 * The service layers are image overlays that render WebGIS map services
 * (dynamic map images, collections, selections, static/tiled services) in a Leaflet map:
 *
 *   webgis-servicelayer.image_service_base.js     L.ImageOverlay.ImageServiceBase (common base class)
 *   webgis-servicelayer.webgis_service.js         L.ImageOverlay.webgis_service / L.imageOverlay.webgis_service
 *   webgis-servicelayer.webgis_collection.js      L.ImageOverlay.webgis_collection / L.imageOverlay.webgis_collection
 *   webgis-servicelayer.webgis_selection.js       L.ImageOverlay.webgis_selection / L.imageOverlay.webgis_selection
 *   webgis-servicelayer.webgis_static_service.js  L.ImageOverlay.webgis_static_service / L.imageOverlay.webgis_static_service
 *
 * The files are bundled (in this order) into leaflet-plugins.min.js and
 * leaflet-plugins-gesture-handling.min.js, see bundleconfig.json.
 */
