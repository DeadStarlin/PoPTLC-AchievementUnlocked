# PoPTLC-AchievementUnlocked

A BepInEx 6 mod for Prince of Persia: The Lost Crown on Windows. Press **F8** to open a Windows achievement menu, select a Ubisoft Connect achievement, and request its unlock.

Achievements are sorted A–Z. The loaded list is retained when reopening the window; **Refresh** fetches updated status. Select a row to see its description. Unlocking requires confirmation of the selected achievement.

## Requirements

- Prince of Persia: The Lost Crown, launched through Steam with Ubisoft Connect signed in.
- A working **BepInEx 6 Unity IL2CPP Windows x64** installation.
- The **.NET 6 Windows Desktop Runtime x64**, available from [Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/6.0). The `dotnet` command must be available on PATH. This backup was built and tested with Desktop Runtime 6.0.36.

The menu is a separate Windows window, not an in-game overlay. Use it from the main menu or pause menu; opening it does not pause gameplay.

## Install the included build

1. Install BepInEx and the Windows Desktop Runtime if needed.
2. Close the game and all achievement-menu windows.
3. Copy the contents of **release/** into the game directory. The resulting files should be:

   ```text
   <GameRoot>/BepInEx/plugins/AchievementMenu/AchievementMenu.dll
   <GameRoot>/BepInEx/plugins/AchievementMenu/MenuApp.dll
   <GameRoot>/BepInEx/plugins/AchievementMenu/MenuApp.runtimeconfig.json
   ```

   Alternatively, from this repository run:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\install.ps1 -GameRoot "D:\SteamLibrary\steamapps\common\Prince of Persia The Lost Crown"
   ```

4. Launch the game through Steam and sign into Ubisoft Connect.
5. At the main menu or while paused, press **F8** with the game focused.
6. Select an achievement, click **Unlock selected achievement**, and confirm. Refresh to check its status. F8 or the close button closes the window.

Unlocks may be permanent. The mod submits requests only to Ubisoft Connect. Steam synchronization is handled by Ubisoft's existing connection; this mod does not directly unlock Steam achievements or verify synchronization.

## BepInEx setup used for verification

The working installation uses the BepInEx core from [Lyall's PoPTLCFix v0.8.6 UbisoftPlus package](https://github.com/Lyall/PoPTLCFix/releases/tag/v0.8.6). Only its loader was used; the PoPTLCFix display plugin is not required. Loader dependencies are not bundled in this repository.

- Doorstop's proxy is named `version.dll` beside `TheLostCrown.exe`.
- In `doorstop_config.ini`, `ignore_disable_switch = true`.
- In `BepInEx/config/BepInEx.cfg`, under `[Logging]`, `UnityLogListening = false`.

Preserve any existing mod installation before replacing loader files. Run the game once with BepInEx to generate `BepInEx/interop`. Confirm `BepInEx/LogOutput.log` reports chainloader startup and `Achievement menu loaded` after installing this plugin. The plugin uses an existing game update method and does not register a new IL2CPP component.

## Build from source

Building requires an installed .NET SDK, the .NET 6 Desktop Runtime 6.0.36, and a game installation whose BepInEx has generated interop assemblies. The source directory can be anywhere; pass the game path explicitly.

```powershell
# Compile, run offline checks, and install. Close the game/window first.
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -GameRoot "D:\SteamLibrary\steamapps\common\Prince of Persia The Lost Crown"

# Compile and check without changing the installed plugin.
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -GameRoot "D:\SteamLibrary\steamapps\common\Prince of Persia The Lost Crown" -BuildOnly
```

Outputs go into `bin/`. Build and install scripts never launch the game. They do not download dependencies.

## Source and backup contents

- `Plugin.cs`: BepInEx plugin, F8 shortcut, game-thread request handling and Ubisoft API calls.
- `MenuApp.cs`: Windows Forms achievement window.
- `Models.cs`: local command/response records and selection validation.
- `Checks.cs`: offline tests for stale selections, invalid indices, busy/unlocked entries, JSON serialization and async list redraw.
- `build.ps1`: compilation, checks and optional installation.
- `install.ps1`: installation of the included build.
- `release/`: the three files needed to install the verified build.
- `LICENSE`: MIT license text.
- `.gitattributes`: line ending and binary file handling rules.
- `.gitignore`: ignores local compilation output and IDE user files.

Game binaries, IL2CPP dumps, loader packages, logs, local command/response files and abandoned experiments are excluded.

## Verification and limitations

Verified: compilation and offline checks, successful plugin startup, F8 opening the window, displaying 40 live Ubisoft achievements, selection enabling the unlock button, and alphabetical ordering. Actual unlock submission and subsequent Steam synchronization have not been tested. Requests time out after 20 seconds; refresh before retrying.

## Uninstall

Close the game and menu window, then remove only `BepInEx/plugins/AchievementMenu`. This does not reverse any achievements already unlocked.

## Acknowledgments & Notes

- This project was developed with the assistance and support of Large Language Models (LLM).
- BepInEx core loader package referenced from [Lyall's PoPTLCFix](https://github.com/Lyall/PoPTLCFix).

## License

This project is licensed under the [MIT License](LICENSE).
