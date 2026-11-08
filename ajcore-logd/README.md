# ajcore-logd

Kleiner Begleitdienst für den appleJuice-Core: liest dessen Log und stellt die letzten Zeilen per HTTP
bereit. Apfelmus zeigt sie im Tab **Core-Log** an. Läuft auf **Linux, macOS und Windows** (jeweils x64
und ARM64), nur Go-Standardbibliothek, keine weiteren Abhängigkeiten.

Der Dienst läuft auf dem **Rechner, auf dem der Core läuft**, und kann das Log auf zwei Arten lesen:

| Quelle | Wann | Option |
|---|---|---|
| **Logdatei** | Windows, macOS, Linux (auch Docker/Unraid mit eingebundener Datei) | `-file <Pfad>` |
| **systemd-Journal** | Linux, Core läuft als systemd-Dienst und loggt auf stdout | `-unit <Einheit>` (Standard `ajcore`) |

## 1. Programm besorgen

Fertige Programme hängen am [Release](../../../releases/latest): `ajcore-logd-linux-amd64`,
`-linux-arm64`, `-darwin-amd64` (Intel-Mac), `-darwin-arm64` (Apple Silicon), `-windows-amd64.exe`,
`-windows-arm64.exe`. Oder selbst bauen (Go ab 1.21): `./build.sh` erzeugt alle in `dist/`.

## 2. Token erzeugen

Ein geheimes Token (mindestens 16 Zeichen) in einer Datei, die nur der Dienst lesen darf. Es wird gleich
in Apfelmus eingetragen.

```bash
# Linux/macOS
(umask 077; head -c 32 /dev/urandom | base64 | tr -d '=+/\n' > ~/.ajcore-logd.token)
```
```powershell
# Windows (PowerShell)
$b = New-Object byte[] 32; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
([Convert]::ToBase64String($b) -replace '[=+/]','') | Set-Content -NoNewline C:\ajcore-logd\token.txt
icacls C:\ajcore-logd\token.txt /inheritance:r /grant:r "SYSTEM:R" "Administrators:F"
```

## 3. Dienst einrichten

In den Beispielen ist `192.168.1.10` die LAN-Adresse des Core-Rechners und `192.168.0.0/16` das Netz,
aus dem Apfelmus zugreifen darf. Beides bitte anpassen.

### Windows

Der Core schreibt sein Log auf stdout. Damit der Dienst es lesen kann, den Core mit Umleitung in eine
Datei starten, z. B. in einer `start-core.bat`:

```bat
cd /d C:\appleJuice
java -Xmx4096m -Djava.library.path=core\. -jar core\ajcore.jar >> core.log 2>&1
```

(`core.log` wächst dabei ständig; gelegentlich leeren oder den Core neu starten. Wird die Datei
abgeschnitten oder ersetzt, liest der Dienst von vorn weiter.) Dienst als geplante Aufgabe beim Start,
in einer **Administrator-Eingabeaufforderung**:

```bat
schtasks /Create /TN ajcore-logd /SC ONSTART /RU SYSTEM /RL HIGHEST /F ^
  /TR "\"C:\ajcore-logd\ajcore-logd-windows-amd64.exe\" -listen 192.168.1.10:9852 -allow 192.168.0.0/16 -file C:\appleJuice\core.log -token-file C:\ajcore-logd\token.txt"
schtasks /Run /TN ajcore-logd
```

Windows-Firewall: Port nur für das eigene Netz öffnen (PowerShell, Administrator):

```powershell
New-NetFirewallRule -DisplayName ajcore-logd -Direction Inbound -Protocol TCP -LocalPort 9852 `
  -RemoteAddress 192.168.0.0/16 -Action Allow
