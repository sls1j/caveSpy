Cave Spy
========

Reads a .LAS file, and can do some analysis on the data to help locate caves.  Then it outputs either and image or a kml overlay for Google Earth

## Release [1.1.0.2](http://sls1j.ddns.net/bin/CaveSpy-1.1.0.2.zip)
Added scripting command MapDrainage for mapping the drainage of a given las file.
Added scripting command DrawIntArray to draw the logrithmic results of the drainage map
Updated the Command.md 


## Release [1.1.0.1](http://sls1j.ddns.net/bin/CaveSpy-1.1.0.1.zip)
Fixed bugs in the hole filling algorithm, the elevation coloring, and added a geometric filter command to the scripting language.
Added the beginning of the reference guide to the scripting language.  See Commands.md

## Release [1.1.0.0](http://sls1j.ddns.net/bin/CaveSpy-1.1.0.0.zip)
Reworks much of the code to add a lisp based scripting engine.  This is because the commandline options were getting overwhelming.  The scripting is
more sophisticated and much more flexible.  Two scripts were developed one simple and one more complex.  Each script has a list
of it's own commandline arguments.

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
CaveSpy.exe [--script <script path>] [--verbose] [ script arguments ]");
	--script <path> -- the path of the script to execute.  If no script is specified 'default.lisp' is used
	--verbose -- increase the amount of information printed on the console.
	--version -- prints out the version of CaveSpy.exe

	script arguments should be commented with in the script.  If not then each GetArgs command defines the possible arguments
```

## Roadmap
* Add false coloring for slope angle
* Add the ability to use the colors supplied from LIDAR in LAS point format 2 and 3
* Add an option exclude classified types such as vegetation
* Add algorithm to calculate drainage area of given spots.
* Add full depression shading instead of just dot.
* Add a GUI to make it easier to use.
* Implemented 2018-02-07 ~~Add a scripting language to make it easier to configure~~
* Implemented 2018-02-07 ~~Add directionality and intensity to the hillshading~~
* Implemented 2018-02-01 ~~Add false color for elevation~~
* Implemented 2018-02-01 ~~Add support for an intermediate format that saves the processing data~~
* Implemented 2018-01-31 ~~Add a kmz or kml export option to allow for overlaying data on Google Earth~~
