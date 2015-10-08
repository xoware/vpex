// Copyright} © 2010-2014 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Windows;
using CefSharp.Example;
using System.Runtime.InteropServices;
using System.ServiceModel.Dispatcher; // ExceptionHandler
using System.IO;
using System.Net;
using System.Reflection;
using System.Net.NetworkInformation;
using Hardcodet.Wpf.TaskbarNotification;
using System.ComponentModel; // background worker

namespace EK_App
{
    
    public partial class App : Application
    {
        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);
        private const int ATTACH_PARENT_PROCESS = -1;
        public static bool Debug = false;

        string Cef_LogFile = null;
        public static string Web_Console_Log_File = Environment.SpecialFolder.LocalApplicationData + "/log.txt";
        private TaskbarIcon tb = null;
        bool EK_Is_Up = false;
        public static bool Keep_Running = true;
        private BackgroundWorker pipe_serverserv_bw = new BackgroundWorker();
      

        static void Show_Help(String Name)
        {
            Console.WriteLine("Program Usage:");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("--help                 Show this help message");
            Console.WriteLine("--debug                Enable debugging");
            Console.WriteLine("--log <filename>       Log to filename");
            Console.WriteLine("--cef-log <filename>   Chrome Log to filename");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("Example: " + Name + "  --debug  --log c:\\temp\\ek_log.csv");
        }

        static public void Log(string Message)
        {
            try
            {
                if (App.Web_Console_Log_File != null && Message != null)
                {
                    using (System.IO.StreamWriter sw = System.IO.File.AppendText(App.Web_Console_Log_File))
                    {
                        sw.WriteLine(DateTime.Now.ToString("s") + " : " + Message);
                    }
                }
                else
                {
                    Console.WriteLine(Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not log to " + App.Web_Console_Log_File 
                    + "  " + ex.Message);

                try {
                 System.IO.Directory.CreateDirectory(Path.GetDirectoryName(App.Web_Console_Log_File));
                } catch {

                }

            }
        }

        protected void ProcessArgs()
        {
   
            
            string[] args = Environment.GetCommandLineArgs();
            int n_args = args.Length;

            try
            {
                if (n_args > 1)
                {
                    // Attach to the parent process via AttachConsole SDK call
                    AttachConsole(ATTACH_PARENT_PROCESS);
                    Console.WriteLine("This is from the main program");
                    Console.WriteLine("nargs=" + n_args);

                    for (int i = 0; i < n_args; i++)
                    {
                        Console.WriteLine("arg[" + i + "] =" + args[i]);
                        switch (args[i])
                        {

                            case "--debug":
                                App.Debug = true;
                                break;
                            case "--help":
                                Show_Help(args[0]);
                                return;
                            case "--cef-log":
                                Cef_LogFile = args[i + 1];
                                break;
                            case "--log":
                                if ((i < n_args) && args[i + 1] != null)
                                {
                                    App.Web_Console_Log_File = args[i + 1];
                                    i++;
                                }
                                else
                                {
                                    Console.WriteLine("Logfile name required");
                                    Show_Help(args[0]);
                                    return;
                                }
                                break;
                        }
                    } // For

                    

                    

                } // if
                if (App.Web_Console_Log_File != null)
                {
                    System.IO.Directory.CreateDirectory(Environment.SpecialFolder.LocalApplicationData.ToString());
                    // Create a file to write to. 
                    using (StreamWriter sw = File.CreateText(Web_Console_Log_File))
                    {
                        sw.WriteLine(DateTime.Now.ToString("s") + " : Startup");
                    }
                }
            } catch (Exception ex)
            {
                Console.WriteLine("Exception in main: " + ex.Message);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {

            try
            {
                base.OnStartup(e);
  

                if (!Keep_Running)
                    return;
                

                //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
                tb = (TaskbarIcon)FindResource("XOkeyNotifyIcon");

                if (tb == null)
                    return;

      
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in OnStartup: " + ex.Message);
                Console.WriteLine("Exception in OnStartup: " + ex.StackTrace.ToString());
            }

            tb.Visibility = Visibility.Visible;
        }
        protected override void OnExit(ExitEventArgs e)
        {

            if (Globals.ek != null)
            {
                Globals.ek.Stop();
           //     ek.Dispose();
            }
            if (tb != null)  
               tb.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
        }

        private void PipeServerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                String Data_Read = Xoware.IpcAnonPipe.PipeServer.ExecServer();

                if (Data_Read.Contains("EXIT"))
                    Keep_Running = false;
            } catch
            {

            }
        }

        private void PipeServerWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!Keep_Running)
                return;

