using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using log4net;
using System.Text;
using ApfelmusFramework.Classes.Allgemein;

namespace ApfelmusFramework.Classes.Logic
{
    /// <summary>
    /// Kapselt die gesamte HTTP-Kommunikation mit dem appleJuice-Core ueber rohe Socket-GET-Requests.
    /// Zwei Aufruf-Familien: /xml/*.xml (abgefragter Zustand, via GetHttpResult als XML) und /function/*
    /// (Kommandos; StartXMLFunction feuert ohne Antwort, ExecuteCommand liest Status und Body der Antwort).
    /// Jede Anfrage traegt das Core-Passwort als MD5-Hex. IDisposable, da pro Poll-Zyklus viele
    /// Instanzen entstehen und der Socket garantiert geschlossen werden muss (frueher Leak).
    /// </summary>
    public class WebConnect : IDisposable
    {
        // Obergrenzen, damit ein fehlerhafter oder boesartiger Core den Speicher nicht fuellen kann.
        // Grosszuegig bemessen (sehr grosse Share-Listen), aber endlich.
        private const int MaxResponseBytes = 128 * 1024 * 1024;     // empfangene Rohdaten je Abfrage
        private const int MaxDecompressedChars = 256 * 1024 * 1024; // entpackter XML-Text
        private const int MaxCommandReplyChars = 64 * 1024;         // Antwort auf ein Kommando

        private string serverName;
        private int serverPort;
        private ILog logger;

        // Other managed resource this class uses.
        private Component component = new Component();
        // Track whether Dispose has been called.
        private bool disposed = false;

        #region Constructors
        // log4net EINMALIG konfigurieren. Frueher lief das in jedem Instanz-Ctor - bei den
        // vielen WebConnect-Instanzen pro Poll-Zyklus unnoetige, teure Wiederholung.
        static WebConnect()
        {
            log4net.Config.XmlConfigurator.Configure();
        }

        public WebConnect(string serverName, int serverPort)
        {
            this.serverName = serverName;
            this.serverPort = serverPort;
            logger = LogManager.GetLogger(typeof(WebConnect));
        }
        #endregion
        #region Methods

        /// <summary>
        /// Baut die GET-Anfrage. Steuerzeichen (v.a. CR/LF) in Pfad/Query oder Hostname werden
        /// abgewiesen: sonst koennten Werte aus Ordnernamen, Links oder Eingaben eigene Header bzw.
        /// eine zweite Anfrage einschleusen (HTTP Request Splitting).
        /// </summary>
        private string BuildRequest(string anfrage)
        {
            if (anfrage == null || HasControlChars(anfrage) || HasControlChars(serverName))
                throw new ArgumentException("Ungueltige Zeichen in der Core-Anfrage");

            return string.Format("GET {0} HTTP/1.1\r\nHost: {1}\r\nConnection: Close\r\n\r\n", anfrage, serverName);
        }

        private static bool HasControlChars(string value)
        {
            return value != null && value.Any(c => c < 0x20 || c == 0x7F);
        }

