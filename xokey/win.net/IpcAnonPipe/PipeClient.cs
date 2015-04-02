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

    public class PipeClient
    {
        public static readonly string PIPE_HANDLE = "ExoKey";


        public static void Send_Msg(string Mesg)
        {
           using (NamedPipeClientStream pipeClient =
            new NamedPipeClientStream(".", PIPE_HANDLE, PipeDirection.Out))
            {
                // Show that anonymous Pipes do not support Message mode. 
                try
                {
                    Console.WriteLine("[CLIENT] Setting ReadMode to \"Message\".");
           //         pipeClient.ReadMode = PipeTransmissionMode.Message;
                }
                catch (NotSupportedException e)
                {
                    Console.WriteLine("[CLIENT] Execption:\n    {0}", e.Message);
                }

                Console.WriteLine("Connecting to server...\n");
                pipeClient.Connect(321);

                Console.WriteLine("[CLIENT] Connected: {0} ",
                   pipeClient.IsConnected.ToString());

 //               Console.WriteLine("[CLIENT] Current TransmissionMode: {0}.",
//                   pipeClient.TransmissionMode);

           
                 try
                {
                    // Read user input and send that to the client process. 
                    using (StreamWriter sw = new StreamWriter(pipeClient))
                    {
                        sw.AutoFlush = true;
                        // Send a 'sync message' and wait for client to receive it.
                        sw.WriteLine("SYNC");
                        pipeClient.WaitForPipeDrain();
                        // Send the console input to the client process.
                        Console.Write("[CLIENT] Enter text: ");
                        sw.WriteLine(Mesg);
                    }
                }
                // Catch the IOException that is raised if the pipe is broken 
                // or disconnected. 
                catch (IOException e)
                {
                    Console.WriteLine("[CLIENT] Error: {0}", e.Message);
                }
            }


        }

    }
}
