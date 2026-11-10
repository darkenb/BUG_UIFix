# BUG UI Fix

Fixes layout problems in the UI of three popular mods. Each fix is a Harmony patch on that mod; nothing is replaced, and the original mods stay as they are on disk.

## DSPAllPlanetInfo: Interstellar Logistics window

- **Window on top of the planet panel.** With the game's UI scale set to automatic, the window opened at the right edge, over the game's own planet details, and listed no stations. It now opens on screen and lists the stations again. Cause: the mod read a game setting that stays 0 while the UI scale is automatic.
- **Window as tall as the screen.** It is now 600 UI units high by default (`WindowHeight`); stations that do not fit go to the next page with the mod's own page buttons.
- **Draggable.** Drag the window by its body or title. It opens in the middle of the screen until you first move it, then keeps its place between sessions and never leaves the screen.
- **No empty strip.** The window was wider than its rows and had an empty band on the left; it now fits them.
- **Foldable.** The `-` / `+` button next to the title folds the window to its title bar. The state is kept.
- **Leftover controls removed.** The window is a copy of the game's planet panel and still showed its Info / Route / Description / Note tabs and the LSTM and menu buttons, which did nothing there.

## Galactic Scale: planet details panel

Galactic Scale makes room in the planet panel for its "Planetary Radius" row, but with the spacing of an older game version. The orbit group (Orbit Radius, Revolution Period ...) sat 16 units too high, so its divider line touched the last resource row and the texts were cramped. The panel now uses the game's current spacing plus the radius row.

## UXAssist: starmap filter

The starmap filter (`>>` / `X`, top left) opened under the game's Planet Shield panel, so the planet type list was hard to read. While the filter is open it is now drawn above that panel and the list has a solid background. Closing the filter puts everything back.

## Settings

`BepInEx/config/bug.dsp.uifix.cfg`, section `DSPAllPlanetInfo`:

- `WindowHeight` (default 600, 150-1400)
- `WindowCollapsed`, `WindowPositionSaved`, `WindowX`, `WindowY`: written by the window itself. Set `WindowPositionSaved` to `false` to bring the window back to the middle of the screen.

## Compatibility

- No dependencies. Install it next to any of the three mods; fixes for mods you do not have are skipped.
- Written against DSPAllPlanetInfo 1.1.14, Galactic Scale 2.80.5 and UXAssist 1.6.3. If a later version changes the parts a fix needs, or already contains the fix, that fix is skipped and the BepInEx log says so; the other fixes still apply.

## Building from source

You need a current .NET SDK and the .NET Framework 4.7.2 targeting pack. The game, Unity and BepInEx assemblies are referenced from your own installation and are not part of this repository.

```text
dotnet build -c Release "-p:DSPGamePath=D:\Games\Dyson Sphere Program" "-p:BepInExCorePath=C:\path\to\BepInEx\core"
```

The output is `bin/Release/net472/BUG_UIFix.dll`. Copy it to `BepInEx/plugins/BUG_UIFix/`.

## License

MIT, see [LICENSE](LICENSE).
