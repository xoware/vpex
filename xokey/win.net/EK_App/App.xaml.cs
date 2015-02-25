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
using System.Net.NetworkInformation;
using Hardcodet.Wpf.TaskbarNotification;

namespace EK_App
{
    
    public partial class App : Application
    {
        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);
        private const int ATTACH_PARENT_PROCESS = -1;
        public static bool Debug = false;

        string Cef_LogFile = null;
        public static string Web_Console_Log_File = null;
        private TaskbarIcon tb = null;
        bool EK_Is_Up = false;

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
            catch
            {
                Console.WriteLine("Could not log to " + App.Web_Console_Log_File);
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
                                    Web_Console_Log_File = args[i + 1];
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

                    if (Web_Console_Log_File != null)
                    {
                        // Create a file to write to. 
                        using (StreamWriter sw = File.CreateText(Web_Console_Log_File))
                        {
                            sw.WriteLine(DateTime.Now.ToString("s") + " : Startup");
                        }	
                    }

                } // if
            } catch (Exception ex)
            {
                Console.WriteLine("Exception in main: " + ex.Message);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            tb = (TaskbarIcon)FindResource("ExoKeyNotifyIcon");
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
        void App_Startup(object sender, StartupEventArgs e)
        {
            App.Log("app startup");
       


            NetworkChange.NetworkAddressChanged += new
              NetworkAddressChangedEventHandler(AddressChangedCallback);
            Check_Interfaces();
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
                    App.Log("ExoKey Down");
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
        void Raise_Window()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(new System.Action(() =>
            {
                App.Log("App: RaisingWindow");
                if (Application.Current.MainWindow == null)
                {
                    if (Globals.ek == null)
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
        private App()
        {

            ExceptionHandler.AsynchronousThreadExceptionHandler = new EKExceptionHandler();

            ProcessArgs();
            /*
            if (System.Diagnostics.Process.GetProcessesByName(
                System.IO.Path.GetFileNameWithoutExtension(
                System.Reflection.Assembly.GetEntryAssembly().Location)).Length > 1)
            {
                App.Log("Already Running:" + System.Reflection.Assembly.GetEntryAssembly().Location);
                Application.Current.Shutdown(0);
                System.Windows.MessageBox.Show("The ExoKey App is already running please check the system tray");
                return;

            } */

            CefExample.Init(Cef_LogFile, App.Debug);

        }
    }
}