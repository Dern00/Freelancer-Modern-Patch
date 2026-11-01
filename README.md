# Freelancer Modern Patch v1.0

A focused single-player patch for the original **Freelancer (2003)**: Windows 11
launch compatibility, widescreen, borderless scaling and selectable frame limits,
while preserving the original textures, music, campaign and gameplay balance.

## Download and install

Download **Freelancer-Modern-Patch-v1.0.zip** from this repository's **Releases**
section. The automatic GitHub source-code ZIP is for development, not installation.
An existing unmodified **English retail installation** is required.

1. Extract the release ZIP into its own folder.
2. Open PowerShell there and run (substitute your paths):

   ```powershell
   .\Install.ps1 -Source 'C:\Games\Freelancer' -Destination 'C:\Games\Freelancer-Modern'
   ```

   The destination must not exist. The installer checks the baseline and payload,
   creates a separate game copy, and leaves the source unchanged.
3. Double-click **Freelancer-Launcher.exe** and select the new copy's
   **EXE\Freelancer.exe**. Keep the launcher beside **manifest.json**.
4. In **Options > General > Display**, choose **60**, **120**, or **144 FPS**.
   The choice applies immediately and is remembered. New installs default to 120.

**If PowerShell blocks scripts:** copy the original game into a new folder yourself,
then copy the **contents** of the release's `payload` folder into that new copy,
merging folders and replacing files there. Launch it as in step 3. This manual
method omits the installer's source-file preflight; use only the supported clean
baseline. No execution-policy change or administrator launch is required.

## Features

- Windows 11 startup and selected crash/save/exit fixes.
- Widescreen camera, HUD and font adjustments; 2560x1440 is the primary tested size.
- Borderless scaling that fills the monitor at matching aspect ratios and centers
  other ratios with black bars. Mouse clicks remain aligned with the scaled image.
- Native in-game 60/120/144 FPS selector and a fix for the separate cinematic cap.
- Standalone launcher with Borderless, Fullscreen and Windowed modes.
- Portable settings/saves in the patched copy's `SAVE` directory.
- Selected legacy fixes for waypoints, trade-lane lights, long NPC names,
  inventory/HUD flicker, flight-control state and incorrect information displays.
  See the [complete selected-fix list](patch/BugFixes.txt).

## Validation and limits

Tested: launch/exit, new game, docked save/reload, conversations, flight/combat,
trade lanes, nav map, cockpit/turret cameras, Alt+Tab, menu persistence, scaled
1080p and pillarboxed 1600x1200. Sampled flight averaged **119.98 FPS** at a 120 cap,
**143.98 FPS** at a 144 cap, and **143.99 FPS** during scaled 1080p flight.
See [the test report](patch/TestResults.txt) for sample lengths and qualifications.

144 FPS retains an **EXP.** label. Other languages, modded installs, multiplayer,
ultrawide monitors and old external saves are untested. The game pauses when
unfocused. The stock Default/Cancel controls do not undo an FPS selection, which
saves immediately. Lower render resolutions can look softer than native resolution.

## Saves and rollback

The installer backs up existing Documents saves, but does not import them. Manual
installation requires making that backup yourself. The patched copy uses its own
`SAVE` folder. Keep it to retain progress; return to the untouched original game to
roll back. No personal saves, screenshots, runtime logs or test cheats are bundled.

## Source and credits

Custom native sources are in `native-fps-menu` and `native-borderless`; launcher
source is in `patch/source/launcher`. Build scripts are included. Native DLLs need
Visual Studio C++ x86 tools and a Windows SDK; the launcher uses .NET Framework.
`build_patch.py` assembles a payload from staged dependencies in `downloads` and a
supported source install. Exact upstream versions/hashes are in
[sources.json](patch/sources.json); this is not an automatic dependency downloader.

Components include Microsoft's official 1.1 update, JFLP, HUD Shift, FLSharp and
flplusplus, plus the custom FPS selector, scaling plugin and launcher. No full HD
Edition overhaul or texture replacement is installed. See [credits and licensing](LICENSES.md)
and the [package guide](patch/README.txt) for details.