        /// <summary>
        /// Schickt ein Kommando ohne die Antwort zu lesen. Liefert true, wenn die Anfrage an den Core
        /// gesendet wurde; false, wenn er nicht erreichbar war oder das Senden scheiterte. Ob der Core
        /// das Kommando auch akzeptiert hat (z.B. falsches Passwort), laesst sich daraus NICHT ableiten.
        /// </summary>
        public bool StartXMLFunction(string anfrage)
        {
            try
            {
                string request = BuildRequest(anfrage);

                byte[] bytesSent = Encoding.ASCII.GetBytes(request);

                // using: Socket wird garantiert geschlossen (frueher blieb er offen -> Socket-/
                // Handle-Leak bei jedem Aufruf, ueber die Zeit Ressourcen-Erschoepfung).
                using (Socket s = GetSocket(serverName, serverPort))
                {
                    if (s == null)
                        return false;

                    s.Send(bytesSent, bytesSent.Length, 0);
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Aufruf der XML-Function", ex);
            }

            return false;
        }

        public string GetHttpResult(string anfrage, bool useZip)
        {
            try
            {
                string request = BuildRequest(anfrage);
                if (!useZip)
                    request = request.Replace("&mode=zip", string.Empty);

                byte[] bytesSent = Encoding.ASCII.GetBytes(request);
                byte[] bytesReceived = new byte[0x200];
                List<byte> zipBytes = new List<byte>();
                int count = 0;
                string header = string.Empty;

                // using: Socket wird garantiert geschlossen (frueher Leak bei jedem Aufruf).
                using (Socket s = GetSocket(serverName, serverPort))
                {
                if (s == null)
                    return null;

                s.Send(bytesSent, SocketFlags.None);

                long received = 0;
                do
                {
                    count = s.Receive(bytesReceived, bytesReceived.Length, 0);
                    received += count;
                    if (received > MaxResponseBytes)
                        throw new InvalidDataException("Antwort des Cores ueberschreitet das Limit");
                    if (header == string.Empty && useZip)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (bytesReceived.GetValue(i).ToString() == "10" && bytesReceived.GetValue(i + 1).ToString() == "10")
                            {
                                for (int j = i + 2; j < count; j++)
                                {
                                    zipBytes.Add((byte)bytesReceived.GetValue(j));
                                }
                                break;
                            }
                        }
                        header += Encoding.ASCII.GetString(bytesReceived, 0, count);
                        continue;
                    }

                    header += Encoding.ASCII.GetString(bytesReceived, 0, count);
                    if (useZip && count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            zipBytes.Add((byte)bytesReceived.GetValue(i));
                        }
                    }
                }
                while (count > 0);

                if (!useZip)
                {
                    return GetXmlOutHttpResponse(header);
                }
                else
                {
                    return DecompressHttpResponse(zipBytes.ToArray());
                }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim holen der XML-Informationen", ex);
            }

            return string.Empty;
        }

        /// <summary>
        /// Fuehrt ein /function/*-Kommando aus und liest die (kurze) Antwort des Cores mit.
        /// Sent sagt, ob die Anfrage den Core erreicht hat; StatusCode (0 = keine lesbare Antwort)
        /// und Body zeigen, was er geantwortet hat. Fehlt die Antwort oder reisst die Verbindung ab
        /// (z.B. beendet sich der Core bei exitcore sofort), ist das KEIN Fehler: Sent bleibt true.
        /// Die Antworttexte des Cores sind in der Schnittstellenbeschreibung nicht festgelegt - Aufrufer
        /// sollten sie anzeigen, aber nicht von bestimmten Texten abhaengig machen.
        /// </summary>
        public CommandReply ExecuteCommand(string anfrage)
        {
            try
            {
                string request = BuildRequest(anfrage);
                byte[] bytesSent = Encoding.ASCII.GetBytes(request);
                byte[] bytesReceived = new byte[0x200];
                StringBuilder response = new StringBuilder();

                using (Socket s = GetSocket(serverName, serverPort))
                {
                    if (s == null)
                        return new CommandReply();

                    s.Send(bytesSent, SocketFlags.None);

                    try
                    {
                        int count;
                        do
                        {
                            count = s.Receive(bytesReceived, bytesReceived.Length, 0);
                            response.Append(Encoding.ASCII.GetString(bytesReceived, 0, count));
                        }
                        while (count > 0 && response.Length <= MaxCommandReplyChars);
                    }
                    catch (SocketException)
                    {
                        // Kommando ist raus; die Antwort ging verloren (Timeout/Abbruch).
                    }
                }

                return ParseReply(response.ToString());
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Ausfuehren des Kommandos", ex);
            }

            return new CommandReply();
        }

        private static CommandReply ParseReply(string full)
        {
            int status = 0;
            if (full.StartsWith("HTTP/", StringComparison.Ordinal))
            {
                // Statuszeile: "HTTP/1.1 200 OK"
                int sp = full.IndexOf(' ');
                if (sp > 0 && sp + 4 <= full.Length && int.TryParse(full.AsSpan(sp + 1, 3), out int code))
                    status = code;
            }

            // Der Core trennt Header und Body teils mit \r\n\r\n, teils mit \n\n.
            int crlf = full.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            int lf = full.IndexOf("\n\n", StringComparison.Ordinal);
            string body;
            if (crlf >= 0 && (lf < 0 || crlf < lf)) body = full.Substring(crlf + 4);
            else if (lf >= 0) body = full.Substring(lf + 2);
            else body = status > 0 ? string.Empty : full;

            if (body.Length > MaxCommandReplyChars) body = body.Substring(0, MaxCommandReplyChars);
            return new CommandReply { Sent = true, StatusCode = status, Body = body.Trim() };
        }

