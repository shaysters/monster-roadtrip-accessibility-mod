# Monster Roadtrip Accessibility Mod

Text-to-speech accessibility support for [Monster Prom 3: Monster Roadtrip](https://store.steampowered.com/app/1665190/Monster_Prom_3_Monster_Roadtrip/).

This mod for the game was coded by Codex, Claude, and Shaysters.

## Install with the installer (recommended)

The installer sets up everything for you, including BepInEx. It needs 64-bit Windows 10 or later and supports the Steam version of the game.

1. Download `MonsterRoadtripAccessibilityInstaller.exe` from the [latest release](https://github.com/shaysters/monster-roadtrip-accessibility-mod/releases/latest).
2. Close the game, then run the installer.
   Windows SmartScreen may warn that the app is unrecognized, because the installer is not code-signed. Choose "More info", then "Run anyway".
3. The installer finds the game through Steam. If it doesn't, use Browse to pick the folder that contains `MonsterRoadtrip.exe`.
4. Press Install. The installer adds BepInEx if it is missing, then the mod, and removes files left by older versions of the mod.
5. Start your screen reader, then start the game.

Run the installer again at any time to update, repair, or uninstall the mod.

## Manual install

1. Install the game from Steam.
   Default Steam location:
   `C:\Program Files (x86)\Steam\steamapps\common\Monster Prom 3 - Monster Roadtrip`

2. Install BepInEx 5 for Windows x86. The game is 32-bit, so the x64 package will not work.
   Download `BepInEx_win_x86_5.4.23.5.zip` from:
   <https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5>

   Direct asset link:
   <https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x86_5.4.23.5.zip>

3. Extract BepInEx into the game folder, next to `MonsterRoadtrip.exe`.
   After extraction, the game folder should contain `winhttp.dll` and a `BepInEx` folder.

4. Run the game once, then close it. This lets BepInEx create its folders.

5. Copy the contents of `MonsterRoadtripAccessibility.zip` from the latest release into:
   `C:\Program Files (x86)\Steam\steamapps\common\Monster Prom 3 - Monster Roadtrip\BepInEx\plugins`

   The plugins folder should include:
   - `RoadtripAccessibilityMod.dll`
   - `prism.dll`
   - `Prism\` (Prism license files)
   - `GalleryDescriptions.txt`
   - `ManualPages.en.json`
   - `Localization\English.txt`

6. Start your screen reader, then start the game.

## Speech

The mod uses Prism for speech. If NVDA, JAWS, or another supported screen reader is running when the game starts, speech goes through it, and to your braille display if the screen reader supports it. If no screen reader is running, the mod uses built-in Windows speech. If you start your screen reader after the game, restart the game to switch over.

Prism is licensed under the Mozilla Public License 2.0. Its license files are in the `Prism` folder.

## Features

- Adds TTS support for menus, dialogue, choices, tutorials, stats, resource changes, destination progress, prank dares, credits, and many gallery screens.
- The gallery is accessible with image descriptions. These descriptions were made by AI and may not be completely accurate.
- `GalleryDescriptions.txt` is included so others can translate or edit the image descriptions.
- The in-game manual text was made by AI and edited by Misty Rayburn.
- `Localization\English.txt` can be edited by players to adjust many mod-added spoken strings.
- Campfire spots say who is sitting there and how many seats are free.
- When you host an online match, the online code is read digit by digit.
- Meta-Shop unlocks some gallery images.
- Sticker Studio was not made accessible.

## Controls

Mod-added keys:

- `G`: repeat the last spoken line.
- `T`: read current stats.
- `C`: in an online lobby, read the online code again.
- `L3`: repeat the last spoken line on controller.
- `R3`: read current stats on controller.

Game controls this mod supports with speech:

- `E` or `LB`: open/check the destination guide.
- Hold `Q` or `RT`: check Prank Dollarz.
- `1`, `2`, `3`, `4`, or `RB`: check current Prank Dares.
- Right stick: move through Prank Dares after opening them with controller.

The game's normal navigation, confirm, and back controls are unchanged.

## Building from source

For developers. You need the .NET SDK (version 6 or later) and the game installed with BepInEx, because the build uses the game's own files and BepInEx's files from the game folder.

Build:

```
dotnet build RoadtripAccessibilityProbe.csproj -c Release
```

If the game is not in the default Steam folder, add `-p:GameDir="<game folder>"`. The built mod and its data files are in `bin\Release\net48`.

Build the release zip into `dist\MonsterRoadtripAccessibility.zip`:

```
powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1
```

Add `-GameDir "<game folder>"` if the game is not in the default Steam folder.

To publish a release: update `changes-latest.md`, build the zip, create a draft GitHub release with the zip attached (`gh release create vX.Y.Z --draft --notes-file changes-latest.md dist\MonsterRoadtripAccessibility.zip`), then run the **Release** workflow from the Actions tab with the same tag. It builds the installer, writes `loadstone-release.json`, and publishes the release. The installer's package files are in `loadstone-packages/`, and its settings are in `installer/profile.json`.

## Credits

- Shaysters
- Codex
- Claude
- Image descriptions and manual text were made by AI; the manual was edited by Misty Rayburn.
- The installer is built with [Loadstone](https://github.com/bradjrenshaw/loadstone) by Bradley Renshaw.
