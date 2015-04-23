using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeServerTest
{
    class PipeServerTest
    {
        static void Main(string[] args)
        {
            try
            {
                Xoware.IpcAnonPipe.PipeServer.ExecServer();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception:\n    {0}", e.Message);
            }
        }
    }
}
