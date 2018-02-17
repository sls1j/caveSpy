(	
	
	(Set lasFile (GetArg "--las" "default.las"))
	(Set mapFile  "c:\maps\map.map")
	(Set drainageFile "c:\maps\drain.int")
	(Set slopeFile "c:\maps\slope.int")

	# read the las file
	(Set cloud (ReadFile (Get lasFile) "12T"))	

	# map the las file to a regtangular grid
	(Set map (MakeMap (Get cloud) 3000i 2i 8i 9i))

	# fix any holes in the map -- right now this isn't a good algorithm	
	(FillHoles (Get map))
	#(MapGeometricMeanFilter (Get map) 7i)
	(MapGeometricMeanFilter (Get map) 5i)

	(SaveToFile (Get map) (Get mapFile))
	(SaveToFile (MapDrainage (Get map) 3i) (Get drainageFile))
	(SaveToFile (MapCalculateSlopeAngle (Get map)) (Get slopeFile))
)