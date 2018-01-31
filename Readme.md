Cave Spy
========

Reads a .LAS file.  Supports PointCloud formats 1,2 and 3.

  
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
  --input - the file name of the .las file that will be read in
  --output - the name of the bitmap that will be written to disk
  --image-size -- the number of pixels wide the image will be.  The height will be chosen to preserve the aspect ratio.
  --top -- the northing UTM coordinate of the corner of the map (south,west corner) *
  --left -- the easting UTM coordinate of the corner of the map (south,west corner) *
  --width -- the distance in meters the area will stretch toward the east *
  --height -- the distance in meters the area will stretch toward the north *
  --look-for-caves -- if present then it will apptempt too look for caves using the flood method *
  --flood - the depth in meters that the finding algorithm will use to find pits.  Is only used when --look-for-caves is specified *
  
  * - are optional parameters
```

## Roadmap

* Add a kmz or kml export option to allow for overlaying data on Google Earth
* Add directionality and intensity to the hillshading
* Add the ability to use the colors supplied from LIDAR in LAS point format 2 and 3
* Add an option exclude classified types such as vegetation
* Add a GUI to make it easier to use.
* Add algorithm to calculate drainage area of given spots.
* Add false color for hillshade and elevation
* Add full depression shading instead of just dot.

