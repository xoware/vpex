using System;
using System.Linq;

namespace CefSharp.Example
{
    public static class CefExample
    {
          public const string DefaultUrl = "custom://cefsharp/home";
//        public const string DefaultUrl = "file://exokey.html";

        // Use when debugging the actual SubProcess, to make breakpoints etc. inside that project work.
        private const bool debuggingSubProcess = false;

        public static void Init(String LogFile = null)
        {
            var settings = new CefSettings();
            settings.RemoteDebuggingPort = 8088;
            settings.IgnoreCertificateErrors = true;
            if (LogFile != null)
            {
                settings.LogFile = LogFile;
                settings.LogSeverity = LogSeverity.Verbose;
            }

//            if (debuggingSubProcess)
//            {
//                settings.BrowserSubprocessPath = "..\\..\\..\\..\\CefSharp.BrowserSubprocess\\bin\\x86\\Debug\\CefSharp.BrowserSubprocess.exe";
//            }

            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = CefSharpSchemeHandlerFactory.SchemeName,
                SchemeHandlerFactory = new CefSharpSchemeHandlerFactory()
            });

            if (!Cef.Initialize(settings))
            {
                if (Environment.GetCommandLineArgs().Contains("--type=renderer"))
                {
                    Environment.Exit(0);
                }
                else
                {
                    return;
                }
            }

            Cef.RegisterJsObject("bound", new BoundObject());
        }
    }
}
