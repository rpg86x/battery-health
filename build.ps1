$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /out:"$PSScriptRoot\Battery Health.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Management.dll /reference:System.Xml.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Web.Extensions.dll "$PSScriptRoot\BatteryHealth.cs" "$PSScriptRoot\AndroidDashboard.cs" "$PSScriptRoot\SessionLog.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilatie mislukt.' }
Write-Output 'Battery Health.exe is klaar.'
