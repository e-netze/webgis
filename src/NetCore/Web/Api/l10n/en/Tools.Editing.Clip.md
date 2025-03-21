# name:	Clip Objects

Draw a polygon on the map to use as the clipping area.

# clip: Clip

# clip-has-no-result: Clip has no result

# choose-clip-method: Choose clip method

# clip-intersected-and-difference: Intersection + Difference

# clip-intersected:	Intersection

# clip-difference: Difference

# clip-xor:	Symmetric Difference

# draw-clip-polygon-first: Please draw a clipping area first.

# ApplyClipToIntersected: Apply only to intersected objects

If multiple objects are selected and not all of them are intersected by the clipping
area, only the intersected objects will be affected by the clipping. The other objects
will remain unchanged.

# ApplyClipToAll: Apply to all objects

If multiple objects are selected and not all of them are intersected by the clipping
area, all objects will be affected by the clipping, including those outside the clipping
area, which will be included in the difference layer (optional). If only the intersection
layer is selected as a result, all objects in the difference layer will be deleted.

# DisolveMultipartFeatures: Dissolve multipart features

 If an object is divided into several parts by clipping, a new object is created from each
part. This prevents multipart features from being created. Clipping can create more objects
than were originally selected.

# ClippedFeaturesStayMultiparts:	Clipped features as multiparts

If an object is divided into several parts by clipping, it remains as one object that consists
of multiple parts. In this case, a multipart feature is created. After clipping, there will be
the same number of objects as before clipping. 
