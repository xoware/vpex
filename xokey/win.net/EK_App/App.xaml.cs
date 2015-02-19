// Copyright} © 2010-2014 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Windows;
using CefSharp.Example;
using System.Runtime.InteropServices;
using System.IO;
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
        private TaskbarIcon tb;

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
        private void InitApplication()
        {
            //initialize NotifyIcon
       //     tb = (TaskbarIcon)FindResource("ExoKeyNotifyIcon");
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            tb = (TaskbarIcon)FindResource("ExoKeyNotifyIcon");
        }
        protected override void OnExit(ExitEventArgs e)
        {
            tb.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
        }
        private App()
        {
            ProcessArgs();
            if (System.Diagnostics.Process.GetProcessesByName(
                System.IO.Path.GetFileNameWithoutExtension(
                System.Reflection.Assembly.GetEntryAssembly().Location)).Length > 1)
            {
                Console.WriteLine("Already Running");
                return;
            }

            InitApplication();
            CefExample.Init(Cef_LogFile, App.Debug);
        }
    }
}