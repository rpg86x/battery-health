# Wakeify 1.3 — aparte Android-kandidaat

Deze versie staat naast 1.2 met app-id `com.wakelight.v13` en de naam **Wakeify 1.3**. De broncode, APK, geïnstalleerde 1.2 en het bestaande weekschema van 1.2 blijven behouden. Het nieuwe weekschema van 1.3 staat aanvankelijk uit. Kies op pagina 3 of je 1.3 wilt gebruiken; geef de gevraagde cameratoegang en schakel het schema daarna in. Schakel dan het schema in 1.2 uit om dubbele lichtalarmen te vermijden.

Met **<** en **>** onderaan ga je tussen drie pagina's. De navigatie blijft zichtbaar bij scrollen.

1. Licht starten en alles stoppen. Zaklamp automatisch uit na tien minuten.
2. Muziek na licht inschakelen, Spotify of een andere beschikbare muziek-app kiezen, de app openen, meldingstoegang instellen, afspelen testen en pauzeren.
3. Muziekvertraging van 0, 5 (standaard), 10, 15, 20, 25 of 30 minuten. Ook het aanpasbare weekschema staat hier: maandag–donderdag 07:15, vrijdag 08:00, zondag 09:00; zaterdag uit. Instellingen worden direct opgeslagen.

## Spotify en andere muziek-apps klaarzetten

Wakeify gebruikt Android MediaSession-bediening om de gekozen muziek-app te hervatten. Het downloadt geen muziek en koppelt geen Spotify-account.

Kies je app op pagina 2. Geef **Wakeify 1.3 muziekbediening** eenmalig meldingstoegang in Android; dat is de officiële toegang tot sessies van andere muziek-apps. Wakeify leest en bewaart geen meldingsinhoud, songtitels of afspeelgeschiedenis. Het zoekt uitsluitend een beschikbare MediaController van het gekozen app-pakket.

Open de gekozen muziek-app, speel een nummer/playlist kort af en pauzeer. Keer terug naar Wakeify en test de koppeling. Die test hervat de muziek; gebruik **Muziek pauzeren** om de test te stoppen. Het mediavolume, de geselecteerde muziek en eventuele netwerk-/offlinebeschikbaarheid worden door de muziek-app beheerd.

Als een sessie ontbreekt of de toestemming ontbreekt, meldt Wakeify dat. Een play-opdracht wordt pas als geslaagd gemeld nadat de muziek-app de status PLAYING bevestigt. Het zelfstandig koud starten van een gesloten muziek-app of kiezen van een Spotify-playlist is niet geïmplementeerd. Een muziek-app die zijn sessie verwijdert kan daardoor de automatische muziekstart verhinderen.

## Tijdlijn

De muziekvertraging begint na het succesvol aanzetten van de zaklamp. De zaklamp stopt na tien minuten, terwijl Wakeify bij langere vertraging blijft wachten met een zichtbare voorgrondmelding en begrensde wakelock. Zodra muziekstart bevestigd is en het licht uit is, stopt Wakeify's service; de muziek-app blijft zelf afspelen. Geen automatische stop van de externe muziek. **Alles stoppen** stopt de wachtrij en pauzeert de geselecteerde muziek-app.

## Controle en beperkingen

De bestaande 1.2-zaklamp was op de S10 getest met het scherm uit. Deze 1.3 heeft 22 schema- en tijdlijngevallen getest, inclusief alle vertragingen en de dienstduur na de lichtstop. Op de S10 heeft de gebruiker meldingstoegang gegeven en muziek in Spotify klaargezet; Wakeify hervatte Spotify en ontving daadwerkelijk PLAYING. Spotify is na de test weer gepauzeerd. Pagina 2 is via de native UI gecontroleerd; de overige schermafbeeldingen werden door het vergrendelde scherm beperkt. Exacte alarmen en cameratoegang zijn per app nodig. Een volledige nacht en alle 30 minuten vertraging zijn niet live getest.

Android-documentatie voor deze sessietoegang: https://developer.android.com/reference/android/media/session/MediaSessionManager

Build: build.ps1 met de bestaande lokale Android-tools en privésleutel. Geen GitHub-publicatie gevraagd of uitgevoerd. B blijft een ontwikkelkandidaat.
