# Changelog

## Freelancer Modern Patch v1.0

First release of the focused Windows 11, widescreen and high-FPS patch for
Freelancer (2003), preserving the original assets and single-player gameplay.

### Included
- Windows 11 launch compatibility and selected crash/save/exit fixes.
- Widescreen camera/HUD adjustments and aspect-preserving borderless scaling.
- Native in-game 60/120/144 FPS selection, applied live and saved between launches.
- Cinematic frame-limit fix; tested voice and animation synchronization.
- Standalone borderless launcher, portable saves/settings, source and rollback notes.
- Selected legacy single-player bug fixes for navigation, HUD/inventory display,
  trade-lane lighting, NPC names and flight-control state.

### Tested
Launch, docked save/reload, conversations, extended combat, trade lanes, nav map,
cockpit/turret views, Alt+Tab and resolution switching. Sampled flight: 119.98 FPS
at 120, 143.98 FPS at 144, and 143.99 FPS with 1080p scaled to a 1440p display.
The included test report states sample lengths, focus-loss qualifications and limits.

### Installation
Download **Freelancer-Modern-Patch-v1.0.zip**, extract it and follow **README.txt**.
Requires an unmodified English retail game installation; the full game is not
included. Install into a new copy, then use **Freelancer-Launcher.exe** to select
that copy's **EXE/Freelancer.exe**. Manual instructions are provided if PowerShell
scripts are blocked. **SHA256SUMS.txt** contains the archive checksum.

120 FPS is the default; 144 retains its EXP. label. Multiplayer, other languages,
modded installs, ultrawide monitors and legacy external saves remain untested.
The automatic GitHub source archives are development files, not the ready-to-use patch.
