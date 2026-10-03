param([string]$JavaHome, [string]$AndroidHome)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\android-tools.ps1"
$buildPaths = Get-BatteryBuildPaths -JavaHome $JavaHome -AndroidHome $AndroidHome
$jdkPath = $buildPaths.javaHome
$sdkPath = $buildPaths.androidHome
$androidToolsPath = Join-Path $sdkPath 'build-tools\36.0.0'
$androidJar = Join-Path $sdkPath 'platforms\android-36\android.jar'
$buildRoot = Join-Path $PSScriptRoot 'android\build'
$signingRoot = Join-Path $PSScriptRoot '.signing'
foreach ($directory in @($buildRoot, "$buildRoot\generated", "$buildRoot\classes", "$buildRoot\dex", $signingRoot)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
function Check-BuildExit { if ($LASTEXITCODE -ne 0) { throw "Android build failed: $LASTEXITCODE" } }
& "$androidToolsPath\aapt2.exe" compile --dir "$PSScriptRoot\android\res" -o "$buildRoot\resources.zip"
Check-BuildExit
& "$androidToolsPath\aapt2.exe" link -o "$buildRoot\unsigned.apk" --manifest "$PSScriptRoot\android\AndroidManifest.xml" -I $androidJar --java "$buildRoot\generated" "$buildRoot\resources.zip"
Check-BuildExit
$sources = @(Get-ChildItem -LiteralPath "$PSScriptRoot\android\src" -Filter '*.java' -Recurse | ForEach-Object FullName) + @(Get-ChildItem -LiteralPath "$buildRoot\generated" -Filter '*.java' -Recurse | ForEach-Object FullName)
& "$jdkPath\bin\javac.exe" -encoding UTF-8 -source 8 -target 8 -Xlint:-options -bootclasspath "$androidJar;$androidToolsPath\core-lambda-stubs.jar" -d "$buildRoot\classes" @sources
Check-BuildExit
& "$jdkPath\bin\jar.exe" cf "$buildRoot\classes.jar" -C "$buildRoot\classes" .
Check-BuildExit
& "$jdkPath\bin\java.exe" -cp "$androidToolsPath\lib\d8.jar" com.android.tools.r8.D8 --min-api 26 --lib $androidJar --output "$buildRoot\dex" "$buildRoot\classes.jar"
Check-BuildExit
& "$jdkPath\bin\jar.exe" uf "$buildRoot\unsigned.apk" -C "$buildRoot\dex" classes.dex
Check-BuildExit
& "$androidToolsPath\zipalign.exe" -f 4 "$buildRoot\unsigned.apk" "$buildRoot\aligned.apk"
Check-BuildExit
$passwordPath = Join-Path $signingRoot 'password.txt'
$keyPath = Join-Path $signingRoot 'battery-health.jks'
if (!(Test-Path -LiteralPath $passwordPath)) {
    $randomBytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($randomBytes) } finally { $rng.Dispose() }
    [IO.File]::WriteAllText($passwordPath, [Convert]::ToBase64String($randomBytes), [Text.Encoding]::ASCII)
}
if (!(Test-Path -LiteralPath $keyPath)) {
    $env:BH_SIGNING_PASSWORD = [IO.File]::ReadAllText($passwordPath)
    try {
        & "$jdkPath\bin\keytool.exe" -genkeypair -keystore $keyPath -alias battery-health -keyalg RSA -keysize 3072 -validity 10000 -dname 'CN=Battery Health Local' -storepass:env BH_SIGNING_PASSWORD -keypass:env BH_SIGNING_PASSWORD
        Check-BuildExit
    } finally { Remove-Item Env:BH_SIGNING_PASSWORD }
}
$apkPath = Join-Path $PSScriptRoot 'Battery Health Android.apk'
& "$jdkPath\bin\java.exe" -jar "$androidToolsPath\lib\apksigner.jar" sign --ks $keyPath --ks-key-alias battery-health --ks-pass "file:$passwordPath" --out $apkPath "$buildRoot\aligned.apk"
Check-BuildExit
& "$jdkPath\bin\java.exe" -jar "$androidToolsPath\lib\apksigner.jar" verify --verbose $apkPath
Check-BuildExit
& "$androidToolsPath\aapt.exe" dump badging $apkPath
Check-BuildExit
Write-Output "APK ready: $apkPath"
