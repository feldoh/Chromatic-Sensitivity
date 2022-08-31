# Chromatic Sensitivity

Rimworld mod to make pawns more affected by the colours they interact with.

## Features
Most of the features of this mod are related to the `ChromaticSensitivity` Hediff.
If your pawn has this Hediff they can look forward to some of the following.

### You are what you eat
Eating food will slowly transform the pawn making them
* Approximately the same colour as their food
  * If they eat food with ingredients then the effect will be applied for each ingredient.

### Compatibility
* This should be compatible with Aliens defined using the [HAR Framework](https://github.com/erdelf/AlienRaces)
  * The color modification is applied to the first of the two color channels, it uses `skin` where possible or falls back to `base`.
* Combat extended - Probably ¯\\(ツ)/¯

## Contributing
Please do, this is my first published mod and I'm sure I've made mistakes.
Please let me know so I can learn or raise a PR yourself.

### Building the project
The Project is set up to produce a Debug folder which is gitignored for the assemblies.
This is included via the loadFolders.xml
There are two such loadFolders base files:
* [loadFolders-Debug.xml](loadFolders-Debug.xml)
* [loadFolders-Release.xml](loadFolders-Release.xml)

When you build the Debug profile it will generate you a gitignored loadFolders file.

When you run the release one it creates a ChromaticSensitivity folder one level up.
i.e. if you have cloned this project to `D:\Epic\RimWorld\Mods\Chromatic Sensitivity Dev`
then the release folder it will create is `D:\Epic\RimWorld\Mods\ChromaticSensitivity`.
This avoids the need for publisher plus and means you have a totally clean folder with everything in it.
This in turn makes it nice and easy to zip up this folder for non steam workshop releases.

The same process is used for the About.xml file.

Note that git really doesn't like having DLLs submitted to it because of how it works.
So no DLLs are in the dev folder. However the releases section will include the zips.

## Thanks
* Ludeon for such a great game with excellent mod support.
* ThatBartGuy for the inspiration and help with the art.
* Bratwurstinator for pointing me at some good resources for texture processing.
* [Classifiedgiant](https://github.com/classifiedgiant) for help understanding Unity graphics concepts.
* Evelyn, without whom I would not be where I am today and could never have made something like this.
