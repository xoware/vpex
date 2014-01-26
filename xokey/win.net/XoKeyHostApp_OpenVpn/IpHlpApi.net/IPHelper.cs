using System;
using System.Net;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IpHlpApidotnet
{
	public class IPHelper
	{
		private const int NO_ERROR  = 0;
		private const int MIB_TCP_STATE_CLOSED = 1;
		private const int MIB_TCP_STATE_LISTEN = 2;
		private const int MIB_TCP_STATE_SYN_SENT = 3;
		private const int MIB_TCP_STATE_SYN_RCVD = 4;
		private const int MIB_TCP_STATE_ESTAB = 5;
		private const int MIB_TCP_STATE_FIN_WAIT1 = 6;
		private const int MIB_TCP_STATE_FIN_WAIT2 = 7;
		private const int MIB_TCP_STATE_CLOSE_WAIT = 8;
		private const int MIB_TCP_STATE_CLOSING = 9;
		private const int MIB_TCP_STATE_LAST_ACK = 10;
		private const int MIB_TCP_STATE_TIME_WAIT = 11;
		private const int MIB_TCP_STATE_DELETE_TCB = 12;

		

		
		/*
		 * Tcp Struct
		 * */

		public IpHlpApidotnet.MIB_TCPTABLE TcpConnexion;
		public IpHlpApidotnet.MIB_TCPSTATS TcpStats;
		public IpHlpApidotnet.MIB_EXTCPTABLE TcpExConnexions;
		

		/*
		 * Udp Struct
		 * */
		public IpHlpApidotnet.MIB_UDPSTATS UdpStats;
		public IpHlpApidotnet.MIB_UDPTABLE UdpConnexion;
		public IpHlpApidotnet.MIB_EXUDPTABLE UdpExConnexion;
		



		
		
		public IPHelper()
		{
		
		}

		
		#region Tcp Function

		public void GetTcpStats()
		{
			TcpStats = new MIB_TCPSTATS();
			IPHlpAPI32Wrapper.GetTcpStatistics(ref TcpStats);
		}


		public void GetExTcpConnexions()
		{
			
			// the size of the MIB_EXTCPROW struct =  6*DWORD
			int rowsize = 24;
			int BufferSize = 100000;
			// allocate a dumb memory space in order to retrieve  nb of connexion
			IntPtr lpTable = Marshal.AllocHGlobal(BufferSize);
			//getting infos
			int res = IPHlpAPI32Wrapper.AllocateAndGetTcpExTableFromStack(ref lpTable, true, IPHlpAPI32Wrapper.GetProcessHeap(),0,2);
			if(res!=NO_ERROR)
			{
				Debug.WriteLine("ERROR : "+IPHlpAPI32Wrapper.GetAPIErrorMessageDescription(res)+" "+res);
				return; // Error. You should handle it
			}
			int CurrentIndex = 0;
			//get the number of entries in the table
			int NumEntries= (int)Marshal.ReadIntPtr(lpTable);
			lpTable = IntPtr.Zero;
			// free allocated space in memory
			Marshal.FreeHGlobal(lpTable);



			///////////////////
			// calculate the real buffer size nb of entrie * size of the struct for each entrie(24) + the dwNumEntries
			BufferSize = (NumEntries*rowsize)+4;
			// make the struct to hold the resullts
			TcpExConnexions = new IpHlpApidotnet.MIB_EXTCPTABLE();
			// Allocate memory
			lpTable = Marshal.AllocHGlobal(BufferSize);
			res = IPHlpAPI32Wrapper.AllocateAndGetTcpExTableFromStack(ref lpTable, true,IPHlpAPI32Wrapper.GetProcessHeap() ,0,2);
			if(res!=NO_ERROR)
			{
				Debug.WriteLine("ERROR : "+IPHlpAPI32Wrapper.GetAPIErrorMessageDescription(res)+" "+res);
				return; // Error. You should handle it
			}
			// New pointer of iterating throught the data
			IntPtr current = lpTable;
			CurrentIndex = 0;
			// get the (again) the number of entries
			NumEntries = (int)Marshal.ReadIntPtr(current);
			TcpExConnexions.dwNumEntries = 	NumEntries;
			// Make the array of entries
			TcpExConnexions.table = new MIB_EXTCPROW[NumEntries];
			// iterate the pointer of 4 (the size of the DWORD dwNumEntries)
			CurrentIndex+=4;
			current = (IntPtr)((int)current+CurrentIndex);
			// for each entries
			for(int i=0; i< NumEntries;i++)
			{
				
				// The state of the connexion (in string)
				TcpExConnexions.table[i].StrgState = this.convert_state((int)Marshal.ReadIntPtr(current));
				// The state of the connexion (in ID)
				TcpExConnexions.table[i].iState = (int)Marshal.ReadIntPtr(current);
				// iterate the pointer of 4
				current = (IntPtr)((int)current+4);
				// get the local address of the connexion
				UInt32 localAddr = (UInt32)Marshal.ReadIntPtr(current);
				// iterate the pointer of 4
				current = (IntPtr)((int)current+4);
				// get the local port of the connexion
				UInt32 localPort = (UInt32)Marshal.ReadIntPtr(current);
				// iterate the pointer of 4
				current = (IntPtr)((int)current+4);
				// Store the local endpoint in the struct and convertthe port in decimal (ie convert_Port())
				TcpExConnexions.table[i].Local = new IPEndPoint(localAddr,(int)convert_Port(localPort));
				// get the remote address of the connexion
				UInt32 RemoteAddr = (UInt32)Marshal.ReadIntPtr(current);
				// iterate the pointer of 4
				current = (IntPtr)((int)current+4);
				UInt32 RemotePort=0;
				// if the remote address = 0 (0.0.0.0) the remote port is always 0
				// else get the remote port
				if(RemoteAddr!=0)
				{
					RemotePort = (UInt32)Marshal.ReadIntPtr(current);
					RemotePort=convert_Port(RemotePort);
				}
				current = (IntPtr)((int)current+4);
				// store the remote endpoint in the struct  and convertthe port in decimal (ie convert_Port())
				TcpExConnexions.table[i].Remote = new IPEndPoint(RemoteAddr,(int)RemotePort);
				// store the process ID
				TcpExConnexions.table[i].dwProcessId = (int)Marshal.ReadIntPtr(current);
				// Store and get the process name in the struct
				TcpExConnexions.table[i].ProcessName = this.get_process_name(TcpExConnexions.table[i].dwProcessId);
				current = (IntPtr)((int)current+4);
		
			}
			// free the buffer
			Marshal.FreeHGlobal(lpTable);
			// re init the pointer
			current = IntPtr.Zero;
		}


		public void GetTcpConnexions()
		{
			byte[] buffer = new byte[20000]; // Start with 20.000 bytes left for information about tcp table
			int pdwSize = 20000;
			int res = IPHlpAPI32Wrapper.GetTcpTable(buffer, out pdwSize, true);
			if (res != NO_ERROR)
			{
				buffer = new byte[pdwSize];
				res = IPHlpAPI32Wrapper.GetTcpTable(buffer, out pdwSize, true);
				if (res != 0)
					return;     // Error. You should handle it
			}
			
			TcpConnexion = new IpHlpApidotnet.MIB_TCPTABLE();

			int nOffset = 0;
			// number of entry in the
			TcpConnexion.dwNumEntries = Convert.ToInt32(buffer[nOffset]);
			nOffset+=4; 
			TcpConnexion.table = new MIB_TCPROW[TcpConnexion.dwNumEntries];

			for(int i=0; i<TcpConnexion.dwNumEntries;i++)
			{
				// state
				int st = Convert.ToInt32(buffer[nOffset]);
               // MIB_TCPROW row;
				// state in string
			//	((MIB_TCPROW)(TcpConnexion.table[i])).StrgState=convert_state(st);
                ((TcpConnexion.table[i])).StrgState = convert_state(st);
				// state  by ID
				TcpConnexion.table[i].iState = st;
				nOffset+=4; 
				// local address
				string LocalAdrr = buffer[nOffset].ToString()+"."+buffer[nOffset+1].ToString()+"."+buffer[nOffset+2].ToString()+"."+buffer[nOffset+3].ToString();
				nOffset+=4; 
				//local port in decimal
				int LocalPort = (((int)buffer[nOffset])<<8) + (((int)buffer[nOffset+1])) + 
					(((int)buffer[nOffset+2])<<24) + (((int)buffer[nOffset+3])<<16);

				nOffset+=4; 
				// store the remote endpoint
			//	((MIB_TCPROW)(TcpConnexion.table[i])).Local = new IPEndPoint(IPAddress.Parse(LocalAdrr),LocalPort);
                TcpConnexion.table[i].Local = new IPEndPoint(IPAddress.Parse(LocalAdrr), LocalPort);

				// remote address
				string RemoteAdrr = buffer[nOffset].ToString()+"."+buffer[nOffset+1].ToString()+"."+buffer[nOffset+2].ToString()+"."+buffer[nOffset+3].ToString();
				nOffset+=4; 
				// if the remote address = 0 (0.0.0.0) the remote port is always 0
				// else get the remote port in decimal
				int RemotePort;
				//
				if(RemoteAdrr == "0.0.0.0")
				{
					RemotePort = 0;
				}
				else
				{
					RemotePort = (((int)buffer[nOffset])<<8) + (((int)buffer[nOffset+1])) + 
						(((int)buffer[nOffset+2])<<24) + (((int)buffer[nOffset+3])<<16);
				}
				nOffset+=4; 
			    //((MIB_TCPROW)(TcpConnexion.table[i])).Remote = new IPEndPoint(IPAddress.Parse(RemoteAdrr),RemotePort);
                TcpConnexion.table[i].Remote = new IPEndPoint(IPAddress.Parse(RemoteAdrr), RemotePort);
			}
		}


		#endregion

		#region Udp Functions

		public void GetUdpStats()
		{
			
			UdpStats = new MIB_UDPSTATS();
			IPHlpAPI32Wrapper.GetUdpStatistics(ref UdpStats);
		}


		public void GetUdpConnexions()
		{
			byte[] buffer = new byte[20000]; // Start with 20.000 bytes left for information about tcp table
			int pdwSize = 20000;
			int res = IPHlpAPI32Wrapper.GetUdpTable(buffer, out pdwSize, true);
			if (res != NO_ERROR)
			{
				buffer = new byte[pdwSize];
				res = IPHlpAPI32Wrapper.GetUdpTable(buffer, out pdwSize, true);
				if (res != 0)
					return;     // Error. You should handle it
			}

			UdpConnexion = new IpHlpApidotnet.MIB_UDPTABLE();

			int nOffset = 0;
			// number of entry in the
			UdpConnexion.dwNumEntries = Convert.ToInt32(buffer[nOffset]);
			nOffset+=4; 
			UdpConnexion.table = new MIB_UDPROW[UdpConnexion.dwNumEntries];
			for(int i=0; i<UdpConnexion.dwNumEntries;i++)
			{
				string LocalAdrr = buffer[nOffset].ToString()+"."+buffer[nOffset+1].ToString()+"."+buffer[nOffset+2].ToString()+"."+buffer[nOffset+3].ToString();
				nOffset+=4; 

				int LocalPort = (((int)buffer[nOffset])<<8) + (((int)buffer[nOffset+1])) + 
					(((int)buffer[nOffset+2])<<24) + (((int)buffer[nOffset+3])<<16);
				nOffset+=4; 
				//((MIB_UDPROW)(UdpConnexion.table[i])).Local = new IPEndPoint(IPAddress.Parse(LocalAdrr),LocalPort);
                UdpConnexion.table[i].Local = new IPEndPoint(IPAddress.Parse(LocalAdrr), LocalPort);
			}
		}


		public void GetExUdpConnexions()
		{
			
			// the size of the MIB_EXTCPROW struct =  4*DWORD
			int rowsize = 12;
			int BufferSize = 100000;
			// allocate a dumb memory space in order to retrieve  nb of connexion
			IntPtr lpTable = Marshal.AllocHGlobal(BufferSize);
			//getting infos
			int res = IPHlpAPI32Wrapper.AllocateAndGetUdpExTableFromStack(ref lpTable, true, IPHlpAPI32Wrapper.GetProcessHeap(),0,2);
			if(res!=NO_ERROR)
			{
				Debug.WriteLine("Erreur : "+IPHlpAPI32Wrapper.GetAPIErrorMessageDescription(res)+" "+res);
				return; // Error. You should handle it
			}
			int CurrentIndex = 0;
			//get the number of entries in the table
			int NumEntries= (int)Marshal.ReadIntPtr(lpTable);
			lpTable = IntPtr.Zero;
			// free allocated space in memory
			Marshal.FreeHGlobal(lpTable);
		
			///////////////////
			// calculate the real buffer size nb of entrie * size of the struct for each entrie(24) + the dwNumEntries
			BufferSize = (NumEntries*rowsize)+4;
			// make the struct to hold the resullts
			UdpExConnexion = new IpHlpApidotnet.MIB_EXUDPTABLE();
			// Allocate memory
			lpTable = Marshal.AllocHGlobal(BufferSize);
			res = IPHlpAPI32Wrapper.AllocateAndGetUdpExTableFromStack(ref lpTable, true,IPHlpAPI32Wrapper.GetProcessHeap() ,0,2);
			if(res!=NO_ERROR)
			{
				Debug.WriteLine("Erreur : "+IPHlpAPI32Wrapper.GetAPIErrorMessageDescription(res)+" "+res);
				return; // Error. You should handle it
			}
			// New pointer of iterating throught the data
			IntPtr current = lpTable;
			CurrentIndex = 0;
			// get the (again) the number of entries
			NumEntries = (int)Marshal.ReadIntPtr(current);
			UdpExConnexion.dwNumEntries = 	NumEntries;
			// Make the array of entries
			UdpExConnexion.table = new MIB_EXUDPROW[NumEntries];
			// iterate the pointer of 4 (the size of the DWORD dwNumEntries)
			CurrentIndex+=4;
			current = (IntPtr)((int)current+CurrentIndex);
			// for each entries
			for(int i=0; i< NumEntries;i++)
			{
				// get the local address of the connexion
				UInt32 localAddr = (UInt32)Marshal.ReadIntPtr(current);
				// iterate the pointer of 4
				current = (IntPtr)((int)current+4);
				// get the local port of the connexion
				UInt32 localPort = (UInt32)Marshal.ReadIntPtr(current);
				// iterate the pointer of 4
				current = (IntPtr)((int)current+4);
				// Store the local endpoint in the struct and convertthe port in decimal (ie convert_Port())
				UdpExConnexion.table[i].Local = new IPEndPoint(localAddr,convert_Port(localPort));
				// store the process ID
				UdpExConnexion.table[i].dwProcessId = (int)Marshal.ReadIntPtr(current);
				// Store and get the process name in the struct
				UdpExConnexion.table[i].ProcessName = this.get_process_name(UdpExConnexion.table[i].dwProcessId);
				current = (IntPtr)((int)current+4);
		
			}
			// free the buffer
			Marshal.FreeHGlobal(lpTable);
			// re init the pointer
			current = IntPtr.Zero;
		}


		#endregion

		#region helper fct

		private UInt16 convert_Port(UInt32 dwPort)
		{
			byte[] b = new Byte[2];
			// high weight byte
			b[0] = byte.Parse((dwPort>>8).ToString());
			// low weight byte
			b[1] = byte.Parse((dwPort & 0xFF).ToString());
			return BitConverter.ToUInt16(b,0);
		}


		private string convert_state(int state)
		{
			string strg_state="";
			switch(state)
			{
				case MIB_TCP_STATE_CLOSED: strg_state = "CLOSED" ;break;
				case MIB_TCP_STATE_LISTEN: strg_state = "LISTEN" ;break;
				case MIB_TCP_STATE_SYN_SENT: strg_state = "SYN_SENT" ;break;
				case MIB_TCP_STATE_SYN_RCVD: strg_state = "SYN_RCVD" ;break;
				case MIB_TCP_STATE_ESTAB: strg_state = "ESTAB" ;break;
				case MIB_TCP_STATE_FIN_WAIT1: strg_state = "FIN_WAIT1" ;break;
				case MIB_TCP_STATE_FIN_WAIT2: strg_state = "FIN_WAIT2" ;break;
				case MIB_TCP_STATE_CLOSE_WAIT: strg_state = "CLOSE_WAIT" ;break;
				case MIB_TCP_STATE_CLOSING: strg_state = "CLOSING" ;break;
				case MIB_TCP_STATE_LAST_ACK: strg_state = "LAST_ACK" ;break;
				case MIB_TCP_STATE_TIME_WAIT: strg_state = "TIME_WAIT" ;break;
				case MIB_TCP_STATE_DELETE_TCB: strg_state = "DELETE_TCB" ;break;
			}
			return strg_state;
		}


		private string get_process_name(int processID)
		{
			//could be an error here if the process die before we can get his name
			try
			{
				Process p = Process.GetProcessById((int)processID);
				return p.ProcessName;
			}
			catch(Exception ex)
			{
				Debug.WriteLine(ex.Message);
				return "unknown";
			}
				
		}


		#endregion
	}
}
