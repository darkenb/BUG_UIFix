# BUG UI Fix

Fixes layout and display problems in the game's own UI (replicator, item tips, tech tree) and in the UI of other mods. Each fix is a Harmony patch; nothing is replaced, and the original mods stay as they are on disk.

Requires [BUG_ModsPatch](https://thunderstore.io/c/dyson-sphere-program/p/BUG/BUG_ModsPatch/), which holds the logic and crash fixes for the same mods.

This mod only fixes and clarifies what is shown; it changes no gameplay and contains no files of the mods it fixes. Each fix works as a patch on its own mod only, so you do not need all of those mods: the fixes for each installed mod are applied to that mod, the others are skipped.

## Game: replicator count

The number above the replicator's craft button counted batches, while a recipe can make several items per batch. A recipe that makes 8 per batch with materials for 285 batches showed 285; it now shows 2280, the items the queue would make, and a number longer than the game's ">999" widens its frame. The game stopped counting at ">999"; the real most is shown now, up to 99999 batches. A recipe with several products shows batches, as the queue does. Each input in the recipe tree also shows how many the inventory holds, under its icon (red when it is less than one batch), so the count can be checked: 20,000 held at 135 per batch is 148. This fix is for the game itself, so it applies with or without the other mods.

## Game: window order

Clicking a building, belt, sorter, station or other object opens its window together with the inventory, and the inventory was drawn on top of it (with a large inventory it covered the clicked window). The clicked object's window now stays in front; the inventory stays open behind it.

## Game: tip names

A long item or recipe name (Ore Enrichment's recycling recipes) wrapped onto a second line, over the line below it. A name too long for its line now gets a smaller font; shorter names keep theirs.

## Game: tech tree names

Zoomed out, a long tech name ran into the name of the next node on its row (They Come From Void's "Meta driver mounting point extension" upgrades). A name wider than the gap to its neighbour now gets a smaller font; shorter names keep theirs. It works for any mod's techs, and without them.

## DSPAllPlanetInfo: Interstellar Logistics window

- **Window on top of the planet panel.** With the game's UI scale set to automatic, the window opened at the right edge, over the game's own planet details, and listed no stations. It now opens on screen and lists the stations again. Cause: the mod read a game setting that stays 0 while the UI scale is automatic.
- **Window as tall as the screen.** It is now 600 UI units high by default (`WindowHeight`); stations that do not fit go to the next page with the mod's own page buttons.
- **Draggable.** Drag the window by its body or title. It opens in the middle of the screen until you first move it, then keeps its place between sessions and never leaves the screen.
- **No empty strip.** The window was wider than its rows and had an empty band on the left; it now fits them.
- **Foldable.** The `-` / `+` button next to the title folds the window to its title bar. The state is kept.
- **Leftover controls removed.** The window is a copy of the game's planet panel and still showed its Info / Route / Description / Note tabs and the LSTM and menu buttons, which did nothing there.
- **Big stations are listed completely.** A station with more slots than fit on a page continues on the next page; before, its remaining slots were skipped, and a very large station made empty pages. Slots of items from removed mods are skipped instead of breaking the list.

## Galactic Scale: planet details panel

Galactic Scale makes room in the planet panel for its "Planetary Radius" row, but with the spacing of an older game version. The orbit group (Orbit Radius, Revolution Period ...) sat 16 units too high, so its divider line touched the last resource row and the texts were cramped. The panel now uses the game's current spacing plus the radius row. On game 0.10.35 the panel's background also shrank to a black strip above the planet name, because the game now re-applies a stored panel height that Galactic Scale never sets; the fix sets it too.

## Galactic Scale: new game screen

Changing a setting while a star system was open on the new game screen made a new galaxy, but the system's planet and star panels stayed open over it. The screen now goes back to the galaxy view with the new galaxy.

The star count slider jumped back to a lower number after it was released: Galactic Scale showed only the primary stars, while its binary companions count toward the star count. It now shows the star count of the galaxy, the number that was chosen.

## UXAssist: starmap filter

The starmap filter (`>>` / `X`, top left) opened under other starmap panels (the game's Planet Shield panel, frames of other mods), so the planet type list was hard to read. While the filter is open it is now drawn above all of them and the list has a solid background. Closing the filter puts everything back.

## PlanetFinder: resource filter buttons

The filter buttons were a fixed list of the vanilla resources on one row. They now come from the loaded game, so GenesisBook's extra veins, liquids and gases (and Galactic Scale's planet themes) get buttons too: veins, then liquids, then gases. When they do not fit on one row they wrap, and the planet list moves down. The list is read again for every new game.

## Multifunction: inventory button

The inventory button in Multifunction's window shows the current inventory size, and with DSPBigInventory also the most that fits on your screen, for example `17×21 (≤30×18)`.

## LaserClearing: toggle button

After the UI is rebuilt (for example when you load another save), the laser toggle button gets its tooltip and colour again.

## DSPBigInventory: windows fit the screen

A large inventory with the delivery panel made the inventory, assembler and miner windows larger than the screen. These windows are scaled down and moved so they always fit; the inventory itself is not changed. (The inventory size and save fixes for DSPBigInventory are in BUG_ModsPatch.)

## New game screen: type the values

A small text box sits at the right end of the star count slider (and of UniverseGenTweaks' four sliders when it is installed). Click it, type a value and press Enter: it is kept inside the slider's limits and applied like a drag. Dot and comma both work as the decimal sign.

## UniverseGenTweaks: new game settings

UniverseGenTweaks adds four sliders to the new game screen. Together with GenesisBook and other mods that add rows there, settings were drawn on top of each other, and the Dark Fog details button was left behind. All setting rows are now stacked under each other with even spacing; hidden options take no space.

## MechaDronesTweaks: drone speed display

The mecha window and the tech tree showed the game's drone speed, not the one MechaDronesTweaks sets (drones already flew at the modded speed). They now show the real speed.

## Settings

`BepInEx/config/bug.dsp.uifix.cfg`, also shown in Gale's and r2modman's config editor. The file is written the first time the game starts with this version.

Section `Fixes`: one switch per fix, all on by default. Turn one off to keep that part of the game or mod as it was; a change takes effect after restarting the game.

| Switch | Fix |
|---|---|
| `ReplicatorCountItems` | Replicator count in items for one-product recipes |
| `ReplicatorCountNoLimit` | Replicator count without the >999 limit |
| `ReplicatorInventoryCounts` | Inventory counts under the recipe inputs |
| `WindowOrder` | Clicked building's window in front of the inventory |
| `TipNames` | Long tip names fit their line |
| `TechTreeNames` | Long tech names fit the tree |
| `NewGameTypedValues` | Typed values on the new game screen |
| `DSPAllPlanetInfo` | Interstellar Logistics window |
| `GalacticScale` | Galactic Scale planet panel and new game screen |
| `UXAssistStarmapFilter` | UXAssist starmap filter on top |
| `PlanetFinderButtons` | PlanetFinder resource filter buttons |
| `MultifunctionInventoryButton` | Multifunction inventory button |
| `LaserClearingButton` | LaserClearing toggle button |
| `BigInventoryWindowsFit` | DSPBigInventory windows fit the screen |
| `UniverseGenTweaksRows` | UniverseGenTweaks setting rows |
| `MechaDronesTweaksSpeed` | MechaDronesTweaks drone speed display |

Section `DSPAllPlanetInfo`:

- `WindowHeight` (default 600, 150-1400)
- `WindowCollapsed`, `WindowPositionSaved`, `WindowX`, `WindowY`: written by the window itself. Set `WindowPositionSaved` to `false` to bring the window back to the middle of the screen.

## Compatibility

- Needs only BepInEx and BUG_ModsPatch. Install it next to any of these mods; fixes for mods you do not have are skipped.
- Written against DSPAllPlanetInfo 1.1.14, Galactic Scale 2.80.8, UXAssist 1.6.3, PlanetFinder 1.3.2, Multifunction 4.0.8, LaserClearing 1.0.8, DSPBigInventory 1.0.8, UniverseGenTweaks 1.2.11 and MechaDronesTweaks 1.1.4. If a later version changes the parts a fix needs, or already contains the fix, that fix is skipped and the BepInEx log says so; the other fixes still apply.

## Licences

BUG_UIFix is MIT licensed. It contains no code, DLLs, images or other files of the mods it fixes; every fix is a Harmony patch that runs while the game runs. The fixed mods stay under their own terms: UXAssist, UniverseGenTweaks and MechaDronesTweaks (Soar Qin), LaserClearing (starfi5h): MIT; PlanetFinder (hetima): Unlicense; Galactic Scale: CC0; Multifunction (blacksnipebiu), DSPAllPlanetInfo and DSPBigInventory (appuns): no licence published, only patched at runtime. These fixes are not made or endorsed by the authors of those mods.

## Building from source

You need a current .NET SDK and the .NET Framework 4.7.2 targeting pack. The game, Unity and BepInEx assemblies are referenced from your own installation and are not part of this repository.

```text
dotnet build -c Release "-p:DSPGamePath=D:\Games\Dyson Sphere Program" "-p:BepInExCorePath=C:\path\to\BepInEx\core"
```

The output is `bin/Release/net472/BUG_UIFix.dll`. Copy it to `BepInEx/plugins/BUG_UIFix/`.
