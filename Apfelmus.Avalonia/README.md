# Apfelmus.Avalonia

Cross-Platform-Umsetzung der Apfelmus-GUI mit [Avalonia](https://avaloniaui.net/)
(Windows / macOS / Linux). Teilt sich mit dem WPF-Client die **plattformneutrale
Kernbibliothek `ApfelmusFramework` (`net10.0`)** – nur die Präsentationsschicht
(Views, ViewModels, Converter, Themes) ist Avalonia-spezifisch neu.

Diese Umsetzung entstand auf dem Git-Branch `avalonia`, aufgesetzt auf der
Entkopplung des Frameworks (siehe Commit „Framework von WPF entkoppeln“ auf `main`).

## Status: funktionsfähig (gebaut & Startup getestet)

Gebaut mit .NET 10 (`dotnet build`, 0 Warnungen/0 Fehler) und per Startup-Smoke-Test
gelaufen. Ein Test gegen einen echten laufenden appleJuice-Core steht noch aus.

**Verdrahtet gegen den echten Core über `WebConnect` (gemeinsame Kernbibliothek):**
- Login/Verbindung: Passwort → MD5 (`CreateMd5Hash`), Socket-Test
  (`WebConnect.CheckSocket`), Speichern der `Config` als `Config.xml` (`ConfigSerializer`).
- Start-/Übersicht: Polling von `information.xml` (Core-Version, Nutzer/Dateien,
  Datenmenge, IP, Firewall-Status).
- Downloads: DataGrid (Datei, Größe, Status, Speed, Quellen, %) + Aktionen
  Info-URL / Pause / Fortsetzen / Abbrechen (`/function/*`), Merge-Update ohne
  Auswahlverlust.
- Uploads: DataGrid (Datei, Nick, Speed, Status).
- Suche: Suchbegriff → `/function/search`, Ergebnisse aus `modified.xml?filter=search`,
  Download starten (`/function/processlink` mit ajfsp-Link) und Info-URL öffnen.
- Server: DataGrid (Name, Host, Port, Versuche) + Verbinden (`/function/serverlogin`).
- Mein Share: DataGrid aus `share.xml` (Datei, Größe, Priorität, Anfragen, Suchtreffer).
- Einstellungen: Refreshrate, Info-URL (`ReleaseInfoHost`, %s = Dateilink),
  Design-Umschaltung Dunkel/Hell zur Laufzeit; alles über `ConfigSerializer` persistiert.
- „Suche nach mehr Informationen“ nutzt direkt `ReleaseInfo.Open` aus dem Framework
  (öffnet die konfigurierbare URL im Standardbrowser, plattformübergreifend).

**Bewusst (noch) nicht portiert / vereinfacht:**
- Partlisten-Balken (WPF: `RenderPartList`, `WriteableBitmap`) – noch nicht dargestellt.
- Mehrsprachigkeit (DE/EN/IT): die Avalonia-UI ist aktuell deutschsprachig; die
  Sprachumschaltung des WPF-Clients wurde nicht übernommen.
- Custom Title Bar / rahmenloses Fenster (WPF: `WindowChrome`) – hier normale OS-Chrome.
- Datei-Icons pro Typ (WPF: Windows-Shell-Icon via `FilenameToImage`, Windows-only) –
  cross-platform bräuchte es ein mitgeliefertes Icon-Set pro Endung.
- Erweiterte Download-Aktionen (Powerdownload-Gebote, Priorität, Zielverzeichnis).

## Bauen & Starten

Voraussetzung: .NET-SDK (net10.0). Avalonia ist plattformübergreifend baubar.

```bash
dotnet restore
dotnet run --project Apfelmus.Avalonia
```

> Getestet mit .NET-SDK 10.0.301 (macOS/arm64): baut mit 0 Warnungen/0 Fehlern und
> startet. Avalonia-Pakete sind auf `11.2.1` gepinnt; `Tmds.DBus.Protocol` ist auf
> `0.21.3` hochgezogen (schließt Advisory GHSA-xrw6-gwf8-vvr9 des transitiven 0.20.0).

Es muss – wie beim WPF-Client – ein separater appleJuice-**Core** laufen, gegen den
sich die GUI verbindet (Standard: `localhost:9851`).

## Architektur

```
Program.cs            → Avalonia-Bootstrap
App.axaml(.cs)        → Theme/Ressourcen, Login→MainWindow-Übergang
Services/CoreClient   → async-Hülle um WebConnect (baut /xml/*-Abfragen)
ViewModels/           → LoginViewModel, MainWindowViewModel, ViewModelBase, RelayCommand
Views/                → LoginWindow, MainWindow (.axaml + code-behind)
Converters/           → Avalonia-IValueConverter (Portierungen)
Assets/Images/        → Bildressourcen (avares://)
```

Der gesamte Netzwerk-/Datenkern bleibt in `ApfelmusFramework` und wird unverändert
mit dem WPF-Client geteilt.
