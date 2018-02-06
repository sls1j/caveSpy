#  This is the default script for the CaveSpy
#  It takes two arguments --input [lasFileName] and --width [image width in pixels]
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
	(Set includedValues "elevation color")
	(Set includedClassifications "ground largeVegetation mediumVegetation smallVegetation buildings other")
	(Set defaultZone "12T")

	# make all the output file names
	(Set cloudFile (ChangeExtension (Get lasFile) ".cloud"))
	(Set mapFile (ChangeExtension (Get lasFile) ".map"))
	(Set imageFile (ChangeExtension (Get lasFile) ".bmp"))
	(Set kmlFile (ChangeExtension (Get lasFile) ".kml"))	

	# read or make the mapping to a 2d grid
	(If (FileExists (Get mapFile))
		(Set map (ReadFile (Get mapFile)))
		# else
		(
			(Set cloud (ReadFile (Get lasFile) (Get defaultZone")))				
			(Set map (MakeMap (Get cloud) (Get mapWidth) (Get includedValues) (Get includedClassifications)))
			(SaveToFile (Get map) (Get mapFile))
		)
	)

	# fix any holes in the map
	(FillHoles (Get map))

	# run the algorithm to find caves
	(Set caves (FindCavesByFlood (Get map) 1.0d))

	# define the first layer
	(Set image (MakeImage (Get map)))
	(DrawElevationColor (Get image) (Get map) 150d, 1.0d)	
	(DrawHillsideShade (Get image) (Get map) 45d 5d 0.7d, 0.4d)
	(DrawCaves (Get image) (Get caves))

	# save the first layer as an image
	(SaveToFile (Get image) (Get imageFile))

	# save all the layers in a Kml
	#(SaveToFile (Get kmlFile) (Get image))
)