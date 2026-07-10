# Apfelmus

Ein WPF-Desktop-Client für das appleJuice-Netzwerk (ein eDonkey2000-verwandtes P2P-Filesharing-Protokoll). Die GUI verbindet sich über eine XML/HTTP-API mit einem separat laufenden appleJuice-Core und zeigt/steuert dessen Zustand.

## Funktionsbereiche

- **Start** – Client-/Core-Version, Netzwerkstatus (User, Dateien, Firewall, IP), verbundener Server
- **Mein Share** – Freigegebene Verzeichnisse und Dateien, Prioritäten setzen
- **Suchen** – Volltextsuche im Netzwerk, Ergebnisse in dynamischen Tabs
- **Downloads** – laufende Downloads inkl. Part-/Verfügbarkeitsanzeige und Powerdownload-Bietsystem
- **Uploads** – aktive Uploads und Warteschlange
- **Server** – bekannte appleJuice-Server, Verbindungsstatus

Mehrsprachig (Deutsch/Englisch/Italienisch), umschaltbar über das Menü. Zwei Farbschemata (Dunkel/Hell, Anthrazit + Grün) über den Menüpunkt **Design**.

## Projektstruktur

| Projekt | Inhalt |
|---|---|
| `Apfelmus` | Die WPF-GUI selbst (Hauptfenster, Dialoge, Splashscreen) samt WPF-spezifischem Präsentationscode: Value-Converter (`Converters/`), Theme-/Sprachverwaltung und Single-Instance-Logik (`Logic/`) sowie die Bildressourcen (`Images/`) |
| `ApfelmusFramework` | **Plattformneutrale Kernbibliothek (`net10.0`, kein WPF/Windows):** Core-Kommunikation (XML/HTTP-API), Datenmodelle/DTOs, XML-(De)Serialisierung, Config und Hilfslogik. Bewusst UI-frameworkunabhängig, damit der WPF-Client und eine künftige Cross-Platform-Umsetzung (Avalonia) sich denselben Kern teilen |
| `WpfCustomControlLibrary1` | Eigenes `CloseableTabItem`-Control für die Such-Ergebnis-Tabs |
| `ConfigMigrator` | Einmal-Kommandozeilentool zur Migration alter `Config.dat` (BinaryFormatter) auf `Config.xml` (XmlSerializer), siehe unten |

## Konfigurationsmigration (Config.dat → Config.xml)

Ältere Installationen speichern ihre Einstellungen in `%AppData%\Apfelmus\Config.dat` (per `BinaryFormatter`). Ab dieser Version verwendet Apfelmus stattdessen `Config.xml` (per `XmlSerializer`) – dadurch hat die App selbst keine `BinaryFormatter`-Abhängigkeit mehr, was `BinaryFormatter` ab .NET 9 ohnehin nicht mehr existiert.

Beim ersten Start ohne vorhandene `Config.xml` legt Apfelmus einfach eine neue, leere Konfiguration an. Um stattdessen die bisherigen Einstellungen (Server, Passwort, Sprache, Theme, …) zu übernehmen, einmalig das Migrationstool ausführen:

```
dotnet run --project ConfigMigrator
```

Das Tool liest `Config.dat`, schreibt `Config.xml` im selben Ordner und benennt die alte Datei anschließend in `Config.dat.bak` um (sie wird nicht gelöscht).

## Bauen

Die WPF-GUI (`Apfelmus`) hat als Zielplattform **.NET 10 (`net10.0-windows7.0`) mit WPF – ausschließlich unter Windows**. WPF hat keine Cross-Plattform-Runtime, das gilt unabhängig von .NET Framework vs. .NET (Core). Die Kernbibliothek `ApfelmusFramework` ist dagegen plattformneutral (`net10.0`) und ließe sich auch außerhalb von Windows bauen.

Voraussetzung: Visual Studio 2022 (oder neuer) mit der Workload „.NET-Desktopentwicklung“.

```
dotnet restore
dotnet build
```

## Bekannte Einschränkungen

- Dateien müssen aktuell **unter 2 GB** bleiben (`Part.FromPosition` / `FileInformation.Filesize` sind als `int` statt `long` modelliert).

## Lizenz

GPL-2.0-or-later, siehe [LICENSE](LICENSE).
