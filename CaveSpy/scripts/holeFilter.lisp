#  This is the default script for the CaveSpy
#  It takes three arguments --input <lasFileName> and --image-size <image width in pixels> --output <output file> [--default-zone <UTM zone of las>]
#  --input   :the name of the file that will be loaded
#  --image-size :width of the produced image size in pixels
#  --output  :the name of the output file.  The extension determines the type of file to be produced.  .bmp and .kml are supported
#  --default-zone  :Some of the .las files don't seem to include UTM zone information.  If not specify the correct zone with this parameter.  The default is value is 12T
#

(	
	# get the input filename
	(Set lasFile (GetArg "--input" "default.las"))	

	# check to make sure the extension is a las file
	(Assert (Equals (GetExtension (Get lasFile)) ".las"))

	# gets the width argument for the image
	(Set mapWidth (GetArg "--image-size" 500i))	

	# read the las file
	(Set pointCloud (ReadFile (Get lasFile) (GetArg "--default-zone", "12T")))

	# map the las file to a regtangular grid
	(Set map (MakeMap (Get pointCloud) (Get mapWidth)))

	# fix any holes in the map -- right now this isn't a good algorithm
	(FillHoles (Get map))
	#(MapGeometricMeanFilter (Get map) 5i)

	(Set filteredMap (LevelDetectFilter (Get map)))
	

	# draw an image based on the map and cave analysis
	(Set image (MakeImage (Get filteredMap)))
	(DrawElevationColor (Get image) (Get filteredMap) 300d, 0.75d)	# parmaters <image> <map> <meter per color cycle> <opacity>
	(DrawHillsideShade (Get image) (Get map) 45d 5d 0.7d, 0.25d) #parameters <image> <map> <angle of hillshade> <distance from point of interest> <intensity of shading> <opacity>

	# save the image as defined by the output tag
	(SaveToFile (Get image) (GetArg "--output", "default.bmp"))
)