# ajcore-logd

Kleiner Begleitdienst für den appleJuice-Core: liest dessen Log aus dem systemd-Journal (der Core
loggt auf stdout, Einheit `ajcore`) und stellt die letzten Zeilen per HTTP bereit. Apfelmus zeigt sie
im Tab „Core-Log“ an (Einstellungen: Core-Log-Dienst URL + Token). Nur Go-Standardbibliothek.

## API

`GET /log?since=<id>&limit=<n>` mit `Authorization: Bearer <token>` →
`{"epoch":…,"next":<id>,"entries":[{"id","time","text"}]}`. Ändert sich `epoch`, wurde der Dienst neu
gestartet und der Client beginnt bei `since=0`. `GET /healthz` antwortet ohne Token mit `ok`.

Gefiltert wird: Core-Uhrzeit (das Journal hat eine eigene), der 10-Sekunden-Statusticker
(`…kb - 0 - 8`) und die `check /pfad`-Zeilen vom Start (`-keep-noise` schaltet das ab);
`password=…` o. ä. wird maskiert.

## Absicherung

- Token Pflicht (mindestens 16 Zeichen), zeitkonstanter Vergleich.
- Nur Anfragen aus `-allow` (Standard `10.1.0.0/16`) – zusätzlich `IPAddressDeny/Allow` in der Unit.
- Unverschlüsselt (HTTP): nur in einem vertrauenswürdigen LAN betreiben, sonst TLS-Proxy davor.

## Installation (archsrv01, root@10.1.5.250)

```bash
go build -o ajcore-logd .
install -m 755 ajcore-logd /usr/local/bin/
(umask 077; head -c 32 /dev/urandom | base64 | tr -d '=+/\n' > /etc/ajcore-logd.token)
install -m 644 ajcore-logd.service /etc/systemd/system/
systemctl daemon-reload && systemctl enable --now ajcore-logd
```

Token auslesen: `ssh root@10.1.5.250 cat /etc/ajcore-logd.token`. Die Unit liest es per
`LoadCredential` (Dienst läuft als `DynamicUser`, Gruppe `systemd-journal`).
Adresse/Port stehen in der Unit (`-listen 10.1.5.250:9852`).
