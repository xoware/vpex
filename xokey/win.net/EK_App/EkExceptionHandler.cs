using System;
using System.ServiceModel.Dispatcher; // ExceptionHandler 
using System.Collections.Generic;
using System.Linq;
using System.Text;

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



            return ShouldTerminateProcess(ex);
        }

        public bool ShouldTerminateProcess(Exception ex)
        {
            // Write your logic here.
            return true;
        }
    }
}
