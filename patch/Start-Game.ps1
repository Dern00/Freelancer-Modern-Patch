param(
 [Parameter(Mandatory=$true)][string]$GamePath,
 [ValidateSet('Borderless','Fullscreen','Windowed')][string]$Mode='Borderless',
 [ValidateSet(60,120,144)][int]$FPS=120
)
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'Set-DisplayMode.ps1') -GamePath $GamePath -Mode $Mode
# Preserve a cap selected inside Options unless the caller explicitly overrides it.
if ($PSBoundParameters.ContainsKey('FPS')) {
 & (Join-Path $PSScriptRoot 'Set-FPS.ps1') -GamePath $GamePath -FPS $FPS
}
$exe=Join-Path $GamePath 'EXE\Freelancer.exe'
$oldLayer=$env:__COMPAT_LAYER
try{
 # Process-local DPI awareness prevents Windows scaling the window/mouse coordinates.
 $env:__COMPAT_LAYER='HIGHDPIAWARE'
 Start-Process -FilePath $exe -WorkingDirectory (Split-Path -Parent $exe) -WindowStyle Normal
}finally{$env:__COMPAT_LAYER=$oldLayer}
