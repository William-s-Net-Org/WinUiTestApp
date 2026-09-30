# Hockeytruppen

WinUI 3-applikation för Windows x64 med .NET 10, DDD-struktur och MVVM.
Svenskt gränssnitt för att registrera och lista hockeyspelare med namn,
position, tröjnummer, vikt i kg, längd i cm och klubbfattning.

## Starta

Kräver Windows 10 version 1809 eller senare och .NET 10 SDK för att bygga.
Öppna `HockeyRoster.slnx` i en Visual Studio-version som stöder .NET 10
och WinUI, eller kör följande från den här mappen:

```powershell
dotnet build HockeyRoster.slnx -c Release
dotnet run --project src/HockeyRoster.WinUI -c Release
```

Appen är unpackaged och inkluderar Windows App SDK-runtime i byggutdata.
NuGet-paketen är versionslåsta i projektfilerna; första bygget kan behöva internet.
För att distribuera en mapp som även inkluderar .NET-runtime:

```powershell
dotnet publish src/HockeyRoster.WinUI -c Release -r win-x64 --self-contained true -o publish
```

Distribuera hela mappen, inte enbart exe-filen.
Se [Microsofts dokumentation om unpackaged WinUI](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/unpackage-winui-app).

## Struktur

| Projekt | Ansvar |
| --- | --- |
| Domain | `Player` som aggregate root, enumtyper och domänregler. Inga externa beroenden. |
| Application | Användningsfall för att lägga till och läsa spelare samt repository-port. |
| Infrastructure | JSON-repository med fillås och atomiskt filbyte. |
| Presentation | Testbar MVVM med CommunityToolkit.Mvvm, formulärtillstånd och kommandon. |
| WinUI | XAML-vyer, startpunkt och sammansättning av beroenden. |
| Tests | Domän-, lagrings- och ViewModel-tester. |

Beroenden går inåt: UI → Presentation → Application → Domain.
Infrastructure implementerar porten i Application. WinUI kopplar samman objekten
i `App.xaml.cs`. Code-behind hanterar enbart fönster och livscykel.

## Data och regler

Spelare lagras i `%LOCALAPPDATA%\HockeyRoster\players.json`.
Listan läses in vid start. Knappen **Uppdatera listan** läser om den.
Sparade spelare visas direkt och formuläret töms efter lyckad sparning.
Fel visas på svenska och formuläret behålls vid misslyckad sparning.
En oläsbar datafil blockerar ny sparning tills problemet åtgärdats.

- Namn: 2–100 tecken efter trimning.
- Position: målvakt, back, center, vänsterforward eller högerforward.
- Tröjnummer: heltal 1–99. Samma nummer får användas av flera spelare.
- Vikt: 15–250 kg. Längd: 80–250 cm. Komma och punkt accepteras som decimaltecken.
- Klubbfattning: vänster eller höger, avser den nedre handen på klubban.

Intervallen är valda grundregler och kan justeras i domänen.
Det här är ett lokalt spelarregister; inloggning, lagindelning, redigering och
borttagning ingår inte i denna version. Datafilen är okrypterad.

## Tester

```powershell
dotnet test tests/HockeyRoster.Tests -c Release
```

Testerna använder isolerade temporära filer och omfattar gränsvärden,
ogiltiga enumvärden, alla fälts återläsning, samtidiga skrivningar,
skadad datafil, svenska decimaler och bibehållet formulär vid valideringsfel.
