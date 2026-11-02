$ErrorActionPreference = 'Stop'
$grave = 'D:\App Graveyard'
$devRoot = 'C:\Users\Gebruiker\Documents\Codex\2026-10-05\kunnen-we-een-simpel-windows-applicatie'
$gitRoot = 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\Git Sync'
$jobs = @()
$jobs += [pscustomobject]@{ Source = (Join-Path $devRoot 'outputs\Dev Log'); Dest = (Join-Path $grave 'Dev Log\outputs\Dev Log') }
foreach ($name in @('deliver-dev-log.ps1','DevLogTests.cs','DevLogTests.exe','dev-log-test.csv','dev-log-test.xml','dev-log-test.xml.bak')) {
  $jobs += [pscustomobject]@{ Source = (Join-Path $devRoot "work\$name"); Dest = (Join-Path $grave "Dev Log\work\$name") }
}
$jobs += [pscustomobject]@{ Source = $gitRoot; Dest = (Join-Path $grave 'Git Sync\project') }
$gitData = Join-Path $env:LOCALAPPDATA 'Git Sync'
if (Test-Path -LiteralPath $gitData) { $jobs += [pscustomobject]@{ Source = $gitData; Dest = (Join-Path $grave 'Git Sync\userdata') } }
foreach ($name in @('Dev Log','Git Sync')) {
  if (Test-Path -LiteralPath (Join-Path $grave $name)) { throw "Archief bestaat al: $name" }
}
$running = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -in @('Dev Log','Git Sync','GitSync','DevLog') }
if ($running) { throw 'Dev Log of Git Sync is nog actief; sluit de app eerst.' }
$manifest = @()
foreach ($job in $jobs) {
  $src = Get-Item -LiteralPath $job.Source
  if ($src.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse point niet toegestaan' }
  $files = if ($src.PSIsContainer) { @(Get-ChildItem -LiteralPath $src.FullName -Recurse -Force -File) } else { @($src) }
  if ($src.PSIsContainer -and (Get-ChildItem -LiteralPath $src.FullName -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint })) { throw 'Reparse point in bron' }
  New-Item -ItemType Directory -Path (Split-Path $job.Dest -Parent) -Force | Out-Null
  Copy-Item -LiteralPath $job.Source -Destination $job.Dest -Recurse -Force
  foreach ($file in $files) {
    $target = if ($src.PSIsContainer) { Join-Path $job.Dest ([IO.Path]::GetRelativePath($src.FullName,$file.FullName)) } else { $job.Dest }
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    if ($hash -ne (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash) { throw "Verificatie mislukt: $target" }
    $manifest += [pscustomobject]@{ Original=$file.FullName; Archive=$target; SHA256=$hash; Bytes=$file.Length }
  }
}
Set-Content -LiteralPath (Join-Path $grave 'Dev Log\README.md') -Encoding utf8 -Value '# Dev Log

Gearchiveerd op 22 oktober 2026 wegens lage prioriteit. Dev Log 1.0 is een lokaal Windows-releaselogboek met bewerkbare appversies, kleurstatussen, doelversies, zoeken en CSV-export. Broncode, programma, pictogram, bestaande XML-logboekgegevens en bouw-/testbestanden zijn bewaard in outputs en work. Hervatten: kopieer deze map naar een werkmap; bouwen kan met work\deliver-dev-log.ps1 vanuit die werkmap (vereist Windows .NET Framework 4.x; het script start de app en maakt een bureaubladsnelkoppeling). De oorspronkelijke locatie was Documents\Codex\2026-10-05\kunnen-we-een-simpel-windows-applicatie; overige apps in die gedeelde projectmap zijn behouden.'
Set-Content -LiteralPath (Join-Path $grave 'Git Sync\README.md') -Encoding utf8 -Value '# Git Sync

Gearchiveerd op 22 oktober 2026: het idee is leuk, maar de gebruiker kan met een simpele prompt aangeven wat naar GitHub mag en vindt dat deze app momenteel weinig toevoegt. Git Sync 0.1-dev (B, niet goedgekeurd) is een NL/EN Windows-hulpprogramma om projectversies en goedkeuringen bij te houden en goedgekeurde Git-inhoud te committen en pushen. De volledige projectmap staat in project; eventuele lokale instellingen staan in userdata. Hervatten: kopieer project naar een werkmap, lees B\source\README.md en bouw met B\source\build.ps1 (Windows .NET Framework 4.x en Git vereist); zet eventuele userdata terug naar %LOCALAPPDATA%\Git Sync. Online repositories en releases zijn niet gewijzigd.'
foreach ($name in @('Dev Log','Git Sync')) {
  $manifest | Where-Object { $_.Archive.StartsWith((Join-Path $grave $name) + '\',[StringComparison]::OrdinalIgnoreCase) } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $grave "$name\archive-manifest.json") -Encoding utf8
}
foreach ($job in $jobs) {
  $resolved = (Resolve-Path -LiteralPath $job.Source).Path
  if (!($resolved.StartsWith($devRoot + '\',[StringComparison]::OrdinalIgnoreCase) -or $resolved -eq $gitRoot -or $resolved -eq $gitData)) { throw "Onverwacht verwijderpad: $resolved" }
  Remove-Item -LiteralPath $resolved -Recurse -Force
}
Write-Output "Gearchiveerd en SHA256 gecontroleerd: $($manifest.Count) bestanden."
Get-ChildItem -LiteralPath $grave -Directory | Select-Object Name,FullName
