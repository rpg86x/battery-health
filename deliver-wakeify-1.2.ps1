$ErrorActionPreference = 'Stop'
$candidate = Join-Path $PSScriptRoot 'Wakeify\B'
$destination = 'C:\Users\Gebruiker\Documents\ChatGPT\Android Dev\Wakeify\B'
if (Test-Path -LiteralPath $destination) {
    $existing = Get-Content (Join-Path $destination 'release.json') | ConvertFrom-Json
    if ($existing.app -ne 'Wakeify' -or $existing.status -ne 'candidate' -or $existing.version -ne '1.2') { throw 'Unexpected destination; candidate update stopped.' }
}
New-Item -ItemType Directory -Force -Path "$destination\source","$destination\android" | Out-Null
foreach ($file in @('AndroidManifest.xml','build.ps1','README.md')) { Copy-Item -LiteralPath (Join-Path $candidate "source\$file") -Destination "$destination\source" }
foreach ($folder in @('src','res')) { Copy-Item -LiteralPath (Join-Path $candidate "source\$folder") -Destination "$destination\source" -Recurse -Force }
New-Item -ItemType Directory -Force -Path "$destination\source\tests" | Out-Null
Copy-Item -LiteralPath "$candidate\source\tests\WeeklyScheduleTest.java" -Destination "$destination\source\tests"
Copy-Item -LiteralPath "$candidate\android\Wakeify-1.2.apk" -Destination "$destination\android"
Copy-Item -LiteralPath "$candidate\release.json" -Destination $destination
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Wakeify\.gitignore') -Destination (Split-Path $destination -Parent)
Get-FileHash -LiteralPath "$destination\android\Wakeify-1.2.apk" -Algorithm SHA256
