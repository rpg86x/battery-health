$ErrorActionPreference='Stop'
$dest='D:\App Graveyard\Dev Dash'
if(Test-Path -LiteralPath $dest){throw 'Archief bestaat al'}
$sources=@(
 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\Dev Dash 1.4',
 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\Dev Dash 1.5',
 'C:\Users\Gebruiker\AppData\Local\Dev Dash',
 'C:\Users\Gebruiker\AppData\Local\Packages\OpenAI.Codex_2p2nqsd0c76g0\LocalCache\Local\Dev Dash'
)
$labels=@('project-1.4','project-1.5','userdata-local','userdata-codex')
$processes=@(Get-Process | Where-Object ProcessName -eq 'Dev Dash')
foreach($p in $processes){if($p.Path -notlike '*\Dev Dash*'){throw 'Onverwacht procespad'}; Stop-Process -Id $p.Id}
$manifest=@()
for($i=0;$i -lt $sources.Count;$i++){
 $src=(Resolve-Path -LiteralPath $sources[$i]).Path
 if($src -ne $sources[$i]){throw 'Onverwacht bronpad'}
 $items=@(Get-ChildItem -LiteralPath $src -Recurse -Force)
 if($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }){throw 'Reparse point gevonden'}
 New-Item -ItemType Directory -Path $dest -Force | Out-Null
 $target=Join-Path $dest $labels[$i]
 Copy-Item -LiteralPath $src -Destination $target -Recurse -Force
 foreach($f in ($items | Where-Object { !$_.PSIsContainer })){
  $copy=Join-Path $target ([IO.Path]::GetRelativePath($src,$f.FullName))
  $hash=(Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash
  if($hash -ne (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash){throw "Hashcontrole mislukt: $copy"}
  $manifest += [pscustomobject]@{Original=$f.FullName;Archive=$copy;SHA256=$hash;Bytes=$f.Length}
 }
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $dest 'archive-manifest.json') -Encoding utf8
Set-Content -LiteralPath (Join-Path $dest 'README.md') -Encoding utf8 -Value '# Dev Dash

Gearchiveerd op 22 oktober 2026: het idee was leuk, maar de gebruiker houdt dit overzicht voortaan bij in een spreadsheet in Google Drive. Dev Dash is een Windows-projectdashboard met versies, platformstatussen, scores, notities en lokaal zoeken naar nieuwe releases. De beschikbare broncode en builds van 1.4 en 1.5 staan in project-1.4 en project-1.5; de oude geïnstalleerde versies 1.0 t/m 1.5 en bestaande projectgegevens zijn bewaard in userdata-local en userdata-codex. Hervatten: kopieer project-1.5 naar een werkmap, lees LEESMIJ.md en bouw met source/build.ps1 (Windows .NET Framework vereist); persoonlijke gegevens staan in projects.json en projects.json.bak onder userdata-local. De oude versies en snelkoppelingen zijn van C: verwijderd. Er is geen Google-spreadsheet aangemaakt.'
$wsh=New-Object -ComObject WScript.Shell
$links=@()
foreach($folder in @('C:\Users\Gebruiker\Desktop','C:\Users\Public\Desktop','C:\CODEX ONE\Snelkoppelingen','C:\Users\Gebruiker\Documents\Codex\2026-10-05\referenced-chatgpt-conversation-this-is-an\outputs\Project-snelkoppelingen')){
 if(!(Test-Path -LiteralPath $folder)){continue}
 foreach($f in (Get-ChildItem -LiteralPath $folder -Filter '*.lnk' -Recurse -File)){
  $l=$wsh.CreateShortcut($f.FullName)
  if($f.Name -match '^Dev[ -]?Dash(?:\s+\d+(?:\.\d+)*)?\.lnk$' -or $l.TargetPath -match '\\Dev Dash(?: [\d.]+)?\\.*Dev Dash\.exe$'){
   $links += $f.FullName
  }
 }
}
$links | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dest 'removed-shortcuts.json') -Encoding utf8
foreach($src in $sources){
 $resolved=(Resolve-Path -LiteralPath $src).Path
 if($resolved -notin $sources){throw 'Verwijderpad ongeldig'}
 Remove-Item -LiteralPath $resolved -Recurse -Force
}
foreach($link in $links){Remove-Item -LiteralPath $link -Force}
foreach($src in $sources){if(Test-Path -LiteralPath $src){throw "Bron blijft aanwezig: $src"}}
foreach($link in $links){if(Test-Path -LiteralPath $link){throw "Snelkoppeling blijft aanwezig: $link"}}
Write-Output "Dev Dash gearchiveerd: $($manifest.Count) bestanden met SHA256 gecontroleerd; $($links.Count) snelkoppelingen verwijderd; 4 bron-/installatiemappen verwijderd."
