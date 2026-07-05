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
| `Apfelmus` | Die GUI selbst (Hauptfenster, Dialoge, Splashscreen) |
| `ApfelmusFramework` | Core-Kommunikation (XML-API), Datenmodelle, Value-Converter, Theme-Verwaltung |
| `WpfCustomControlLibrary1` | Eigenes `CloseableTabItem`-Control für die Such-Ergebnis-Tabs |

## Bauen

Zielplattform ist **.NET 8 (`net8.0-windows`) mit WPF – ausschließlich unter Windows**. WPF hat keine Cross-Plattform-Runtime, das gilt unabhängig von .NET Framework vs. .NET (Core).

Voraussetzung: Visual Studio 2022 (oder neuer) mit der Workload „.NET-Desktopentwicklung“.

```
dotnet restore
dotnet build
```

## Bekannte Einschränkungen

- Dateien müssen aktuell **unter 2 GB** bleiben (`Part.FromPosition` / `FileInformation.Filesize` sind als `int` statt `long` modelliert).
- Die lokale Konfiguration (`Config.dat`) wird über `BinaryFormatter` serialisiert – das funktioniert nur bis einschließlich .NET 8 über einen expliziten Kompatibilitätsschalter und wird in .NET 9+ komplett entfernt. Eine Migration auf `XmlSerializer` (wie der Rest der Datenmodelle es bereits macht) steht noch aus.

## Verwandtes Projekt

`gui-java` ist der offizielle, plattformübergreifende appleJuice-Client (Java/Swing) für dasselbe Protokoll.
