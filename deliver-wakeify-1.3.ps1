$ErrorActionPreference = 'Stop'
$candidate = Join-Path $PSScriptRoot 'Wakeify-1.3\B'
$destination = 'C:\Users\Gebruiker\Documents\ChatGPT\Android Dev\Wakeify 1.3\B'
if (Test-Path -LiteralPath $destination) { throw 'Destination exists; inspect before overwriting.' }
New-Item -ItemType Directory -Force -Path "$destination\source","$destination\android","$destination\source\tests" | Out-Null
foreach ($file in @('AndroidManifest.xml','build.ps1','README.md')) { Copy-Item -LiteralPath (Join-Path $candidate "source\$file") -Destination "$destination\source" }
foreach ($folder in @('src','res')) { Copy-Item -LiteralPath (Join-Path $candidate "source\$folder") -Destination "$destination\source" -Recurse }
foreach ($file in @('WeeklyScheduleTest.java','WakeTimelineTest.java')) { Copy-Item -LiteralPath (Join-Path $candidate "source\tests\$file") -Destination "$destination\source\tests" }
Copy-Item -LiteralPath "$candidate\android\Wakeify-1.3.apk" -Destination "$destination\android"
Copy-Item -LiteralPath "$candidate\release.json" -Destination $destination
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Wakeify-1.3\.gitignore') -Destination (Split-Path $destination -Parent)
Get-FileHash -LiteralPath "$destination\android\Wakeify-1.3.apk" -Algorithm SHA256
