# Apfelmus.Avalonia

Plattformübergreifende Desktop-GUI für das **appleJuice**-P2P-Netzwerk, gebaut mit
[Avalonia](https://avaloniaui.net/) und lauffähig unter **Windows, macOS und Linux**.
Die Anwendung verbindet sich über eine XML/HTTP-API mit einem separat laufenden
appleJuice-**Core** und zeigt bzw. steuert dessen Zustand. Der Netzwerk-/Datenkern
liegt in der plattformneutralen Bibliothek `ApfelmusFramework` (`net10.0`); Avalonia
liefert die gesamte Oberfläche (Views, ViewModels, Converter, Themes).

Anthrazit-/Grün-Design mit umschaltbarem Hell-/Dunkel-Theme, mehrsprachig
(Deutsch / Englisch / Italienisch, zur Laufzeit umschaltbar), mit eigener rahmenloser
Titelleiste.

## Oberfläche

Die Funktionen sind auf Tabs verteilt:

- **Start** – GUI- und Core-Version, Netzwerkstatus (Nutzer, Dateien, Gesamtgröße,
  offene Verbindungen, Upload-Queue, Firewall, eigene IP) und der verbundene Server
  samt Willkommensnachricht.
- **Downloads** – laufende Downloads mit Datei, Status, Größe, geladen, Speed,
  Restzeit, Fortschrittsbalken, Rest und Powerdownload. Darunter die **Quellen** des
  gewählten Downloads und ein **Partlisten-Verfügbarkeitsbalken** (einstellbare Größe,
  Hover-Tooltip mit dem Typ unter dem Cursor). Aktionen (auch für Mehrfachauswahl):
  Fortsetzen, Pause, Abbrechen, Umbenennen, Zielverzeichnis setzen, Fertige entfernen,
  Powerdownload/Priorität, Link/Quelle kopieren, Info-URL. Tastenkürzel **F2–F9**.
- **Uploads** – aktive Uploads und Warteschlange getrennt.
- **Suchen** – Volltextsuche mit einem eigenen Ergebnis-Tab je Suchlauf (Tab schließen
  bricht die Suche ab); Treffer herunterladen, Info-URL, Link kopieren.
- **Server** – bekannte Server (Name, Host, Port, zuletzt gesehen, Versuche) mit
  Verbinden, Entfernen, offizielle Serverliste holen und Serverlink kopieren; der
  verbundene Server ist hervorgehoben.
- **Mein Share** – Verzeichnisbaum zum Freigeben/Entfernen von Ordnern, Anzeige
  erneuern, Shareprüfung starten, Gruppierung nach Ordner, Freitextfilter, Anzeige der
  freigegebenen Ordner, Mehrfachauswahl sowie Link(s)/Quelle(n) kopieren, Priorität,
  Info-URL.
- **Einstellungen** – GUI-Optionen (Refreshrate, Info-URL, Theme, Login-Fenster,
  ajfsp-Verknüpfung) sowie die kompletten Core-Einstellungen (Nick, Verzeichnisse,
  Ports, Limits, Speed pro Slot als Schieberegler, Autoconnect) und Passwortänderung.

**Tabellen** sind standardmäßig alphabetisch sortiert, per Spaltenkopf sortierbar; ein
Doppelklick auf den Spaltentrenner passt die Spalte an den Inhalt an. Spaltenlayout
aller Tabellen sowie die Fenstergröße werden gemerkt. Kopfzeile mit AJLink-Feld zum
direkten Übergeben eines `ajfsp://`-Links.

## ajfsp://-Links aus dem Browser

Geklickte `ajfsp://`-Links werden an Apfelmus (und damit den Core) übergeben; die App
holt sich dabei in den Vordergrund und wechselt auf den Downloads-Tab.

- **Windows / Linux:** Verknüpfung über die Checkbox in den Einstellungen (Windows:
  `HKCU\Software\Classes`; Linux: `.desktop` + `xdg-mime`). Mehrere gleichzeitig
  übergebene Links starten **keine** neuen Instanzen – dank Single-Instance landen sie
  in der bereits laufenden Anwendung.
- **macOS:** über das `.app`-Bundle (`build-macos-app.sh`) mit `ajfsp`-URL-Scheme. Der
  Link wird über einen nativen `NSAppleEventManager`-Handler entgegengenommen, weil
  Avalonia 11.2 das entsprechende Ereignis unter macOS nicht selbst auslöst.

## Bauen & Starten

Voraussetzung: .NET-SDK (net10.0).

```bash
dotnet restore
dotnet run --project Apfelmus.Avalonia
```

Es muss ein appleJuice-**Core** laufen, gegen den sich die GUI verbindet
(Standard: `localhost:9851`).

```bash
# macOS-App-Bundle bauen (Version aus Directory.Build.props):
./Apfelmus.Avalonia/build-macos-app.sh
# -> Apfelmus.Avalonia/bin/macos-app/Apfelmus.app (+ .zip)
```

> Avalonia-Pakete sind auf `12.1.0` gepinnt; `Tmds.DBus.Protocol` auf `0.94.1` (von
> Avalonia 12 gefordert, behebt zugleich Advisory GHSA-xrw6-gwf8-vvr9). Die letzte
> Avalonia-11-Fassung (5.4.0) liegt als Fallback im Branch `avalonia-11`.
>
> Migrations-Notizen (11 → 12): Doppelklick-Event `Gestures.DoubleTappedEvent` →
> `InputElement.DoubleTappedEvent`; Fenster-Chrome `ExtendClientAreaChromeHints` →
> `WindowDecorations` (None/BorderOnly/Full); `IClipboard.SetTextAsync` ist jetzt
> Extension-Methode in `Avalonia.Input.Platform`. Build ohne Telemetrie-Task:
> `AVALONIA_TELEMETRY_OPTOUT=1`.

## Aufbau

```
Program.cs            → Bootstrap + Single-Instance (Windows/Linux)
App.axaml(.cs)        → Theme/Ressourcen, Login→Hauptfenster, ajfsp-Verteilung
Services/CoreClient   → async-Hülle um WebConnect (baut /xml/*-Abfragen)
Services/MacUrlScheme → nativer macOS-Apple-Event-Handler (kAEGetURL) für ajfsp://
Services/            → SingleInstance, ProtocolHandlerService, PartlistRenderer, LanguageManager
ViewModels/           → MainWindowViewModel, LoginViewModel, ViewModelBase, RelayCommand
Views/                → MainWindow, LoginWindow, SplashWindow, RenameDialog
Converters/           → Avalonia-IValueConverter
Assets/               → Bilder, apfelmus.ico (.exe-Icon), i18n (Lang.de/en/it.axaml)
```

Zwei Fallstricke, die beim Nachvollziehen helfen:

- **DataGrid-Spalten mit Converter** binden mit `Mode=OneWay`: `DataGridTextColumn.Binding`
  ist sonst `TwoWay` und liefert dem Converter unter Avalonia 11.2 den Ziel-Default
  statt des Quellwerts (die Tabellen sind ohnehin read-only).
- **Netzwerk-Kennzahlen** stammen aus `modified.xml?filter=informations` – nicht aus
  `information.xml`, das keinen `<networkinfo>`-Block enthält.
