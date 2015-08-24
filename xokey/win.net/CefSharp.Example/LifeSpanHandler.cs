using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CefSharp.Example
{
    public class LifeSpanHandler : ILifeSpanHandler
    {
        public event Action<string> PopupRequest;

        public bool OnBeforePopup(IWebBrowser browser, string targetUrl, ref int x, ref int y, ref int width,
            ref int height)
        {
  //          if (PopupRequest != null)
 //               PopupRequest(targetUrl);

            System.Diagnostics.Process.Start(targetUrl);

            return true;
        }

        public void OnBeforeClose(IWebBrowser browser)
        {

        }
    }
}