        private Socket GetSocket(string hostName, int port)
        {
            Socket socket2 = null;
            try
            {
                IPAddress address = Dns.GetHostAddresses(serverName).FirstOrDefault();
                if (address == null)
                {
                    return null;
                }

                IPEndPoint remoteEP = new IPEndPoint(address, port);

                socket2 = new Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // Timeouts, damit ein haengender/nicht antwortender Core den aufrufenden Thread
                // nicht unbegrenzt blockiert (Send/Receive liefen sonst ewig).
                socket2.ReceiveTimeout = 15000;
                socket2.SendTimeout = 15000;

                // Connect mit Timeout: ohne diesen blockiert Connect bei nicht erreichbarem Core
                // die OS-Standardzeit (~20 s). BeginConnect + WaitOne begrenzt das auf 5 s.
                IAsyncResult connectResult = socket2.BeginConnect(remoteEP, null, null);
                if (!connectResult.AsyncWaitHandle.WaitOne(5000) || !socket2.Connected)
                {
                    socket2.Close();
                    return null;
                }

                socket2.EndConnect(connectResult);
                return socket2;
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Aufbau der Socketverbindung!", ex);
                socket2?.Close();
            }

            return null;
        }

        public static Serverlist GetXMLServerList()
        {

            Serverlist serverList = new Serverlist();
            string xmlUrl = "http://www.applejuicenet.cc/serverlist/xmllist.php";

            return (Serverlist)serverList.DeserializeToObj(xmlUrl);
        }

        public bool CheckSocket(string serverName, int serverPort)
        {
            try
            {
                using (Socket s = GetSocket(serverName, serverPort))
                {
                    return s != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private string GetXmlOutHttpResponse(string response)
        {
            try
            {
                return response.Substring(response.IndexOf("<"), response.LastIndexOf(">") + 1 - response.IndexOf("<")).Replace("\n\r", string.Empty);
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Formatieren der XML_Ausgabe", ex);
            }

            return string.Empty;
        }

        private string DecompressHttpResponse(byte[] zipBytes)
        {
            try
            {
                StringBuilder result = new StringBuilder();
                byte[] buffer = new byte[64 * 1024];
                using (MemoryStream mStream = new MemoryStream(zipBytes, 0, zipBytes.Length))
                {
                    for (int i = 0; i < 2; i++)
                        mStream.ReadByte();
                    using (DeflateStream stream = new DeflateStream(mStream, CompressionMode.Decompress))
                    {
                        int num;
                        do
                        {
                            num = stream.Read(buffer, 0, buffer.Length);
                            result.Append(Encoding.ASCII.GetString(buffer, 0, num));
                            if (result.Length > MaxDecompressedChars)
                                throw new InvalidDataException("Entpackte Antwort ueberschreitet das Limit");
                        }
                        while (num > 0);

                        return result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Entpacken der XML-Ausgabe", ex);
            }

            return string.Empty;
        }
        #endregion

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // Managed Ressourcen freigeben. Es gibt keine unmanaged Ressourcen mehr
                    // (Sockets werden direkt in den Methoden per using geschlossen), daher auch
                    // kein Finalizer noetig - das spart bei den vielen Instanzen GC-Druck.
                    component.Dispose();
                }
            }
            disposed = true;
        }
    }

    /// <summary>Ergebnis von <see cref="WebConnect.ExecuteCommand"/>.</summary>
    public sealed class CommandReply
    {
        /// <summary>true, wenn die Anfrage an den Core gesendet wurde.</summary>
        public bool Sent { get; init; }

        /// <summary>HTTP-Status der Antwort; 0, wenn keine lesbare Antwort kam.</summary>
        public int StatusCode { get; init; }

        /// <summary>Antwort-Body (getrimmt, auf 64 KB begrenzt).</summary>
        public string Body { get; init; } = string.Empty;
    }
}
