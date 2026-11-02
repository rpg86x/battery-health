$ErrorActionPreference='Stop'
$source='C:\Users\Gebruiker\.lmstudio\models'
$destination='D:\AI Models'
$settingsPath='C:\Users\Gebruiker\.lmstudio\settings.json'
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$manifestPath="D:\LM Studio model move $stamp.json"
if ([IO.Path]::GetFullPath($source) -ne 'C:\Users\Gebruiker\.lmstudio\models' -or [IO.Path]::GetFullPath($destination) -ne 'D:\AI Models') { throw 'Invalid move paths' }
if (Test-Path -LiteralPath $destination) { throw 'Destination already exists; separate review needed' }
if (@(Get-CimInstance Win32_Process | Where-Object { $_.Name -match '^(LM Studio|llmster|llama-server).*\.exe$' }).Count) { throw 'LM Studio is running; close it before moving models' }
$items=@(Get-Item -LiteralPath $source)+@(Get-ChildItem -LiteralPath $source -Recurse -Force)
if (@($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Linked files require separate review' }
$files=@($items | Where-Object { -not $_.PSIsContainer })
$bytes=($files | Measure-Object Length -Sum).Sum
if ([IO.DriveInfo]::new('D:\').AvailableFreeSpace -lt ($bytes+100MB)) { throw 'Insufficient free space on D:' }
$settingsRaw=[IO.File]::ReadAllText($settingsPath)
$settings=$settingsRaw | ConvertFrom-Json
if ($settings.downloadsFolder -ne $source) { throw 'LM Studio model location changed since inspection' }
$records=@(foreach ($file in $files) { [pscustomobject]@{RelativePath=$file.FullName.Substring($source.Length+1);Bytes=$file.Length;LastWriteUtc=$file.LastWriteTimeUtc;SHA256=$null} })
Write-Output ("Copying $($files.Count) files; $([math]::Round($bytes/1GB,2)) GB")
& robocopy.exe $source $destination /E /J /MT:2 /R:1 /W:1 /XJ /NP /NDL /NJH
if ($LASTEXITCODE -ge 8) { throw "Copy failed: $LASTEXITCODE; source preserved" }
$copied=@(Get-ChildItem -LiteralPath $destination -File -Recurse -Force)
if ($files.Count -ne $copied.Count) { throw 'Copied file count mismatch; source preserved' }
foreach ($record in $records) {
    $from=Join-Path $source $record.RelativePath
    $to=Join-Path $destination $record.RelativePath
    Write-Output ('Verifying '+$record.RelativePath)
    $original=Get-Item -LiteralPath $from
    if ($original.Length -ne $record.Bytes -or $original.LastWriteTimeUtc -ne $record.LastWriteUtc) { throw 'Source changed during copying' }
    $fromHash=(Get-FileHash -LiteralPath $from -Algorithm SHA256).Hash
    if ($fromHash -ne (Get-FileHash -LiteralPath $to -Algorithm SHA256).Hash) { throw "Hash mismatch: $($record.RelativePath); source preserved" }
    $record.SHA256=$fromHash
}
if (@(Get-CimInstance Win32_Process | Where-Object { $_.Name -match '^(LM Studio|llmster|llama-server).*\.exe$' }).Count) { throw 'LM Studio started during the move; source preserved' }
if ([IO.File]::ReadAllText($settingsPath) -ne $settingsRaw) { throw 'Settings changed during the move; source preserved' }
$settingsBackup="C:\Users\Gebruiker\.lmstudio\settings.before-model-move-$stamp.json"
Copy-Item -LiteralPath $settingsPath -Destination $settingsBackup
$pattern='("downloadsFolder"\s*:\s*)"[^"\r\n]*"'
if ([regex]::Matches($settingsRaw,$pattern).Count -ne 1) { throw 'Unexpected settings format' }
$replacement='$1'+(ConvertTo-Json -InputObject $destination -Compress)
$newSettings=[regex]::Replace($settingsRaw,$pattern,$replacement)
if (($newSettings | ConvertFrom-Json).downloadsFolder -ne $destination) { throw 'Settings validation failed' }
[IO.File]::WriteAllText($settingsPath,$newSettings,[Text.UTF8Encoding]::new($false))
if ((Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json).downloadsFolder -ne $destination) { throw 'Saved settings validation failed; source preserved' }
[pscustomobject]@{Source=$source;Destination=$destination;Files=$records;Bytes=$bytes;Verified='SHA256';SettingsBackup=$settingsBackup;SourceRemoved=$false} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
if ([IO.Path]::GetFullPath($source) -ne 'C:\Users\Gebruiker\.lmstudio\models') { throw 'Unsafe removal path' }
foreach ($record in $records) {
    $from=Join-Path $source $record.RelativePath
    $original=Get-Item -LiteralPath $from
    if ($original.Length -ne $record.Bytes -or $original.LastWriteTimeUtc -ne $record.LastWriteUtc) { throw 'Source changed before removal; remaining source preserved' }
    Remove-Item -LiteralPath $from -Force
}
# Remove only empty source folders; never recursively remove newly added files.
Get-ChildItem -LiteralPath $source -Directory -Recurse -Force | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object { if (@(Get-ChildItem -LiteralPath $_.FullName -Force).Count -eq 0) { [IO.Directory]::Delete($_.FullName) } }
if (@(Get-ChildItem -LiteralPath $source -Force).Count -eq 0) { [IO.Directory]::Delete($source) }
[pscustomobject]@{Source=$source;Destination=$destination;Files=$records;Bytes=$bytes;Verified='SHA256';SettingsBackup=$settingsBackup;SourceRemoved=(-not (Test-Path -LiteralPath $source))} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
[pscustomobject]@{MovedFiles=$records.Count;MovedGB=[math]::Round($bytes/1GB,2);Destination=$destination;SettingsModelsPath=(Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json).downloadsFolder;FreeSpaceCGB=[math]::Round([IO.DriveInfo]::new('C:\').AvailableFreeSpace/1GB,2);Manifest=$manifestPath} | ConvertTo-Json
