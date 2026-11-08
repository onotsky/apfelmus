// ajcore-logd: liest das Log des appleJuice-Cores (systemd-Journal oder Logdatei) und
// stellt die letzten Zeilen per HTTP (Bearer-Token) bereit. Nur Standardbibliothek.
package main

import (
	"bufio"
	"crypto/subtle"
	"encoding/json"
	"flag"
	"log"
	"net"
	"net/http"
	"os"
	"os/exec"
	"regexp"
	"runtime"
	"strconv"
	"strings"
	"sync"
	"time"
)

type Entry struct {
	ID   uint64 `json:"id"`
	Time string `json:"time"` // RFC3339, aus dem Journal
	Text string `json:"text"`
}

type Ring struct {
	mu      sync.Mutex
	buf     []Entry
	max     int
	nextID  uint64
	started time.Time
}

func (r *Ring) Add(t time.Time, text string) {
	r.mu.Lock()
	defer r.mu.Unlock()
	r.nextID++
	r.buf = append(r.buf, Entry{ID: r.nextID, Time: t.Format(time.RFC3339), Text: text})
	if len(r.buf) > r.max {
		r.buf = append([]Entry(nil), r.buf[len(r.buf)-r.max:]...)
	}
}

// Since liefert Einträge mit ID > since (höchstens limit, die ältesten zuerst).
func (r *Ring) Since(since uint64, limit int) ([]Entry, uint64) {
	r.mu.Lock()
	defer r.mu.Unlock()
	out := []Entry{}
	for _, e := range r.buf {
		if e.ID > since {
			out = append(out, e)
			if len(out) >= limit {
				break
			}
		}
	}
	next := since
	if len(out) > 0 {
		next = out[len(out)-1].ID
	}
	return out, next
}

var (
	reTime   = regexp.MustCompile(`^\d{2}:\d{2}:\d{2} `)
	reTicker = regexp.MustCompile(`^\d+kb - \d+ - \d+$`)
	reSecret = regexp.MustCompile(`(?i)(password|passwd|pass|pw)=\S+`)
)

// clean entfernt die Uhrzeit des Cores (das Journal hat einen eigenen Zeitstempel),
// verwirft den 10-Sekunden-Statusticker und die Start-"check"-Zeilen und maskiert Passwörter.
func clean(msg string, dropNoise bool) (string, bool) {
	msg = strings.TrimSpace(msg)
	msg = reTime.ReplaceAllString(msg, "")
	if msg == "" {
		return "", false
	}
	if dropNoise && (reTicker.MatchString(msg) || strings.HasPrefix(msg, "check ")) {
		return "", false
	}
	return reSecret.ReplaceAllString(msg, "$1=***"), true
}

type jline struct {
	Message json.RawMessage `json:"MESSAGE"`
	TS      string          `json:"__REALTIME_TIMESTAMP"`
}

// message: journald liefert MESSAGE als String oder (bei Nicht-UTF-8) als Byte-Array.
func message(raw json.RawMessage) string {
	var s string
	if json.Unmarshal(raw, &s) == nil {
		return s
	}
	var b []byte
	var ints []int
	if json.Unmarshal(raw, &ints) == nil {
		for _, i := range ints {
			b = append(b, byte(i))
		}
		return strings.ToValidUTF8(string(b), "?")
	}
	return ""
}

func follow(unit string, backlog int, dropNoise bool, ring *Ring) {
	backoff := time.Second
	first := true
	for {
		n := "0"
		if first {
			n = strconv.Itoa(backlog * 4) // Ticker wird verworfen, daher mehr einlesen
		}
		cmd := exec.Command("journalctl", "-u", unit, "-o", "json", "--no-pager", "-f", "-n", n,
			"--output-fields=MESSAGE")
		out, err := cmd.StdoutPipe()
		if err == nil {
			err = cmd.Start()
		}
		if err != nil {
			log.Printf("journalctl: %v", err)
		} else {
			backoff = time.Second
			sc := bufio.NewScanner(out)
			sc.Buffer(make([]byte, 0, 64*1024), 1024*1024)
			for sc.Scan() {
				var j jline
				if json.Unmarshal(sc.Bytes(), &j) != nil {
					continue
				}
				text, ok := clean(message(j.Message), dropNoise)
				if !ok {
					continue
				}
				t := time.Now()
				if us, e := strconv.ParseInt(j.TS, 10, 64); e == nil {
					t = time.UnixMicro(us)
				}
				ring.Add(t, text)
			}
			_ = cmd.Wait()
			log.Printf("journalctl beendet, starte neu")
		}
		first = false
		time.Sleep(backoff)
		if backoff < 30*time.Second {
			backoff *= 2
		}
	}
}

