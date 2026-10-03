# Battery Health op Android

Dezelfde **Battery Health Android.apk** werkt op je Google Pixel 10, Oppo Find X5 Pro, Samsung S10 en Galaxy Tab S9. Minimale versie: Android 8.0. De app bevat geen native processorafhankelijke bibliotheken en ondersteunt zowel telefoons als tablets.

## Installeren

1. Kopieer de APK naar elk apparaat, bijvoorbeeld met USB-bestandsoverdracht of Quick Share.
2. Open het bestand in de bestandsapp. Geef die bestandsapp, wanneer Android daarom vraagt, toestemming om deze app te installeren. Dit is een persoonlijke APK, buiten Google Play.
3. Open **Battery Health**. Kies Nederlands of Engels met de vlaggen bovenin.

## Met je Windows-laptop verbinden

1. Verbind beide apparaten met hetzelfde wifi-netwerk.
2. Tik op Android op **Delen met Windows**. Geef toestemming voor meldingen als je de melding voor het actieve delen wilt zien.
3. De Android-app toont het **wifi-adres**, zoals `192.168.1.25:8765`, en een **toegangscode van 32 tekens**. Deze code blijft gelijk totdat je de appgegevens wist of de app opnieuw installeert.
4. Open op Windows je bestaande **Battery Health**-snelkoppeling en klik op **Android / Wi-Fi**.
5. Kies bovenin **Android-app**, selecteer onderaan het juiste apparaat, en vul de toegangscode en het verbindingsadres uit Android in. Klik **Verbinden**.
6. Herhaal dit voor je andere drie apparaten. Alle vier kaarten vernieuwen elke minuut zolang het Windows-overzicht open is.

Er is geen draadloze debugging, root of USB-debugging nodig voor deze APK-verbinding. De ADB-keuze in Windows blijft beschikbaar als alternatief.

## Gebruik en beschikbare gegevens

De Android-app toont laadpercentage, laadstatus, batterijtemperatuur en Androids batterijstatus. Ontwerpcapaciteit, volle laadcapaciteit en laadcycli verschijnen alleen wanneer Android of de firmware ze beschikbaar stelt. Een gewone APK heeft minder toegang tot systeemgegevens dan ADB; die ontbrekende gegevens worden niet verzonnen. ‘Goed’ betekent Androids batterijstatus en is geen percentage resterende capaciteit.

Wanneer Android de resterende lading en een geldig gemiddeld ontlaadverbruik doorgeeft, berekent de APK een gebruiksduurschatting: resterende lading / gemiddeld verbruik. Deze kan veranderen bij ander gebruik. Zij is niet de systeemschatting die de ADB-modus soms geeft. Tijdens het opladen is gebruiksduur niet van toepassing. Als de benodigde metingen ontbreken, staat er **Niet beschikbaar**.

**Delen is standaard uit.** Het draait na inschakelen als voorgrondservice met een melding en een knop om te stoppen. Het start niet automatisch na een herstart. Androids diepe slaap, batterijbesparing of fabrikantspecifiek energiebeheer kan bereikbaarheid onderbreken. Open dan de app opnieuw. Bij Oppo/Samsung kun je zo nodig achtergrondgebruik toestaan in de batterijinstellingen van de app.

Bij een nieuw wifi-IP-adres vul je op Windows het nieuwe verbindingsadres in. Een gastnetwerk dat apparaten van elkaar isoleert verhindert de verbinding. De Android-app luistert lokaal op poort **8765**; Windows maakt uitgaande verbindingen naar dat adres.

Er is geen cloudaccount of cloudserver. Het lokale HTTP-endpoint vereist de toegangscode en accepteert alleen verbindingen uit lokale netwerken. Gebruik dit op je eigen vertrouwde wifi; de lokale verbinding gebruikt HTTP. Windows slaat de toegangscode versleuteld op voor je Windows-gebruikersaccount. Stop delen op Android om de toegang uit te zetten.

## Bouw en verificatie

De APK is ondertekend met een lokale ontwikkelsleutel. Bewaar de map `.signing` om toekomstige APK-updates met dezelfde sleutel te kunnen maken. Deel deze map niet mee. Bouwen: `build-android.ps1`. De aanwezige Android SDK 36 en Java 21 worden gebruikt, zonder Gradle- of netwerkdownloads. De APK richt zich op Android API 35 en ondersteunt API 26 en hoger.

`verify-android.ps1` controleert het echte Java-HTTP-endpoint, juiste/onjuist/ontbrekende toegangscodes, de Windows-client, het JSON-protocol, ontbrekende waarden en de bestaande ADB-parser. APK-ondertekening en manifest worden door de Android-buildtools gecontroleerd. Installatie, schermweergave en batterijgegevens op je vier fysieke apparaten moeten nog worden gecontroleerd.

Bronnen: [Android BatteryManager](https://developer.android.com/reference/android/os/BatteryManager), [Android — foreground service voor verbonden apparaten](https://developer.android.com/develop/background-work/services/fgs/service-types#connected-device).

# English

Install **Battery Health Android.apk** on each phone/tablet by transferring and opening the file. Android may ask you to allow your file manager to install this personal APK. Android 8.0 or newer is required.

Open the app and choose a language using the flags. Connect to the same Wi-Fi network as the laptop, then tap **Share with Windows**. The app displays its address (for example `192.168.1.25:8765`) and a 32-character access key.

In the Windows app, select **Android / Wi-Fi → Android app**, select the appropriate device, enter its key and connection address, then click **Connect**. Repeat for all four devices. Wireless debugging and root are not needed.

Sharing is off by default. While enabled it runs as a foreground service with a stop action in its notification. It does not restart automatically after a reboot. Deep sleep or battery saving may interrupt connectivity; reopen the app if needed. Update the saved address if the device's Wi-Fi IP changes.

Missing battery capacity/cycle measurements are marked **Not available**. Runtime is an estimate calculated from remaining charge and average discharge current, when both are valid. Android's **Good** status is not a battery health percentage. Local HTTP access requires the key; use your trusted Wi-Fi. The Windows app encrypts saved keys for the current Windows user. No cloud service is involved.
