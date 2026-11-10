# Battery Health

A small Windows battery monitor with an Android companion app. Check your laptop battery and monitor up to four Android phones/tablets over your local Wi-Fi network. Both apps support **Dutch and English**, selected using flags.

[Download the latest release](https://github.com/rpg86x/battery-health/releases/latest)

## Downloads

- **Battery Health Windows + Android.zip**: complete portable package with the Windows app, signed Android APK, optional ADB tools and setup guides. Extract the ZIP before use.
- **Battery Health.exe**: portable Windows app. No installer needed. Android companion connections do not require ADB.
- **Battery Health Android.apk**: install the same APK on each Android phone/tablet. Android 8.0 or newer is required.

## Windows laptop

Launch `Battery Health.exe`. The app shows battery health relative to design capacity, design capacity, current full-charge capacity, cycle count when available, current charge level, charging status and Windows' estimated remaining runtime. Click **Refresh** to measure again.

Battery health is full-charge capacity divided by design capacity. For multiple laptop batteries, available capacities are combined. Firmware measurements can be unavailable or exceed 100% due to factory margin/calibration. The health percentage is separate from current charge level.

## Battery sessions (1.1)

A background logger (`Battery Health.exe --log`) samples the power status every 30 seconds and records each **on-battery session** (unplug → plug in) and each **charge session** (charging starts → stops/full) to `%LOCALAPPDATA%\Battery Health\sessions.csv`. Click **Sessions** to see them, with time spent asleep excluded and an estimated full 100→0% runtime and 0→100% charge time. Run `install.ps1` to install for the current user and start the logger at every logon. Sessions shorter than 2 minutes or with no change in charge are ignored.

## Android over Wi-Fi

1. Install `Battery Health Android.apk` and open it on your device.
2. Connect the device and laptop to the same local network.
3. Tap **Share with Windows** on Android.
4. In Windows select **Android / Wi-Fi → Android app**, choose the appropriate device card, then enter the address and access key shown on Android.
5. Repeat for the other devices. The Windows dashboard refreshes every minute while open.

Four cards are initially named Google Pixel 10, Oppo Find X5 Pro, Samsung S10 and Galaxy Tab S9. The app also displays the actual reported model. The companion APK does not require root or wireless debugging.

The Android app reports charge level, charging state, temperature and Android's health status. Design capacity, full-charge capacity and cycle count are shown only when exposed by the device. **Android's “Good” status is not a battery health percentage.** Missing values are marked **Not available**.

When remaining charge and a valid average discharge current are available, the companion estimates runtime from those readings. This estimate changes with use. The alternative ADB mode can use Android's system estimate when available. Phone capacities use **mAh**, while laptop capacities use **mWh**.

Full setup: [Android APK guide, Dutch and English](ANDROID-APK.md). Alternative without a phone app: [wireless ADB guide](ANDROID-WIFI.md).

## Local connection

No cloud account or cloud server is used. Sharing is off by default. While enabled, Android runs a foreground service with a stop action in its notification. It does not restart automatically after reboot. Deep sleep, battery saving or manufacturer power management can interrupt connectivity; reopen the app if needed.

The Android app serves a read-only authenticated HTTP endpoint on port 8765. Use a trusted local Wi-Fi network. Windows encrypts saved access keys for the current Windows user. If a device's Wi-Fi address changes, update its connection address in Windows. Guest-network device isolation can block communication.

## Build from source on Windows

Windows requires .NET Framework 4.x and uses its built-in C# compiler:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

For Android, install JDK 21, Android SDK platform 36 and build-tools 36.0.0. Set `JAVA_HOME` and `ANDROID_HOME`, or pass explicit paths:

```powershell
.\build-android.ps1 -JavaHome 'C:\path\to\jdk-21' -AndroidHome 'C:\path\to\android-sdk'
```

No Gradle dependencies or network downloads are needed once the tools are installed. The APK targets API 35 and supports API 26+. Building creates a local signing key in `.signing`; preserve it for your own APK updates and never publish it. Builds using a different key cannot update an already installed release without reinstalling.

Optional ADB mode requires the official Android SDK Platform Tools. Place `adb.exe` and its accompanying DLLs in `tools` beside the executable, or use the complete release ZIP. The bundled third-party notice is in [tools/NOTICE.txt](tools/NOTICE.txt).

## Validation

```powershell
.\verify-android.ps1 -JavaHome 'C:\path\to\jdk-21' -AndroidHome 'C:\path\to\android-sdk'
```

Tests cover the actual Java HTTP server and Windows client, missing/incorrect access keys, protocol and identity validation, unavailable data, capacity units, runtime conversion, existing ADB parsing and rediscovery, and language/mode switching. `verify.ps1` also checks and renders the Dutch/English Windows dashboard outside the visible desktop.

The release APK's signature and manifest have been checked. **Installation, Android screen layout and hardware data availability on physical devices have not yet been verified.**

## Nederlands

Download de complete ZIP, pak deze uit en open `Battery Health.exe`. Installeer de APK op je Android-apparaten. Kies met de vlaggen de gewenste taal. Tik op Android op **Delen met Windows** en vul in het Windows-overzicht bij **Android-app** het adres en de toegangscode in.

De laptop toont batterijconditie, oorspronkelijke en volle capaciteit, laadcycli, laadstatus en geschatte gebruiksduur. Android toont alleen de metingen die het apparaat beschikbaar stelt; ontbrekende waarden worden niet verzonnen. Zie de [installatiehandleiding](ANDROID-APK.md) voor alle stappen.
