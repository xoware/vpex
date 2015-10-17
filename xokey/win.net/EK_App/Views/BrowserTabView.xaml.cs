// Copyright © 2010-2014 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CefSharp.Example;

namespace EK_App.Views
{
    public partial class BrowserTabView : UserControl
    {
        public BrowserTabView()
        {
            InitializeComponent();

            browser.RequestHandler = new RequestHandler();
            browser.LifeSpanHandler = new LifeSpanHandler();
            browser.DownloadHandler = new DownloadHandler();
            browser.LoadError += browser_LoadError;

        }



        private void ExecuteJavaScript(string s)
        {
            try
            {
                if (Application.Current == null || Application.Current.MainWindow == null)
                    return;

                //      if (Application.Current.MainWindow.Visibility == Visibility.Visible)
                browser.ExecuteScriptAsync(s);
            }
            catch (Exception e)
            {
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
                App.Log("InvokeExecuteJavaScript exception " + e.Message);
            }
        }

        private void Check_And_Reload_If_No_Login_Button()
        {
            InvokeExecuteJavaScript("setInterval(function(){  if(!document.getElementById('login_button'))  "
                       + "{ document.location.href='https://192.168.137.2/'; }}, 1000);");
        }

        void browser_LoadError(object sender, CefSharp.LoadErrorEventArgs e)
        {
            App.Log("LoadError: " + e.FailedUrl);
            try
            {
                if (e.FailedUrl.Contains("192.168."))
                {
                    Check_And_Reload_If_No_Login_Button();
                }
            }
            catch (Exception ex)
            {
                App.Log("Load error exception " + ex.Message);
            }
        }

        private void OnTextBoxGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        { 
            var textBox = (TextBox) sender;
            textBox.SelectAll();
        }

        private void OnTextBoxGotMouseCapture(object sender, MouseEventArgs e)
        {
            var textBox = (TextBox) sender;
            textBox.SelectAll();
        }
    }
}
