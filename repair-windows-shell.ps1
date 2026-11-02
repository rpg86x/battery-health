$taskRoot = 'C:\Users\Gebruiker\Documents\ChatGPT\PC battery health'
Start-Transcript -Path (Join-Path $taskRoot 'windows-shell-repair.log') -Force
try {
    & "$env:WINDIR\System32\DISM.exe" /Online /Cleanup-Image /RestoreHealth
    $dismResult = $LASTEXITCODE
    & "$env:WINDIR\System32\sfc.exe" /scannow
    $sfcResult = $LASTEXITCODE
    @{ DismExitCode = $dismResult; SfcExitCode = $sfcResult; Completed = (Get-Date).ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot 'windows-shell-repair-result.json')
} finally {
    Stop-Transcript
}
