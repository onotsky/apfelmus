# Apfelmus.Avalonia

Cross-Platform-Umsetzung der Apfelmus-GUI mit [Avalonia](https://avaloniaui.net/)
(Windows / macOS / Linux). Teilt sich mit dem WPF-Client die **plattformneutrale
Kernbibliothek `ApfelmusFramework` (`net10.0`)** – nur die Präsentationsschicht
(Views, ViewModels, Converter, Themes) ist Avalonia-spezifisch neu.

Diese Umsetzung entstand auf dem Git-Branch `avalonia`, aufgesetzt auf der
Entkopplung des Frameworks (siehe Commit „Framework von WPF entkoppeln“ auf `main`).

## Status: weitgehende Funktionsparität zum WPF-Client

Gebaut mit .NET 10 (`dotnet build`, 0 Warnungen/0 Fehler); Startup und Laden des
Hauptfensters (inkl. aller Tabs) per Smoke-Test geprüft. Ein Test gegen einen echten
laufenden appleJuice-Core steht noch aus.

**Verdrahtet gegen den echten Core über `WebConnect` (gemeinsame Kernbibliothek):**
- **Menüleiste:** Design (Dunkel/Hell) und Core → „Core beenden“ (`/function/exitcore`).
- **AJLink-Transfer** (Kopfzeile): Link einfügen → `/function/processlink`.
- **Login/Verbindung:** Passwort → MD5, Socket-Test, `Config.xml` (`ConfigSerializer`).
- **Start/Übersicht:** Client (GUI-/Core-Version), Netzwerk (Nutzer, Dateien,
  Gesamtgröße, Verbindungen, Upload-Queue, Firewall-Icon, IP), Server (Name,
  verbunden-seit, Willkommensnachricht) – aus `information.xml` + `modified.xml?filter=informations`.
- **Statusleiste:** Credits, Up-/Download-Speed, Session-Traffic (in/out).
- **Downloads:** DataGrid (Datei, Status-Text, Größe, geladen, Speed, Restzeit, %,
  Rest, Power) + Quellen-Unterliste des gewählten Downloads (`filter=user`) +
  Aktionen Fortsetzen/Pause/Abbrechen/Fertige-entfernen/Powerdownload/Priorität/
  Info-URL/Link-kopieren.
- **Uploads:** DataGrid (Datei, Nick, Speed, %, Priorität, Version).
- **Suche:** Start/Stopp (`/function/search`, `/function/cancelsearch`), Treffer-/
  Quellen-Zähler, Ergebnisliste (`filter=search`), Download (`processlink`),
  Info-URL, Link-kopieren.
- **Server:** DataGrid (Name, Host, Port, zuletzt gesehen, Versuche) + Verbinden
  (`serverlogin`), Entfernen (`removeserver`), offizielle Serverliste holen.
- **Mein Share:** DataGrid (`share.xml`) + Priorität setzen/zurück, Link-kopieren, Info-URL.
- **Einstellungen:** Refreshrate, Info-URL (`ReleaseInfoHost`), Theme; via `ConfigSerializer` persistiert.
- „Suche nach mehr Informationen“ über `ReleaseInfo.Open` (Framework, plattformübergreifend).

**Noch offen / bewusst vereinfacht gegenüber dem WPF-Client:**
- Partlisten-Balken (WPF `RenderPartList`/`WriteableBitmap`) – nicht dargestellt.
- Mehrsprachigkeit (DE/EN/IT) – Avalonia-UI aktuell deutschsprachig.
- Rahmenlose Custom-Titelleiste (WPF `WindowChrome`) – hier normale OS-Chrome.
- Datei-Typ-Icons (WPF Windows-Shell-Icons, Windows-only) – keine Per-Datei-Icons.
- Verzeichnisbaum/Freigabe-Verwaltung im Share-Tab (Ordner freigeben, Baum-Browsing).
- Voller Core-Einstellungsdialog (Verzeichnisse, Ports, Nick, Limits, Slots,
  Passwortänderung, Protokoll-Handler) – hier nur lokale GUI-Optionen.
- Download-Umbenennen-Dialog, „Quelle kopieren“, Zeilenfärbung nach Status.

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
