// Copyright © 2010-2014 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp;
using CefSharp.Example;
using CefSharp.Wpf;
using EK_App.Mvvm;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Net;


namespace EK_App.ViewModels
{
    public class BrowserTabViewModel : INotifyPropertyChanged
    {

        public delegate void Str_Msg_Handler(string msg);
        public delegate void Url_Changed(String url);
        public delegate void Load_Error(String url, String Error_Text, CefErrorCode code);
        public delegate void Console_Message_Handler(String msg);

        private string address;
        public string Address
        {
            get { return address; }
            set { PropertyChanged.ChangeAndNotify(ref address, value, () => Address); }
        }

        private string addressEditable;
        public string AddressEditable
        {
            get { return addressEditable; }
            set { PropertyChanged.ChangeAndNotify(ref addressEditable, value, () => AddressEditable); }
        }

        private string outputMessage;
        public string OutputMessage
        {
            get { return outputMessage; }
            set { PropertyChanged.ChangeAndNotify(ref outputMessage, value, () => OutputMessage); }
        }

        private string statusMessage;
        public string StatusMessage
        {
            get { return statusMessage; }
            set { PropertyChanged.ChangeAndNotify(ref statusMessage, value, () => StatusMessage); }
        }

        private string title;
        public string Title
        {
            get { return title; }
            set { PropertyChanged.ChangeAndNotify(ref title, value, () => Title); }
        }

        private IWpfWebBrowser webBrowser;
        public IWpfWebBrowser WebBrowser
        {
            get { return webBrowser; }
            set { PropertyChanged.ChangeAndNotify(ref webBrowser, value, () => WebBrowser); }
        }

        private object evaluateJavaScriptResult;

        public object EvaluateJavaScriptResult
        {
            get { return evaluateJavaScriptResult; }
            set { PropertyChanged.ChangeAndNotify(ref evaluateJavaScriptResult, value, () => EvaluateJavaScriptResult); }
        }

        private bool showSidebar;
        public bool ShowSidebar
        {
            get { return showSidebar; }
            set { PropertyChanged.ChangeAndNotify(ref showSidebar, value, () => ShowSidebar); }
        }
        private bool showConsoleMessage;
        public bool ShowConsoleMessage
        {
            get { return showConsoleMessage; }
            set { PropertyChanged.ChangeAndNotify(ref showConsoleMessage, value, () => ShowConsoleMessage); }
        }
        public event Url_Changed Url_Changed_Event = null;
        public event Load_Error Load_Error_Event = null;
        public event Console_Message_Handler Console_Message_Event = null;
        public ICommand GoCommand { get; set; }
        public ICommand HomeCommand { get; set; }
        public ICommand ExecuteJavaScriptCommand { get; set; }
        public ICommand EvaluateJavaScriptCommand { get; set; }
        public ICommand SaveLogCommand { get; set; }
        int Retries = 0;
        private string Old_Url = "";

        public event PropertyChangedEventHandler PropertyChanged;
        /*
        public bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {   // Accept all SSL certs
            Console.WriteLine("Accept SSL Cert");
            return true;
        }
         * */
        public BrowserTabViewModel(string address)
        {
            Address = address;
            AddressEditable = Address;

            GoCommand = new DelegateCommand(Go, () => !String.IsNullOrWhiteSpace(Address));
            SaveLogCommand = new DelegateCommand(SaveLog, () => true);
            HomeCommand = new DelegateCommand(() => AddressEditable = Address = CefExample.DefaultUrl);
            ExecuteJavaScriptCommand = new DelegateCommand<string>(ExecuteJavaScript, s => !String.IsNullOrWhiteSpace(s));
            EvaluateJavaScriptCommand = new DelegateCommand<string>(EvaluateJavaScript, s => !String.IsNullOrWhiteSpace(s));

            PropertyChanged += OnPropertyChanged;

            var version = String.Format("Chromium: {0}, CEF: {1}, CefSharp: {2}", Cef.ChromiumVersion, Cef.CefVersion, Cef.CefSharpVersion);
            OutputMessage = version;
//            ExecuteJavaScript("console.log('starting BrowserTabViewModel');");
        //    ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
        }

