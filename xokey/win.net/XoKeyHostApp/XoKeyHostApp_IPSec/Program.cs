using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace XoKeyHostApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Tell the WidowsInterop to Hook messages
            WindowsInterop.Hook();

            Application.Run(new ExoKeyHostAppForm());
        }
    }
}