```

Beim ersten Start warnt SmartScreen vor dem nicht signierten Programm („Weitere Informationen →
Trotzdem ausführen“). ARM-Rechner (Snapdragon/Surface) nehmen die `arm64`-Datei.

### macOS

Der Core muss in eine Datei loggen, z. B. mit `java -jar core/ajcore.jar >> core.log 2>&1` oder, wenn
er per launchd läuft, mit `StandardOutPath`/`StandardErrorPath` im Plist. Programm installieren, vom
Download-Schutz befreien und als LaunchAgent starten:

```bash
sudo install -m 755 ajcore-logd-darwin-arm64 /usr/local/bin/ajcore-logd     # Intel: -darwin-amd64
sudo xattr -d com.apple.quarantine /usr/local/bin/ajcore-logd 2>/dev/null
cp macos/ajcore-logd.plist ~/Library/LaunchAgents/local.ajcore-logd.plist
$EDITOR ~/Library/LaunchAgents/local.ajcore-logd.plist     # Adresse, Netz, Pfade (DEINNAME) anpassen
launchctl load ~/Library/LaunchAgents/local.ajcore-logd.plist
```

Die macOS-Firewall bleibt dafür aus oder erlaubt `ajcore-logd`; die Netzfreigabe übernimmt `-allow`.

### Linux mit systemd (Journal)

Funktioniert ohne Logdatei, solange der Core als systemd-Dienst läuft. `ajcore` steht für den Namen
der Core-Einheit (`systemctl list-units | grep -i aj`).

```bash
sudo install -m 755 ajcore-logd-linux-amd64 /usr/local/bin/ajcore-logd       # ARM: -linux-arm64
(umask 077; head -c 32 /dev/urandom | base64 | tr -d '=+/\n' | sudo tee /etc/ajcore-logd.token >/dev/null)
sudo install -m 644 ajcore-logd.service /etc/systemd/system/
sudo $EDITOR /etc/systemd/system/ajcore-logd.service
#   ExecStart:        -listen 192.168.1.10:9852   -allow 192.168.0.0/16   -unit ajcore
#   IPAddressAllow=   192.168.0.0/16   (dasselbe Netz wie bei -allow)
#   After=            Name der Core-Einheit
sudo systemctl daemon-reload && sudo systemctl enable --now ajcore-logd
```

Mit Logdatei statt Journal (z. B. Core in Docker): in der Unit `-unit ajcore` durch `-file /pfad/core.log`
ersetzen und der Unit Leserecht auf die Datei geben (`ReadOnlyPaths=`, bei `ProtectHome=yes` liegt
die Datei am besten außerhalb von `/home`).

## 4. Prüfen und in Apfelmus eintragen

```bash
curl -i http://192.168.1.10:9852/log                                  # -> 401 (ohne Token)
curl -s -H "Authorization: Bearer <token>" "http://192.168.1.10:9852/log?limit=3"   # -> JSON mit Zeilen
```

In Apfelmus unter *Einstellungen → GUI* **Core-Log-Dienst (URL)** `http://192.168.1.10:9852` und
**Core-Log-Dienst (Token)** eintragen, *GUI speichern*. Beim Kopieren aus dem Terminal kein Leerzeichen
und kein `%` (zsh-Markierung) mitnehmen.

## Optionen

| Option | Standard | Bedeutung |
|---|---|---|
| `-listen` | `127.0.0.1:9852` | Adresse:Port, auf dem der Dienst lauscht |
| `-allow` | `10.1.0.0/16` | erlaubtes Netz (CIDR); Anfragen von anderswo bekommen 403 |
| `-token-file` | – (Pflicht) | Datei mit dem Token (mindestens 16 Zeichen) |
| `-file` | – | Logdatei des Cores verfolgen; **Pflicht außerhalb von Linux** |
| `-unit` | `ajcore` | systemd-Einheit des Cores (Linux, wenn `-file` fehlt) |
| `-buffer` | `2000` | Zeilen im Ringpuffer |
| `-keep-noise` | aus | Statusticker und `check`-Zeilen nicht verwerfen |

## API

`GET /log?since=<id>&limit=<n>` mit `Authorization: Bearer <token>` →
`{"epoch":…,"next":<id>,"entries":[{"id","time","text"}]}`. Ändert sich `epoch`, wurde der Dienst neu
gestartet und der Client beginnt bei `since=0`. `GET /healthz` antwortet ohne Token mit `ok`.

Gefiltert wird: die Core-Uhrzeit (Journal/Zeitpunkt des Lesens liefern einen eigenen), der
10-Sekunden-Statusticker (`…kb - 0 - 8`) und die `check /pfad`-Zeilen vom Start; `password=…` o. ä. wird
maskiert. Logdateien in der Systemcodepage (Windows) werden als Latin-1 gelesen, Umlaute bleiben erhalten.

## Absicherung

- Token Pflicht, zeitkonstanter Vergleich.
- Nur Anfragen aus `-allow`; unter Linux zusätzlich `IPAddressDeny/Allow` in der Unit, unter Windows eine
  Firewall-Regel mit `-RemoteAddress`.
- Linux: läuft als `DynamicUser` mit Leserecht aufs Journal, gehärtet (`ProtectSystem=strict`,
  `NoNewPrivileges` …), Token per `LoadCredential`.
- Unverschlüsselt (HTTP): nur in einem vertrauenswürdigen Netz betreiben, sonst TLS-Proxy davor.
