$ErrorActionPreference='Stop'
$source='C:\Users\Gebruiker\.ollama\models'
$destination='D:\AI Models\Ollama'
$install='C:\Users\Gebruiker\AppData\Local\Programs\Ollama'
$app=Join-Path $install 'ollama app.exe'
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$manifestPath="D:\Ollama model move $stamp.json"
$oldUser=[Environment]::GetEnvironmentVariable('OLLAMA_MODELS','User')
$oldMachine=[Environment]::GetEnvironmentVariable('OLLAMA_MODELS','Machine')
if ($oldUser -or $oldMachine) { throw 'Existing custom model path requires review' }
if ([IO.Path]::GetFullPath($source) -ne 'C:\Users\Gebruiker\.ollama\models' -or [IO.Path]::GetFullPath($destination) -ne 'D:\AI Models\Ollama') { throw 'Invalid move paths' }
if (Test-Path -LiteralPath $destination) { throw 'Destination exists; original data will not be overwritten' }
$items=@(Get-Item -LiteralPath $source)+@(Get-ChildItem -LiteralPath $source -Recurse -Force)
if (@($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Linked model files require review' }
$files=@($items | Where-Object { -not $_.PSIsContainer })
$bytes=($files | Measure-Object Length -Sum).Sum
if ([IO.DriveInfo]::new('D:\').AvailableFreeSpace -lt ($bytes+100MB)) { throw 'Insufficient space on D:' }
$before=Invoke-RestMethod -Uri 'http://127.0.0.1:11434/api/tags' -TimeoutSec 10
$originalModels=@($before.models | ForEach-Object { [pscustomobject]@{Name=$_.name;Digest=$_.digest} })
$running=@(Get-CimInstance Win32_Process | Where-Object { $_.Name -match '^ollama.*\.exe$' })
foreach ($process in $running) { if (-not $process.ExecutablePath -or -not $process.ExecutablePath.StartsWith($install+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected Ollama process path' } }
$restartApp=@($running | Where-Object Name -eq 'ollama app.exe').Count -gt 0
$configChanged=$false
$restarted=$false
function Stop-InstalledOllama {
    foreach ($name in @('ollama app.exe','ollama.exe')) {
        $processes=@(Get-CimInstance Win32_Process | Where-Object { $_.Name -eq $name -and $_.ExecutablePath -and $_.ExecutablePath.StartsWith($install+'\',[StringComparison]::OrdinalIgnoreCase) })
        foreach ($process in $processes) { Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue }
    }
    Start-Sleep -Milliseconds 500
    if (@(Get-CimInstance Win32_Process | Where-Object { $_.Name -match '^ollama.*\.exe$' }).Count) { throw 'Ollama still running' }
}
try {
    Write-Output 'Temporarily stopping Ollama'
    Stop-InstalledOllama
    $records=@(foreach ($file in (Get-ChildItem -LiteralPath $source -File -Recurse -Force)) { [pscustomobject]@{RelativePath=$file.FullName.Substring($source.Length+1);Bytes=$file.Length;LastWriteUtc=$file.LastWriteTimeUtc;SHA256=$null} })
    Write-Output ("Copying $($records.Count) files; $([math]::Round($bytes/1GB,2)) GB")
    & robocopy.exe $source $destination /E /J /MT:2 /R:1 /W:1 /XJ /NP /NDL /NJH
    if ($LASTEXITCODE -ge 8) { throw "Copy failed: $LASTEXITCODE; source preserved" }
    if (@(Get-ChildItem -LiteralPath $destination -File -Recurse -Force).Count -ne $records.Count) { throw 'Copied file count mismatch' }
    foreach ($record in $records) {
        Write-Output ('Verifying '+$record.RelativePath)
        $from=Join-Path $source $record.RelativePath
        $to=Join-Path $destination $record.RelativePath
        $original=Get-Item -LiteralPath $from
        if ($original.Length -ne $record.Bytes -or $original.LastWriteTimeUtc -ne $record.LastWriteUtc) { throw 'Original file changed during copying' }
        $record.SHA256=(Get-FileHash -LiteralPath $from -Algorithm SHA256).Hash
        if ($record.SHA256 -ne (Get-FileHash -LiteralPath $to -Algorithm SHA256).Hash) { throw 'Hash mismatch; source preserved' }
    }
    [Environment]::SetEnvironmentVariable('OLLAMA_MODELS',$destination,'User')
    $env:OLLAMA_MODELS=$destination
    $configChanged=$true
    if ([Environment]::GetEnvironmentVariable('OLLAMA_MODELS','User') -ne $destination) { throw 'Persistent setting validation failed' }
    # Discover models with an isolated server that explicitly reads the destination.
    $oldHost=$env:OLLAMA_HOST
    $env:OLLAMA_HOST='127.0.0.1:11435'
    if (Get-NetTCPConnection -LocalPort 11435 -State Listen -ErrorAction SilentlyContinue) { throw 'Verification port already in use' }
    $testProcess=Start-Process -FilePath (Join-Path $install 'ollama.exe') -ArgumentList 'serve' -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $destination 'verification-server.stdout.log') -RedirectStandardError (Join-Path $destination 'verification-server.stderr.log')
    try {
        $after=$null
        for ($attempt=0;$attempt -lt 30;$attempt++) {
            try { $after=Invoke-RestMethod -Uri 'http://127.0.0.1:11435/api/tags' -TimeoutSec 1; break } catch { Start-Sleep -Milliseconds 500 }
        }
        if (-not $after) { throw 'Destination model discovery server did not start' }
        $discovered=@($after.models | ForEach-Object { $_.name+'|'+$_.digest } | Sort-Object)
        $expected=@($originalModels | ForEach-Object { $_.Name+'|'+$_.Digest } | Sort-Object)
        if ($discovered.Count -ne $expected.Count -or @(Compare-Object $expected $discovered).Count) { throw 'Model discovery mismatch; source preserved' }
    } finally {
        Stop-Process -Id $testProcess.Id -Force -ErrorAction SilentlyContinue
        $env:OLLAMA_HOST=$oldHost
    }
    [pscustomobject]@{Source=$source;Destination=$destination;Bytes=$bytes;Files=$records;Models=$originalModels;Verified='SHA256 and isolated Ollama model discovery';PreviousUserSetting=$oldUser;SourceRemoved=$false} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    if ([IO.Path]::GetFullPath($source) -ne 'C:\Users\Gebruiker\.ollama\models') { throw 'Unsafe removal path' }
    foreach ($record in $records) {
        $from=Join-Path $source $record.RelativePath
        $original=Get-Item -LiteralPath $from
        if ($original.Length -ne $record.Bytes -or $original.LastWriteTimeUtc -ne $record.LastWriteUtc) { throw 'Source changed before removal' }
        Remove-Item -LiteralPath $from -Force
    }
    Get-ChildItem -LiteralPath $source -Directory -Recurse -Force | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object { if (@(Get-ChildItem -LiteralPath $_.FullName -Force).Count -eq 0) { [IO.Directory]::Delete($_.FullName) } }
    if (@(Get-ChildItem -LiteralPath $source -Force).Count -eq 0) { [IO.Directory]::Delete($source) }
    if ($restartApp) { Start-Process -FilePath $app -WindowStyle Hidden } else { Start-Process -FilePath (Join-Path $install 'ollama.exe') -ArgumentList 'serve' -WindowStyle Hidden }
    $restarted=$true
    $restored=$null
    for ($attempt=0;$attempt -lt 30;$attempt++) { try { $restored=Invoke-RestMethod -Uri 'http://127.0.0.1:11434/api/tags' -TimeoutSec 1; break } catch { Start-Sleep -Milliseconds 500 } }
    if (-not $restored) { throw 'Files moved and verified, but normal Ollama restart failed' }
    $normalModels=@($restored.models | ForEach-Object { $_.name+'|'+$_.digest } | Sort-Object)
    if (@(Compare-Object $expected $normalModels).Count) { throw 'Files moved, but normal Ollama model discovery differs' }
    [pscustomobject]@{Source=$source;Destination=$destination;Bytes=$bytes;Files=$records;Models=$originalModels;Verified='SHA256, isolated and normal Ollama model discovery';PreviousUserSetting=$oldUser;SourceRemoved=(-not (Test-Path -LiteralPath $source))} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    [pscustomobject]@{Destination=$destination;MovedFiles=$records.Count;MovedGB=[math]::Round($bytes/1GB,2);Models=@($restored.models.name);PersistentModelPath=[Environment]::GetEnvironmentVariable('OLLAMA_MODELS','User');FreeSpaceCGB=[math]::Round([IO.DriveInfo]::new('C:\').AvailableFreeSpace/1GB,2);Manifest=$manifestPath} | ConvertTo-Json -Depth 4
} finally {
    if (-not $restarted) {
        if ($configChanged) { $env:OLLAMA_MODELS=$destination } else { $env:OLLAMA_MODELS=$oldUser }
        if ($restartApp) { Start-Process -FilePath $app -WindowStyle Hidden } else { Start-Process -FilePath (Join-Path $install 'ollama.exe') -ArgumentList 'serve' -WindowStyle Hidden }
    }
}
