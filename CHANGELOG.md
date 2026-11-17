# Changelog

## 1.1.6

- Window order: a clicked building's window, the replicator (F) and the mecha panel (C) open in front of the inventory that opens with them; the inventory used to cover them. Clicking a window still brings it to the front.
- Every fix can be turned off on its own: section `Fixes` of the config file (Gale's config editor), all on by default.

## 1.1.5

- Replicator: for a recipe with several products the count above the craft button is batches again, as in the queue. It was multiplied by the first product only (for example ">2997" for 999 batches of 3 + 2).
- Replicator: the count no longer stops at ">999"; it shows the real most, up to 99999 batches, the number the MAX buttons of DSPReplicatorPlus now add (with BUG_ModsPatch 1.0.3).
- Replicator: each input in the recipe tree shows how many the inventory holds, under its icon; red when it is less than one batch.
- Item and recipe tips: a name too long for its line gets a smaller font instead of running over the line below.

## 1.1.4

- The "already fixed?" checks no longer write "Could not find" warnings to the log.

## 1.1.3

- Game replicator: the craftable count above the craft button counts items, not batches (a recipe that makes 8 per batch showed an eighth); a longer number widens its frame.
- Game tech tree: a long tech name (They Come From Void's upgrades) gets a smaller font instead of running into the next node's name.

## 1.1.2

- New game screen: the value boxes are small and sit at the right end of each slider; the number on the slider handle is shown again.

## 1.1.1

- New game screen: the star count and UniverseGenTweaks slider values can be typed in, kept within the slider limits.
- Galactic Scale: the star count slider no longer jumps back to a lower number after a drag (it showed only primary stars).

## 1.1.0

- Now requires BUG_ModsPatch.
- Galactic Scale: a new galaxy made from the new game screen is shown in the galaxy view, without the old system's panels.
- UXAssist: the open starmap filter is drawn above every other starmap panel, also those of other mods.
- Galactic Scale: the planet panel keeps its full background; on game 0.10.35 it shrank to a black strip above the planet name.

- DSPAllPlanetInfo: stations with more slots than a page continue on the next page; slots of removed items no longer break the list.
- PlanetFinder: resource filter buttons for every vein, liquid and gas in the loaded game, wrapping onto more rows.
- Multifunction: the inventory button shows the inventory size and the screen limit.
- LaserClearing: the toggle button keeps its tooltip and colour after the UI is rebuilt.
- DSPBigInventory: inventory, assembler and miner windows are scaled to fit the screen.
- UniverseGenTweaks: new game setting rows no longer overlap.
- MechaDronesTweaks: the mecha window and tech tree show the modded drone speed.

## 1.0.1

- UXAssist: closing the starmap while its filter is open puts the Planet Shield panel back in its place.

## 1.0.0

- DSPAllPlanetInfo: the Interstellar Logistics window no longer opens over the planet panel and lists stations again with the automatic UI scale; it is draggable, foldable, 600 units high by default, as wide as its rows, remembers its place and state, and the leftover tabs and buttons of the copied planet panel are gone.
- Galactic Scale: the planet panel's orbit group no longer overlaps the last resource row.
- UXAssist: the open starmap filter is drawn above the Planet Shield panel, with a solid background behind the planet type list.
