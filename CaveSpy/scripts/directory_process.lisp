
( 
 # https://github.com/sls1j/caveSpy/blob/master/Commands.md
 (ForEach lasFile (EnumerateDirectory "c:\maps\franklin_bear" "*.las" 1i)
   (Echo (Get lasFile))
   (Set imageFile (ChangeExtension (Get lasFile) ".kml"))
   (Echo (Get imageFile))
   # read the las file
   (Set cloud (ReadFile (Get lasFile) "12T")) 

   # map the las file to a regtangular grid
   (Set map (MakeMap (Get cloud) 1000i 1i 2i 3i 8i))

     
  (Set map (FillHoles (Get map)))
	(Set lowSpaces (LevelDetectFilter (Get map)))
  
   # draw an image based on the map and cave analysis
	(Set image (MakeImage (Get map)))	
	(DrawElevationColor (Get image) (Get lowSpaces) -3000d, 1.0d)	# parmaters <image> <map> <meter per color cycle> <opacity>
	(DrawHillsideShade (Get image) (GetMember (Get map) elevations) 45d 2d 0.8d, 0.6d) 

   # save the image as defined by the output tag
   (SaveToFile (Get image) (Get imageFile))
  )
)