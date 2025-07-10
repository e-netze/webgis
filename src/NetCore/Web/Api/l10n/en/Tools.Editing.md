# name: Edit

Edit geo-objects on the map

# container: Tools

# error-on-insert: Error on INSERT

# error-on-update: Error on UPDATE

# error-on-delete: Error on DELETE

# desktop:

## point-selection: Point selection
## rectangle-selection: Rectangle selection
## rectangle-selection-querybuilder: Rectangle Selection/QueryBuilder
## new-feature: Create new object
## undo: Undo: Edit step

## querybuilder: Query Builder

## exception-select-edit-theme: To create a new object, first select a theme from the list and click the 'Create new object' button again.



# mobile:

## new-feature: Create new object
## edit-feature: Edit existing object
## delete-feature: Delete existing object
## attributes_and_save: Attributes & Save...

## selected-features: Selectted objects
## edit-selected-feature: Edit object
## delete-selected-feature: Delete object

## explode-multipart-feature: explode multipart object
## cut-feature: Cut object
## clip-feature: Clip object
## merge-features: Merge objects
## massattribution: Mass-Attribution

## selection-label1:

Currently, no objects are selected. For selected objects, additional editing
functions are available (merge, explode, cut)


## use-selection-tool: Use Selection Tool...

## selection-label2:

Or simply click on the map to select an object

## undo: Undo: Edit step

# mask:

## edit-attributes: Edit attributes
## label-geometry: Geometry
## button-geometry: Edit

## apply-attributes: Apply attributes
## save-and-select: Save & Select
## stop-editing: Stop editing
## explode: Explode
## merge: Merge
## cut: Cut
## clip: Clip

## warning-massattribution1: All selected attributes will be taken over. NO Undo is possible for this operation!


# warning-affected-layer1: Warning: The affected layer is not displayed at the current map scale.
# warning-affected-layer2: Warning: The affected layer is currently not visible. Existing objects are not displayed.
# button-affected-layer-visible: Make affected layer visible!

# confirm-delete-object: Delete object?

# update-in-layer: Edit {0}
# delete-in-layer: Delete {0}

# shortcuts: Shortcuts
 
md:
For the **editing selection tool**, the following keyboard shortcuts are available:

- **Spacebar**: Select only one object. The object closest to the clicked point will be selected.
- **E**: As above, but the edit mask opens immediately.
- **D**: As above, but the delete mask opens immediately.

**Requirement**: The selection tool must be active (point selection), and a theme must be selected from the list.

**Procedure**: Press the corresponding key, then click on the map to select the object. 
Hold the key down until the object is selected. Then release the key.

Other important keyboard shortcuts:

- **ESC**:
  Cancel editing. The current tool (e.g., insert/update/delete mask) will be closed. If changes have been made in the mask, canceling must be confirmed with **Discard changes**.