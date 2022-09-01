# Chromatic Sensitivity

Rimworld mod to make pawns more affected by the colors they interact with.

## Features
Most of the features of this mod are related to the `ChromaticSensitivity` Hediff.
The Hediff is very rare by default, you can stop it naturally occurring altogether in the mod settings.
If your pawn has this Hediff they can look forward to some of the following.

### You are what you eat
Eating food will slowly transform the pawn making them approximately the same color as their food.
* If they eat food with ingredients then the effect will be applied for each ingredient.
* The severity dictates how quickly their color will change. This defaults to 5% per item consumed. This is quite slow but makes it smooth and natural. You can change this in the modsettings all the way up to 100% if you want them to change color after every snack.
* The color is determined by simply finding the most common pixel color in the texture so sometimes this can be a little off, e.g. if the border ends up being the most common color. Core items should all have reasonable colors but you can add your own if you really want someone eating something silver to go blue.
  * If you wish to exclude specific colors or specify a color for a specific item you can do so in the mod settings or via XML patch.
  * To patch a new color in XML add the `Chromatic_Sensitivity.CompProperties_ChromaticFood` with a `forcedColor` tag in the RGB format, e.g. `(255,255,255)` for white into the comps for the `ThingDef` you want to set a color for.
    * Note that as this is a comp it must be a `ThingWithComps` for this to work. You can set the color on any ThingDef in the mod settings but XML is restricted to ThingDefs where the thing supports comps i.e. where the `ThingDef` has `<thingClass>ThingWithComps</thingClass>`.
* If you want to see what colors specific foods will turn you hit the dump all button in the mod settings. This will export each graphic along with the color you can expect to become when you eat it. See one you don't like, then change it!
* Moodlets for chromatic food choices:
  * Chance for a small mood buff when eating any food that results in a color change.
  * Bigger buff if they eat things very similar to their favourite color (Ideology only). 
  * Small mood debuff when eating food with no determinable colour (List of foods in mod settings, e.g. Packaged Survival Meals with no ingredients).
  * Chance for a small food boredom mood debuff for eating food which has a defined colour but doesn't result in any changes. This might trigger if you have them eat only one thing constantly for example.   

### Compatibility
* Safe to add mid-save: yes
* Safe to remove mid-save: yes
  * The first time you load in post removal you'll see errors about an unknown `Hediff` but it is benign and simply saving and reloading again clears it.
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
* Marnador for the RimWorld font