        private void EvaluateJavaScript(string s)
        {
            try
            {
                EvaluateJavaScriptResult = webBrowser.EvaluateScript(s) ?? "null";
            }
            catch (Exception e)
            {
                MessageBox.Show("Error while evaluating Javascript: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteJavaScript(string s)
        {
            try
            {
                if (Application.Current == null || Application.Current.MainWindow == null)
                    return;

          //      if (Application.Current.MainWindow.Visibility == Visibility.Visible)
                webBrowser.ExecuteScriptAsync(s);
            }
            catch (Exception e)
            {
                if (App.Debug)
                    MessageBox.Show("Error while executing Javascript: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    App.Log("Error while executing Javascript: " + e.Message);
            }
        }
        public void InvokeExecuteJavaScript(string s)
        {
            try
            {

                Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                { ExecuteJavaScript(s); }));

  //              ExecuteJavaScript(s);
         //       Str_Msg_Handler callback = new Str_Msg_Handler(ExecuteJavaScript);
             //   this.Invoke(callback, new object[] { s });

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("InvokeExecuteJavaScript exception " + e.Message);
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Address":
                    AddressEditable = Address;
                    break;

                case "WebBrowser":
                    if (WebBrowser != null)
                    {
                        WebBrowser.ConsoleMessage += OnWebBrowserConsoleMessage;
                        WebBrowser.StatusMessage += OnWebBrowserStatusMessage;
                        WebBrowser.LoadError += OnWebBrowserLoadError;

                        // TODO: This is a bit of a hack. It would be nicer/cleaner to give the webBrowser focus in the Go()
                        // TODO: method, but it seems like "something" gets messed up (= doesn't work correctly) if we give it
                        // TODO: focus "too early" in the loading process...
                        WebBrowser.FrameLoadEnd += delegate { Application.Current.Dispatcher.BeginInvoke((Action)(() => webBrowser.Focus())); };
                        WebBrowser.FrameLoadEnd += FrameLoadEndEventHandler;

                    }

                    break;
            }
        }
        private void FrameLoadEndEventHandler(object sender, FrameLoadEndEventArgs url)
        {
            Console.WriteLine("Finished loading: " + url);

            if (Old_Url != url.Url)
            {
                Old_Url = url.Url;
                if (Url_Changed_Event != null)
                    Url_Changed_Event(url.Url);                
            }
                
        }

        private void OnWebBrowserConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {

            if (ShowConsoleMessage)
                OutputMessage = e.Message;
            Console.WriteLine(e.Message);

            if (Console_Message_Event != null && Globals.ek.Browser != null)
                Console_Message_Event(e.Message);

            if (App.Web_Console_Log_File != null)
            {
                try
                {

                    using (System.IO.StreamWriter sw = System.IO.File.AppendText(App.Web_Console_Log_File))
                    {
                        sw.WriteLine(DateTime.Now.ToString("s") + " : " + e.Message);
                    }
                } 
                catch
                {
                    // error writing to file
                }
            }
        }

        private void OnWebBrowserStatusMessage(object sender, StatusMessageEventArgs e)
        {
            StatusMessage = e.Value;
        }

        private void OnWebBrowserLoadError(object sender, LoadErrorEventArgs args)
        {
            // Don't display an error for downloaded files where the user aborted the download.
            if (args.ErrorCode == CefErrorCode.Aborted)
                return;

            if (Retries > 3)
            {
                var errorMessage = "<html><body><h2>Failed to load URL " + args.FailedUrl +
                      " with error " + args.ErrorText + " (" + args.ErrorCode +
                      ").</h2></body></html>";

                webBrowser.LoadHtml(errorMessage, args.FailedUrl);

                if (Load_Error_Event != null)
                    Load_Error_Event(args.FailedUrl, args.ErrorText, args.ErrorCode);

                return;
            }
            Retries++;
            ExecuteJavaScript("document.location.href='" + args.FailedUrl + "'");

        }

        private String Get_Route_Debug()
        {
            System.Text.StringBuilder std_out = new System.Text.StringBuilder();
            // Start the child process.
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "route.exe";
            p.StartInfo.Arguments = "PRINT";

            p.Start();
            while (!p.HasExited)
            {
                std_out.Append(p.StandardOutput.ReadToEnd());
            }

      
            p.WaitForExit();

            p.Close();

            return std_out.ToString();    
        }

        private String Get_If_Debug()
        {
            System.Text.StringBuilder std_out = new System.Text.StringBuilder();
            // Start the child process.
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "ipconfig.exe";
            p.StartInfo.Arguments = "/all";

            p.Start();
            while (!p.HasExited)
            {
                std_out.Append(p.StandardOutput.ReadToEnd());
            }


            p.WaitForExit();

            p.Close();

            return std_out.ToString();
        }

        private void SaveLog()
        {
            Console.WriteLine("Saving Log");
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "XOkey_App_Log"; // Default file name
                dlg.DefaultExt = ".txt"; // Default file extension
                dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    // Save document
                    System.IO.File.Copy(App.Web_Console_Log_File, dlg.FileName, true);
                    string Route_Str = Get_Route_Debug();
                    string Interface_Str = Get_If_Debug();
                    using (System.IO.StreamWriter sw = System.IO.File.AppendText(dlg.FileName))
                    {
                        sw.WriteLine("");
                        sw.WriteLine("");
                        sw.WriteLine(DateTime.Now.ToString("s") + " : Routes");
                        sw.WriteLine(Route_Str);
                        sw.WriteLine("");
                        sw.WriteLine("");
                        sw.WriteLine(DateTime.Now.ToString("s") + " : Interfaces");
                        sw.WriteLine(Interface_Str);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Log("Error saving log: " + ex.Message);
            }

        }

        private void Go()
        {
            Address = AddressEditable;

            // Part of the Focus hack further described in the OnPropertyChanged() method...
            Keyboard.ClearFocus();
        }
    }
}
