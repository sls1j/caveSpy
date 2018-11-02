(	
	(Set map (GenerateMap "fill-test" 500i 500i))


	;fix any holes in the map -- right now this isn't a good algorithm
	(FillHoles (Get map))

	;draw an image based on the map and cave analysis
	(Set image (MakeImage (Get map)))
	(DrawElevationColor (Get image) (Get map) 450d, 1.0d)	;parmaters <image> <map> <meter per color cycle> <opacity>
	(DrawHillsideShade (Get image) (Get map) 45d 5d 0.7d, 0.5d) ;parameters <image> <map> <angle of hillshade> <distance from point of interest> <intensity of shading> <opacity>

	;save the image as defined by the output tag
	(SaveToFile (Get image) "c:\maps\test.bmp")
)