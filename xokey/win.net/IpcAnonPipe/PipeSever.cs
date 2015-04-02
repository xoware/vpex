using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace IpcAnonPipe
{
    public class PipeServer
    {

        public static void ExecServer()
        {
            Console.WriteLine("[SERVER] starting");

            using (NamedPipeServerStream pipeServer =
           new NamedPipeServerStream(PipeClient.PIPE_HANDLE, PipeDirection.In))
            {
                // Show that anonymous pipes do not support Message mode. 
                try
                {
                    Console.WriteLine("[SERVER] Setting ReadMode to \"Message\".");
                  //  pipeServer.ReadMode = PipeTransmissionMode.Message;
                }
                catch (NotSupportedException e)
                {
                    Console.WriteLine("[SERVER] Exception:\n    {0}", e.Message);
                }

                Console.WriteLine("[SERVER] Current TransmissionMode: {0}.",
                    pipeServer.TransmissionMode);

                pipeServer.WaitForConnection();

                using (StreamReader sr = new StreamReader(pipeServer))
                {
                    // Display the read text to the console 
                    string temp;

                    // Wait for 'sync message' from the server. 
                    do
                    {
                        Console.WriteLine("[CLIENT] Wait for sync...");
                        temp = sr.ReadLine();
                    }
                    while (!temp.StartsWith("SYNC"));

                    // Read the server data and echo to the console. 
                    while ((temp = sr.ReadLine()) != null)
                    {
                        Console.WriteLine("[CLIENT] Echo: " + temp);
                    }
                }

            }
            Console.WriteLine("[SERVER] Client quit. Server terminating.");
        }
    }
}
