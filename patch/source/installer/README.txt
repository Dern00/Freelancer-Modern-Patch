Freelancer Modern Patch installer and undo tool

Install.cmd opens Freelancer-Setup.exe, a native .NET Framework Windows Forms app.
Choose an unmodified English retail game folder and click Install. It patches that
folder directly. Saves are untouched. No PowerShell policy changes are needed.
Before starting Install or either Undo operation, the GUI checks write access.
Protected folders trigger Windows administrator consent and resume the selected
operation with the same game path and HD choice. Cancelling leaves setup open
without starting the operation. Already-elevated access denial stops with advice.
No game-folder ACLs or system security settings are changed. Both manifests stay
asInvoker so writable folders do not require administrator permission.
Build.ps1 compiles Installer.cs, PatchEngine.cs and SetupWindow.cs using the
Windows .NET Framework C# compiler; no external package dependencies are required.
It also compiles the existing launcher source to share its complete EXE validation.

HD textures are optional. The full HD download contains hd-payload and a separate
hd-manifest.json. The checkbox defaults on when that payload is present. A clean
game receives the core patch first; a recognized Modern Patch installation can
add only HD textures without resetting settings. Recognition validates the full
normalized game EXE and the core DLL hashes. Arbitrary modded copies are rejected.
Textures have their own ModernTexturesBackup and Undo HD Textures.cmd. Full Undo
restores HD first. On older installations, the HD installer redirects the existing
core Undo shortcut through the new helper, restoring that shortcut on HD-only Undo.
The older core setup binary is not modified. The texture-only launcher/undo helper
lives in ModernTextures and remains after Undo, alongside retained backups.

Before modifying files, setup checks all payload hashes and supported originals,
prepares verified backups and records every change in ModernPatchBackup/state.json.
The original-file backup is verified before Undo writes anything. Undo preserves
current patch files/settings in removed-patch-files, restores originals and removes
added game files. It leaves saves, backups, and the small ModernPatch setup/undo
tools. A repeated install after Undo archives the previous backup automatically.
An ordinary install exception triggers restoration. An interrupted restore can be
retried. Users should retain ModernPatchBackup until they no longer need Undo.

Folders containing links/junctions are rejected. Game paths must use ASCII; an
accented path reproduced an immediate game exit. Spaces work. The extracted
package path can be Unicode. Manifest hashes verify integrity, not publisher
identity. Original-file checks are not a complete scan for every possible mod.

InstallerChecks.cs tests preflight, backups, undo, preservation and rollback.
Compile with Installer.cs and PatchEngine.cs, /main:InstallerChecks /target:exe,
plus ../launcher/Launcher.cs,
and references System.Windows.Forms.dll, System.Drawing.dll and
System.Web.Extensions.dll. Pass an existing scratch folder to the test EXE.
Noninteractive integration modes use the exact same engine as the GUI:
  Freelancer-Setup.exe --install-check "game folder" "result text file"
  Freelancer-Setup.exe --undo-check "game folder" "result text file"
  Freelancer-Setup.exe --install-hd-check "game folder" "result text file"
  Freelancer-Setup.exe --undo-hd-check "game folder" "result text file"
TextureChecks.cs uses /main:TextureChecks with the same sources/references; pass
the assembled patch folder and an existing scratch folder to its executable.
DetectionChecks.cs uses /main:DetectionChecks; pass the patch folder, a supported
retail game folder and a scratch folder. It verifies retail detection, supported
display modes and rejection of modified, truncated or missing game files.
The GUI and --selection-check / --selection-hd-check share InstallSelection, so
integration checks exercise the same detection-and-install route as the UI.
Installer hotfix (2026-11-01): treat InvalidDataException from patched-EXE
validation as a negative detection result. .NET Framework does not derive this
exception from IOException. The full retail/payload hash checks still apply.
Exit codes: 0 success, 1 failure. --undo "game folder" opens the Undo interface.

MIT License

Copyright (c) 2026 Freelancer Modern Patch contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to
do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
