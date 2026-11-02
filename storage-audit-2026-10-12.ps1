param([switch]$Details)
$ErrorActionPreference='Stop'
function Get-FolderUsage([string]$Path) {
    $bytes=0L; $count=0L; $skipped=0L
    $large=[Collections.Generic.List[object]]::new()
    $pending=[Collections.Generic.Stack[IO.DirectoryInfo]]::new()
    if (-not [IO.Directory]::Exists($Path)) { return }
    $pending.Push([IO.DirectoryInfo]::new($Path))
    while ($pending.Count) {
        $dir=$pending.Pop()
        try {
            foreach ($file in $dir.EnumerateFiles()) {
                $bytes += $file.Length; $count++
                if ($file.Length -ge 150MB) { $large.Add([pscustomobject]@{Path=$file.FullName;MB=[math]::Round($file.Length/1MB,1)}) }
            }
            foreach ($child in $dir.EnumerateDirectories()) {
                if ($child.Attributes -band [IO.FileAttributes]::ReparsePoint) { $skipped++; continue }
                $pending.Push($child)
            }
        } catch { $skipped++ }
    }
    [pscustomobject]@{Path=$Path;GB=[math]::Round($bytes/1GB,3);Bytes=$bytes;Files=$count;Skipped=$skipped;LargeFiles=@($large | Sort-Object MB -Descending | Select-Object -First 8)}
}
$roots=if ($Details) { @('C:\Users\Gebruiker\.lmstudio','C:\Users\Gebruiker\AppData\Local\Programs','C:\Users\Gebruiker\.cache','C:\Users\Gebruiker\Documents\ChatGPT\Apps\android-build-tools','C:\Users\Gebruiker\AppData\Local\Packages\OpenAI.Codex_2p2nqsd0c76g0\LocalCache\Local') } else { @('C:\Users\Gebruiker\Documents\ChatGPT\Apps','C:\Users\Gebruiker\Documents\ChatGPT\PC battery health') }
$paths=[Collections.Generic.List[string]]::new()
foreach ($root in $roots) { foreach ($dir in [IO.DirectoryInfo]::new($root).EnumerateDirectories()) { if (-not ($dir.Attributes -band [IO.FileAttributes]::ReparsePoint)) { $paths.Add($dir.FullName) } } }
if ($Details) {
    foreach ($path in @('C:\Users\Gebruiker\Documents\ChatGPT\Apps\LPOA Server 1.1\backups','C:\Users\Gebruiker\Documents\ChatGPT\Apps\codex-meter\release')) { $paths.Add($path) }
} else {
    foreach ($path in @('C:\Users\Gebruiker\Downloads','C:\Users\Gebruiker\Videos','C:\Users\Gebruiker\.gradle','C:\Users\Gebruiker\.android','C:\Users\Gebruiker\.cache','C:\Users\Gebruiker\AppData\Local\Temp','C:\Users\Gebruiker\AppData\Local\npm-cache','C:\Users\Gebruiker\AppData\Local\Android','C:\Users\Gebruiker\AppData\Local\Packages\OpenAI.Codex_2p2nqsd0c76g0','C:\Users\Gebruiker\AppData\Local\Programs','C:\Users\Gebruiker\AppData\Roaming\LM Studio','C:\Users\Gebruiker\.lmstudio','C:\Users\Gebruiker\.ollama')) { $paths.Add($path) }
}
$results=@(foreach ($path in $paths) { Get-FolderUsage $path })
$output=if ($Details) { 'C:\Users\Gebruiker\Documents\ChatGPT\PC battery health\storage-audit-details-2026-10-12.json' } else { 'C:\Users\Gebruiker\Documents\ChatGPT\PC battery health\storage-audit-2026-10-12.json' }
$results | Sort-Object Bytes -Descending | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $output -Encoding utf8
[IO.DriveInfo]::GetDrives() | Where-Object { $_.IsReady -and $_.Name -in @('C:\','D:\') } | ForEach-Object { [pscustomobject]@{Drive=$_.Name;FreeGB=[math]::Round($_.AvailableFreeSpace/1GB,2);TotalGB=[math]::Round($_.TotalSize/1GB,2)} } | ConvertTo-Json
$results | Sort-Object Bytes -Descending | Select-Object -First 18 Path,GB,Files,Skipped,LargeFiles | ConvertTo-Json -Depth 5
