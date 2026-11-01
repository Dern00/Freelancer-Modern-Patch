$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$package=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
& $compiler /nologo /warnaserror /optimize+ /target:winexe /platform:anycpu /main:FreelancerModernSetup.Program /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "/win32manifest:$PSScriptRoot\Setup.manifest" "/out:$package\Freelancer-Setup.exe" "$PSScriptRoot\Installer.cs" "$PSScriptRoot\PatchEngine.cs" "$PSScriptRoot\SetupWindow.cs" "$PSScriptRoot\..\launcher\Launcher.cs"
if ($LASTEXITCODE -ne 0) { throw 'Setup compilation failed.' }
