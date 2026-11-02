$ErrorActionPreference = 'Stop'
$root = 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\CMA'
$export = Join-Path $root 'public-1.0'
$paths = Get-Content 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\android-build-tools\paths.json' | ConvertFrom-Json
$env:JAVA_HOME = $paths.javaHome
$env:ANDROID_HOME = $paths.androidHome
$signing = Join-Path $root '.release-signing'
New-Item -ItemType Directory -Force -Path $signing | Out-Null
$passwordPath = Join-Path $signing 'password.txt'
if (!(Test-Path -LiteralPath $passwordPath)) { [IO.File]::WriteAllText($passwordPath,[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(36))) }
$env:CMA_SIGNING_PASSWORD = [IO.File]::ReadAllText($passwordPath)
$env:CMA_SIGNING_KEY = Join-Path $signing 'cma-release.jks'
try {
    if (!(Test-Path -LiteralPath $env:CMA_SIGNING_KEY)) {
        & "$env:JAVA_HOME\bin\keytool.exe" -genkeypair -keystore $env:CMA_SIGNING_KEY -storepass:env CMA_SIGNING_PASSWORD -keypass:env CMA_SIGNING_PASSWORD -alias cma-release -keyalg RSA -keysize 3072 -validity 10000 -dname 'CN=Codex Meter 4 Android'
        if ($LASTEXITCODE -ne 0) { throw 'Release signing-key generation failed.' }
    }
    & (Join-Path $export 'build.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
} finally {
    Remove-Item Env:CMA_SIGNING_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:CMA_SIGNING_KEY -ErrorAction SilentlyContinue
}
