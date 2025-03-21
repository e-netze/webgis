# name: Merge objects

# merge: Merge

# merged-pbject: Merged object

# polyline-merge-method: Merge method (polylines)

# merge-has-no-result: No solution was found that includes all original objects during the merge.

# merge-origin-feature: Take attributes from this object

Merging creates a new object. The attribute data for the new object is taken
from the object listed here (select the appropriate object ID). 
The selected object is highlighted on the map. The attribute data for 
the selected element is displayed in the (readonly) edit mask below.

# create-multipart: Create multipart object

The original objects are taken 1:1 and a multipart object is created. 
Each part of the new object corresponds to a part of the original objects.

# create-singlepart: Create singlepart object

An attempt is made to create a single part feature from the objects. 
This cuts and reassembles the original objects. Some parts of the 
original objects may be lost (artifacts) during the cutting and reassembling 
process. There are usually several solutions offered in the next step. 
The method is only successful if a new object is created that contains 
at least one part from each original object.

