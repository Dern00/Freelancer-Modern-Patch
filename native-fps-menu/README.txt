Native Freelancer FPS menu - v1.0

Adds 60, 120 and 144 FPS choices inside General Options, below Gamma.
120 is the primary target. 144 is an experimental option.
Uses the game's native controls and existing flplusplus frame limiter.
No renderer replacement, overlay, hotkey, or second limiter is required.

Build with .\build.ps1 from Windows PowerShell and Visual Studio 2022 x86 C++ tools.
Load FpsMenu.dll after FLSharp.dll and flplusplus.dll in EXE\dacom.ini.
The DLL refuses to install its hooks if known native UI signatures or the
expected FLSharp/flplusplus hook state differ. English compatible base only.
The installer must continue checking complete executable/component hashes.

Selections currently apply immediately and are saved to EXE\flplusplus.ini.
Start-Game.ps1 preserves that value unless -FPS is explicitly supplied.
The stock Default/Cancel buttons currently affect stock settings only.
Remove FpsMenu.dll from dacom.ini after exiting to roll back the menu addition.
The existing configured cap still works without this menu plugin.

Main-menu layout and all three caps were checked at 2560x1440. Fifteen-second
captures measured 60.00, 120.00 and 144.00 FPS after user selection. Gameplay
coverage, in-game Options and persistence checks are recorded in TestResults.txt.

References and credits:
- TheStarport/FLUF: native control constructors, layouts and button execute ABI.
  https://github.com/TheStarport/FLUF/tree/master/modules/FLUF.UI
- BC46/FLSharp 0.9.2: NN_Preferences initialization and resolution hook chaining.
  https://github.com/BC46/FLSharp/tree/v0.9.2
- FLHDE/flplusplus: the existing frame cap and its three runtime locations.
  https://github.com/FLHDE/flplusplus
- Original control/render behavior checked against the supplied compatible
  Freelancer executable, SHA1 659be06fbbffd0b6cef48824471b94447ae5f65b.

Custom source is provided under GNU AGPL version 3 or later; see LICENSE.txt.
This is an independent small plugin; FLUF is not installed as a dependency.
