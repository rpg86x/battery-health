$ErrorActionPreference='Stop'
$settingsPath='C:\Users\Gebruiker\.lmstudio\settings.json'
$newPath='D:\AI Models'
if ((Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json).downloadsFolder -ne $newPath) { throw 'Model move not completed' }
if (Test-Path -LiteralPath 'C:\Users\Gebruiker\.lmstudio\models') { throw 'Original models folder still exists; review move first' }
if (@(Get-CimInstance Win32_Process | Where-Object { $_.Name -match '^(LM Studio|llmster|llama-server).*\.exe$' }).Count) { throw 'LM Studio running; cannot safely update download records' }
$oldJsonPath='C:\\Users\\Gebruiker\\.lmstudio\\models'
$newJsonPath='D:\\AI Models'
$files=@('download-jobs-info.json','single-downloads-info.json')
$updated=@()
foreach ($name in $files) {
    $path=Join-Path 'C:\Users\Gebruiker\.lmstudio\.internal' $name
    $raw=[IO.File]::ReadAllText($path)
    if (-not $raw.Contains($oldJsonPath)) { continue }
    $next=$raw.Replace($oldJsonPath,$newJsonPath)
    $null=$next | ConvertFrom-Json
    Copy-Item -LiteralPath $path -Destination ($path+'.before-model-move-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.bak')
    [IO.File]::WriteAllText($path,$next,[Text.UTF8Encoding]::new($false))
    if ([IO.File]::ReadAllText($path).Contains($oldJsonPath)) { throw 'Old download references remain' }
    $updated += $name
}
[pscustomobject]@{ModelDirectory=$newPath;FilesOnD=@(Get-ChildItem -LiteralPath $newPath -File -Recurse -Force).Count;UpdatedDownloadRecords=$updated;FreeSpaceCGB=[math]::Round([IO.DriveInfo]::new('C:\').AvailableFreeSpace/1GB,2)} | ConvertTo-Json -Depth 3
