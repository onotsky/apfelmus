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
    public class WebConnect : IDisposable
    {
        private string serverName;
        private int serverPort;
        private ILog logger;

        // Pointer to an external unmanaged resource.
        private IntPtr handle;
        // Other managed resource this class uses.
        private Component component = new Component();
        // Track whether Dispose has been called.
        private bool disposed = false;

        #region Constructors
        public WebConnect(IntPtr handle)
        {
            this.handle = handle;
            log4net.Config.XmlConfigurator.Configure();
            logger = LogManager.GetLogger(typeof(WebConnect));
        }

        public WebConnect(string serverName, int serverPort)
        {
            this.serverName = serverName;
            this.serverPort = serverPort;
            log4net.Config.XmlConfigurator.Configure();
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

                Socket s = GetSocket(serverName, serverPort);
                if (s == null)
                    return;

                s.Send(bytesSent, bytesSent.Length, 0);
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

                Socket s = GetSocket(serverName, serverPort);

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
            catch (Exception ex)
            {
                logger.Error("Fehler beim holen der XML-Informationen", ex);
            }

            return string.Empty;
        }

        private Socket GetSocket(string hostName, int port)
        {
            try
            {
                IPAddress address = Dns.GetHostAddresses(serverName).First();
                IPEndPoint remoteEP = new IPEndPoint(address, port);

                Socket socket2 = new Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // Timeouts, damit ein haengender/nicht antwortender Core den aufrufenden Thread
                // nicht unbegrenzt blockiert. Ohne diese liefen Send/Receive ewig - der Such-
                // bzw. UI-Thread blieb dann stehen.
                socket2.ReceiveTimeout = 15000;
                socket2.SendTimeout = 15000;
                socket2.Connect(remoteEP);
                if (socket2.Connected)
                {
                    return socket2;
                }


            }
            catch (SocketException ex)
            {
                logger.Error("Fehler beim Aufbau der Socketverbindung!", ex);
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
                Socket s = GetSocket(serverName, serverPort);

                if (s == null)
                {
                    return false;
                }
                else
                {
                    return true;
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
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    component.Dispose();
                }

                // Call the appropriate methods to clean up 
                // unmanaged resources here.
                // If disposing is false, 
                // only the following code is executed.
                CloseHandle(handle);
                handle = IntPtr.Zero;
            }
            disposed = true;
        }

        // Use interop to call the method necessary  
        // to clean up the unmanaged resource.
        [System.Runtime.InteropServices.DllImport("Kernel32")]
        private extern static Boolean CloseHandle(IntPtr handle);

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method 
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~WebConnect()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }
    }
}
