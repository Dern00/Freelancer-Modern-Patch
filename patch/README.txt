Freelancer Modern Patch v1.0
Release: v1.0. Focused single-player validation completed; see TestResults.txt.
Menu, opening-cinematic and controlled-flight samples measured 120 FPS; see
TestResults.txt for coverage and frame-time statistics.
The tested bar conversation now also averages 119.96 FPS, with user-confirmed
normal voice/animation synchronization after the cinematic-limit fix.

INSTALL
Extract this patch folder. Open PowerShell here and run:
  .\Install.ps1 -Source 'C:\Games\Freelancer' -Destination 'C:\Games\Freelancer-Modern'
Substitute your own paths. Destination must not already exist. The installer
checks original file hashes and refuses modified/incompatible installations.
This build supports the inspected English retail baseline, not arbitrary mods.
Run the new copy's EXE\Freelancer.exe normally, without compatibility settings.
No administrator rights or global Windows changes are required by this patch.

If PowerShell scripts are blocked, use this manual method without changing policy:
1. Start with an unmodified English retail installation. Do not overlay a modded copy.
2. Copy the entire game folder to a NEW folder, e.g. C:\Games\Freelancer-Modern.
3. Copy the CONTENTS of this archive's payload folder into that new game folder,
   merging DATA, EXE, DLLS and credits, and replace files in the new copy only.
4. Double-click Freelancer-Launcher.exe from this extracted patch folder and
   select the NEW copy's EXE\Freelancer.exe. Keep manifest.json beside the launcher.
The scripted installer validates all source/payload hashes before copying. Manual
copying does not perform that preflight; the launcher still checks the patched EXE.
Neither method imports old saves automatically. Back up any old saves separately.

BORDERLESS / FULLSCREEN
Double-click Freelancer-Launcher.exe and select the patched copy's
EXE\Freelancer.exe. It starts borderless and preserves your chosen FPS cap.
Keep the launcher beside manifest.json. It does not require PowerShell scripts
or administrator rights; startup errors are shown in a dialog.
For shortcuts, use Freelancer-Launcher.exe with these arguments:
  "C:\Games\Freelancer-Modern" Borderless
Use Fullscreen or Windowed instead of Borderless for the other modes.
Exit before changing modes. Source/build instructions are in source/launcher.
Start-Game.ps1 remains an advanced helper for environments that permit scripts.
Borderless scales the selected game resolution to fit the current monitor.
Matching aspect ratios fill the screen; different aspect ratios have centered
black bars so the image is not stretched out of proportion. Mouse coordinates
are mapped to the scaled image. Native resolution gives the sharpest result;
lower resolutions can look softer. Exclusive fullscreen and normal windowed
modes keep their original behavior.
The mode switch validates the complete executable, changes only three documented
window-mode locations, and retains a before-display-mode backup.
Existing FLSharp cursor and mouse-warp fixes apply in windowed modes.
The original gamma slider may not work in windowed modes; no color shader is added.
Fullscreen remains available for comparison. Borderless flight, Alt+Tab and
physical mouse behavior have been checked on the primary display.

DISPLAY AND FRAME CAP
The main display resolution is selected automatically on first launch.
Use the game's display options to select another resolution. JFLP adjusts
field of view/fonts and HUD Shift adjusts HUD position for the aspect ratio.
1440p is the primary target. Scaled 1920x1080 and pillarboxed 1600x1200 menu/input
checks passed on the primary display, along with Alt+Tab and restoring 1440p.
A scaled 1080p flight sample averaged 143.99 FPS with normal user-reported
controls. See TestResults.txt for capture limits. Ultrawide monitors are untested.
No HD textures, music replacements, balance changes, or extended draw distances.
Rendering currently uses native Direct3D 8, with no external wrapper.
In Options > General > Display, use the arrows beside FRAME LIMIT to choose
60, 120 or 144 FPS. Changes apply immediately and save for the next launch.
120 is the default. 144 passed measured flight and conversation samples on this
PC, plus user-reported extended combat, nav map, turret/cockpit and full
trade-lane checks. It retains the EXP. label pending broader validation.
The game's Default/Cancel buttons currently affect the stock settings only;
the custom frame-limit control saves each selection immediately.
All three choices have been measured in the menu (60.00/120.00/144.00 FPS).
The launcher preserves your menu choice. The optional PowerShell helper's
explicit -FPS argument can override it in environments that permit scripts.
flplusplus.ini starts with max_fps = 120 and vsync = false.
The command-line fallback remains available after exiting the game:
  .\Set-FPS.ps1 -GamePath 'C:\Games\Freelancer-Modern' -FPS 60
