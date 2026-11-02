$ErrorActionPreference='Stop'
$dest='D:\App Graveyard\Dev Dash'
$manifest=Get-Content -LiteralPath (Join-Path $dest 'archive-manifest.json') -Raw | ConvertFrom-Json
foreach($f in $manifest){if((Get-FileHash -LiteralPath $f.Archive -Algorithm SHA256).Hash -ne $f.SHA256){throw 'Archiefcontrole mislukt'}}
$links=@(Get-Content -LiteralPath (Join-Path $dest 'removed-shortcuts.json') -Raw | ConvertFrom-Json)
foreach($dataRoot in @('C:\Users\Gebruiker\AppData\Local\Dev Dash','C:\Users\Gebruiker\AppData\Local\Packages\OpenAI.Codex_2p2nqsd0c76g0\LocalCache\Local\Dev Dash')){
 if(!(Test-Path -LiteralPath $dataRoot)){continue}
 $resolved=(Resolve-Path -LiteralPath $dataRoot).Path
 if($resolved -ne $dataRoot){throw 'Onverwacht gegevenspad'}
 foreach($file in (Get-ChildItem -LiteralPath $resolved -Recurse -Force -File)){
  $rel=[IO.Path]::GetRelativePath($resolved,$file.FullName)
  $hash=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
  $matched=$false
  foreach($label in @('userdata-local','userdata-codex')){
   $copy=Join-Path (Join-Path $dest $label) $rel
   if((Test-Path -LiteralPath $copy) -and (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash -eq $hash){$matched=$true}
  }
  if(!$matched){throw "Niet gearchiveerd bestand: $($file.FullName)"}
 }
 Remove-Item -LiteralPath $resolved -Recurse -Force
}
foreach($link in $links){
 if([IO.Path]::GetFileName($link) -notmatch '^Dev[ -]?Dash(?:\s+\d+(?:\.\d+)*)?\.lnk$'){throw 'Onverwachte snelkoppeling'}
 if(Test-Path -LiteralPath $link){Remove-Item -LiteralPath $link -Force}
}
foreach($p in @('C:\Users\Gebruiker\Documents\ChatGPT\Apps\Dev Dash 1.4','C:\Users\Gebruiker\Documents\ChatGPT\Apps\Dev Dash 1.5','C:\Users\Gebruiker\AppData\Local\Dev Dash','C:\Users\Gebruiker\AppData\Local\Packages\OpenAI.Codex_2p2nqsd0c76g0\LocalCache\Local\Dev Dash')){if(Test-Path -LiteralPath $p){throw "Map nog aanwezig: $p"}}
foreach($link in $links){if(Test-Path -LiteralPath $link){throw 'Snelkoppeling nog aanwezig'}}
Write-Output "Voltooid: $($manifest.Count) archiefbestanden gecontroleerd, $($links.Count) snelkoppelingen verwijderd, alle oorspronkelijke Dev Dash-mappen weg."
