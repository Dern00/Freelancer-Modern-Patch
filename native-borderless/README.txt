BorderlessScale: local native Direct3D 8 presentation scaling

Purpose
In borderless mode the selected game resolution remains the rendering resolution,
while the game window fits the current monitor. Matching aspect ratios fill the
display; different aspect ratios are centered with black bars. Native Direct3D 8
performs the presentation stretch; no replacement renderer or frame limiter is used.
Lower rendering resolutions can look softer than the monitor's native resolution.

Implementation
Loaded after FLSharp and FpsMenu through dacom.ini. The checked main-window routine
and original USER32 imports are validated before installing import-table hooks.
Only Freelancer.exe and FLSharp.dll imports are changed; system DLLs are untouched.
The window is physically fitted to its monitor while game-visible client dimensions
remain at the selected resolution. Screen/client mouse conversions are mapped both
ways so FLSharp's cursor and reacquisition fixes use the same coordinates as the UI.
WM_SIZE carries the logical rendering dimensions to the original window procedure.
An inactive tool window supplies black bars when needed and hides on focus loss.
Normal windowed and exclusive fullscreen modes use the original behavior.
No saves, gameplay rules, assets, global display settings or FPS limits are changed.

Build
Requires Visual Studio C++ x86 tools and a Windows SDK. Run build.ps1 in a PowerShell
environment that permits scripts. It compiles the DLL with warnings as errors and
runs ScaleChecks.exe for aspect-fit and mouse-coordinate boundary/round-trip checks.
Runtime/manual coverage is recorded in the package's TestResults.txt.

Install/rollback
Place build/BorderlessScale.dll in EXE and add BorderlessScale.dll after FpsMenu.dll
in dacom.ini [Libraries], with the game closed. Remove that entry to restore the
previous unscaled borderless behavior. BorderlessScale.log records activation and
render/client dimensions. Unsupported imports or signatures disable the plugin.
This build targets only the checked English executable and FLSharp combination.

References
BC46/FLSharp 0.9.2, src/mouse.cpp and src/resolutions.cpp (MIT):
https://github.com/BC46/FLSharp/tree/v0.9.2
Archived Microsoft DirectX 8.1 SDK, IDirect3DDevice8::Present:
https://alpyne.me/archive/dx8/directx_cpp/graphics/reference/cpp/d3d/interfaces/idirect3ddevice8/present.htm

License: MIT; see LICENSE.txt. Source and build instructions are supplied.
