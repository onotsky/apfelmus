using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;

namespace ApfelmusFramework.Classes.Logic
{
    /// <summary>
    /// Erkennt eine bereits laufende Instanz ueber einen benannten Mutex und reicht
    /// Kommandozeilenargumente (z.B. aus einem ajfsp://-Link) per Named Pipe an sie weiter.
    /// Ersetzt die fruehere Implementierung ueber System.Runtime.Remoting (IpcChannel), das es
    /// unter modernem .NET (Core/5+) nicht mehr gibt.
    /// </summary>
    public class SingleInstance : IDisposable
    {
        public delegate void ArgsHandler(string[] args);

        public event ArgsHandler ArgsRecieved;

        private readonly string _pipeName;

        private readonly Mutex _mutex;
        private bool _owned;
        private Window _window;
        private CancellationTokenSource _serverCts;

        public SingleInstance(Guid appGuid)
        {
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            _pipeName = assemblyName + appGuid;

            _mutex = new Mutex(true, _pipeName, out _owned);
        }

        public void Dispose()
        {
            _serverCts?.Cancel();

            if (_owned) // always release a mutex if you own it
            {
                _owned = false;
                _mutex.ReleaseMutex();
            }
        }

        public void Run(Func<Window> showWindow, string[] args)
        {
            if (_owned)
            {
                // show the main app window
                _window = showWindow();
                // and start the service
                StartServer();
            }
            else
            {
                SendCommandLineArgs(args);
                Application.Current.Shutdown();
            }
        }

        private void StartServer()
        {
            _serverCts = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(_ => ServerLoop(_serverCts.Token));
        }

        private void ServerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (NamedPipeServerStream server = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1))
                    {
                        server.WaitForConnection();

                        using (StreamReader reader = new StreamReader(server, Encoding.UTF8))
                        {
                            string payload = reader.ReadToEnd();
                            string[] receivedArgs = string.IsNullOrEmpty(payload)
                                ? Array.Empty<string>()
                                : payload.Split('\n');

                            BringToFront();
                            ProcessArgs(receivedArgs);
                        }
                    }
                }
                catch (Exception)
                {
                    // log it
                }
            }
        }

        private void BringToFront()
        {
            _window.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (_window.WindowState == WindowState.Minimized)
                    _window.WindowState = WindowState.Normal;
                _window.Activate();
            }));
        }

        private void ProcessArgs(string[] args)
        {
            if (ArgsRecieved != null)
            {
                _window.Dispatcher.BeginInvoke((Action)(() =>
                {
                    ArgsRecieved(args);
                }));
            }
        }

        private void SendCommandLineArgs(string[] args)
        {
            try
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out))
                {
                    client.Connect(2000);

                    using (StreamWriter writer = new StreamWriter(client, Encoding.UTF8))
                    {
                        writer.Write(string.Join("\n", args));
                        writer.Flush();
                    }
                }
            }
            catch (Exception)
            {
                // log it
            }
        }
    }
}
