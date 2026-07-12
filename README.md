# Apfelmus

Plattformübergreifende Desktop-GUI für das **appleJuice**-Netzwerk (ein eDonkey2000-verwandtes P2P-Filesharing-Protokoll), gebaut mit [Avalonia](https://avaloniaui.net/) und lauffähig unter **Windows, macOS und Linux**. Die Anwendung verbindet sich über eine XML/HTTP-API mit einem separat laufenden appleJuice-**Core** und zeigt bzw. steuert dessen Zustand.

Mehrsprachig (Deutsch/Englisch/Italienisch, zur Laufzeit umschaltbar), zwei Farbschemata (Dunkel/Hell, Anthrazit + Grün) und eine eigene rahmenlose Titelleiste.

> **Hinweis:** Bis 5.2.x war Apfelmus ein reiner **WPF**-Client (nur Windows). Ab 5.3 ist die Avalonia-Variante die Hauptanwendung. Der alte WPF-Client ist im Branch [`wpf-legacy`](../../tree/wpf-legacy) archiviert.

## Funktionsbereiche

- **Start** – GUI-/Core-Version, Netzwerkstatus (Nutzer, Dateien, Gesamtgröße, offene Verbindungen, Upload-Queue, Firewall, eigene IP) und der verbundene Server
- **Downloads** – laufende Downloads inkl. Status, Speed, Restzeit, Fortschritt und Powerdownload-Bietsystem; darunter die **Quellen** und ein **Partlisten-Verfügbarkeitsbalken**. Aktionen auch für Mehrfachauswahl, Tastenkürzel **F2–F9**
- **Uploads** – aktive Uploads und Warteschlange
- **Suchen** – Volltextsuche mit einem eigenen Ergebnis-Tab je Suchlauf
- **Server** – bekannte appleJuice-Server, Verbindungsstatus, offizielle Serverliste holen
- **Mein Share** – Verzeichnisbaum zum Freigeben/Entfernen von Ordnern, Freitextfilter, Prioritäten, Link(s)/Quelle(n) kopieren
- **Einstellungen** – GUI-Optionen sowie die kompletten Core-Einstellungen (Nick, Verzeichnisse, Ports, Limits, Speed pro Slot, Autoconnect) und Passwortänderung

Geklickte `ajfsp://`-Links werden an Apfelmus übergeben (Windows/Linux über die Einstellungen, macOS über das `.app`-Bundle). Details siehe [`Apfelmus.Avalonia/README.md`](Apfelmus.Avalonia/README.md).

## Projektstruktur

| Projekt | Inhalt |
|---|---|
| `Apfelmus.Avalonia` | Die Avalonia-GUI (Views, ViewModels, Converter, Themes, i18n, Services) – die eigentliche Anwendung |
| `ApfelmusFramework` | **Plattformneutrale Kernbibliothek (`net10.0`):** Core-Kommunikation (XML/HTTP-API), Datenmodelle/DTOs, XML-(De)Serialisierung, Config und Hilfslogik. Bewusst UI-frameworkunabhängig |
| `ConfigMigrator` | Einmal-Kommandozeilentool zur Migration alter `Config.dat` (BinaryFormatter) auf `Config.xml` (XmlSerializer), siehe unten |

Der alte WPF-Client (`Apfelmus`, `WpfCustomControlLibrary1`) liegt im Branch `wpf-legacy`.

## Konfigurationsmigration (Config.dat → Config.xml)

Ältere Installationen speichern ihre Einstellungen in `%AppData%\Apfelmus\Config.dat` (per `BinaryFormatter`). Aktuelle Versionen verwenden stattdessen `Config.xml` (per `XmlSerializer`) – dadurch hat die App selbst keine `BinaryFormatter`-Abhängigkeit mehr, die es ab .NET 9 ohnehin nicht mehr gibt.

Beim ersten Start ohne vorhandene `Config.xml` legt Apfelmus einfach eine neue, leere Konfiguration an. Um stattdessen die bisherigen Einstellungen (Server, Passwort, Sprache, Theme, …) zu übernehmen, einmalig das Migrationstool ausführen:

```
dotnet run --project ConfigMigrator
```

Das Tool liest `Config.dat`, schreibt `Config.xml` im selben Ordner und benennt die alte Datei anschließend in `Config.dat.bak` um (sie wird nicht gelöscht).

## Bauen & Starten

Voraussetzung: .NET-SDK (`net10.0`). Es muss ein appleJuice-**Core** laufen, gegen den sich die GUI verbindet (Standard: `localhost:9851`).

```bash
dotnet restore
dotnet run --project Apfelmus.Avalonia
```

```bash
# macOS-App-Bundle bauen (Version aus Directory.Build.props):
./Apfelmus.Avalonia/build-macos-app.sh
# -> Apfelmus.Avalonia/bin/macos-app/Apfelmus.app (+ .zip)
```

Fertige Builds für Windows, Linux und macOS gibt es unter [Releases](../../releases).

## Bekannte Einschränkungen

- Dateien müssen aktuell **unter 2 GB** bleiben (`Part.FromPosition` / `FileInformation.Filesize` sind als `int` statt `long` modelliert).

## Lizenz

GPL-2.0-or-later, siehe [LICENSE](LICENSE).
