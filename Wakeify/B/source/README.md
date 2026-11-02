# Wakeify 1.2 — Android ontwikkelversie

Eenvoudige zaklampwekker voor de Galaxy S10. Geen helderheidsstanden, kalibratie of geluid.

Pagina 1: **Zaklamp aan**, **Zaklamp uit** en de eerstvolgende alarmtijd. De zaklamp gaat na tien minuten automatisch uit. Handmatig uitzetten annuleert het weekschema niet. Ook de melding heeft een knop om het licht uit te zetten.

Pagina 2: wijzig de standaardtijd, vink dagen aan of uit, zet het schema aan of uit en kies **Schema opslaan**. Vrijdag en zondag hebben elk een aparte knop om hun eigen tijd te wijzigen. De standaardtijd geldt voor de andere aangevinkte dagen. Instellingen blijven bij het opnieuw openen en compatibele updates bewaard.

Het aangevraagde schema staat aan op maandag, dinsdag, woensdag en donderdag om 07:15, vrijdag om 08:00 en zondag om 09:00. Zaterdag is uit. Vrijdag en zondag worden eenmalig toegevoegd bij de betreffende kandidaat-update; later door de gebruiker gewijzigde tijden en dagen blijven behouden. De volgende alarmtijd wordt na iedere wekker opnieuw gepland. Na een telefoonherstart, tijd-/tijdzonewijziging en een app-update wordt het schema hersteld. Na een herstart moet de telefoon eerst ontgrendeld zijn; na Android **Geforceerd stoppen** moet je Wakeify opnieuw openen.

De app gebruikt cameratoegang uitsluitend voor de gewone publieke zaklamp-aan/uit-API. Geen camera-opnamen en geen internettoegang. De zaklamp draait tijdens de tien minuten als voorgrondservice met een stopmelding en een begrensde wakelock. Exacte alarmtoegang wordt gecontroleerd. Camera-gebruik door andere apps en Samsung-batterijbeheer kunnen de zaklamp nog onderbreken.

## Controle

De APK is met dezelfde lokale sleutel als 1.1 ondertekend om bestaande appgegevens en toestemmingen te behouden. Versiecode 12, zichtbare versie 1.2, status B/kandidaat. Geen GitHub-publicatie in deze taak en geen vervanging van de bewaarde 1.1-broncode.

Dertien pure Java-schematests controleren donderdagmorgen, doorschuiven na de alarmtijd, weekends, gewijzigde dag/tijd, geen gekozen dagen, het weekend met wintertijd, de aparte vrijdagtijd 08:00 en zondagtijd 09:00 en de terugkeer naar maandag 07:15. Een afzonderlijke korte alarmtest op de fysieke S10 met het scherm uit liet het alarm starten, de CameraManager-zaklampstatus naar aan veranderen en de service na vijf seconden stoppen. De gebruiker zag de echte zaklamp aan gaan. Een volledige nacht tot 07:15 en een echte telefoonherstart zijn nog niet getest.

Build: `build.ps1`, met de bestaande lokale Android-SDK/JDK. De sleutel blijft buiten de bronmap. `verifyAlarmSeconds` is een begrensde ontwikkeltest (10–60 seconden), uitsluitend actief in een debuggable kandidaat; hij verandert het weekschema niet.
