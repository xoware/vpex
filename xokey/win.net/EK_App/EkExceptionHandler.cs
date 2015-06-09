using System;
using System.ServiceModel.Dispatcher; // ExceptionHandler 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading; // DispatcherUnhandledExceptionEventArgs 

namespace EK_App
{
    class EKExceptionHandler : ExceptionHandler
    {
        // HandleException method override gives control to 
        // your code.
        public override bool HandleException(Exception ex)
        {
            // This method contains logic to decide whether 
            // the exception is serious enough
            // to terminate the process.

            EK_App.App.Log("exception:" + ex.Message);
            if (ex.InnerException != null)
            {
                EK_App.App.Log("Inner exception:" + ex.InnerException.Message);
            }

            try
            {
                Send_Exception(ex);
            }  catch (Exception send_ex)
            {
                EK_App.App.Log("send exception:" + send_ex.InnerException.Message);
            }

            return ShouldTerminateProcess(ex);
        }

        public static UnhandledExceptionEventHandler UnhandledException
        {
           get
			{
					return UnhandledExceptionHandler;
			
			}
        }
    		/// <summary>
		/// Used for handling general exceptions bound to the main thread.
		/// Handles the <see cref="AppDomain.UnhandledException"/> events in <see cref="System"/> namespace.
		/// </summary>
		/// <param name="sender">Exception sender object.</param>
		/// <param name="e">Real exception is in: ((Exception)e.ExceptionObject)</param>
		private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
		{
//			var executionFlow = new BugReport().Report((Exception)e.ExceptionObject, ExceptionThread.Main);
            try
            {

                MessageBoxResult result = System.Windows.MessageBox.Show(
                      "An un expected eror occured, and the App must close.  Press OK to send a report to x.o.ware or Cancel to Exit", 
                      "Unexpected Error",
                      MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Warning,
                       MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
                if (result == MessageBoxResult.OK)
                    Send_Exception((Exception)e.ExceptionObject);

                Xoware.NetUtil.DNS.Remove_XOkey_DNS(); 
            }
            catch (Exception send_ex)
            {
                EK_App.App.Log("send exception:" + send_ex.InnerException.Message);
            }

	        Environment.Exit(0);
		}

        public static void Send_Exception(Exception send_ex)
        {

            System.Net.WebResponse response = null;

            System.Net.HttpWebRequest wr = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://updates.vpex.org/windows/crash_report.cgi");
            wr.Method = "POST";
            wr.Timeout = 30000;
            wr.ContentType = "application/x-www-form-urlencoded";

            using (var writer = new System.IO.StreamWriter(wr.GetRequestStream()))
            {
                writer.Write("version=" + System.Web.HttpUtility.UrlEncode(Properties.Resources.BuildDate) +"&");

                String Trace_Str = "Ex: " + send_ex.ToString();

                if (send_ex.InnerException != null)
                    Trace_Str += "\nInner Ex: " + send_ex.InnerException.ToString();

                writer.Write("trace=" + System.Web.HttpUtility.UrlEncode(Trace_Str) + "&");

                String Log_Str = System.IO.File.ReadAllText(App.Web_Console_Log_File);
                writer.Write("log=" + System.Web.HttpUtility.UrlEncode(Log_Str) + "&");
            }

            try
            {
                response = wr.GetResponse();

            }
            catch (Exception ex)
            {
                EK_App.App.Log("exception:" + ex.Message);
                return;
            }
            //    Send_Log_Msg("GetVpnStatus: status=" + ((HttpWebResponse)response).StatusDescription, LogMsg.Priority.Debug);

         
            response.Close(); // cleanup;

        }
        public static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {

                MessageBoxResult result = System.Windows.MessageBox.Show(
                      "An un expected eror occured, and the App must close.  Press OK to send a report to x.o.ware or Cancel to Exit",
                      "Unexpected Error",
                      MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Warning,
                       MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
                if (result == MessageBoxResult.OK)
                    Send_Exception((Exception)e.Exception);

                Xoware.NetUtil.DNS.Remove_XOkey_DNS();
            }
            catch (Exception send_ex)
            {
                EK_App.App.Log("send exception:" + send_ex.InnerException.Message);
            }

            Environment.Exit(0);
        }
        public bool ShouldTerminateProcess(Exception ex)
        {
            // Write your logic here.
            return true;
        }
    }
}
