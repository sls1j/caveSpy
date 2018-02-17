
(	
	
	(Set lasFile (GetArg "--las" "default.las"))
	(Set imageFile (ChangeExtension (Get lasFile) ".kml"))
	(Set mapFile  "c:\maps\map.map")
	(Set drainageFile "c:\maps\drain.int")
	(Set slopeFile "c:\maps\slope.int")

	# map the las file to a regtangular grid
	(Set map (ReadFile (Get mapFile)))

	# draw an image based on the map and cave analysis
	(Set image (MakeImage (Get map)))

	#(DrawElevationColor (Get image) (Get map) 100d, 1.0d)	# parmaters <image> <map> <meter per color cycle> <opacity>
	(DrawHillsideShade (Get image) (Get map) 100d 5d 3d 1.0d)
	(DrawLogIntArray (Get image) (ReadFile (Get drainageFile)) "6666ff" 1.0d)
	(DrawIntArray (Get image) (ReadFile (Get slopeFile)) "ff0000" 1.0d)

	# save the image as defined by the output tag
	(SaveToFile (Get image) (Get imageFile))
)