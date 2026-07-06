//-----------------------------------------------------------------------
// <copyright file="WebConnect.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
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
    /// (Kommandos; StartXMLFunction feuert ohne Antwort, GetFunctionResult liest den Klartext-Body).
    /// Jede Anfrage traegt das Core-Passwort als MD5-Hex. IDisposable, da pro Poll-Zyklus viele
    /// Instanzen entstehen und der Socket garantiert geschlossen werden muss (frueher Leak).
    /// </summary>
    public class WebConnect : IDisposable
    {
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

        public void StartXMLFunction(string anfrage)
        {
            try
            {
                string request = string.Format("GET {0} HTTP/1.1\r\nHost: {1}\r\nConnection: Close\r\n\r\n", anfrage, serverName);

                byte[] bytesSent = Encoding.ASCII.GetBytes(request);

                // using: Socket wird garantiert geschlossen (frueher blieb er offen -> Socket-/
                // Handle-Leak bei jedem Aufruf, ueber die Zeit Ressourcen-Erschoepfung).
                using (Socket s = GetSocket(serverName, serverPort))
                {
                    if (s == null)
                        return;

                    s.Send(bytesSent, bytesSent.Length, 0);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Aufruf der XML-Function", ex);
            }
        }

        public string GetHttpResult(string anfrage, bool useZip)
        {
            try
            {
                string request = string.Format("GET {0} HTTP/1.1\r\nHost: {1}\r\nConnection: Close\r\n\r\n", anfrage, serverName);
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

                do
                {
                    count = s.Receive(bytesReceived, bytesReceived.Length, 0);
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
        /// Fuehrt einen /function/*-Aufruf aus und liefert den reinen Antwort-Body des Cores
        /// zurueck (HTTP-Header entfernt, KEINE XML-Extraktion). Im Gegensatz zu
        /// StartXMLFunction wird die Antwort ueberhaupt gelesen; im Gegensatz zu GetHttpResult
        /// wird sie nicht als XML interpretiert - die /function-Antworten (z.B. "ok",
        /// "already downloaded") sind kein wohlgeformtes XML und wuerden dort verworfen.
        /// </summary>
        public string GetFunctionResult(string anfrage)
        {
            try
            {
                string request = string.Format("GET {0} HTTP/1.1\r\nHost: {1}\r\nConnection: Close\r\n\r\n", anfrage, serverName);
                byte[] bytesSent = Encoding.ASCII.GetBytes(request);
                byte[] bytesReceived = new byte[0x200];
                StringBuilder response = new StringBuilder();
                int count;

                using (Socket s = GetSocket(serverName, serverPort))
                {
                    if (s == null)
                        return null;

                    s.Send(bytesSent, SocketFlags.None);

                    do
                    {
                        count = s.Receive(bytesReceived, bytesReceived.Length, 0);
                        response.Append(Encoding.ASCII.GetString(bytesReceived, 0, count));
                    }
                    while (count > 0);
                }

                string full = response.ToString();
                int idx = full.IndexOf("\r\n\r\n");
                return idx >= 0 ? full.Substring(idx + 4).Trim() : full.Trim();
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Ausfuehren der Function", ex);
            }

            return string.Empty;
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

        public List<string> GetServerOutHtml(string anfrage)
        {
            try
            {
                string request = String.Format("GET {0} HTTP/1.1\r\nHost: {1}\r\nConnection: Close\r\n\r\n", anfrage, serverName);
                byte[] bytesSent = Encoding.ASCII.GetBytes(request);
                byte[] bytesReceived = new byte[0x200];
                List<string> serverList = new List<string>();
                int count = 0;
                string header = string.Empty;

                using (TcpClient client = new TcpClient(serverName, serverPort))
                {
                    using (NetworkStream nStream = client.GetStream())
                    {
                        nStream.Write(bytesSent, 0, bytesSent.Length);
                        do
                        {
                            count = nStream.Read(bytesReceived, 0, bytesReceived.Length);
                            header += Encoding.ASCII.GetString(bytesReceived, 0, count);
                        }
                        while (count > 0);
                    }
                }

                int index = header.IndexOf("ajfsp://server|");
                int num2 = header.IndexOf("<!--TYPO3SEARCH_end-->");
                if ((index != -1) && (num2 != -1))
                {
                    header = header.Substring(index, num2 - index);
                    while (header.Contains("ajfsp://server|"))
                    {
                        int startIndex = header.IndexOf("ajfsp");
                        int endIndex = header.IndexOf("/\">");
                        if ((endIndex - startIndex) <= 0)
                        {
                            return serverList;
                        }

                        string serverLink = header.Substring(startIndex, endIndex - startIndex);
                        header = header.Substring(endIndex + 3, header.Length - (endIndex + 3));
                        if (header.IndexOf("<font color=#ff0000>") != 0)
                        {
                            serverList.Add(serverLink);
                        }
                    }
                }
                return serverList;
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim holen der Serverliste", ex);
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
                string str = string.Empty;

                byte[] buffer = new byte[zipBytes.Length];
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
                            str += Encoding.ASCII.GetString(buffer, 0, num);
                        }
                        while (num > 0);

                        return str;
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
}
