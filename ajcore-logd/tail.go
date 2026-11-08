package main

import (
	"bytes"
	"log"
	"os"
	"time"
	"unicode/utf8"
)

// toUTF8 liefert gueltiges UTF-8. Unter Windows schreiben Java-Programme das Log oft in der
// Systemcodepage; ungueltige Bytes werden dann wie Latin-1/CP1252 gelesen statt verworfen.
func toUTF8(b []byte) string {
	if utf8.Valid(b) {
		return string(b)
	}
	r := make([]rune, len(b))
	for i, c := range b {
		r[i] = rune(c)
	}
	return string(r)
}

// tailFile verfolgt eine Logdatei wie "tail -F": liest beim Start den Rueckstand (die letzten
// Zeilen), danach nur noch Neues. Wird die Datei abgeschnitten (Core mit ">" neu gestartet) oder
// ausgetauscht (Rotation), beginnt es wieder am Anfang. Laeuft auf allen Systemen (Windows, macOS, Linux).
func tailFile(path string, backlog int, dropNoise bool, ring *Ring) {
	const chunk = 64 * 1024
	const maxLine = 64 * 1024
	var (
		f       *os.File
		off     int64
		partial []byte
		first   = true
		warned  bool
	)
	emit := func(raw []byte, t time.Time) {
		raw = bytes.TrimRight(raw, "\r")
		if text, ok := clean(toUTF8(raw), dropNoise); ok {
			ring.Add(t, text)
		}
	}
	// feed zerlegt neue Bytes in Zeilen; ein unvollstaendiges Ende bleibt fuer den naechsten Durchlauf liegen.
	feed := func(data []byte, t time.Time) {
		data = append(partial, data...)
		partial = nil
		for {
			i := bytes.IndexByte(data, '\n')
			if i < 0 {
				break
			}
			emit(data[:i], t)
			data = data[i+1:]
		}
		if len(data) > maxLine { // Zeile ohne Ende: nicht endlos puffern
			emit(data, t)
			data = nil
		}
		partial = append([]byte(nil), data...)
	}

	for {
		if f == nil {
			nf, err := os.Open(path)
			if err != nil {
				if !warned {
					log.Printf("Logdatei nicht lesbar (%v), versuche es weiter", err)
					warned = true
				}
				time.Sleep(2 * time.Second)
				continue
			}
			warned = false
			f = nf
			partial = nil
			off = 0
			if first {
				// Rueckstand: die letzten ~256 KB lesen, davon die letzten backlog*4 Zeilen behalten
				// (der Ticker wird verworfen, daher mehr einlesen).
				if st, err := f.Stat(); err == nil {
					start := st.Size() - 256*1024
					if start < 0 {
						start = 0
					}
					buf := make([]byte, st.Size()-start)
					n, _ := f.ReadAt(buf, start)
					buf = buf[:n]
					if start > 0 { // erste (angeschnittene) Zeile verwerfen
						if i := bytes.IndexByte(buf, '\n'); i >= 0 {
							buf = buf[i+1:]
						}
					}
					lines := bytes.Split(buf, []byte("\n"))
					if len(lines) > 0 && len(lines[len(lines)-1]) > 0 { // unvollstaendiges Ende
						partial = append([]byte(nil), lines[len(lines)-1]...)
					}
					if len(lines) > 0 {
						lines = lines[:len(lines)-1]
					}
					if keep := backlog * 4; len(lines) > keep {
						lines = lines[len(lines)-keep:]
					}
					for _, l := range lines {
						emit(l, st.ModTime())
					}
					off = start + int64(n)
				}
			}
			first = false
		}

		st, err := f.Stat()
		if err != nil {
			f.Close()
			f = nil
			continue
		}
		if st.Size() < off { // abgeschnitten
			off = 0
			partial = nil
		}
		for st.Size() > off {
			buf := make([]byte, chunk)
			n, _ := f.ReadAt(buf, off)
			if n <= 0 {
				break
			}
			off += int64(n)
			feed(buf[:n], time.Now())
		}
		// Rotation: unter dem Pfad liegt jetzt eine andere Datei -> die alte war schon leergelesen.
		if cur, err := os.Stat(path); err == nil && !os.SameFile(st, cur) {
			f.Close()
			f = nil
			continue
		}
		time.Sleep(500 * time.Millisecond)
	}
}
