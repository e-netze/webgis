// WebGIS UI Builder
// Registry of the element builders used by webgis.ui.builder.core.js (insertElement):
//   webgis.ui.builder["<type>"] = (map, $newElement, element, options) => { ... }
// plus optional hooks ("<type>.tagname", ".attributes", ".toolid", ".formelement", ".properties",
// ".afterinsert", ".create"), see docs/architecture/ui-builder.md.
//
// The builders are registered in the files webgis.ui.builder.<topic>.js (one file per topic).
// Files named webgis.ui.plugins.<topic>.js contain jQuery/UI plugins without element builders.

webgis.ui = webgis.ui || {};
webgis.ui.builder = webgis.ui.builder || [];