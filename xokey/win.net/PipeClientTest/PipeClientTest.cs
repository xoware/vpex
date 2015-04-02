using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeClientTest
{
    class PipeClientTest
    {
        static void Main(string[] args)
        {
            try
            {
                IpcAnonPipe.PipeClient.Send_Msg("TEST");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception:\n    {0}", e.Message);
            }
        }
    }
}
