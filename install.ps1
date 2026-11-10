# Builds Battery Health, installs it for the current user, adds a desktop shortcut
# and starts the session logger now and at every logon (Startup folder shortcut).
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot\build.ps1"

$target = Join-Path $env:LOCALAPPDATA 'Programs\Battery Health'
$exe = Join-Path $target 'Battery Health.exe'
New-Item -ItemType Directory -Force $target | Out-Null

# Stop a running logger/app from the install folder so the exe can be replaced.
Get-Process 'Battery Health' -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe } | Stop-Process -Force
Start-Sleep -Milliseconds 500
Copy-Item "$PSScriptRoot\Battery Health.exe" $exe -Force

$shell = New-Object -ComObject WScript.Shell
function New-Shortcut($path, $arguments, $description) {
    $s = $shell.CreateShortcut($path)
    $s.TargetPath = $exe; $s.Arguments = $arguments; $s.WorkingDirectory = $target; $s.Description = $description
    $s.Save()
}
New-Shortcut (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Battery Health.lnk') '' 'Battery Health 1.1'
New-Shortcut (Join-Path ([Environment]::GetFolderPath('Startup')) 'Battery Health logger.lnk') '--log' 'Records battery and charging sessions'

Start-Process $exe -ArgumentList '--log' -WindowStyle Hidden
Write-Output "Installed to $target; logger running and set to start at logon."
