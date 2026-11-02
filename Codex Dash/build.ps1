$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /win32icon:"$PSScriptRoot\Codex Dash.ico" /out:"$PSScriptRoot\Codex Dash.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll "$PSScriptRoot\Dash.cs"
if ($LASTEXITCODE -ne 0) { throw 'Codex Dash compilation failed.' }
Write-Output 'Codex Dash.exe is ready.'
