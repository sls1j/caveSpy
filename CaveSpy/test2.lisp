
(	
	
	(Set lasFile "c:\maps\UT_WastachFault_L1_L2_2013_001428.las")
	(Set imageFile (ChangeExtension (Get lasFile) ".kml"))

	# read the las file
	(Set cloud (ReadFile (Get lasFile) "12T"))	

	# map the las file to a regtangular grid
	(Set map (MakeMap (Get cloud) 3000i 2i 8i 9i))

	# fix any holes in the map -- right now this isn't a good algorithm	
	(FillHoles (Get map))
	#(MapGeometricMeanFilter (Get map) 7i)
	(MapGeometricMeanFilter (Get map) 5i)

	# run the algorithm to find caves -- this also isn't very good especially in terrain with lots of trees
	#(Set caves (FindCavesByFlood (Get map) 0.3d)) # parameters <map> <minimum depth of the hole in meters>	

	# draw an image based on the map and cave analysis
	(Set image (MakeImage (Get map)))
	#(DrawElevationColor (Get image) (Get map) 100d, 1.0d)	# parmaters <image> <map> <meter per color cycle> <opacity>
	(DrawIntArray (Get image) (MapDrainage (Get map) 5i) 1.0d)
	(DrawHillsideShade (Get image) (Get map) 45d 5d 0.7d, 0.5d) #parameters <image> <map> <angle of hillshade> <distance from point of interest> <intensity of shading> <opacity>
	#(DrawCaves (Get image) (Get caves))
	#(DrawClassification (Get image) (Get map) 13i)

	# save the image as defined by the output tag
	(SaveToFile (Get image) (Get imageFile))
)