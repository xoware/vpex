using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace XoKeyHostApp
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        static void Show_Help(String Name)
        {
            Console.WriteLine("Program Usage:");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("--help              Show this help message");
            Console.WriteLine("--debug             Enable debugging");
            Console.WriteLine("--log <filename>    Log to filename");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("Example: "+ Name  +"  --debug  --log c:\\temp\\ek_log.csv");
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool Debug = false;

            string LogFile = "";
            string[] args = Environment.GetCommandLineArgs();
            int n_args = args.Length;

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

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
                                Debug = true;
                                break;
                            case "--help":
                                Show_Help(args[0]);
                                return;
                            case "--log":
                                if ((i < n_args) && args[i + 1] != null)
                                {
                                    LogFile = args[i + 1];
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



                // Tell the WidowsInterop to Hook messages
                WindowsInterop.Hook();

                Application.Run(new ExoKeyHostAppForm(Debug, LogFile));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in main: " + ex.Message);
            }
        } 
       
    }
}
