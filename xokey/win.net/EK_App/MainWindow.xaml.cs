// Copyright © 2010-2014 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel; // CancelEventArgs 
//using System.Windows.Controls;
using System.Net;
using System.Net.NetworkInformation;
using CefSharp.Example;
using EK_App.ViewModels;
using EK_App.Mvvm;

namespace EK_App
{
    public partial class MainWindow : Window
    {
        private const string DefaultUrlForAddedTabs = "https://www.google.com";

        public ObservableCollection<BrowserTabViewModel> BrowserTabs { get; set; }

        bool EK_Is_Up = false;
       
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
     

            BrowserTabs = new ObservableCollection<BrowserTabViewModel>();

 //           CommandBindings.Add(new CommandBinding(ApplicationCommands.New, OpenNewTab));
  //          CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, CloseTab));

            Loaded += MainWindowLoaded;

          //  Application.Current.MainWindow.Visibility = System.Windows.Visibility.Hidden;

          //  NetworkChange.NetworkAddressChanged += new
        //      NetworkAddressChangedEventHandler(AddressChangedCallback);


            if (System.Diagnostics.Process.GetProcessesByName(
               System.IO.Path.GetFileNameWithoutExtension(
               System.Reflection.Assembly.GetEntryAssembly().Location)).Length > 1)
            {
                App.Log("Already Running:" + System.Reflection.Assembly.GetEntryAssembly().Location);
                Application.Current.Shutdown(0);
                System.Windows.MessageBox.Show("The ExoKey App is already running please check the system tray");
                return;

            } 


        }
        /*
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
         * */
        /*
        void AddressChangedCallback(object sender, System.EventArgs e)
        {
            Check_Interfaces();
        }
        */
        private void CloseTab(object sender, ExecutedRoutedEventArgs e)
        {
            if (BrowserTabs.Count > 0)
            {
                //Obtain the original source element for this event
                var originalSource = (FrameworkElement)e.OriginalSource;

                BrowserTabViewModel browserViewModel = null;

                if (originalSource is MainWindow)
                {
                    browserViewModel = BrowserTabs[TabControl.SelectedIndex];
                    BrowserTabs.RemoveAt(TabControl.SelectedIndex);
                }
                else
                {
                    //Remove the matching DataContext from the BrowserTabs collection
                    browserViewModel = (BrowserTabViewModel)originalSource.DataContext;
                    BrowserTabs.Remove(browserViewModel);
                }

                browserViewModel.WebBrowser.Dispose();
            }
        }

        private void OpenNewTab(object sender, ExecutedRoutedEventArgs e)
        {
            CreateNewTab();

            TabControl.SelectedIndex = TabControl.Items.Count - 1;
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            CreateNewTab(CefExample.DefaultUrl, App.Debug, App.Debug);

            if (Globals.ek == null)
                Globals.ek = new ExoKey(null, BrowserTabs[0]);

            Application.Current.MainWindow.Visibility = System.Windows.Visibility.Hidden;
         //   ek.Browser.InvokeExecuteJavaScript("console.log('MainWindowLoaded');");
        //    Check_Interfaces();
        }
      /*
        void Raise_Window()
        {
           
            System.Windows.Application.Current.Dispatcher.Invoke( new System.Action(() =>
            {
                App.Log("MainWindow: RaisingWindow");
                if (Application.Current.MainWindow == null)

                {

                    if (Globals.ek == null)
                        Application.Current.MainWindow = new MainWindow();
                    return;
                }
                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.Visibility = System.Windows.Visibility.Visible;
            }));
        }
            */
        private void CreateNewTab(string url = DefaultUrlForAddedTabs, bool showSideBar = false, bool showConsoleMessage = false)
        {
            BrowserTabViewModel bt = new BrowserTabViewModel(url) { 
                ShowSidebar = showSideBar,
                ShowConsoleMessage = showConsoleMessage
            };
            BrowserTabs.Add(bt);
        }

        private void OnClosing(System.ComponentModel.CancelEventArgs e) {
            App.Log("OnClosing");
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            App.Log("MainWindow_Closing");         
            
            //Hide Window
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (System.Windows.Threading.DispatcherOperationCallback)delegate(object o)
            {
                Hide();
                return null;
            }, null);
            //Do not close application
            e.Cancel = true;


            /*

            if (ek != null)
            {
                ek.Stop();
                ek.Dispose();
            }
             * */
        }

    }
}
