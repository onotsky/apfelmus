# ajcore-logd

Kleiner Begleitdienst für den appleJuice-Core: liest dessen Log aus dem systemd-Journal und stellt die
letzten Zeilen per HTTP bereit. Apfelmus zeigt sie im Tab **Core-Log** an. Nur Go-Standardbibliothek,
keine weiteren Abhängigkeiten.

**Voraussetzungen:** Linux mit systemd, der Core läuft als systemd-Dienst und loggt auf stdout (also
ins Journal; prüfen mit `journalctl -u <Einheit> -n 20`), Go ab 1.21 zum Bauen.

## Einrichtung

Alles auf dem **Core-Rechner**, als root. `ajcore` steht hier für den Namen deiner Core-Einheit
(`systemctl list-units | grep -i aj`), `192.168.1.10` für die LAN-Adresse des Rechners und
`192.168.0.0/16` für das Netz, aus dem Apfelmus zugreifen darf – bitte anpassen.

```bash
# 1. Bauen und installieren
cd ajcore-logd
go build -o ajcore-logd .
install -m 755 ajcore-logd /usr/local/bin/

# 2. Token erzeugen (nur root lesbar). Es wird gleich in Apfelmus eingetragen.
(umask 077; head -c 32 /dev/urandom | base64 | tr -d '=+/\n' > /etc/ajcore-logd.token)

# 3. Unit installieren und anpassen
install -m 644 ajcore-logd.service /etc/systemd/system/
$EDITOR /etc/systemd/system/ajcore-logd.service
#   ExecStart:        -listen 192.168.1.10:9852   -allow 192.168.0.0/16   -unit ajcore
#   IPAddressAllow=   192.168.0.0/16   (dasselbe Netz wie bei -allow)
#   After=            Name der Core-Einheit

# 4. Starten
systemctl daemon-reload
systemctl enable --now ajcore-logd
```

Prüfen (vom Rechner mit Apfelmus aus):

```bash
curl -i http://192.168.1.10:9852/log                                   # -> 401 (ohne Token)
curl -s -H "Authorization: Bearer $(ssh root@192.168.1.10 cat /etc/ajcore-logd.token)" \
     "http://192.168.1.10:9852/log?limit=3"                            # -> JSON mit Zeilen
```

Dann in Apfelmus unter *Einstellungen → GUI* **Core-Log-Dienst (URL)** `http://192.168.1.10:9852` und
**Core-Log-Dienst (Token)** (Inhalt von `/etc/ajcore-logd.token`) eintragen und *GUI speichern*.
Beim Kopieren aus dem Terminal kein Leerzeichen und kein `%` (zsh-Markierung) mitnehmen.

## Optionen

| Option | Standard | Bedeutung |
|---|---|---|
| `-listen` | `127.0.0.1:9852` | Adresse:Port, auf dem der Dienst lauscht |
| `-allow` | `10.1.0.0/16` | erlaubtes Netz (CIDR); Anfragen von anderswo bekommen 403 |
| `-token-file` | – (Pflicht) | Datei mit dem Token (mindestens 16 Zeichen); in der Unit per `%d/token` |
| `-unit` | `ajcore` | systemd-Einheit des Cores |
| `-buffer` | `2000` | Zeilen im Ringpuffer |
| `-keep-noise` | aus | Statusticker und `check`-Zeilen nicht verwerfen |

## API

`GET /log?since=<id>&limit=<n>` mit `Authorization: Bearer <token>` →
`{"epoch":…,"next":<id>,"entries":[{"id","time","text"}]}`. Ändert sich `epoch`, wurde der Dienst neu
gestartet und der Client beginnt bei `since=0`. `GET /healthz` antwortet ohne Token mit `ok`.

Gefiltert wird: die Core-Uhrzeit (das Journal hat eine eigene), der 10-Sekunden-Statusticker
(`…kb - 0 - 8`) und die `check /pfad`-Zeilen vom Start; `password=…` o. ä. wird maskiert.

## Absicherung

- Token Pflicht, zeitkonstanter Vergleich.
- Nur Anfragen aus `-allow` – zusätzlich `IPAddressDeny=any`/`IPAddressAllow=` in der Unit
  (Absicherung auf Dienstebene, falls keine Firewall läuft).
- Läuft als `DynamicUser` mit Leserecht aufs Journal (`SupplementaryGroups=systemd-journal`), gehärtet
  (`ProtectSystem=strict`, `NoNewPrivileges` …); das Token wird per `LoadCredential` übergeben.
- Unverschlüsselt (HTTP): nur in einem vertrauenswürdigen Netz betreiben, sonst TLS-Proxy davor.
