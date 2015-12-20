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


            Loaded += MainWindowLoaded;

        }
     
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
                Globals.ek = new XOkey(null, BrowserTabs[0]);
            else
            {
                // reasign browser to EK thread and restart detection
                Globals.ek.Set_Browser(BrowserTabs[0]);
                Globals.ek.Force_Restart_Detction = true;
            }

            Application.Current.MainWindow.Visibility = System.Windows.Visibility.Hidden;
         //   ek.Browser.InvokeExecuteJavaScript("console.log('MainWindowLoaded');");
        //    Check_Interfaces();
        }
  
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

        // This is called when closing or hiding the main UI
        private void MainWindow_Closing(object sender, CancelEventArgs e)  
        {
            App.Log("MainWindow_Closing");
            if (Globals.ek != null)
            {
                Globals.ek.Force_Restart_Detction = true;
                Globals.ek.Browser = null;  // detach browser from running EK thread
            }
            App.Log("MainWindow_Closing remove DNS");
            Xoware.NetUtil.DNS.Remove_XOkey_DNS(); // Now disconneced

            // if exiting just return
            if (App.Keep_Running == false)
                return;

            App.Log("MainWindow_Closing hide window");
            //Hide Window
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (System.Windows.Threading.DispatcherOperationCallback)delegate(object o)
            {
                Hide();
                return null;
            }, null);
            //Do not close application

        }

    }
}
