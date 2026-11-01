$ErrorActionPreference='Stop'
$vs = & 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe' -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (!$vs) { throw 'Visual Studio C++ tools were not found.' }
$vc = (Get-ChildItem -LiteralPath (Join-Path $vs 'VC\Tools\MSVC') -Directory | Sort-Object Name -Descending | Select-Object -First 1).FullName
$sdk='C:\Program Files (x86)\Windows Kits\10'
$version=(Get-ChildItem -LiteralPath (Join-Path $sdk 'Include') -Directory | Sort-Object Name -Descending | Select-Object -First 1).Name
$env:INCLUDE="$vc\include;$sdk\Include\$version\ucrt;$sdk\Include\$version\shared;$sdk\Include\$version\um"
$env:LIB="$vc\lib\x86;$sdk\Lib\$version\ucrt\x86;$sdk\Lib\$version\um\x86"
New-Item -ItemType Directory -Force (Join-Path $PSScriptRoot 'build') | Out-Null
Push-Location $PSScriptRoot
try {
 & "$vc\bin\Hostx64\x86\cl.exe" /nologo /W4 /WX /O2 /MT /EHsc /LD BorderlessScale.cpp /Fo:build\BorderlessScale.obj /link /OUT:build\BorderlessScale.dll /MACHINE:X86 /DYNAMICBASE /NXCOMPAT
 if ($LASTEXITCODE -ne 0) { throw 'Borderless scaling build failed.' }
 & "$vc\bin\Hostx64\x86\cl.exe" /nologo /W4 /WX /O2 /MT /EHsc ScaleChecks.cpp /Fo:build\ScaleChecks.obj /Fe:build\ScaleChecks.exe
 if ($LASTEXITCODE -ne 0) { throw 'Scaling checks build failed.' }
 & '.\build\ScaleChecks.exe'
 if ($LASTEXITCODE -ne 0) { throw 'Scaling checks failed.' }
} finally { Pop-Location }

