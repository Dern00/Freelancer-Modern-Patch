$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$package=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
& $compiler /nologo /warnaserror /optimize+ /target:winexe /platform:anycpu /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/win32manifest:$PSScriptRoot\Launcher.manifest" "/out:$package\Freelancer-Launcher.exe" "$PSScriptRoot\Launcher.cs"
if ($LASTEXITCODE -ne 0) { throw 'Launcher compilation failed.' }
