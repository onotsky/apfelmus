using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>
    /// Faengt eingehende ajfsp://-Links auf macOS ueber das klassische Apple-Event kAEGetURL ab.
    /// Notwendig, weil Avalonia 11.2 auf macOS das OpenUri-Event von IActivatableLifetime NICHT
    /// ausloest - die App kommt zwar in den Vordergrund, der Link wird aber nie zugestellt.
    /// Registriert einen Handler bei NSAppleEventManager (funktioniert bei Kalt- und Warmstart).
    /// </summary>
    [SupportedOSPlatform("macos")]
    public static class MacUrlScheme
    {
        private const string Objc = "/usr/lib/libobjc.A.dylib";

        // FourCharCodes: 'GURL' (Internet-Event-Klasse + GetURL) und '----' (keyDirectObject).
        private const uint kInternetEventClass = 0x4755524C;
        private const uint kAEGetURL = 0x4755524C;
        private const uint keyDirectObject = 0x2D2D2D2D;

        [DllImport(Objc)] private static extern IntPtr objc_getClass(string name);
        [DllImport(Objc)] private static extern IntPtr sel_registerName(string name);
        [DllImport(Objc)] private static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, IntPtr extraBytes);
        [DllImport(Objc)] private static extern void objc_registerClassPair(IntPtr cls);
        [DllImport(Objc)] private static extern bool class_addMethod(IntPtr cls, IntPtr sel, IntPtr imp, string types);

        [DllImport(Objc, EntryPoint = "objc_msgSend")] private static extern IntPtr Send(IntPtr receiver, IntPtr sel);
        [DllImport(Objc, EntryPoint = "objc_msgSend")] private static extern IntPtr Send_u(IntPtr receiver, IntPtr sel, uint arg);
        [DllImport(Objc, EntryPoint = "objc_msgSend")] private static extern void Send_reg(IntPtr receiver, IntPtr sel, IntPtr target, IntPtr selArg, uint cls, uint id);

        // Delegate festhalten, damit der Funktionszeiger nicht wegoptimiert/gesammelt wird.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void HandleUrlDelegate(IntPtr self, IntPtr cmd, IntPtr theEvent, IntPtr replyEvent);
        private static HandleUrlDelegate? _handlerDelegate;
        private static Action<string>? _onUrl;

        private static IntPtr _handler;   // einmalig erzeugte Handler-Instanz

        /// <summary>
        /// Registriert den Apple-Event-Handler. Idempotent: die ObjC-Klasse wird nur EINMAL erzeugt
        /// (erneutes objc_allocateClassPair mit gleichem Namen liefert nil -> Absturz), das
        /// setEventHandler darf aber mehrfach laufen (um Cocoas eigenen Handler zu ueberschreiben).
        /// Muss auf dem Main-Thread laufen (nach NSApp-Init).
        /// </summary>
        public static void Register(Action<string> onUrl)
        {
            _onUrl = onUrl;
            try
            {
                if (_handler == IntPtr.Zero)
                {
                    IntPtr nsObject = objc_getClass("NSObject");
                    IntPtr cls = objc_allocateClassPair(nsObject, "ApfelmusUrlHandler", IntPtr.Zero);
                    if (cls == IntPtr.Zero) return; // Klasse existiert schon o.ae. -> nicht erneut anlegen

                    _handlerDelegate = HandleUrl;
                    IntPtr imp = Marshal.GetFunctionPointerForDelegate(_handlerDelegate);
                    // Signatur: v@:@@  (void; self, _cmd, event, replyEvent)
                    class_addMethod(cls, sel_registerName("handleGetURLEvent:withReplyEvent:"), imp, "v@:@@");
                    objc_registerClassPair(cls);
                    _handler = Send(Send(cls, sel_registerName("alloc")), sel_registerName("init"));
                }

                if (_handler == IntPtr.Zero) return;
                IntPtr aem = Send(objc_getClass("NSAppleEventManager"), sel_registerName("sharedAppleEventManager"));
                Send_reg(aem, sel_registerName("setEventHandler:andSelector:forEventClass:andEventID:"),
                    _handler, sel_registerName("handleGetURLEvent:withReplyEvent:"), kInternetEventClass, kAEGetURL);
            }
            catch
            {
                // Interop fehlgeschlagen -> Links funktionieren nur nicht; App laeuft normal weiter.
            }
        }

        private static void HandleUrl(IntPtr self, IntPtr cmd, IntPtr theEvent, IntPtr replyEvent)
        {
            try
            {
                IntPtr desc = Send_u(theEvent, sel_registerName("paramDescriptorForKeyword:"), keyDirectObject);
                if (desc == IntPtr.Zero) return;
                IntPtr nsString = Send(desc, sel_registerName("stringValue"));
                if (nsString == IntPtr.Zero) return;
                IntPtr utf8 = Send(nsString, sel_registerName("UTF8String"));
                string? url = Marshal.PtrToStringUTF8(utf8);
                if (!string.IsNullOrWhiteSpace(url)) _onUrl?.Invoke(url!);
            }
            catch { }
        }
    }
}
