

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;

namespace Xoware.SocksServerLib {

///<summary>Implements the SOCKS5 protocol.</summary>
internal class Socks5Handler : SocksHandler {

    private IPEndPoint Client_UDP_Port;

	///<summary>Initializes a new instance of the Socks5Handler class.</summary>
	///<param name="ClientConnection">The connection with the client.</param>
	///<param name="Callback">The method to call when the SOCKS negotiation is complete.</param>
	///<param name="AuthList">The authentication list to use when clients connect.</param>
	///<exception cref="ArgumentNullException"><c>Callback</c> is null.</exception>
	///<remarks>If the AuthList parameter is null, no authentication will be required when a client connects to the proxy server.</remarks>
    public Socks5Handler(Socket ClientConnection, NegotiationCompleteDelegate Signaler, AuthenticationList AuthList) : base(ClientConnection, Signaler)
    {
		this.AuthList = AuthList;
	}
	///<summary>Initializes a new instance of the Socks5Handler class.</summary>
	///<param name="ClientConnection">The connection with the client.</param>
	///<param name="Callback">The method to call when the SOCKS negotiation is complete.</param>
	///<exception cref="ArgumentNullException"><c>Callback</c> is null.</exception>
	public Socks5Handler(Socket ClientConnection, NegotiationCompleteDelegate Callback) : this(ClientConnection, Callback, null) {}
	///<summary>Checks whether a specific request is a valid SOCKS request or not.</summary>
	///<param name="Request">The request array to check.</param>
	///<returns>True is the specified request is valid, false otherwise</returns>
	protected override bool IsValidRequest(byte [] Request) {
		try {
			return (Request.Length == Request[0] + 1);
		} catch {
			return false;
		}
	}
	///<summary>Processes a SOCKS request from a client and selects an authentication method.</summary>
	///<param name="Request">The request to process.</param>
	protected override void ProcessRequest(byte [] Request) {
		try {
			byte Ret = 255;
			for (int Cnt = 1; Cnt < Request.Length; Cnt++) {
				if (Request[Cnt] == 0 && AuthList == null) { //0 = No authentication
					Ret = 0;
					AuthMethod = new AuthNone();
					break;
				} else if (Request[Cnt] == 2 && AuthList != null) { //2 = user/pass
					Ret = 2;
					AuthMethod = new AuthUserPass(AuthList);
					if (AuthList != null)
						break;
				}
			}
			Connection.BeginSend(new byte[]{5, Ret}, 0, 2, SocketFlags.None, new AsyncCallback(this.OnAuthSent), Connection);
		} catch {
			Dispose(false);
		}
	}
	///<summary>Called when client has been notified of the selected authentication method.</summary>
	///<param name="ar">The result of the asynchronous operation.</param>
	private void OnAuthSent(IAsyncResult ar) {
		try {
			if (Connection.EndSend(ar) <= 0 || AuthMethod == null) {
				Dispose(false);
				return;
			}
			AuthMethod.StartAuthentication(Connection, new AuthenticationCompleteDelegate(this.OnAuthenticationComplete));
		} catch {
			Dispose(false);
		}
	}
	///<summary>Called when the authentication is complete.</summary>
	///<param name="Success">Indicates whether the authentication was successful ot not.</param>
	private void OnAuthenticationComplete(bool Success) {
		try {
			if (Success) {
				Bytes = null;
				Connection.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, new AsyncCallback(this.OnRecvRequest), Connection);
			} else {
				Dispose(false);
			}
		} catch {
			Dispose(false);
		}
	}
	///<summary>Called when we received the request of the client.</summary>
	///<param name="ar">The result of the asynchronous operation.</param>
	private void OnRecvRequest(IAsyncResult ar) {
		try {
			int Ret = Connection.EndReceive(ar);
			if (Ret <= 0) {
				Dispose(false);
				return;
			}
			AddBytes(Buffer, Ret);
			if (IsValidQuery(Bytes))
				ProcessQuery(Bytes);
			else
				Connection.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, new AsyncCallback(this.OnRecvRequest), Connection);
		} catch {
			Dispose(false);
		}
	}
	///<summary>Checks whether a specified query is a valid query or not.</summary>
	///<param name="Query">The query to check.</param>
	///<returns>True if the query is valid, false otherwise.</returns>
	private bool IsValidQuery(byte [] Query) {
		try {
			switch(Query[3]) {
				case 1: //IPv4 address
					return (Query.Length == 10);
				case 3: //Domain name
					return (Query.Length == Query[4] + 7);
				case 4: //IPv6 address
					//Not supported
					Dispose(8);
					return false;
				default:
					Dispose(false);
					return false;
			}
		} catch {
			return false;
		}
	}

    protected void OnClientRecv(object sender, SocketAsyncEventArgs ea)
    {
        try
        {

            IPAddress Dest = null;
            var info = ea.ReceiveMessageFromPacketInfo;
           // byte[] address = info.Address.GetAddressBytes();
            UInt16 offset = 0;
            UInt16 dport;
            Debug.WriteLine("Data recieved from client " + ea.RemoteEndPoint.ToString());

            if (ea.BytesTransferred < 10)
            {
                goto next_packet;
            }
            UInt16 Reserved = (UInt16)((ea.Buffer[0] << 8) | (byte)ea.Buffer[1]);
            if (Reserved != 0)
            {
                Debug.WriteLine("protocol error");
            }
            Byte Frag = ea.Buffer[2];
            if (Frag != 0)
            {
                Debug.WriteLine("FIXME fragmentation");
            }
            Byte Atyp = ea.Buffer[3];
            if (Atyp == 1)
            {
                byte[] IPv4Addr = new byte[4];
                IPv4Addr[0] = ea.Buffer[4];
                IPv4Addr[1] = ea.Buffer[5];
                IPv4Addr[2] = ea.Buffer[6];
                IPv4Addr[3] = ea.Buffer[7];
                Dest = new IPAddress(IPv4Addr);
                dport = (UInt16)((ea.Buffer[8] << 8) | (byte)ea.Buffer[9]);
                offset = 10;
                Debug.WriteLine("OnClientRecv Dest=" + Dest.ToString() + " Port=" + dport);
            }
            else
            {
                Debug.WriteLine("FIXME address type");
                Dispose(false);
                return;
            }

            Client_UDP_Port = ea.RemoteEndPoint as IPEndPoint;

                           
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(Dest, dport);
            RemoteConnection.SendTo(ea.Buffer, offset, ea.BytesTransferred - offset, SocketFlags.None, RemoteIpEndPoint);
            next_packet:
            ea.Dispose();
            SetupClientRecv(); // recv next packet
        }
        catch (Exception ex)
        {
            Debug.WriteLine("OnClientRecv:" + ex.ToString());
        }

        
    }
    protected void OnServerRecv(object sender, SocketAsyncEventArgs ea)
    {
        try
        {
            IPEndPoint ep = ea.RemoteEndPoint as IPEndPoint;

            Debug.WriteLine("OnServerRecv Recieved From Server " + ea.BytesTransferred + " Bytes from: " + ea.RemoteEndPoint.ToString());

            if (ea.BytesTransferred < 1)
            {
                goto next_packet;
            }
            byte[] addrBytes = ep.Address.GetAddressBytes();

            ea.Buffer[0] = 0; // reserved
            ea.Buffer[1] = 0; // reserved
            ea.Buffer[2] = 0; // Fragmentation  ToDo  implement this if neeed
            ea.Buffer[3] = 1;  // Address type  (1 == IPV4)
            ea.Buffer[4] = addrBytes[0];
            ea.Buffer[5] = addrBytes[1];
            ea.Buffer[6] = addrBytes[2];
            ea.Buffer[7] = addrBytes[3];
            ea.Buffer[8] = (byte)((ep.Port >> 8) & 0xFF);
            ea.Buffer[9] = (byte)(ep.Port & 0xFF);

            Debug.WriteLine("OnServerRecv Send " + (ea.BytesTransferred + 10) + " Bytes To:" + Client_UDP_Port.ToString());
            AcceptSocket.SendTo(ea.Buffer, ea.BytesTransferred + 10, SocketFlags.None, Client_UDP_Port);

            next_packet:
            ea.Dispose();
            SetupServerRecv(); // recv next packet
        }
        catch (Exception ex)
        {
            Debug.WriteLine("OnServerRecv Ex:" + ex.ToString());
        }
    }
    private void SetupClientRecv()
    {
        try
        {
            SocketAsyncEventArgs sockClientEventArg = new SocketAsyncEventArgs();
            byte[] udpRecvBuffer = new byte[1024 * 10]; // 10K buffer incase of jumbo packet
            sockClientEventArg.SetBuffer(udpRecvBuffer, 0, udpRecvBuffer.Length);
            sockClientEventArg.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0); // TODO can this be more secure if we know IP?
            sockClientEventArg.Completed += OnClientRecv;
            if (!AcceptSocket.ReceiveMessageFromAsync(sockClientEventArg))
            {
                Debug.WriteLine("!ReceiveMessageFromAsync client");
                OnClientRecv(null, sockClientEventArg);
            }
        }
        catch (Exception ex)
        {
            // Note, this happens when we were disposed of. when TCP controlling connection was closed
            Debug.WriteLine("SetupClientRecv Ex:" + ex.Message);
        }
    }

    private void SetupServerRecv()
    {
        try
        {
            SocketAsyncEventArgs sockServerEventArg = new SocketAsyncEventArgs();

            byte[] udpServerRecvBuffer = new byte[1024 * 10]; // 10K buffer incase of jumbo packet

            // set buffer with 10 byte header space for encapsulation
            sockServerEventArg.SetBuffer(udpServerRecvBuffer, 10, udpServerRecvBuffer.Length - 10);
            sockServerEventArg.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0); // TODO can this be more secure if we know IP?
            sockServerEventArg.Completed += OnServerRecv;
            if (!RemoteConnection.ReceiveMessageFromAsync(sockServerEventArg))
            {
                Debug.WriteLine("!ReceiveMessageFromAsync server");
                OnServerRecv(null, sockServerEventArg);
            }
        }
        catch (Exception ex)
        {
            // Note, this happens when we were disposed of. when TCP controlling connection was closed
            Debug.WriteLine("SetupServerRecv Ex:" + ex.Message);
        }
    }
    ///<summary </summary>
	///<param name="ar">The result of the asynchronous operation.</param>
    protected void OnStartRecv(IAsyncResult ar)
    {
        try
        {
          
            if (Connection.EndSend(ar) <= 0)
            {
                Debug.WriteLine("OnStartRecv EndSend Err");
                Dispose(false);
                return;
            }
            SetupClientRecv();

            SetupServerRecv();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("OnStartRecv:" +ex.ToString());
            Dispose(false);
        }
    }

	///<summary>Processes a received query.</summary>
	///<param name="Query">The query to process.</param>
	private void ProcessQuery(byte [] Query) {
		try {
            IPAddress RemoteIP = null;
            int RemotePort = 0;
            byte[] Reply = new byte[10];
  //          long LocalIP = Listener.GetLocalExternalIP().Address;
            IPEndPoint RemoteEP = Connection.RemoteEndPoint as IPEndPoint;
            IPEndPoint LocalEP = Connection.LocalEndPoint as IPEndPoint;
            byte[] LocalAddrBytes = LocalEP.Address.GetAddressBytes();
            Debug.WriteLine("ProcessQuery Connection From " + RemoteEP.ToString() + " " + RemoteEP.Port//
                + " On " + LocalEP.ToString());

			switch(Query[1]) {
				case 1: //CONNECT

					if (Query[3] == 1) {
						RemoteIP = IPAddress.Parse(Query[4].ToString() + "." + Query[5].ToString() + "." + Query[6].ToString() + "." + Query[7].ToString());
						RemotePort = Query[8] * 256 + Query[9];
					} else if( Query[3] == 3) {
						RemoteIP = Dns.GetHostEntry(Encoding.ASCII.GetString(Query, 5, Query[4])).AddressList[0];
						RemotePort = Query[4] + 5;
						RemotePort = Query[RemotePort] * 256 + Query[RemotePort + 1];
					}
					RemoteConnection = new Socket(RemoteIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					RemoteConnection.BeginConnect(new IPEndPoint(RemoteIP, RemotePort), new AsyncCallback(this.OnConnected), RemoteConnection);
					break;
				case 2: //BIND
                    
					AcceptSocket = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					AcceptSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
					AcceptSocket.Listen(50);
					Reply[0] = 5;  //Version 5
					Reply[1] = 0;  //Everything is ok :)
					Reply[2] = 0;  //Reserved
					Reply[3] = 1;  //We're going to send a IPv4 address
                    Reply[4] = LocalAddrBytes[0];  //IP Address/1
					Reply[5] = LocalAddrBytes[1];  //IP Address/2
					Reply[6] = LocalAddrBytes[2];  //IP Address/3
					Reply[7] = LocalAddrBytes[3];  //IP Address/4
					Reply[8] = (byte) ((((IPEndPoint)AcceptSocket.LocalEndPoint).Port >> 8) & 0xFF);  //Port/1
					Reply[9] = (byte) (((IPEndPoint)AcceptSocket.LocalEndPoint).Port & 0xFF);  //Port/2

                    Connection.BeginSend(Reply, 0, Reply.Length, SocketFlags.None, new AsyncCallback(this.OnStartAccept), Connection);
					break;
				case 3: //ASSOCIATE
					
                    System.Diagnostics.Debug.WriteLine("starting UDP Associate:" + Query.ToString());

					if (Query[3] == 1) {
                        // IP address 
						RemoteIP = IPAddress.Parse(Query[4].ToString() + "." + Query[5].ToString() + "." + Query[6].ToString() + "." + Query[7].ToString());
						RemotePort = Query[8] * 256 + Query[9];
					} else if( Query[3] == 3) {
                        // DNS address
						RemoteIP = Dns.GetHostEntry(Encoding.ASCII.GetString(Query, 5, Query[4])).AddressList[0];
						RemotePort = Query[4] + 5;
						RemotePort = Query[RemotePort] * 256 + Query[RemotePort + 1];
					}
                    Debug.WriteLine("Remote IP=" + RemoteIP.ToString() + " Port: " + RemotePort);
                    RemoteConnection = new Socket(RemoteIP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    RemoteConnection.Bind(new IPEndPoint(IPAddress.Any, 0));

                    // Fixme  Put this only on listening interface IP
                    AcceptSocket = new Socket(IPAddress.Any.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    // Bind to the same interface address that we recieved the incomming TCP socks connection
                    AcceptSocket.Bind(new IPEndPoint(LocalEP.Address, 0));

					Reply[0] = 5;  //Version 5
					Reply[1] = 0;  //reserved
					Reply[2] = 0;  //Reserved
					Reply[3] = 1;  //We're going to send a IPv4 address
                    Reply[4] = LocalAddrBytes[0];  //IP Address/1
					Reply[5] = LocalAddrBytes[1];  //IP Address/2
					Reply[6] = LocalAddrBytes[2];  //IP Address/3
					Reply[7] = LocalAddrBytes[3];  //IP Address/4
					Reply[8] = (byte) ((((IPEndPoint)AcceptSocket.LocalEndPoint).Port >> 8) & 0xFF);  //Port/1
					Reply[9] = (byte) (((IPEndPoint)AcceptSocket.LocalEndPoint).Port & 0xFF);  //Port/2
                    
                    Connection.BeginSend(Reply, 0, Reply.Length, SocketFlags.None, new AsyncCallback(this.OnStartRecv), Connection);
                    System.Diagnostics.Debug.WriteLine("UDP Associate finished");
                    Connection.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, new AsyncCallback(this.OnRecvRequest), Connection);

                    break;
				default:
					Dispose(7);
					break;
			}
		} catch (Exception ex){
            Debug.WriteLine("Exception " + ex.ToString());
			Dispose(1);
		}
	}
	///<summary>Called when we're successfully connected to the remote host.</summary>
	///<param name="ar">The result of the asynchronous operation.</param>
	private void OnConnected(IAsyncResult ar) {
		try {
			RemoteConnection.EndConnect(ar);
			Dispose(0);
		} catch {
			Dispose(1);
		}
	}
	///<summary>Called when there's an incoming connection in the AcceptSocket queue.</summary>
	///<param name="ar">The result of the asynchronous operation.</param>
	protected override void OnAccept(IAsyncResult ar) {
		try {
			RemoteConnection = AcceptSocket.EndAccept(ar);
			AcceptSocket.Close();
			AcceptSocket = null;
			Dispose(0);
		} catch {
			Dispose(1);
		}
	}
	///<summary>Sends a reply to the client connection and disposes it afterwards.</summary>
	///<param name="Value">A byte that contains the reply code to send to the client.</param>
	protected override void Dispose(byte Value) {
		byte [] ToSend;
		try {
            IPEndPoint RemEP = (IPEndPoint)RemoteConnection.RemoteEndPoint;
            byte[] AddrBytes = RemEP.Address.GetAddressBytes();

			ToSend = new byte[]{5, Value, 0, 1,
						(byte) AddrBytes[0],
		                (byte) AddrBytes[1],
		                (byte) AddrBytes[2],
		                (byte) AddrBytes[3],
						(byte)(Math.Floor((decimal)((IPEndPoint)RemoteConnection.LocalEndPoint).Port / 256)),
						(byte)(((IPEndPoint)RemoteConnection.LocalEndPoint).Port % 256)};
		} catch {
			ToSend = new byte[] {5, 1, 0, 1, 0, 0, 0, 0, 0, 0};
		}
		try {
			Connection.BeginSend(ToSend, 0, ToSend.Length, SocketFlags.None, (AsyncCallback)(ToSend[1] == 0 ? new AsyncCallback(this.OnDisposeGood) : new AsyncCallback(this.OnDisposeBad)), Connection);
		} catch {
			Dispose(false);
		}
	}
	///<summary>Gets or sets the the AuthBase object to use when trying to authenticate the SOCKS client.</summary>
	///<value>The AuthBase object to use when trying to authenticate the SOCKS client.</value>
	///<exception cref="ArgumentNullException">The specified value is null.</exception>
	private AuthBase AuthMethod {
		get {
			return m_AuthMethod;
		}
		set {
			if (value == null)
				throw new ArgumentNullException();
			m_AuthMethod = value;
		}
	}
	///<summary>Gets or sets the AuthenticationList object to use when trying to authenticate the SOCKS client.</summary>
	///<value>The AuthenticationList object to use when trying to authenticate the SOCKS client.</value>
	private AuthenticationList AuthList {
		get {
			return m_AuthList;
		}
		set {
			m_AuthList = value;
		}
	}
	// private variables
	/// <summary>Holds the value of the AuthList property.</summary>
	private AuthenticationList m_AuthList;
	/// <summary>Holds the value of the AuthMethod property.</summary>
	private AuthBase m_AuthMethod;
}

}
