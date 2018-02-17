
(	
	
	(Set lasFile "c:\maps\UT_WastachFault_L1_L2_2013_001428.las")
	(Set imageFile (ChangeExtension (Get lasFile) ".kml"))
	(Set mapFile  "c:\maps\map.map")
	(Set drainageFile "c:\maps\drain.int")
	(Set slopeFile "c:\maps\slope.int")

	# map the las file to a regtangular grid
	(Set map (ReadFile (Get mapFile)))

	# draw an image based on the map and cave analysis
	(Set image (MakeImage (Get map)))

	(DrawElevationColor (Get image) (Get map) 100d, 1.0d)	# parmaters <image> <map> <meter per color cycle> <opacity>
	(DrawLogIntArray (Get image) (ReadFile (Get drainageFile)) "6666ff" 0.8d)
	(DrawIntArray (Get image) (ReadFile (Get slopeFile)) "ffffff" 0.5d)

	# save the image as defined by the output tag
	(SaveToFile (Get image) (Get imageFile))
)