func main() {
	listen := flag.String("listen", "127.0.0.1:9852", "Adresse:Port")
	tokenFile := flag.String("token-file", "", "Datei mit dem Bearer-Token (Pflicht)")
	file := flag.String("file", "", "Logdatei des Cores verfolgen (Windows/macOS/Linux); Pflicht ausserhalb von Linux")
	unit := flag.String("unit", "ajcore", "systemd-Dienst des Cores (Linux, wenn -file nicht gesetzt ist)")
	size := flag.Int("buffer", 2000, "Zeilen im Ringpuffer")
	allow := flag.String("allow", "10.1.0.0/16", "erlaubtes Netz (CIDR); Anfragen von anderswo werden abgewiesen")
	keepNoise := flag.Bool("keep-noise", false, "Statusticker und check-Zeilen nicht verwerfen")
	flag.Parse()

	if *tokenFile == "" {
		log.Fatal("-token-file ist Pflicht")
	}
	tb, err := os.ReadFile(*tokenFile)
	if err != nil {
		log.Fatal(err)
	}
	token := strings.TrimSpace(string(tb))
	if len(token) < 16 {
		log.Fatal("Token zu kurz (mindestens 16 Zeichen)")
	}

	_, allowNet, err := net.ParseCIDR(*allow)
	if err != nil {
		log.Fatalf("-allow: %v", err)
	}

	ring := &Ring{max: *size, started: time.Now()}
	switch {
	case *file != "":
		go tailFile(*file, *size, !*keepNoise, ring)
		log.Printf("liest Logdatei %s", *file)
	case runtime.GOOS == "linux":
		go follow(*unit, *size, !*keepNoise, ring)
	default:
		log.Fatal("auf diesem System gibt es kein systemd-Journal: bitte -file <Logdatei des Cores> angeben")
	}

	mux := http.NewServeMux()
	mux.HandleFunc("/healthz", func(w http.ResponseWriter, r *http.Request) { w.Write([]byte("ok\n")) })
	mux.HandleFunc("/log", func(w http.ResponseWriter, r *http.Request) {
		host, _, _ := net.SplitHostPort(r.RemoteAddr)
		if ip := net.ParseIP(host); ip == nil || !allowNet.Contains(ip) {
			http.Error(w, "forbidden", http.StatusForbidden)
			return
		}
		got := strings.TrimPrefix(r.Header.Get("Authorization"), "Bearer ")
		if subtle.ConstantTimeCompare([]byte(got), []byte(token)) != 1 {
			http.Error(w, "unauthorized", http.StatusUnauthorized)
			return
		}
		since, _ := strconv.ParseUint(r.URL.Query().Get("since"), 10, 64)
		limit, _ := strconv.Atoi(r.URL.Query().Get("limit"))
		if limit <= 0 || limit > 1000 {
			limit = 500
		}
		entries, next := ring.Since(since, limit)
		w.Header().Set("Content-Type", "application/json")
		w.Header().Set("Cache-Control", "no-store")
		// epoch ändert sich bei Neustart des Dienstes; dann since zurücksetzen.
		json.NewEncoder(w).Encode(map[string]any{
			"epoch": ring.started.Unix(), "next": next, "entries": entries,
		})
	})
	srv := &http.Server{Addr: *listen, Handler: mux, ReadHeaderTimeout: 5 * time.Second,
		WriteTimeout: 10 * time.Second}
	log.Printf("ajcore-logd lauscht auf %s", *listen)
	log.Fatal(srv.ListenAndServe())
}
