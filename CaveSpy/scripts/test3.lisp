(	
	
	(Set lasFile "c:\maps\UT_WastachFault_L1_L2_2013_001428.las")
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
			(Set map (MakeMap (Get cloud) (Get mapWidth)))
			(SaveToFile (Get map) (Get mapFile))
		)
	)



	# fix any holes in the map -- right now this isn't a good algorithm	
	(FillHoles (Get map))
	(MapGeometricMeanFilter (Get map) 5i)

	(Set drainage (MapDrainage (Get map) 3i))	
)