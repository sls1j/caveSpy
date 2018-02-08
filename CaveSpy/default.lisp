#  This is the default script for the CaveSpy
#  It takes three arguments --input <lasFileName> and --image-size <image width in pixels> --output <output file> [--default-zone <UTM zone of las>]
#
#

(	
	# get the input filename
	(Set lasFile (GetArg "--input" "default.las"))	

	# check to make sure the extension is a las file
	(Assert (Equals (GetExtension (Get lasFile)) ".las"))

	# gets the width argument for the image
	(Set mapWidth (GetArg "--image-size" 3000i))	

	# set some variables
	(Set includedValues "elevation color") # this doesn't do anything yet
	(Set includedClassifications "ground largeVegetation mediumVegetation smallVegetation buildings other")	 # this doesn't do anything yet

	# read the las file
	(Set cloud (ReadFile (Get lasFile) (GetArg "--default-zone", "12T")))

	# map the las file to a regtangular grid
	(Set map (MakeMap (Get cloud) (Get mapWidth) (Get includedValues) (Get includedClassifications)))

	# fix any holes in the map -- right now this isn't a good algorithm
	(FillHoles (Get map))

	# run the algorithm to find caves -- this also isn't very good especially in terrain with lots of trees
	(Set caves (FindCavesByFlood (Get map) 1.0d)) # parameters <map> <minimum depth of the hole in meters>

	# draw an image based on the map and cave analysis
	(Set image (MakeImage (Get map)))
	(DrawElevationColor (Get image) (Get map) 450d, 1.0d)	# parmaters <image> <map> <meter per color cycle> <opacity>
	(DrawHillsideShade (Get image) (Get map) 45d 5d 0.7d, 0.5d) #parameters <image> <map> <angle of hillshade> <distance from point of interest> <intensity of shading> <opacity>
	(DrawCaves (Get image) (Get caves))

	# save the image as defined by the output tag
	(SaveToFile (Get image) (GetArg "--output", "default.bmp"))
)