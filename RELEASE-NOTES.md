Battery Health 1.1 adds battery session tracking on Windows.

- Background logger records how long each on-battery session lasts and how long charging takes (CSV in %LOCALAPPDATA%\Battery Health).
- New Sessions window with live current session, history, and averages for full runtime and full charge time; sleep time excluded.
- install.ps1 installs per user, adds a desktop shortcut and starts the logger at logon.

Battery Health 1.0 provides a portable Windows app and a signed Android companion APK.

- Windows laptop battery health, design capacity, full-charge capacity, cycle count, current charge and estimated runtime.
- Four Android device cards with authenticated local Wi-Fi connectivity through the companion app; wireless ADB remains available as an alternative.
- Dutch and English language selection using flags in both apps.
- Android charge level, charging state and temperature; capacity and cycle count when the device exposes them.
- Android companion runtime estimate based on remaining charge and average discharge current, when available.

Downloads:

- **Battery Health Android.apk** — install the same APK on each phone/tablet, then enable Share with Windows.
- **Battery Health.exe** — portable Windows app; companion connections need no ADB tools.
- **Battery Health Windows + Android.zip** — complete package including Android APK, Windows app, ADB tools and installation guides. Extract before use.

Validation: Windows build, APK signature and manifest verification, Java HTTP server authentication, Java-to-Windows communication, protocol parsing, missing data handling, ADB parser and Dutch/English Windows layout checks passed. Installation, Android screen layout and battery availability on physical devices have not yet been verified.

See ANDROID-APK.md for installation and connection instructions. Use the local HTTP connection on a trusted Wi-Fi network. Android's Good status is not a battery capacity percentage.
