(	
	
	(Set lasFile "c:\maps\franklin_bear\USGS_LPC_ID_Franklin_Bear_2017_12TVM5153_LAS_2018.las")
	(Set defaultZone "12T")
	(Set mapFile (ChangeExtension (Get lasFile) ".map"))
	(Set imageFile (ChangeExtension (Get lasFile) ".kml"))
	(Set mapWidth 1000i)

	# read the las file
	(Set haveMap (FileExists (Get mapFile)))
	(If (Get haveMap)
		(Set map (ReadFile (Get mapFile)))
		# else
		(
			(Set cloud (ReadFile (Get lasFile) (Get defaultZone)))				
			(Set map (MakeMap (Get cloud) (Get mapWidth) 0i 1i 2i 6i 7i 8i 9i 10i 11i 12i))
			(SaveToFile (Get map) (Get mapFile))
		)
	)



	# fix any holes in the map -- right now this isn't a good algorithm		
	(Set map (FillHoles (Get map)))
	(Set lowSpaces (LevelDetectFilter (Get map)))

	# draw an image based on the map and cave analysis
	(Set image (MakeImage (Get map)))	
	(DrawElevationColor (Get image) (Get lowSpaces) -25000d, 1.0d)	# parmaters <image> <map> <meter per color cycle> <opacity>
	(DrawHillsideShade (Get image) (Get lowSpaces) 45d 2d 0.001d, 0.6d) 
	#(DrawHillsideShade (Get image) (GetMember (Get map) elevations) 45d 2d 0.8d, 0.6d) 

	#parameters <image> <map> <angle of hillshade> <distance from point of interest> <intensity of shading> <opacity>

	# save the image as defined by the output tag
	(SaveToFile (Get image) "c:\maps\franklin_bear\filtered.bmp")	
)