            Raise_Window();

            pipe_serverserv_bw.RunWorkerAsync(); // restart
        }
        private static void PreLoad(string path)
        {
            //S.O. NOTE: ELIDED - ALL EXCEPTION HANDLING FOR BREVITY

            //get all .dll files from the specified path and load the lot
            FileInfo[] files = null;
            //you might not want recursion - handy for localised assemblies 
            //though especially.
            files = new DirectoryInfo(path).GetFiles("*.dll",
                SearchOption.AllDirectories);

            System.Reflection.AssemblyName a = null;
            string s = null;
            foreach (var fi in files)
            {
                s = fi.FullName;
                App.Log("Trying " + s);
                try 
                {

                   

                    //now get the name of the assembly you've found, without loading it
                    //though (assuming .Net 2+ of course).
                    a = System.Reflection.AssemblyName.GetAssemblyName(s);
                    //sanity check - make sure we don't already have an assembly loaded
                    //that, if this assembly name was passed to the loaded, would actually
                    //be resolved as that assembly.  Might be unnecessary - but makes me
                    //happy :)


                    AppDomain currentDomain = AppDomain.CurrentDomain;
	
            	    //Make an array for the list of assemblies.
	        	    Assembly[] assems = currentDomain.GetAssemblies();
	
        		    //List the assemblies in the current application domain.
	        	    Console.WriteLine("List of assemblies loaded in current appdomain:");
                    foreach (Assembly assem in assems)
                    {

                        try
                        {
                            App.Log("assem:" + assem.ToString());
                            System.Reflection.Assembly.Load(assem.GetName());
                        } catch
                        {
                            continue;
                        }
                        break;
                    }
                    /*
                    if (!AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
                      System.Reflection.AssemblyName.ReferenceMatchesDefinition(a, assembly.GetName())))
                    {
                        //crucial - USE THE ASSEMBLY NAME.
                        //in a web app, this assembly will automatically be bound from the 
                        //Asp.Net Temporary folder from where the site actually runs.
                        System.Reflection.Assembly.Load(a);
                    }
                     * */
                    App.Log("loaded " + s);
                 } 
                catch
                {
                    continue;
                }
            }
        }
        private static void Load_Dlls(object state)
        {
            App.Log("Starting DLL Load");
            GC.KeepAlive(typeof(CefSharp.CefBrowserBase));
            GC.KeepAlive(typeof(CefSharp.Internals.CefSubprocess));
            GC.KeepAlive(typeof(CefSharp.Example.CefExample));
            PreLoad(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
            App.Log("Finished DLL Load");
        }
        private DateTime RetrieveLinkerTimestamp()
        {
            string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            System.IO.Stream s = null;

            try
            {
                s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.ToLocalTime();
            return dt;
        }
        void App_Startup(object sender, StartupEventArgs e)
        {
            App.Log("app startup: " + System.IO.Path.GetFileNameWithoutExtension(
                System.Reflection.Assembly.GetEntryAssembly().Location)
                + " Build Date;" + RetrieveLinkerTimestamp().ToString() );
            App.Log(System.Reflection.Assembly.GetEntryAssembly().Location);
            
 
            try
            {
                Xoware.IpcAnonPipe.PipeClient.Send_Msg("RAISE");
                App.Log("Already Running.  Raise message sent");
                Application.Current.Shutdown(0);
                return;
            }
            catch (Exception ex)
            { 
                // this is supposed to occur when not already running
                Console.WriteLine("Exception:\n    {0}", ex.Message);
            }

            pipe_serverserv_bw.DoWork += PipeServerWorker_DoWork;
            pipe_serverserv_bw.RunWorkerCompleted += PipeServerWorker_Completed;
            pipe_serverserv_bw.RunWorkerAsync();

            Console.WriteLine("App: " +System.IO.Path.GetFileNameWithoutExtension(
                 System.Reflection.Assembly.GetEntryAssembly().Location));
            Console.WriteLine("App2: " + 
                 System.Reflection.Assembly.GetEntryAssembly().Location);

            if (System.Diagnostics.Process.GetProcessesByName(
               System.IO.Path.GetFileNameWithoutExtension(
               System.Reflection.Assembly.GetEntryAssembly().Location)).Length > 1) 
            {
                Keep_Running = false;
                App.Log("Already Running:" + System.Reflection.Assembly.GetEntryAssembly().Location);
                Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                { Application.Current.Shutdown(0); }));
                System.Windows.MessageBox.Show("The XOkey App is already running please check the system tray");
                return;

            }
            Xoware.NetUtil.DNS.Remove_XOkey_DNS(); // Now disconneced at startup

            this.StartupUri = new System.Uri("MainWindow.xaml", System.UriKind.Relative);
            NetworkChange.NetworkAddressChanged += new
              NetworkAddressChangedEventHandler(AddressChangedCallback);
            Check_Interfaces();
            if (!EK_Is_Up)
            {
                // Key is unplugged, get started on background loading
                System.Threading.ThreadPool.QueueUserWorkItem(Load_Dlls);
            }
        }

        void Check_Interfaces()
        {

            NetworkInterface EK_Interface = null;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface n in adapters)
            {
                //         System.Console.WriteLine(n.Name + " : " + n.Description + " is " + n.OperationalStatus);

                if (n.Description.Contains("XoWare") || n.Description.Contains("x.o.ware"))
                {
                    EK_Interface = n;

                    if (n.OperationalStatus == OperationalStatus.Up)
                    {
                        if (EK_Is_Up == false)
                        {
                            Raise_Window();
                        }

                        EK_Is_Up = true;
                    }
                }
                else if (n.OperationalStatus == OperationalStatus.Down
                 && (n.Description.Contains("XoWare") || (n.Description.Contains("x.o.ware"))))
                {
                    App.Log("XOkey Down");
                }

            }
            if (EK_Interface == null)
            {
                EK_Is_Up = false;
            }
        }

        void AddressChangedCallback(object sender, System.EventArgs e)
        {
            Check_Interfaces();
        }
        public static void Raise_Window()
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(new System.Action(() =>
                {
                    App.Log("App: RaisingWindow");
                    if (Application.Current.MainWindow == null)
                    {
                    //    if (Globals.ek == null)
                        {
                            Application.Current.MainWindow = new MainWindow();
                            Application.Current.MainWindow.Show();
                            Application.Current.MainWindow.Visibility = System.Windows.Visibility.Visible;
                        }

                        return;
                    }

                    Application.Current.MainWindow.Show();
                    if (Application.Current.MainWindow.WindowState == System.Windows.WindowState.Minimized)
                        Application.Current.MainWindow.WindowState = System.Windows.WindowState.Normal;
                    Application.Current.MainWindow.Visibility = System.Windows.Visibility.Visible;
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in Raise_Window: " + ex.Message);
                Console.WriteLine("Exception in Raise_Window: " + ex.StackTrace.ToString());
            }
        }
        private App()
        {
            AppDomain.CurrentDomain.UnhandledException += EKExceptionHandler.UnhandledException;
            Application.Current.DispatcherUnhandledException += EKExceptionHandler.App_DispatcherUnhandledException;
            ExceptionHandler.AsynchronousThreadExceptionHandler = new EKExceptionHandler();

            ProcessArgs();
          
            CefExample.Init(Cef_LogFile, App.Debug);    
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {

        }
    }
}