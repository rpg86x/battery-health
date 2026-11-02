$ErrorActionPreference = 'Stop'
$config = Get-Content 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\android-build-tools\paths.json' | ConvertFrom-Json
$jdk=$config.javaHome
$bt=Join-Path $config.androidHome 'build-tools\36.0.0'
$androidJar=Join-Path $config.androidHome 'platforms\android-36\android.jar'
$build=Join-Path $PSScriptRoot 'build'
foreach($folder in @("$build\classes","$build\dex","$PSScriptRoot\..\android")){New-Item -ItemType Directory -Force -Path $folder | Out-Null}
function Check {if($LASTEXITCODE -ne 0){throw "Build failed: $LASTEXITCODE"}}
& "$bt\aapt2.exe" compile --dir "$PSScriptRoot\res" -o "$build\resources.zip"
Check
& "$bt\aapt2.exe" link -o "$build\unsigned.apk" --manifest "$PSScriptRoot\AndroidManifest.xml" -I $androidJar --min-sdk-version 26 --target-sdk-version 32 --version-code 13 --version-name '1.3' --debug-mode "$build\resources.zip"
Check
$sources=@(Get-ChildItem "$PSScriptRoot\src" -Recurse -Filter '*.java' | ForEach-Object FullName)
& "$jdk\bin\javac.exe" -encoding UTF-8 -source 8 -target 8 -Xlint:-options -classpath $androidJar -d "$build\classes" @sources
Check
& "$jdk\bin\jar.exe" cf "$build\classes.jar" -C "$build\classes" .
Check
& "$jdk\bin\java.exe" -cp "$bt\lib\d8.jar" com.android.tools.r8.D8 --lib $androidJar --min-api 26 --output "$build\dex" "$build\classes.jar"
Check
& "$jdk\bin\jar.exe" uf "$build\unsigned.apk" -C "$build\dex" classes.dex
Check
& "$bt\zipalign.exe" -f 4 "$build\unsigned.apk" "$build\aligned.apk"
Check
$key='C:\Users\Gebruiker\Documents\ChatGPT\Android Dev\Wakeify 1.1\build\debug.keystore'
$apk="$PSScriptRoot\..\android\Wakeify-1.3.apk"
& "$jdk\bin\java.exe" -jar "$bt\lib\apksigner.jar" sign --ks $key --ks-pass pass:android --out $apk "$build\aligned.apk"
Check
& "$jdk\bin\java.exe" -jar "$bt\lib\apksigner.jar" verify --verbose $apk
Check
Get-FileHash $apk -Algorithm SHA256

