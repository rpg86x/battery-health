$ErrorActionPreference = 'Stop'
$appPath = Join-Path $PSScriptRoot 'Codex Dash.exe'
$iconPath = Join-Path $PSScriptRoot 'Codex Dash.ico'
if (!(Test-Path -LiteralPath $appPath) -or !(Test-Path -LiteralPath $iconPath)) { throw 'App or icon missing.' }
$desktopPath = [Environment]::GetFolderPath('DesktopDirectory')
$shortcutPath = Join-Path $desktopPath 'Codex Dash.lnk'
if (Test-Path -LiteralPath $shortcutPath) {
    $existing = (New-Object -ComObject WScript.Shell).CreateShortcut($shortcutPath)
    if ($existing.TargetPath -ne $appPath) { throw 'An unrelated Codex Dash shortcut already exists.' }
}
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $appPath
$shortcut.WorkingDirectory = $PSScriptRoot
$shortcut.IconLocation = "$iconPath,0"
$shortcut.Description = 'Codex Dash - local permissions dashboard'
$shortcut.Save()
$verify = $shell.CreateShortcut($shortcutPath)
if ($verify.TargetPath -ne $appPath -or $verify.IconLocation -ne "$iconPath,0") { throw 'Shortcut verification failed.' }
Write-Output "Created and verified: $shortcutPath"
