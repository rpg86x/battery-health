# Android-batterijen via wifi — Nederlands

Deze handleiding beschrijft de ADB-modus. Voor gebruik van de Android-APK zonder draadloze debugging, zie [ANDROID-APK.md](ANDROID-APK.md). Kies voor de onderstaande stappen in Windows bovenin **Draadloze debugging / ADB**.

Open **Battery Health** met je bestaande bureaubladsnelkoppeling en klik bovenin op **Android / Wi-Fi**. Er zijn vier kaarten: Google Pixel 10, Oppo Find X5 Pro, Samsung S10 en Galaxy Tab S9. De vlaggen wisselen tussen Nederlands en Engels. Vernieuwen gebeurt elke minuut terwijl het Android-venster open is.

## Eenmalig koppelen, per apparaat

1. Verbind je laptop en Android-apparaat met hetzelfde lokale netwerk. Een gastnetwerk met apparaatisolatie kan de verbinding blokkeren.
2. Schakel **Ontwikkelaarsopties** in door bij **Over de telefoon/tablet → Software-informatie → Buildnummer** zeven keer te tikken. De menunamen verschillen per merk. Bij Oppo kan dit onder **Versie → Versienummer** staan.
3. Open **Ontwikkelaarsopties → Draadloze debugging** en zet deze aan. Dit vereist Android 11 of hoger. Werk de S10 zo nodig bij als deze optie ontbreekt; deze app gebruikt geen USB-fallback.
4. Noteer **IP-adres en poort** op het hoofdscherm van Draadloze debugging. Dit is het **verbindingsadres**, bijvoorbeeld `192.168.1.25:37123`.
5. Kies **Apparaat koppelen met koppelcode**. Noteer het nieuwe **IP-adres en poort** en de **zes cijfers**. Dit is het **koppeladres**; de poort is doorgaans anders dan in stap 4. Laat dit scherm open tijdens het koppelen.
6. Selecteer in de Windows-app het juiste apparaat. Vul het koppeladres, de koppelcode en het verbindingsadres in. Klik **Koppelen / verbinden**.
7. Herhaal dit voor de andere drie apparaten. Controleer de modelnaam op elke kaart na de eerste meting.

## Later opnieuw verbinden

De app bewaart de verbindingsadressen en een apparaatidentiteit in `AndroidDevices.xml` naast de exe. De koppelcode wordt niet opgeslagen. Android bewaart de toestemming voor deze laptop. Laat Draadloze debugging aan tijdens het gebruik.

De app probeert een eerder gemeten apparaat bij een gewijzigde poort automatisch opnieuw te vinden via ADB/mDNS. Dat hangt af van wat het apparaat en je netwerk aankondigen. Lukt dat niet, kies dan het betreffende apparaat, vul het actuele **verbindingsadres** in en laat de koppelcode leeg. Klik **Koppelen / verbinden**. Alleen als Android de koppeling vergeten is, moet je opnieuw een code gebruiken. Verwijder de laptop uit de gekoppelde apparaten op Android als je de toegang wilt intrekken.

## Welke gegevens zie je?

- Huidig laadpercentage, laadstatus en temperatuur uit Androids batterijservice.
- Ontwerpcapaciteit en huidige volle capaciteit in **mAh**, wanneer de firmware deze leesbaar aanbiedt via de standaard `power_supply`-velden. De Windows-laptop toont **mWh**; die eenheden zijn verschillend.
- Conditieschatting: volle capaciteit gedeeld door ontwerpcapaciteit, uitsluitend wanneer beide beschikbaar zijn. De firmware kan fabrieksmarge of kalibratieverschillen rapporteren.
- Laadcycli, indien leesbaar. Nul cycli is geldig.
- Geschatte resterende gebruiksduur uit Androids batterijstatistieken, indien Android een schatting heeft. Bij opladen is deze niet van toepassing.
- Android-status, bijvoorbeeld **Goed** of **Oververhit**. **Goed betekent niet 100% resterende batterijcapaciteit.**

De app verzint geen ontbrekende capaciteit, laadcycli of gebruiksduur. De actuele resterende lading (`Charge counter`) wordt niet als volle laadcapaciteit gebruikt. De beschikbaarheid moet op elk echt apparaat worden gecontroleerd. Er is geen root nodig. De app leest gegevens; zij wijzigt geen batterijinstellingen. De ADB-koppeling geeft deze laptop wel de gebruikelijke Android-debugtoegang.

ADB staat in de map `tools`. Houd die map naast de exe. Windows kan bij het eerste gebruik een firewallmelding tonen: lokale netwerktoegang is nodig om je eigen apparaten te bereiken. Er is geen cloudserver of telefoon-app betrokken.

Bronnen: [Android — draadloos ADB koppelen](https://developer.android.com/tools/adb), [Android batterijservice](https://android.googlesource.com/platform/frameworks/base/+/refs/heads/main/services/core/java/com/android/server/BatteryService.java), [AOSP batterijstatistieken en dtr-eenheid](https://android.googlesource.com/platform/frameworks/base/+/15c45c854e01e8ab518d6b6cc56659bba6e0820b/core/java/android/os/BatteryStats.java).

# Android batteries over Wi-Fi — English

Open **Battery Health** using your existing desktop shortcut, then click **Android / Wi-Fi**. Four cards are prepared for your Pixel 10, Oppo Find X5 Pro, Samsung S10 and Galaxy Tab S9. The flags switch languages. Measurements refresh every minute while the Android window is open.

## Pair each device

1. Connect the laptop and device to the same local network.
2. Enable **Developer options** by tapping **Build number** seven times in About phone/tablet (menu names vary).
3. Enable **Wireless debugging** in Developer options. Android 11 or newer is required.
4. Copy **IP address & port** from the main Wireless debugging screen into the Windows app's **Connection address** field.
5. Select **Pair device with pairing code** on Android. Copy its **IP address & port** into **Pairing address**, and enter the six-digit **Pairing code**. Leave that Android dialog open while pairing. The two ports are usually different.
6. Select the correct device in the Windows app, then click **Pair / connect**. Repeat for all four devices. Check the reported model on each card.

For a previously paired device, leave the code empty and enter its current connection address. IP addresses and connection ports may change after a restart or network change. The app saves addresses and device identities locally in `AndroidDevices.xml`; it never saves pairing codes. Keep Wireless debugging enabled while using the dashboard.

The dashboard displays charge level, charging status, temperature, and any readable design capacity, full-charge capacity, cycle count and Android runtime estimate. Phone capacities are in **mAh**, whereas laptop capacities are in **mWh**. Missing data appears as **Not available**. Android's **Good** health status is not a battery health percentage. Root and a phone app are not required. The laptop receives standard Android debugging access through pairing.

The app attempts to rediscover a previously measured device via ADB/mDNS if its connection port changes. This depends on the device and network; if it fails, enter the current connection address manually. On smaller screens, scroll down to reach the pairing fields.

Keep the `tools` folder beside the executable. ADB uses local network access; no cloud service is involved.
