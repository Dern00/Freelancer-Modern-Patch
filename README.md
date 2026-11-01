# Freelancer Modern Patch v1.1.0

A focused single-player patch for the original **Freelancer (2003)**: Windows 11
launch compatibility, widescreen, borderless scaling and selectable frame limits,
with optional HD textures while preserving the original music, campaign and gameplay balance.

## Download and install

Choose a file from **Releases**:

- **Freelancer-Modern-Patch-v1.1.0.zip** — small core patch, original textures.
- **Freelancer-Modern-Patch-HD-v1.1.0.zip** — the complete installer with optional
  HD interiors/planetscapes, characters, icons and HUD backgrounds (~1.4 GB download).

1. Close the game and extract the ZIP outside your game folder.
2. Double-click **Install.cmd**, then browse to the game folder.
3. In the HD download, leave **Install HD textures** checked to include them.
4. Click **Install**, then **Play Freelancer**.

**Existing Modern Patch v1.0/v1.0.1 users:** select your already-patched game with
HD textures checked. Setup adds HD assets without reinstalling the core or resetting
FPS settings/saves. Clean English retail games receive the core first.

The HD download contains about 2.4 GB of assets; allow several GB for extraction,
installation and backups. Use a writable ASCII game path (spaces work). No commands
to type or PowerShell-policy changes required. Other mods/languages are unsupported.
The automatic GitHub source ZIP is for development, not installation.

## Features

- Optional HD interiors/planetscapes, characters, icons and HUD backgrounds.

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
1080p and pillarboxed 1600x1200. Core-only sampled flight averaged **119.98 FPS** at a 120 cap,
**143.98 FPS** at a 144 cap, and **143.99 FPS** during scaled 1080p flight.
See [the test report](patch/TestResults.txt) for sample lengths and qualifications.

144 FPS retains an **EXP.** label. Other languages, modded installs, multiplayer,
ultrawide monitors and old external saves are untested. After the borderless scaling
change, the tester reports that Alt+Tab mutes audio while gameplay continues in the
background; switching away does not pause the game. The stock Default/Cancel controls do not undo an FPS selection, which
saves immediately. Lower render resolutions can look softer than native resolution.

## Saves and rollback

**Undo HD Textures.cmd** restores original assets while keeping the core patch,
FPS settings and saves. **Undo Modern Patch.cmd** restores both components when
a core backup is available. Older hand-patched copies may only have an HD backup.

Keep **ModernTexturesBackup** and **ModernPatchBackup** while installed. Undo keeps
saves, removed patch files/settings, and the small helper tools. Reinstallation
archives the previous restored backup. Existing Documents saves are not imported;
the patched game uses its own **SAVE** folder. No personal saves or logs are bundled.

## Source and credits

Custom native sources are in `native-fps-menu` and `native-borderless`; launcher
source is in `patch/source/launcher`; installer source is in `patch/source/installer`. Build scripts are included. Native DLLs need
Visual Studio C++ x86 tools and a Windows SDK; the launcher uses .NET Framework.
`build_patch.py` assembles a payload from staged dependencies in `downloads` and a
supported source install. Exact upstream versions/hashes are in
[sources.json](patch/sources.json); this is not an automatic dependency downloader.

Components include Microsoft's official 1.1 update, JFLP, HUD Shift, FLSharp and
flplusplus, plus the custom FPS selector, scaling plugin and launcher. The optional HD pack uses the three reusable FLHDE asset packs; the full HD
Edition overhaul is not installed. See [credits and licensing](LICENSES.md)
and the [package guide](patch/README.txt) for details.

HD source versions and licenses: [hd-sources.json](patch/hd-sources.json).
The assets are bundled unchanged under their MIT notices, including attribution
to BC46 / C.S. Melis and Freestalker.fr. Texture loading/memory requirements are
higher; historical FPS measurements were taken before the HD packs were installed.
