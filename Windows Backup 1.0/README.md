# Windows Backup 1.0

Herstelcode en diagnose voor de Windows-taakbalk en het startmenu. Vastgelegd op **13 oktober 2026**.

Dit pakket bevat geen volledige Windows-back-up, systeemimage of herstelpunt.

## Welke problemen waren er?

- **Startknop werkte niet:** klikken op de Windows-startknop op de taakbalk opende het startmenu niet.
- **Windows-toets werkte niet:** de toets met het Windows-logo op het toetsenbord opende het startmenu niet.
- **Ctrl + Esc werkte ook niet:** ook deze alternatieve sneltoets opende het startmenu niet.
- **Appvensters leken geen rekening te houden met de taakbalk:** de gebruiker meldde dat apps niet goed rekening hielden met de ruimte die de zichtbare taakbalk innam. De precieze vensterindeling is niet afzonderlijk gemeten.

Met 'docker' bedoelde de gebruiker de Windows-taakbalk met app-iconen. Docker Desktop was niet het onderwerp van deze reparatie.

## Wat hebben we gevonden?

- Het proces `StartMenuExperienceHost` reageerde op een gegeven moment niet meer.
- Na een nieuwe start verdween het startmenuproces opnieuw. Windows registreerde een crash van `StartMenuExperienceHost.exe` in `Windows.UI.Xaml.dll` (versie `10.0.26100.9549`, uitzonderingscode `0xc0000409`).
- Er waren eerdere crashes van Windows Verkenner (`Explorer.exe`), waaronder een crash in hetzelfde interfacebestand `Windows.UI.Xaml.dll` met uitzonderingscode `0xc000027b`.
- Het startmenupakket en de interfacepakketten `Microsoft.UI.Xaml.CBS`, `MicrosoftWindows.Client.CBS` en `MicrosoftWindows.Client.Core` waren geregistreerd met status `Ok`.
- De gecontroleerde Windows-diensten draaiden.
- Windows meldde een wachtende herstart via `Component Based Servicing\RebootPending`.
- DISM vond ontbrekende of beschadigde bestanden en probeerde herstelbestanden via Windows Update op te halen. De downloadfase bleef op 50% staan; er was vóór de herstart geen bevestigd eindresultaat.

Dit zijn diagnostische aanwijzingen. De exacte onderliggende oorzaak van de crashes is niet definitief vastgesteld.

## Hoe hebben we het opgelost?

| Stap | Uitgevoerd | Resultaat |
| --- | --- | --- |
| 1 | Windows Verkenner en het startmenuproces opnieuw gestart. | Startmenu bleef onbruikbaar. |
| 2 | Het startmenupakket opnieuw geregistreerd voor het account `Gebruiker`. | Registratie slaagde zonder fouten, maar het menu opende nog steeds niet. |
| 3 | DISM met administratorrechten gestart om Windows-systeembestanden te herstellen. | Beschadiging gevonden; herstel wachtte op Windows Update. Geen bevestigd succesvol eindresultaat. |
| 4 | Het startmenuproces afzonderlijk opnieuw gestart. | Reageerde kort volgens Windows, maar crashte opnieuw. |
| 5 | Na het opslaan van open werk en toestemming van de gebruiker de pc volledig opnieuw gestart. | De gebruiker bevestigde daarna: 'het werkt weer super'. |

**De volledige Windows-herstart was de stap waarna het startmenu weer werkte.** De gebruiker gaf vervolgens aan dat appvensters vermoedelijk ook weer goed rekening hielden met de taakbalk. Dat laatste is een gebruikerswaarneming en is niet afzonderlijk technisch getest.

We kunnen niet bevestigen dat DISM de systeembestanden volledig heeft hersteld. SFC was vóór de herstart nog niet begonnen. De herregistratie en procesherstarts waren op zichzelf onvoldoende.

## Als dezelfde problemen terugkomen

1. Sla open werk op en start Windows normaal opnieuw op.
2. Test de startknop, de Windows-toets, Ctrl + Esc en een gemaximaliseerd appvenster.
3. Als het probleem blijft bestaan, open `Start.cmd` voor diagnose of een gerichte startmenuherstelpoging. Gebruik je eigen account zonder 'Als administrator uitvoeren'. De hersteloptie kan Verkenner-vensters sluiten.
4. Voor een nieuwe systeembestandscontrole: open Windows PowerShell **als administrator** en voer uit:

   ```powershell
   & 'D:\Windows Backup 1.0\Herstel-Startmenu.ps1' -Actie Systeembestanden
   ```

   Laat DISM en SFC afronden en lees hun eindmeldingen. DISM kan internet gebruiken en lang duren.

Het script herstart de pc nooit automatisch. Nieuwe logboeken worden opgeslagen in `Logs`.

## Bestanden

- `Start.cmd`: menu voor diagnose en startmenuherstel.
- `Herstel-Startmenu.ps1`: diagnose, startmenuregistratie/procesherstart en optionele DISM/SFC-controle.
- `Diagnose-2026-10-13.txt`: oorspronkelijke diagnose van vóór de herstart.
- `Reparatie-2026-10-13.log`: logboek van de eerdere reparatiepoging; bevat geen bevestigd voltooid herstel.
- `LEESMIJ.txt`: korte gebruiksinstructies.
- `README.md`: problemen, bevindingen, herstelstappen en uitkomst.

## Microsoft-documentatie

- [Startmenu-fouten oplossen](https://learn.microsoft.com/nl-nl/troubleshoot/windows-client/shell-experience/troubleshoot-start-menu-errors)
