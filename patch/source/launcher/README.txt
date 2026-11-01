Freelancer launcher

Freelancer-Launcher.exe "patched game folder" [Borderless|Fullscreen|Windowed]
With no arguments, the launcher asks you to select the patched EXE\Freelancer.exe.
Keep the launcher beside the package's manifest.json.

This is a Windows .NET Framework executable, not a PowerShell script. It does
not require script execution to be enabled or administrator rights. It preserves
the FPS choice saved in the game menu and applies process-local DPI awareness.
Errors appear in a dialog and are written to Launcher.log when the folder permits.

The whole executable is hash-checked after normalizing the three documented
display-mode offsets. Only those offsets can change, with a backup retained.
An already-selected mode avoids rewriting the game executable.

Build.ps1 compiles with the .NET Framework C# compiler included on this PC.
LauncherChecks.cs tests mode round-trips and rejection of incompatible files.
To compile the checks, include Launcher.cs and LauncherChecks.cs, set
/main:LauncherChecks /target:exe, and reference System.Windows.Forms.dll and
System.Web.Extensions.dll. Supply patched and original game executable paths.

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
