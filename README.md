# Monster Roadtrip Accessibility

Monster Roadtrip Accessibility is a mod that makes Monster Prom 3: Monster Roadtrip, the multiplayer dating sim by Beautiful Glitch, playable with a screen reader. It reads menus, dialogue, choices, tutorials, stats and resource changes, destination progress, prank dares, campfire seats, credits and the in-game manual, and describes the pictures in the gallery. It also reads the online code when you host a match.

It works with NVDA, JAWS and other Windows screen readers, and falls back to Windows' built-in voices if no screen reader is running. If your screen reader has a braille display connected, everything the mod reads goes to braille too.

- Join the Discord to chat, get help or report problems: <https://discord.gg/KNDXhztGGK>
- Support shaysters on Ko-fi: <https://ko-fi.com/shaysters>

Version 1.0.2. See [CHANGELOG.md](CHANGELOG.md) for what has changed.

## Requirements

- Monster Prom 3: Monster Roadtrip for Windows, from Steam or GOG:
  - Steam: <https://store.steampowered.com/app/1665190/Monster_Prom_3_Monster_Roadtrip/>
  - GOG: <https://www.gog.com/en/game/monster_prom_3_monster_roadtrip>
- A screen reader such as NVDA or JAWS (optional; without one, the mod uses Windows' built-in speech).

The mod has been tested with the Steam version. The GOG and DRM-free versions should work too, but they haven't been tested yet.

## Installing

1. Close the game if it's running.
2. Download **MonsterRoadtripAccessibilityInstaller.exe** from the [latest release](https://github.com/shaysters/monster-roadtrip-accessibility-mod/releases/latest). It needs 64-bit Windows 10 or later.
3. Run it. Windows SmartScreen may warn that the app is unrecognized, because the installer is not code-signed. Choose **More info**, then **Run anyway**.
4. The installer finds your Steam copy of the game by itself. For a GOG or DRM-free copy, or if it can't find the game, use the Browse button next to the game folder and choose the folder that contains `MonsterRoadtrip.exe`.
5. Choose **Install**. The installer adds BepInEx if it's missing, then the mod, and removes files left by older versions of the mod.
6. Start your screen reader, then start the game.

### Updating and uninstalling

Keep the installer. Run it again to update to the newest version, repair the mod, or uninstall it. Close the game first. The installer says on its first page when an update is available, and the mod tells you too, once the title menu has been read.

### Installing by hand

1. Download BepInEx 5 for 32-bit Windows, [BepInEx_win_x86_5.4.23.5.zip](https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x86_5.4.23.5.zip). The game is 32-bit, so the x64 package won't work.
2. Extract it into the game folder, next to `MonsterRoadtrip.exe`. The game folder should then contain `winhttp.dll` and a `BepInEx` folder. On a default Steam install the game folder is:

   ```
   C:\Program Files (x86)\Steam\steamapps\common\Monster Prom 3 - Monster Roadtrip
   ```

   For GOG or DRM-free copies, use the folder you installed the game to.
3. Start the game once so BepInEx can create its folders, then close it.
4. Download **MonsterRoadtripAccessibility.zip** from the [latest release](https://github.com/shaysters/monster-roadtrip-accessibility-mod/releases/latest) and extract everything in it into `BepInEx\plugins` in the game folder. That folder should then contain `RoadtripAccessibilityMod.dll`, `prism.dll`, the `Prism` folder (Prism's license files), `GalleryDescriptions.txt`, `ManualPages.en.json` and `Localization\English.txt`.
5. Start your screen reader, then start the game.

To uninstall a manual install, delete the files and folders listed in step 4 from `BepInEx\plugins`. To remove BepInEx too, delete the `BepInEx` folder, `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` and `changelog.txt` from the game folder.

## Keys

The game's own navigation, confirm and back controls work as normal.

| Keyboard | Controller | What it does |
|---|---|---|
| g | L3 | Repeat the last thing the mod said. |
| t | R3 | Read the current stats. |
| c | None | In an online lobby, read the online code again. |
| e | Left bumper | Open or check the destination guide (a game key the mod reads). |
| Hold q | Right trigger | Check your Prank Dollarz (a game key the mod reads). |
| 1, 2, 3, 4 | Right bumper | Check your current Prank Dares (a game key the mod reads). On a controller, move through them with the right stick. |

Notes:

- Campfire spots say who is sitting there and how many seats are free.
- When you host an online match, the online code is read digit by digit.
- Some gallery pictures are unlocked in the Meta-Shop.

## Changing what the mod says

These files in `BepInEx\plugins` are plain text, so you can edit or translate them:

- `GalleryDescriptions.txt`: the gallery picture descriptions.
- `Localization\English.txt`: many of the phrases the mod adds.

## Known limitations

- The Sticker Studio isn't accessible.
- The gallery descriptions were written by AI and may not be completely accurate.

## Troubleshooting

BepInEx keeps a log in the game folder:

```
BepInEx\LogOutput.log
```

It shows whether the mod loaded. If something goes wrong, include the log when you report it.

- **No speech at all:** make sure `RoadtripAccessibilityMod.dll`, `prism.dll` and the `Prism` folder are in `BepInEx\plugins`, and that BepInEx is installed. Running the installer again and choosing Repair fixes most problems.
- **Windows speech talks instead of your screen reader:** start your screen reader before the game. If you start it after, restart the game.

To report a problem or suggest something, open an issue on the [GitHub page](https://github.com/shaysters/monster-roadtrip-accessibility-mod/issues).

## Credits

This mod was developed by [Shaysters](https://github.com/shaysters), with code written by Codex (OpenAI) and Claude (Anthropic).

The gallery descriptions and the in-game manual text were made by AI. The manual was edited by Misty Rayburn.

The installer is built with [Loadstone](https://github.com/bradjrenshaw/loadstone) by [Bradley Renshaw](https://github.com/bradjrenshaw).

Speech is provided by [Prism](https://github.com/ethindp/prism) by Ethin Probst, under the MPL-2.0 license. Its license files are in the `Prism` folder.

Monster Prom 3: Monster Roadtrip is made by Beautiful Glitch. This mod isn't affiliated with or endorsed by Beautiful Glitch.

## For developers

Build and release instructions are in [DEVELOPING.md](DEVELOPING.md).
