# Freelancer Modern Patch v1.0.1

A focused single-player patch for the original **Freelancer (2003)**: Windows 11
launch compatibility, widescreen, borderless scaling and selectable frame limits,
while preserving the original textures, music, campaign and gameplay balance.

## Download and install

Download **Freelancer-Modern-Patch-v1.0.1.zip** from this repository's **Releases**
section. The automatic GitHub source-code ZIP is for development, not installation.
An existing unmodified **English retail installation** is required.

1. Extract the complete release ZIP into its own folder.
2. Double-click **Install.cmd**.
3. Click **Browse**, select your original Freelancer game folder, and click **Install**.
   Setup patches that folder directly, backing up replaced files first.
4. Click **Play Freelancer**. Next time, double-click **Play Freelancer.cmd** in
   your game folder.
5. In **Options > General > Display**, choose **60**, **120**, or **144 FPS**.

No commands to type, PowerShell policy changes, or administrator launch required.
Choose a game folder your account can write to. Close Freelancer before installing.
Keep the extracted patch outside the game folder.
The game path must use ASCII characters; spaces are supported. Setup explains
this legacy game limitation before copying if the chosen path is incompatible.
Existing saves remain unchanged; replaced game files are backed up. Setup checks supported
original-file hashes, patch conflicts, and the complete payload before installing.
It does not detect every possible third-party modification; use a clean baseline.

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
ultrawide monitors and old external saves are untested. After the borderless scaling
change, the tester reports that Alt+Tab mutes audio while gameplay continues in the
background; switching away does not pause the game. The stock Default/Cancel controls do not undo an FPS selection, which
saves immediately. Lower render resolutions can look softer than native resolution.

## Saves and rollback

The installer leaves existing saves untouched; it does not import or back them up. The patched copy uses its own
`SAVE` folder. Keep it to retain progress. To roll back, close the game, open
**Undo Modern Patch.cmd** in its folder, and click **Undo Patch**. Replaced originals
are restored and added game patch files removed. Keep **ModernPatchBackup** while
the patch is installed. Undo retains saves, removed patch files/settings, and the
small setup/undo tools. Installing again after Undo archives the previous backup. No personal saves, screenshots, runtime logs or test cheats are bundled.

## Source and credits

Custom native sources are in `native-fps-menu` and `native-borderless`; launcher
source is in `patch/source/launcher`; installer source is in `patch/source/installer`. Build scripts are included. Native DLLs need
Visual Studio C++ x86 tools and a Windows SDK; the launcher uses .NET Framework.
`build_patch.py` assembles a payload from staged dependencies in `downloads` and a
supported source install. Exact upstream versions/hashes are in
[sources.json](patch/sources.json); this is not an automatic dependency downloader.

Components include Microsoft's official 1.1 update, JFLP, HUD Shift, FLSharp and
flplusplus, plus the custom FPS selector, scaling plugin and launcher. No full HD
Edition overhaul or texture replacement is installed. See [credits and licensing](LICENSES.md)
and the [package guide](patch/README.txt) for details.
