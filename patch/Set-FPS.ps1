param([Parameter(Mandatory=$true)][string]$GamePath,[ValidateSet(60,120,144)][int]$FPS=120)
$ErrorActionPreference='Stop'
$path=Join-Path $GamePath 'EXE\flplusplus.ini'
if(!(Test-Path -LiteralPath $path)){throw 'Select the patched game folder.'}
$fullExe=[IO.Path]::GetFullPath((Join-Path $GamePath 'EXE\Freelancer.exe'))
if(Get-Process Freelancer -ErrorAction SilentlyContinue | Where-Object Path -EQ $fullExe){throw 'Exit the game before changing the cap.'}
$text=Get-Content -LiteralPath $path -Raw
if(([regex]::Matches($text,'(?m)^max_fps\s*=')).Count -ne 1){throw 'Unexpected frame-cap configuration.'}
$text=[regex]::Replace($text,'(?m)^max_fps[^\S\r\n]*=[^\r\n]*',"max_fps = $FPS")
[IO.File]::WriteAllText($path,$text,[Text.Encoding]::ASCII)
Write-Host "Frame cap set to $FPS. VSync remains off in the game plugin."
