# Architektur & Entwicklungshinweise

Aufbau, Konventionen und Fallstricke des Repositories für die Weiterentwicklung. Funktionsumfang und Installation stehen im [README](README.md).

## Überblick

Apfelmus ist eine plattformübergreifende Desktop-GUI (Avalonia, Windows/macOS/Linux) für das **appleJuice**-Netzwerk, ein eDonkey2000-verwandtes P2P-Protokoll. Die GUI enthält selbst keine Netzwerklogik des P2P-Netzes: Sie spricht über eine einfache **XML/HTTP-Schnittstelle** mit einem separat laufenden **appleJuice-Core** (ein anderes Programm, nicht Teil dieses Repos).

Bis 5.2.x war Apfelmus ein reiner WPF-Client. Dieser liegt im Branch `wpf-legacy` (zusammen mit dem Migrationstool `ConfigMigrator`); `main` enthält nur noch die Avalonia-Variante.

## Befehle

```bash
dotnet restore
dotnet build                                   # ganze Solution
dotnet run --project Apfelmus.Avalonia         # GUI starten (ein Core muss laufen)

./Apfelmus.Avalonia/build-macos-app.sh [rid]   # macOS-.app + DMG (osx-arm64 | osx-x64)
./release.sh [-n notes.md] [--dry-run]         # alle 6 Plattformen bauen + GitHub-Release
```

Es gibt kein Testprojekt und keinen Lint-Schritt. Änderungen lassen sich bauen (`dotnet build` muss ohne Fehler und Warnungen durchlaufen), das Verhalten gegen einen echten Core muss von Hand geprüft werden. Wurde eine Änderung nicht gegen einen Core ausprobiert, gehört das in die Beschreibung.

`AVALONIA_TELEMETRY_OPTOUT=1` setzen, wenn der Telemetrie-Build-Task in einer Sandbox am Schreiben in `~/Library/...` scheitert (`release.sh` tut das selbst).

## Projekte (`Apfelmus.sln`)

| Projekt | Inhalt |
|---|---|
| `Apfelmus.Avalonia` | Die Anwendung: `Views/` (Fenster, Dialoge), `ViewModels/`, `Converters/`, `Services/`, `Assets/` (Bilder, Sprachdateien `i18n/Lang.*.axaml`), Themes in `App.axaml` |
| `ApfelmusFramework` | Plattformneutrale Bibliothek (`net10.0`, kein UI-Bezug): Core-Kommunikation (`Classes/Logic/WebConnect.cs`), Datenmodelle/DTOs für die XML-Antworten (`Classes/Modified`, `Allgemein`, `Settings`, `Share`, …), Config, MD5-Hilfe |

Versionsnummer und gemeinsame Assembly-Metadaten stehen **einmal** in `Directory.Build.props` (SemVer; bei einer Änderung den Patch-Stand hochzählen, mehrere zusammengehörige Änderungen dürfen sich eine Version teilen).

### Namespace-Falle

`ApfelmusFramework.Classes.Serializer` und `ApfelmusFramework.Classes.Logic` sind Geschwister-Namespaces von `ApfelmusFramework.Classes.Config`. Innerhalb dieser Hierarchie löst ein bloßes `Config` den **Namespace** auf, nicht die Klasse (CS0118). Etabliert ist, die Klasse als `Config.Config` zu schreiben und das `using` für diesen Namespace wegzulassen (siehe `ConfigSerializer.cs`). Im Projekt `Apfelmus.Avalonia` gibt es die Kollision nicht; dort steht `using Config = ApfelmusFramework.Classes.Config.Config;`.

## Kommunikation mit dem Core

`WebConnect` (Framework) schickt **rohe Socket-GET-Anfragen** an den Core, ohne `HttpClient`. Zwei Familien:

- `/xml/*.xml` (`information`, `modified`, `share`, `settings`, `downloadpartlist`, `directory`, …): abgefragter Zustand, deserialisiert über die `IXmlSerializer`-Implementierungen der DTOs. `mode=zip` liefert gepackte Antworten.
- `/function/*` (`search`, `canceldownload`, `setpriority`, `serverlogin`, `setpassword`, `exitcore`, …): einmalige Kommandos.

`CoreClient` (`Apfelmus.Avalonia/Services`) ist die asynchrone Hülle darum und baut die Anfragen. Die Schnittstellenbeschreibung des Cores (PDF, Stand 2004) liegt im Core-Repo unter `doc/ajclientguicore.pdf`. Sie legt die Antworttexte der Kommandos **nicht** fest.

Regeln:

