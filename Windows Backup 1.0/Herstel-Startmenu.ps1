param([ValidateSet('Diagnose','Startmenu','Systeembestanden')][string]$Actie = 'Diagnose')
$ErrorActionPreference = 'Stop'
$logMap = Join-Path $PSScriptRoot 'Logs'
New-Item -ItemType Directory -Path $logMap -Force | Out-Null
$logPad = Join-Path $logMap ('herstel-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.txt')
Start-Transcript -Path $logPad
try {
    if ($Actie -eq 'Diagnose') {
        Get-Process explorer,StartMenuExperienceHost -ErrorAction SilentlyContinue | Select-Object Name,Id,Responding
        Get-AppxPackage Microsoft.Windows.StartMenuExperienceHost | Select-Object Name,Status
        Write-Output ('Windows wacht op herstart: ' + (Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending'))
        Get-WinEvent -FilterHashtable @{LogName='Application';StartTime=(Get-Date).AddDays(-2);Id=1000} -ErrorAction SilentlyContinue |
            Where-Object { $_.Message -match 'StartMenuExperienceHost|Explorer.exe' } |
            Select-Object -First 8 TimeCreated,Message | Format-List
    }
    elseif ($Actie -eq 'Startmenu') {
        Write-Host 'De taakbalk verdwijnt even. Verkenner-vensters kunnen sluiten.'
        Stop-Process -Name StartMenuExperienceHost -Force -ErrorAction SilentlyContinue
        Add-AppxPackage -Register -Path (Join-Path $env:WINDIR 'SystemApps\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\AppxManifest.xml') -DisableDevelopmentMode
        Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
        if (-not (Get-Process explorer -ErrorAction SilentlyContinue)) { Start-Process (Join-Path $env:WINDIR 'explorer.exe') }
        Write-Host 'Test de startknop en Ctrl+Esc. Werkt het nog niet? Sla je werk op en kies normaal Opnieuw opstarten.'
    }
    else {
        $identiteit = [Security.Principal.WindowsIdentity]::GetCurrent()
        $principal = New-Object Security.Principal.WindowsPrincipal($identiteit)
        if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
            throw 'Open Windows PowerShell als administrator voor Systeembestanden.'
        }
        & "$env:WINDIR\System32\DISM.exe" /Online /Cleanup-Image /RestoreHealth
        $dismCode = $LASTEXITCODE
        Write-Host "DISM exitcode: $dismCode"
        & "$env:WINDIR\System32\sfc.exe" /scannow
        Write-Host "SFC exitcode: $LASTEXITCODE"
        Write-Host 'Lees de eindmeldingen. Een exitcode alleen bevestigt niet dat het startmenu werkt.'
    }
} finally { Stop-Transcript }
