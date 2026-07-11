# Apfelmus.Avalonia

Cross-Platform-Umsetzung der Apfelmus-GUI mit [Avalonia](https://avaloniaui.net/)
(**Windows / macOS / Linux**). Teilt sich mit dem WPF-Client die plattformneutrale
Kernbibliothek **`ApfelmusFramework` (`net10.0`)** – nur die Präsentationsschicht
(Views, ViewModels, Converter, Themes) ist Avalonia-spezifisch neu. Liegt auf dem
Git-Branch `avalonia`.

## Verhältnis zum WPF-Client

Der **Funktionsumfang** entspricht dem Windows-WPF-Original – Start/Übersicht,
Downloads (inkl. Quellen, Partliste, Powerdownload), Uploads, Suche, Server, Mein
Share und Einstellungen. Die Beschreibung dieser Funktionsbereiche steht bewusst nur
einmal, in der Projekt-README ([`../README.md`](../README.md)); hier wird sie **nicht
wiederholt**. Gegen einen echten, laufenden appleJuice-Core getestet.

Dieses Dokument beschreibt nur, was an der Avalonia-Fassung **anders oder zusätzlich**
ist.

## Was hier anders/zusätzlich ist

**Plattform & Verteilung**
- Läuft unter Windows, macOS und Linux; Releases als self-contained ZIPs je Plattform,
  macOS zusätzlich als `.app`-Bundle (`build-macos-app.sh`).
- Rahmenlose Custom-Titelleiste (statt WPF `WindowChrome`).
- Datei-Icons sind plattformneutrale Kategorie-Vektor-Icons (echte Windows-Shell-Icons
  sind cross-platform nicht reproduzierbar).

**Bedienkomfort (über den WPF-Stand hinaus bzw. neu umgesetzt)**
- Tabellen standardmäßig alphabetisch sortiert, Sortierung per Spaltenkopf,
  **Doppelklick auf den Spaltentrenner** passt die Spalte an den Inhalt an.
- Spaltenlayout **aller** Tabellen sowie Fenstergröße/-zustand werden gespeichert.
- Link-Übergabe holt das Fenster in den Vordergrund und wechselt auf den Downloads-Tab.
- Einstellungen als eigener Tab (im WPF-Client ein separates Fenster).

**ajfsp://-Verknüpfung – plattformabhängig** (siehe unten).

**Single-Instance (Windows/Linux):** mehrere gleichzeitig übergebene Links starten
nicht mehr mehrere Instanzen – weitere Instanzen reichen ihren Link über eine Named
Pipe an die laufende Instanz weiter (Primärwahl über exklusive Lock-Datei). macOS ist
über das `.app`-Bundle ohnehin Single-Instance.

## Bauen & Starten

Voraussetzung: .NET-SDK (net10.0). Avalonia ist plattformübergreifend baubar.

```bash
dotnet restore
dotnet run --project Apfelmus.Avalonia
```

> Avalonia-Pakete sind auf `11.2.1` gepinnt; `Tmds.DBus.Protocol` ist auf `0.21.3`
> hochgezogen (schließt Advisory GHSA-xrw6-gwf8-vvr9 des transitiven 0.20.0).

Es muss – wie beim WPF-Client – ein separater appleJuice-**Core** laufen
(Standard: `localhost:9851`).

## Architektur

```
Program.cs            → Avalonia-Bootstrap + Single-Instance (Win/Linux)
App.axaml(.cs)        → Theme/Ressourcen, Login→MainWindow-Übergang, ajfsp-Verteilung
Services/CoreClient   → async-Hülle um WebConnect (baut /xml/*-Abfragen)
Services/MacUrlScheme → nativer macOS-Apple-Event-Handler (kAEGetURL) für ajfsp://
Services/SingleInstance, ProtocolHandlerService, PartlistRenderer, LanguageManager
ViewModels/           → MainWindowViewModel, LoginViewModel, ViewModelBase, RelayCommand
Views/                → MainWindow, LoginWindow, SplashWindow, RenameDialog
Converters/           → Avalonia-IValueConverter (Portierungen)
Assets/               → Bilder, apfelmus.ico (.exe-Icon), i18n (Lang.de/en/it.axaml)
```

### Wissenswerte Fallstricke

- **DataGrid-Converter-Spalten:** `DataGridTextColumn.Binding` ist standardmäßig
  `TwoWay`; zusammen mit einem `IValueConverter` liefert Avalonia 11.2 dem Converter
  den Ziel-Default (0) statt des Quellwerts. Alle solchen Spalten binden daher mit
  `Mode=OneWay` (die Tabellen sind ohnehin read-only).
- **Netzwerk-Kennzahlen** kommen aus `modified.xml?filter=informations` (nicht aus
  `information.xml` – dort fehlt der `<networkinfo>`-Block).

## ajfsp://-Protokoll-Verknüpfung

Der Browser übergibt geklickte `ajfsp://`-Links an Apfelmus; der Link wird an den Core
weitergereicht. Registrierung je Plattform:

- **Windows:** Checkbox „ajfsp-Links mit Apfelmus verknüpfen“ (Einstellungen) →
  `HKCU\Software\Classes\ajfsp`, kein Admin nötig. Link kommt als Argument.
- **Linux:** dieselbe Checkbox → legt `~/.local/share/applications/apfelmus-ajfsp.desktop`
  an und meldet den Handler per `xdg-mime` an. Link kommt als Argument (`%u`).
- **macOS:** über das `.app`-Bundle mit `Info.plist`-URL-Scheme. **Wichtig:** Avalonia
  11.2 löst das `IActivatableLifetime`-OpenUri-Event auf macOS nicht aus – der Link
  wird stattdessen über einen nativen **`NSAppleEventManager`-Handler** (`kAEGetURL`,
  `Services/MacUrlScheme`) abgefangen (leicht verzögert registriert, damit Cocoas
  eigener Handler nicht überschreibt; mehrere Links werden gesammelt).

```bash
# macOS-App-Bundle bauen (Version aus Directory.Build.props):
./Apfelmus.Avalonia/build-macos-app.sh
# -> Apfelmus.Avalonia/bin/macos-app/Apfelmus.app (+ .zip)
```