- **Passwort:** Jede Anfrage trägt `password=<md5>` in der Query. Der Core verlangt den MD5-Hex des Klartexts (`CreateMd5Hash`). Das ist eine Vorgabe des Protokolls, kein Sicherheitsentscheid der GUI – nicht „verbessern“ (z. B. bcrypt), sonst funktioniert kein Login. Folge: Die Query ist ein Zugangsdatum und darf nie in Logs, Fehlermeldungen oder das Protokoll-Tab geraten.
- **Kodierung:** Werte in der Query immer mit `Uri.EscapeDataString` kodieren, nicht nur Leerzeichen ersetzen. `WebConnect.BuildRequest` weist Steuerzeichen (CR/LF) ab (HTTP Request Splitting).
- **Grenzen:** `WebConnect` begrenzt empfangene und entpackte Antworten (128 MB / 256 MB) sowie Kommando-Antworten (64 KB); die `HttpClient`-Aufrufe (Updates, News, Serverliste) haben Timeout und Größenlimit. Neue Abfragen nicht daran vorbei bauen.
- **Kommandos:** `Fire` in `CoreClient` nutzt `ExecuteCommand` und schreibt Probleme ins Protokoll: Core nicht erreichbar, HTTP-Status ≥ 400, ungewöhnliche Antworttexte (nur als Info; als normal gelten eine leere Antwort, „ok“ und das leere HTML-Gerüst `<html><body></body></html>`, das der Core z. B. auf `/function/search` schickt). Ob der Core ein Kommando inhaltlich akzeptiert hat, lässt sich daraus nicht sicher ableiten.
- **Größen:** Alle Größen und Positionen aus dem XML sind `long` (oder `string` + `ParseLong`), weil Dateien über 2 GB vorkommen (ab 5.5.13, Core ≥ 0.35.185.x). `modified.xml` wird in einem Stück deserialisiert; ein einziges überlaufendes `int`-Attribut bricht das ganze Update.
- Externe Daten (Serverliste, GitHub-Releases, News) nur nach Prüfung weiterverwenden; URLs, die per Shell geöffnet werden, nur als `https://github.com/…` bzw. `http(s)`.

## GUI

- **MVVM ohne Framework:** `MainWindowViewModel` pollt den Core per `DispatcherTimer` (Refreshrate aus der Config) und hält alle Tabs. `RelayCommand` führt `Func<Task>` aus. Kompilierte Bindings sind Standard (`AvaloniaUseCompiledBindingsByDefault`); Stellen mit Item-Templates ohne Datentyp setzen `x:CompileBindings="False"`.
- **Tabs:** Start, Downloads, Uploads, Suchen, Server, Mein Share, Einstellungen, Protokoll. Neue Tabs am Ende anhängen: `SelectedTabIndex` wird per Index gesetzt (z. B. `FocusDownloads()` = 1).
- **Eigene Titelleiste:** `MainWindow` zeichnet seine Titelleiste selbst (`WindowDecorations="None"`, erweiterter Client-Bereich), damit sie dem App-Theme folgt statt der Systemeinstellung.
- **Themes:** Dunkel/Hell als `ThemeDictionaries` in `App.axaml` (Anthrazit + Grün). Farben nur als Token über `{DynamicResource …Brush}` verwenden (`SurfaceBrush`, `AccentBrush`, `TextPrimaryBrush`, `LogErrorBrush`, …), damit sie beim Wechsel live umschalten. Neue Token immer für **beide** Themes anlegen und den Kontrast prüfen (WCAG ≥ 4,5:1). Symbole sind Vektorgrafiken, die sich mit dem Theme umfärben; keine PNGs für Icons.
- **Sprachen:** Deutsch, Englisch, Italienisch zur Laufzeit umschaltbar. Jeder sichtbare Text kommt aus `Assets/i18n/Lang.{de,en,it}.axaml` (`{DynamicResource key}` im XAML, `LanguageManager.Get(key)` im Code). Neue Schlüssel in **allen drei** Dateien anlegen; es gibt keine Prüfung, ein fehlender Schlüssel erscheint als Schlüsselname. Präfixe: `t_` Tabs, `col_` Spalten, `msg_` Statusmeldungen, `lg_` Login, `log_` Protokoll, `ds_`/`us_` Download- bzw. Quellen-Status. Die Sprache wird schon vor dem Login-Fenster angewendet. Converter lesen ihren Text beim Aufbereiten der Zeile; nach einem Sprachwechsel erscheint er in einer Zeile neu, sobald sich ihr Wert ändert.
- **Protokoll-Tab (`EventLog`):** Ereignisse der GUI, nicht das Core-Log (die Schnittstelle liefert keins). `EventLog.Info/Warning/Error(key, args)` nimmt **nur Schlüssel** aus den Sprachdateien, keine freien Texte, damit nie eine rohe Anfrage mit Passwort im Protokoll landet. Nur im Speicher, höchstens 500 Einträge. Meldungen sind für Nicht-ITler geschrieben: keine Pfade, Codes oder Fachwörter, sondern was passiert ist und was der Nutzer tun kann („Download abbrechen“ ist nicht möglich: Der Core ist nicht erreichbar). Befehle heißen dafür `cmd_<name>` in den Sprachdateien und werden über `LogKey` eingesetzt; Dateinamen liest `LinkInfo` aus ajfsp-Links.
- **Partlisten-Balken:** `PartlistRenderer` ist ein **unabhängig entworfener** Entwurf (eine `WriteableBitmap`-Zeile pro Streifen, zeilenweiser Zwei-Zeiger-Durchlauf, formelbasierte Färbung nach Quellenzahl). Die frühere Fassung war eine Übersetzung der Java-GUI und damit ein GPL-Derivat; sie wurde bewusst neu geschrieben. **Nicht** erneut aus dem Quelltext von `gui-java` ableiten, das würde genau dieses Problem wieder einführen.

