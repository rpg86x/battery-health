function Get-BatteryBuildPaths {
    param([string]$JavaHome, [string]$AndroidHome)
    $localPathsFile = 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\android-build-tools\paths.json'
    if (!$JavaHome) { $JavaHome = $env:JAVA_HOME }
    if (!$AndroidHome) { $AndroidHome = $env:ANDROID_HOME }
    if (!$AndroidHome) { $AndroidHome = $env:ANDROID_SDK_ROOT }
    if ((!$JavaHome -or !$AndroidHome) -and (Test-Path -LiteralPath $localPathsFile)) {
        $localPaths = Get-Content -LiteralPath $localPathsFile -Raw | ConvertFrom-Json
        if (!$JavaHome) { $JavaHome = $localPaths.javaHome }
        if (!$AndroidHome) { $AndroidHome = $localPaths.androidHome }
    }
    if (!$JavaHome -or !$AndroidHome) { throw 'Set JAVA_HOME (JDK 21) and ANDROID_HOME (Android SDK with platform 36 and build-tools 36.0.0), or pass -JavaHome and -AndroidHome.' }
    $javaExecutable = Join-Path $JavaHome 'bin\java.exe'
    $androidPlatform = Join-Path $AndroidHome 'platforms\android-36\android.jar'
    $androidCompiler = Join-Path $AndroidHome 'build-tools\36.0.0\aapt2.exe'
    foreach ($required in @($javaExecutable, $androidPlatform, $androidCompiler)) { if (!(Test-Path -LiteralPath $required)) { throw "Build dependency not found: $required" } }
    return @{ javaHome = $JavaHome; androidHome = $AndroidHome }
}
