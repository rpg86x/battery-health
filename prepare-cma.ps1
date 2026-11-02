$ErrorActionPreference = 'Stop'
$origin = 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\codex-meter'
$candidate = 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\CMA\B\source'
New-Item -ItemType Directory -Force -Path $candidate | Out-Null
foreach ($file in @('index.html','style.css','renderer.js','languages.js','flag-nl.svg','flag-en.svg','logo.png','client.cjs','build-android.ps1')) { Copy-Item -LiteralPath (Join-Path $origin $file) -Destination $candidate }
foreach ($folder in @('mobile','service')) { Copy-Item -LiteralPath (Join-Path $origin $folder) -Destination $candidate -Recurse -Force }
& robocopy (Join-Path $origin 'android') (Join-Path $candidate 'android') /E /XD .gradle build /NFL /NDL /NJH /NJS | Out-Null
if ($LASTEXITCODE -gt 7) { throw 'Copy failed' }
Set-Content -LiteralPath (Join-Path $candidate 'package.json') -Value '{"name":"cma","version":"1.0-dev","private":true}'
$gradle = Join-Path $candidate 'android\app\build.gradle'
$text = Get-Content -LiteralPath $gradle -Raw
$text = $text.Replace("applicationId 'com.rpg86x.codexmeter'", "applicationId 'com.rpg86x.cma'").Replace('versionCode 120','versionCode 1').Replace("versionName '1.2.0'", "versionName '1.0-dev'")
Set-Content -LiteralPath $gradle -Value $text
$manifest = Join-Path $candidate 'android\app\src\main\AndroidManifest.xml'
$text = (Get-Content -LiteralPath $manifest -Raw).Replace('android:label="Codex Meter"','android:label="CMA 1.0"').Replace('android:usesCleartextTraffic="false"','android:networkSecurityConfig="@xml/network_security_config"')
Set-Content -LiteralPath $manifest -Value $text
$xml = Join-Path $candidate 'android\app\src\main\res\xml'
New-Item -ItemType Directory -Force -Path $xml | Out-Null
Set-Content -LiteralPath (Join-Path $xml 'network_security_config.xml') -Value '<network-security-config><base-config cleartextTrafficPermitted="false"/><domain-config cleartextTrafficPermitted="true"><domain>127.0.0.1</domain></domain-config></network-security-config>'
$java = Join-Path $candidate 'android\app\src\main\java\com\rpg86x\codexmeter\MainActivity.java'
$text = Get-Content -LiteralPath $java -Raw
$text = $text.Replace('import java.net.URL;', "import java.net.URL;`nimport java.net.HttpURLConnection;")
$text = $text.Replace('if (!"https".equals(url.getProtocol()) ||', 'if (!("https".equals(url.getProtocol()) || ("http".equals(url.getProtocol()) && "127.0.0.1".equals(url.getHost()))) ||')
$text = $text.Replace('HttpsURLConnection connection = null;', 'HttpURLConnection connection = null;').Replace('(HttpsURLConnection) new URL', '(HttpURLConnection) new URL')
# Provisioning file is transferred privately over USB after installation, never packaged.
$text = $text.Replace('prefs = getSharedPreferences("connection", MODE_PRIVATE);', @'
prefs = getSharedPreferences("connection", MODE_PRIVATE);
        File provision = new File(getFilesDir(), "usb-connection.json");
        if (provision.exists()) {
            try {
                JSONObject config = new JSONObject(new String(java.nio.file.Files.readAllBytes(provision.toPath()), StandardCharsets.UTF_8));
                new Bridge().configure(config.getString("url"), config.getString("key"));
            } catch (Exception ignored) { }
            provision.delete();
        }
'@)
Set-Content -LiteralPath $java -Value $text
$html = Join-Path $candidate 'index.html'
$text = (Get-Content -LiteralPath $html -Raw).Replace('<title>Codex Meter</title>','<title>CMA 1.0</title>').Replace('<h1>Codex Meter</h1>','<h1>CODEX Meter</h1>').Replace('Jouw ruimte om te bouwen','CMA 1.0 · Android')
Set-Content -LiteralPath $html -Value $text
$mobile = Join-Path $candidate 'mobile\android.js'
$text = Get-Content -LiteralPath $mobile -Raw
$text = $text.Replace('Online service koppelen','Verbinding instellen').Replace('Connect online service','Connection settings').Replace('Vul het HTTPS-adres en de toegangssleutel van je eigen Codex Meter-service in.','Via USB: houd deze pc en de USB-verbinding actief. Voor gebruik zonder pc is een eigen HTTPS-service nodig.').Replace('Enter the HTTPS address and access key for your own Codex Meter service.','USB: keep this PC and the USB connection active. To use the app without a PC, connect your own HTTPS service.').Replace('Verbonden met online service','Verbonden met CODEX Meter').Replace('Connected to online service','Connected to CODEX Meter').Replace('Gebruik een volledig HTTPS-adres, zonder pad of parameters.','Gebruik HTTPS, of http://127.0.0.1:43128 voor USB.').Replace('Use a full HTTPS address without a path or parameters.','Use HTTPS, or http://127.0.0.1:43128 for USB.')
Set-Content -LiteralPath $mobile -Value $text
Add-Content -LiteralPath (Join-Path $candidate 'mobile\android.css') -Value @'
body{overflow-x:hidden}main{width:100%;max-width:480px;padding:18px 16px 26px}h1{font-size:22px}header{gap:10px}.badge{font-size:10px;padding:5px 8px}.languages{margin-top:12px}.card{padding:16px}.card h2{font-size:14px}.percent{font-size:29px}.reset-date{font-size:14px}.countdown{font-size:15px}.meta{flex-wrap:wrap;line-height:1.5}.section-title{font-size:14px}.connection summary{min-height:32px;display:flex;align-items:center}p{font-size:13px}footer p,.explanation{font-size:12px}button{touch-action:manipulation;min-width:44px}.hero{padding:16px}#credits{font-size:34px}#error{font-size:13px}.connection{margin-top:14px}.connection input{font-size:16px}
'@
$build = Join-Path $candidate 'build-android.ps1'
$text = (Get-Content -LiteralPath $build -Raw).Replace("Join-Path (Split-Path $PSScriptRoot -Parent) 'android-build-tools'", "'C:\Users\Gebruiker\Documents\ChatGPT\Apps\android-build-tools'").Replace("Join-Path (Split-Path $PSScriptRoot -Parent) 'java-sockets'", "'C:\Users\Gebruiker\Documents\ChatGPT\Apps\java-sockets'").Replace('Codex-Meter-Android-$meterVersion-test.apk','CMA-$meterVersion.apk')
Set-Content -LiteralPath $build -Value $text
Set-Content -LiteralPath 'C:\Users\Gebruiker\Documents\ChatGPT\Apps\CMA\B\release.json' -Value '{"app":"CMA","platform":"Android","version":"1.0-dev","status":"candidate","applicationId":"com.rpg86x.cma"}'
Write-Output $candidate
