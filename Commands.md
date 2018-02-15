Cave Spy Scriping Command Reference
====================================

A command reference for the scripting language

(Add <n1 : number> <n2 : number>)
returns n1 + n2

(And <condition 1 : boolean> <condition 2 : boolean>)
returns true if condition 1 and 2 are both true

(Assert <condition : boolean>)
throws an exception if the condition is false

(ChangeExtension <fileName : string> <extension : string>)
changes the extension of the file name passed into to the one that is specified
Example
```
(ChangeExtension (Get someVariable) ".bmp")
```

(DrawCaves <image> <caves>)
draws the found caves into the given image.

(DrawClassification <image : Image> <map : Map> <classification: int>)
Colors all of the matching classification items to black

(DrawDrainageIntensity <image : Image> <map : Map>)
Draws the drainage to the image calculated from the map.  
-- not implemented --

(DrawElevationColor <image : Image> <map : Map> <meters per cycle : double> <opacity: double>)
Draws the colored elevation.  A full color cycle is determined by the "meters per cycle" parameter.

(DrawHillsideShade <image : Image> <map : Map> <heading : double> <step: double> <intensity: double> <opacity : double>)

(DrawRealColor <image : Image> <map : Map> <opacity: double>)
-- not implemented --

(DrawSlopeColor <image : Image> <map: Map> <opacity: double>)

Equals
(FileExists <file name>)

(FillHoles <map : Map>)

(FindCavesByFlood <map : Map> <depth : double>)

(Get <variable name : string>)

(GetArg <agument name : string> <default value : any>)

(GetEnvironment <variable name>)

(GetExtension <file name>)

(GreaterThan <n1 : number> <n2 : number>)

(GreaterThanEqual <n1: number> <n2: number>)

(If <condition> <True Clause> <False Clause>)

(LessThan <n1 : number> <n2: number>)

(LessThanEqual <n1 : number> <n2: number>)

(Loop <n: int> <item to execute: any> [<item to execute: any>...])

(MakeImage <map: Map>)

(MakeMap <cloud: Cloud> <width: int>)

(MapGeometricMeanFilter <map: Map> <N: int>)

(Not <condition: boolean>)

(NotEquals <item1: any> <item2: any>)

(Or <condition 1: boolean> <condition 2: boolean>)

(ReadFile <file name>)

(SaveToFile <object: image or map>)

(Set <variable name: string> <value: any>)

(Sub <n1 : number> <n2: number>)

(While <condition> [<item to execute>...]