Repeat with 120 or 144 for the other caps.
Driver/RTSS overrides may supersede these settings. Do not change global settings.

SAVES AND ROLLBACK
The patched copy stores settings and saves in its SAVE folder, keeping the
original Documents profile separate. Existing Documents saves are backed up
to OriginalSavesBackup by the installer. They are not automatically imported.
To roll back, close this copy and launch the original installation.
Keep the patched SAVE folder if you want to retain new progress.

TEST CHECKLIST
1. Launch twice and exit normally. Confirm 2560x1440 on the intended monitor.
2. New Game: watch the opening cinematic; check image proportions and dialogue.
3. Check Manhattan bar/equipment/ship menus, text, and clickable buttons.
4. Play mission 1 through the first flight/combat and trade lane. Check nav map,
   targeting, cockpit/third-person camera, mouse flight, and Alt+Tab.
5. Save, exit, relaunch, and reload that save. Existing-save coverage requires
   a compatible original save; none was found during initial inspection.
6. Repeat the same segment at 60 and 120 FPS. Compare timing, movement, firing,
   camera behavior, cinematic dialogue and recurring stutter. Record FPS and
   frame times with your existing monitor overlay/capture tool.
7. Briefly check another widescreen resolution. 144 FPS testing is secondary.

COMPONENTS / CREDITS
Microsoft official 1.1 update: downloaded from https://r.je/freelancer/flpatch.exe
Microsoft Authenticode signature verified before extracting its files.
Compatible base executable: FLHDE/OriginalFLBinaries, EXE/Freelancer.exe.
Base SHA1: 659be06fbbffd0b6cef48824471b94447ae5f65b.
Checked binary edit at 0x1B2D72 (68 80 -> EB 0F) uses EXE-local EBUEula.dll
instead of loading the root copy, as documented by FLHDE/BC46.
Checked edit at 0x02D7B8 (6A 01 -> EB DD) removes an extra Sleep(1) delay from
the frame-time calculation. The first borderless menu measurement without this
fix averaged 105.99 FPS with a 120 cap (15 seconds, PresentMon 2.6.0).
Checked edit at 0x158A10 (BC -> C0) removes the separate cinematic frame limit.
The tested bar conversations improved from approximately 30 to 120 FPS.
Credit: BC46 and FriendlyFire, documented in FLHDE BinaryEdits.patch.
This does not increase the frame rate of pre-rendered video files.
https://github.com/FLHDE/freelancer-hd-edition/blob/main/FLDOCS/BinaryEdits.patch
Jason Hood: JFLP DLL 1.27, HUD Shift (2.12 file version; 2.12.1 documentation),
automatic cameras and HUD configuration. Optional strafe/stat features disabled.
https://adoxa.altervista.org/freelancer/
FLSharp 0.9.2, BC46: selected camera, cursor, menu and crash/exit fixes only.
https://github.com/BC46/FLSharp/releases/tag/v0.9.2
flplusplus, FLHDE build: frame cap, VSync control and portable save location.
https://github.com/FLHDE/flplusplus
Bundled licenses and original plugin notes are included in payload.
Exact downloaded build identifiers and SHA256 hashes are in sources.json and
manifest.json. No full HD Edition package or broad JFLP data overhaul is applied.
FpsMenu.dll: custom native Options selector, with source and build script in
source/native-fps-menu. Native UI references: TheStarport/FLUF and BC46/FLSharp;
limiter locations: FLHDE/flplusplus. Source is supplied under AGPL-3.0-or-later.
FpsMenu changes only its own controls and the existing limiter's cap values.
It does not add a renderer, overlay, gameplay hotkey, or second frame limiter.
BorderlessScale.dll: custom borderless window fitting and mouse-coordinate
mapping. Source, build script, checks and MIT license are in source/native-borderless.
It uses native Direct3D 8 presentation scaling and a black backdrop when needed.
To restore unscaled borderless behavior, close the game and remove the
BorderlessScale.dll line from EXE/dacom.ini. No saved games need to be changed.

KNOWN LIMITS
The observed retail failure was an administrator-privileges error before launch.
The working copy now launches, saves/loads, exits, and presents tested flight
and combat at 120 FPS at 2560x1440. The user reported normal movement, turning,
firing and dialogue timing in the practical 60/120 comparison.
This release has focused gameplay coverage, not complete campaign certification.
First-run license dialog may appear. DirectPlay DLLs exist on the test PC;
the Windows feature state could not be confirmed without OS elevation.
No Windows components, driver settings, or security settings were changed.
Other languages, pre-modded installs, ultrawide, and multiplayer are untested.
