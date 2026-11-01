param(
 [Parameter(Mandatory=$true)][string]$GamePath,
 [ValidateSet('Borderless','Fullscreen','Windowed')][string]$Mode='Borderless'
)
$ErrorActionPreference='Stop'
$exe=[IO.Path]::GetFullPath((Join-Path $GamePath 'EXE\Freelancer.exe'))
if(Get-Process Freelancer -ErrorAction SilentlyContinue | Where-Object Path -EQ $exe){throw 'Exit the game before changing display mode.'}
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$expected=@($manifest | Where-Object path -IEQ 'exe/Freelancer.exe')
if($expected.Count -ne 1){throw 'Missing executable manifest entry.'}
$bytes=[IO.File]::ReadAllBytes($exe)
$normalized=$bytes.Clone()
# Documented adoxa/Laz edits. Only the three recognized mode states are accepted.
$border=[BitConverter]::ToString($bytes[0x2477A..0x2477B])
$border2=[BitConverter]::ToString($bytes[0x2490D..0x2490E])
if($border -ne $border2 -or $border -notin @('CA-02','00-00') -or $bytes[0x1B16CC] -notin @(0,1)){throw 'Unrecognized display-mode bytes.'}
$normalized[0x2477A]=0xCA; $normalized[0x2477B]=2
$normalized[0x2490D]=0xCA; $normalized[0x2490E]=2
$normalized[0x1B16CC]=1
$hasher=[Security.Cryptography.SHA256]::Create()
try{$hash=[BitConverter]::ToString($hasher.ComputeHash($normalized)).Replace('-','')}finally{$hasher.Dispose()}
if($hash -ne $expected[0].sha256){throw 'Unsupported or modified executable; no changes made.'}
$bytes=$normalized
if($Mode -ne 'Fullscreen'){$bytes[0x1B16CC]=0}
if($Mode -eq 'Borderless'){
 $bytes[0x2477A]=0; $bytes[0x2477B]=0
 $bytes[0x2490D]=0; $bytes[0x2490E]=0
}
$backup=$exe+'.before-display-mode.bak'
if(!(Test-Path -LiteralPath $backup)){Copy-Item -LiteralPath $exe -Destination $backup}
[IO.File]::WriteAllBytes($exe,$bytes)
Write-Host "$Mode mode selected. Start the game normally; use native display resolution for full-screen borderless coverage."
