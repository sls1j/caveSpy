Cave Spy
========

Reads a .LAS file, and can do some analysis on the data to help locate caves.  Then it outputs either and image or a kml overlay for Google Earth

## Release [1.0.0.4](http://sls1j.ddns.net/bin/CaveSpy-1.0.0.4.zip)
Adds option for false coloring of elevation

## Release [1.0.0.3](http://sls1j.ddns.net/bin/CaveSpy-1.0.0.3.zip)
Fixes bug and adds ability to read the UTM zones so that the kml puts the overlay in the correct place.
  
## Release [1.0.0.2](http://sls1j.ddns.net/bin/CaveSpy-1.0.0.2.zip)
Added support for producing a kml that can be imported into Google Earth

## Release [1.0.0.1](http://sls1j.ddns.net/bin/CaveSpy-1.0.0.1.zip)
Changes include optimizations and multithreading for the cave finding algorithm.  This produces a 2x speed increase in the algorithm


## Usage
To run CaveSpy you must open a command line to specify which input file, output file and image size.
For example: 
```
C:\> CaveSpy.exe --input MyLidar.las --output MyLidar.bmp --image-size 1600 --look-for-caves --flood 1.0
```

## Full command line specification

```
CaveSpy.exe --input <input .las file> --output <output.bmp> --image-size <size in pixels> [--top <GPS coord in UTM> --left <GPS coord in UTM> --width <meters> --height <meters>] [--look-for-caves] [--flood <flood depth in meters>]
	--input - the file name of the .las or a .map file that will be read in
	--output - the name of the output that will be written to disk.  Supports format .bmp, .kml, and .map (a intermediate format for CaveSpy)
	--image-size -- the number of pixels wide the image will be.  The height will be chosen to preserve the aspect ratio. 
	--top -- the northing UTM coordinate of the corner of the map (south,west corner) *
	--left -- the easting UTM coordinate of the corner of the map (south,west corner) *
	--width -- the distance in meters the area will stretch toward the east *
	--height -- the distance in meters the area will stretch toward the north *
	--flood - the depth in meters that the finding algorithm will use to find pits *
	--look-for-caves -- if present then it will apptempt too look for caves using the flood method *
	--false-elevation-coloring -- if present then coloring representing elevation will be added. *

  
  * - are optional parameters
```

## Roadmap
* Add a scripting language to make it easier to configure
* Add directionality and intensity to the hillshading
* Add false coloring for slope angle
* Add the ability to use the colors supplied from LIDAR in LAS point format 2 and 3
* Add an option exclude classified types such as vegetation
* Add algorithm to calculate drainage area of given spots.
* Add full depression shading instead of just dot.
* Add a GUI to make it easier to use.
* Implemented 2018-02-01 ~~Add false color for elevation~~
* Implemented 2018-02-01 ~~Add support for an intermediate format that saves the processing data~~
* Implemented 2018-01-31 ~~Add a kmz or kml export option to allow for overlaying data on Google Earth~~