## Konfiguration

`Config` (`Classes/Config/Config.cs`) wird per `ConfigSerializer` als `Config.xml` im Anwendungsdatenordner (`%AppData%\Apfelmus` bzw. das Entsprechende unter macOS/Linux) gespeichert (`XmlSerializer`). Sie enthält u. a. den **MD5-Wert des Core-Passworts**, der für den Core als Login genügt; unter macOS/Linux wird die Datei daher mit Rechten `0600` geschrieben.

Es gibt bewusst **keine** `BinaryFormatter`-Abhängigkeit in diesem Repo. Alte `Config.dat`-Dateien des WPF-Clients migriert das Tool `ConfigMigrator` aus dem Branch `wpf-legacy` (siehe README); das Paket und das Flag, die es dafür braucht, gehören nicht nach `main`.

## Plattformintegration

- **Single-Instance (Windows/Linux):** Lock-Datei im Benutzerordner plus Named Pipe (`CurrentUserOnly`). Weitere Instanzen reichen ihren `ajfsp://`-Link an die laufende weiter und beenden sich. macOS braucht das nicht (das `.app`-Bundle ist ohnehin Single-Instance).
- **`ajfsp://`-Verknüpfung:** Windows `HKCU\Software\Classes`, Linux `.desktop` + `xdg-mime` (`ProtocolHandlerService`), macOS über `CFBundleURLSchemes` in der `Info.plist` und einen nativen `NSAppleEventManager`-Handler (`MacUrlScheme`, per Interop). Links werden dekodiert und einheitlich einmal kodiert, sonst entsteht `%257C`.
- **Linux-Icon:** `ProtocolHandlerService` legt eine passende `.desktop` samt Icon an, weil GNOME/Wayland das Fenster darüber zuordnet.
- **Updates:** `UpdateChecker` (GUI) und `CoreUpdateChecker` (Core inkl. Betas) fragen die GitHub-Releases-API ab, `NewsFetcher` die News-Seite. Fehler → `null`, die App läuft ohne die Info weiter.

## Releases

`release.sh` baut sechs Artefakte (Windows/Linux x64+arm64 als ZIP, macOS arm64+x64 als DMG), setzt den Tag `v<Version>` auf HEAD, erstellt das GitHub-Release und **löscht ältere Releases samt Tags** (nur `AJ_KEEP` und das neue bleiben). Ein bestehendes Release mit gleicher Nummer wird ersetzt.

- Die Builds sind **self-contained** und enthalten die .NET-Runtime des Build-SDKs. Vor jedem Release das SDK auf den aktuellen Patch-Stand bringen (`dotnet sdk check`), sonst wird eine Runtime mit bekannten Sicherheitslücken ausgeliefert.
- Das GitHub-Token liegt in der Datei, auf die `AJ_TOKEN_FILE` zeigt (Standard `~/aj_token`). Tokens laufen ab; schlägt der Tag-Push mit „Bad credentials“ fehl, ist meist das die Ursache.
- macOS-Bundles sind nur ad-hoc signiert und nicht notarisiert, deshalb als **DMG** ausgeliefert (ein ZIP zerstört die Signatur der .NET-DLLs beim Entpacken mit Fremdwerkzeugen). Die Release-Notizen nennen den `xattr`-Befehl dafür als Codeblock.

## Sonstiges

- **Lizenz:** GPL-2.0-or-later (`LICENSE`), eine bewusste Entscheidung, aktuell durch keine Abhängigkeit erzwungen.
- **Commits:** Keine KI-/Tool-`Co-Authored-By`-Zeilen in Commit-Nachrichten dieses Repos (ausdrücklicher Wunsch des Maintainers).
- **Zeilenenden:** Einige ältere Framework-Dateien (u. a. `WebConnect.cs`, `ServerListServer.cs`) haben CRLF und ein BOM. Beim Bearbeiten beibehalten, sonst zeigt der Diff die ganze Datei als geändert.
