using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>
    /// Single-Instance fuer Windows/Linux: verhindert, dass jeder ajfsp://-Link (per
    /// Kommandozeilen-Argument) eine NEUE Instanz startet. Die Primaerwahl laeuft ueber eine
    /// exklusiv gesperrte Lock-Datei (funktioniert prozessuebergreifend auf beiden Plattformen);
    /// weitere Instanzen reichen ihren Link ueber eine Named Pipe an die laufende Instanz weiter
    /// und beenden sich. macOS braucht das nicht (das .app-Bundle ist ohnehin Single-Instance und
    /// bekommt Links per Apple-Event, siehe MacUrlScheme).
    /// </summary>
    public static class SingleInstance
    {
        private const string PipeName = "Apfelmus.SingleInstance.v1";
        private static FileStream? _lock;
        private static Action<string>? _onLink;
        private static readonly List<string> _queued = new();

        /// <summary>Wird von der App gesetzt: verarbeitet weitergeleitete Links (auf dem UI-Thread posten).</summary>
        public static void SetHandler(Action<string> onLink)
        {
            _onLink = onLink;
            lock (_queued)
            {
                foreach (var l in _queued) onLink(l);
                _queued.Clear();
            }
        }

        /// <summary>Versucht, exklusiver Primaerprozess zu werden (exklusive Lock-Datei).</summary>
        public static bool TryBecomePrimary()
        {
            try
            {
                string path = Path.Combine(Path.GetTempPath(), "apfelmus.singleinstance.lock");
                _lock = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                return false; // bereits gesperrt -> es laeuft eine Instanz
            }
        }

        /// <summary>Startet den Hintergrund-Loop, der Links von weiteren Instanzen entgegennimmt.</summary>
        public static void StartServer()
        {
            new Thread(ServerLoop) { IsBackground = true, Name = "SingleInstanceServer" }.Start();
        }

        /// <summary>Reicht den Link an die laufende Instanz weiter (mit kurzen Wiederholungen, falls
        /// der Server gerade erst hochfaehrt).</summary>
        public static void ForwardToRunning(string? link)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                try
                {
                    using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                    client.Connect(400);
                    using var w = new StreamWriter(client) { AutoFlush = true };
                    w.WriteLine(link ?? string.Empty);
                    return;
                }
                catch
                {
                    Thread.Sleep(200);
                }
            }
        }

        private static void ServerLoop()
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1,
                        PipeTransmissionMode.Byte, PipeOptions.None);
                    server.WaitForConnection();
                    using var r = new StreamReader(server);
                    string? link = r.ReadLine();
                    if (!string.IsNullOrWhiteSpace(link))
                    {
                        var cb = _onLink;
                        if (cb != null) cb(link!);
                        else lock (_queued) _queued.Add(link!);
                    }
                }
                catch
                {
                    Thread.Sleep(200);
                }
            }
        }
    }
}
