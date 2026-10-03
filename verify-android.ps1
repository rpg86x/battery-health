param([string]$JavaHome, [string]$AndroidHome)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\android-tools.ps1"
$buildPaths = Get-BatteryBuildPaths -JavaHome $JavaHome -AndroidHome $AndroidHome
$jdkPath = $buildPaths.javaHome
$testClasses = Join-Path $PSScriptRoot 'android\build\test-classes'
New-Item -ItemType Directory -Path $testClasses -Force | Out-Null
& "$jdkPath\bin\javac.exe" -encoding UTF-8 -source 8 -target 8 -Xlint:-options -d $testClasses "$PSScriptRoot\android\src\nl\batteryhealth\companion\LocalBatteryServer.java" "$PSScriptRoot\android\test\ProtocolTest.java"
if ($LASTEXITCODE -ne 0) { throw 'Java protocol test compilation failed' }
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe /main:VerifyFramework /out:"$PSScriptRoot\verify-framework.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Management.dll /reference:System.Xml.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Web.Extensions.dll "$PSScriptRoot\BatteryHealth.cs" "$PSScriptRoot\AndroidDashboard.cs" "$PSScriptRoot\VerifyFramework.cs"
if ($LASTEXITCODE -ne 0) { throw 'Windows test harness compilation failed' }
& "$PSScriptRoot\verify-framework.exe"
if ($LASTEXITCODE -ne 0) { throw 'Windows protocol tests failed' }
& "$jdkPath\bin\java.exe" -cp $testClasses ProtocolTest "$PSScriptRoot\verify-framework.exe"
if ($LASTEXITCODE -ne 0) { throw 'HTTP integration tests failed' }
