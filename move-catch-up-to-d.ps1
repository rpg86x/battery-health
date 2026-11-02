$ErrorActionPreference = 'Stop'
$source = 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\Catch Up'
$destination = 'D:\Apps\Catch Up'
if ([IO.Path]::GetFullPath($source) -ne 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\Catch Up') { throw 'Invalid source' }
if ([IO.Path]::GetFullPath($destination) -ne 'D:\Apps\Catch Up') { throw 'Invalid destination' }
if ((Get-Item -LiteralPath $source).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Source is already a link' }
$items = @(Get-ChildItem -LiteralPath $source -Recurse -Force)
$links = @($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint })
foreach ($link in $links) {
    if ($link.LinkType -ne 'Junction' -or -not ([IO.Path]::GetFullPath($link.Target)).StartsWith($source + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Unsupported link: $($link.FullName)" }
}
$running = @(Get-Process | Where-Object { try { $_.Path.StartsWith($source + '\', [StringComparison]::OrdinalIgnoreCase) } catch { $false } })
if ($running.Count) { throw 'Catch Up is running' }
$files = @($items | Where-Object { -not $_.PSIsContainer })
$bytes = ($files | Measure-Object Length -Sum).Sum
$drive = [IO.DriveInfo]::new('D:\')
if ($drive.AvailableFreeSpace -lt ($bytes + 100MB)) { throw 'Insufficient space on D' }
$freeBefore = [IO.DriveInfo]::new('C:\').AvailableFreeSpace
New-Item -ItemType Directory -Path 'D:\Apps' -Force | Out-Null
& robocopy $source $destination /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP
if ($LASTEXITCODE -ge 8) { throw 'Copy failed' }
foreach ($link in $links) {
    $newPath = Join-Path $destination $link.FullName.Substring($source.Length + 1)
    $newTarget = Join-Path $destination ([IO.Path]::GetFullPath($link.Target)).Substring($source.Length + 1)
    # D does not support junctions; robocopy materializes each package link.
    if (-not (Test-Path -LiteralPath $newPath)) { throw 'Broken copied link' }
}
$copied = @(Get-ChildItem -LiteralPath $destination -File -Recurse -Force)
if ($copied.Count -lt $files.Count) { throw 'File count mismatch' }
foreach ($file in $files) {
    $relative = $file.FullName.Substring($source.Length + 1)
    $target = Join-Path $destination $relative
    if ((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash) { throw "Hash mismatch: $relative" }
}
# Exact paths were checked above; remove only the verified source copy.
foreach ($link in $links) { [IO.Directory]::Delete($link.FullName) }
Remove-Item -LiteralPath $source -Recurse -Force
New-Item -ItemType Junction -Path $source -Target $destination | Out-Null
$link = Get-Item -LiteralPath $source
if ($link.LinkType -ne 'Junction' -or -not (Test-Path -LiteralPath (Join-Path $source 'B'))) { throw 'Junction verification failed' }
$record = [pscustomobject]@{Source=$source;Destination=$destination;Files=$files.Count;Bytes=$bytes;Verification='SHA256';LinkType=$link.LinkType;FreeBefore=$freeBefore;FreeAfter=[IO.DriveInfo]::new('C:\').AvailableFreeSpace}
$record | ConvertTo-Json | Set-Content -LiteralPath 'C:\Users\Gebruiker\Documents\ChatGPT\PC battery health\catch-up-move-result.json'
$record | ConvertTo-Json
