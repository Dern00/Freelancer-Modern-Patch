param(
 [Parameter(Mandatory=$true)][string]$Source,
 [Parameter(Mandatory=$true)][string]$Destination
)
$ErrorActionPreference='Stop'
$sourcePath=(Resolve-Path -LiteralPath $Source).Path.TrimEnd('\')
$destinationPath=[IO.Path]::GetFullPath($Destination).TrimEnd('\')
if(Test-Path -LiteralPath $destinationPath){throw 'Choose a new, empty destination folder that does not exist.'}
if($destinationPath.StartsWith($sourcePath+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Destination must be outside the original game folder.'}
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
# Preflight all files before creating anything. This release supports the inspected English retail baseline.
foreach($entry in $manifest){
 $payload=Join-Path (Join-Path $PSScriptRoot 'payload') $entry.path
 if((Get-FileHash -LiteralPath $payload).Hash -ne $entry.sha256){throw "Patch integrity failed: $($entry.path)"}
 $original=Join-Path $sourcePath $entry.path
 if($entry.original_sha256){
  if(!(Test-Path -LiteralPath $original) -or (Get-FileHash -LiteralPath $original).Hash -ne $entry.original_sha256){throw "Unsupported or modified original: $($entry.path). Use an unmodified English retail installation."}
 } elseif(Test-Path -LiteralPath $original){throw "An existing patch file would conflict: $($entry.path)"}
}
New-Item -ItemType Directory -Path $destinationPath | Out-Null
Copy-Item -Path (Join-Path $sourcePath '*') -Destination $destinationPath -Recurse -Force
Copy-Item -Path (Join-Path $PSScriptRoot 'payload\*') -Destination $destinationPath -Recurse -Force
$savePath=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'My Games\Freelancer'
if(Test-Path -LiteralPath $savePath){Copy-Item -LiteralPath $savePath -Destination (Join-Path $destinationPath 'OriginalSavesBackup') -Recurse}
Write-Host "Created $destinationPath. Launch EXE\Freelancer.exe. Your source game is unchanged."
