# Apfelmus.Avalonia

Cross-Platform-Umsetzung der Apfelmus-GUI mit [Avalonia](https://avaloniaui.net/)
(Windows / macOS / Linux). Teilt sich mit dem WPF-Client die **plattformneutrale
Kernbibliothek `ApfelmusFramework` (`net10.0`)** – nur die Präsentationsschicht
(Views, ViewModels, Converter, Themes) ist Avalonia-spezifisch neu.

Diese Umsetzung entstand auf dem Git-Branch `avalonia`, aufgesetzt auf der
Entkopplung des Frameworks (siehe Commit „Framework von WPF entkoppeln“ auf `main`).

## Status: Grundgerüst / Work in progress

**Fertig verdrahtet (gegen den echten Core über `WebConnect`):**
- Login/Verbindung (`LoginViewModel`): Passwort → MD5 (`CreateMd5Hash`), Socket-Test
  (`WebConnect.CheckSocket`), Speichern der `Config` als `Config.xml`
  (`ConfigSerializer`) – exakt dieselbe Kernbibliothek wie beim WPF-Client.
- Start-/Übersichts-Tab: zyklisches Polling von `information.xml`
  (Core-Version, Nutzer/Dateien im Netz, Datenmenge, eigene IP, Firewall-Status).
- Downloads-/Uploads-Tab: Liste aus `modified.xml?filter=down|uploads`
  (als Nachweis der Wiederverwendung; noch ohne Spalten/Fortschritt).

**Noch offen (Platzhalter-Tabs):**
- Suche inkl. dynamischer Ergebnis-Tabs (WPF: `CloseableTabItem`).
- „Mein Share“ (Verzeichnisbaum, Prioritäten).
- Server-Liste inkl. Verbindungssteuerung.
- Partlisten-Rendering (WPF: `RenderPartList`, `WriteableBitmap`) – für Avalonia
  auf `WriteableBitmap`/`RenderTargetBitmap` neu umzusetzen.
- Vollständige Theme-Umschaltung Dark/Light zur Laufzeit + Mehrsprachigkeit (DE/EN/IT).
- Restliche `IValueConverter` (bisher portiert: Firewall, FileSize, Firewall-Text).
- Custom Title Bar / rahmenloses Fenster (WPF: `WindowChrome`).
- Datei-Icons pro Typ: der WPF-Client zieht das Windows-Shell-Icon
  (`FilenameToImage`, Windows-only) – cross-platform braucht es ein mitgeliefertes
  Icon-Set pro Endung.

## Bauen & Starten

Voraussetzung: .NET-SDK (net10.0). Avalonia ist plattformübergreifend baubar.

```bash
dotnet restore
dotnet run --project Apfelmus.Avalonia
```

> Hinweis: Auf der Maschine, auf der dieses Grundgerüst erstellt wurde, war **kein
> .NET-SDK installiert** – der Build wurde daher **nicht verifiziert**. Vor dem
> ersten Lauf bitte `dotnet build` prüfen; ggf. Avalonia-Paketversionen (aktuell
> `11.2.1`) an eine vorhandene/gewünschte Version anpassen.

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
