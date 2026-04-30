# Monster Roadtrip Accessibility Mod

Text-to-speech accessibility support for [Monster Prom 3: Monster Roadtrip](https://store.steampowered.com/app/1665190/Monster_Prom_3_Monster_Roadtrip/).

This mod for the game was coded by Codex and Shaysters.

## Install

1. Install the game from Steam.
   Default Steam location:
   `C:\Program Files (x86)\Steam\steamapps\common\Monster Prom 3 - Monster Roadtrip`

2. Install BepInEx 5 for Windows x64.
   Download `BepInEx_win_x64_5.4.23.5.zip` from:
   <https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5>

   Direct asset link:
   <https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip>

3. Extract BepInEx into the game folder, next to `MonsterRoadtrip.exe`.
   After extraction, the game folder should contain `winhttp.dll` and a `BepInEx` folder.

4. Run the game once, then close it. This lets BepInEx create its folders.

5. Copy the contents of this mod zip into:
   `C:\Program Files (x86)\Steam\steamapps\common\Monster Prom 3 - Monster Roadtrip\BepInEx\plugins`

   The plugins folder should include:
   - `RoadtripAccessibilityMod.dll`
   - `Tolk.dll`
   - `nvdaControllerClient32.dll`
   - `nvdaControllerClient64.dll`
   - `GalleryDescriptions.txt`
   - `ManualPages.en.json`
   - `Localization\English.txt`

6. Start the game.

## Features

- Adds TTS support for menus, dialogue, choices, tutorials, stats, resource changes, destination progress, prank dares, credits, and many gallery screens.
- The gallery is accessible with image descriptions. These descriptions were made by AI and may not be completely accurate.
- `GalleryDescriptions.txt` is included so others can translate or edit the image descriptions.
- The in-game manual text was made by AI and edited by Misty Rayburn.
- `Localization\English.txt` can be edited by players to adjust many mod-added spoken strings.
- Meta-Shop unlocks some gallery images.
- Sticker Studio was not made accessible.

## Controls

Mod-added keys:

- `G`: repeat the last spoken line.
- `T`: read current stats.
- `L3`: repeat the last spoken line on controller.
- `R3`: read current stats on controller.

Game controls this mod supports with speech:

- `E` or `LB`: open/check the destination guide.
- Hold `Q` or `RT`: check Prank Dollarz.
- `1`, `2`, `3`, `4`, or `RB`: check current Prank Dares.
- Right stick: move through Prank Dares after opening them with controller.

The game's normal navigation, confirm, and back controls are unchanged.
