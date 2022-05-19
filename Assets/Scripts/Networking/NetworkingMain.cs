using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkingMain {

	public const int netVer = 3;

	public const int maxClients = 32;

    public static IPEndPoint localEndPoint;

    private static Socket Sock;

	public static IPEndPoint[] Clients;

    public static int Host = -1;

    public static bool[] ClientsUsed;

    private static byte[,][] ImportantStack;

    private static bool[,] ImportantStackUsed;

    private static int[] ImportantStackPos;

    private static byte[,][] ReceiveStack;

    private static bool[,] ReceiveStackUsed;

    private static int[] ReceiveStackPos;

    private static float[] SockTimeout;

    private static float[,] PacketTimeout;

    private static int[,] ReceiveStackTypes;

    private static int[,] ReceiveStackSizes;

    private static int[] ClientIDs;

    public static int ClientNode;

    private static int ClientID;

    private static System.Random rng;

	public static List<string> banList = new List<string>();

	public static Action<int> GameLayerJoinSend;

	public static Action GameLayerJoinReceive;

	public static Action<int> GameLayerJoinOthers;

	public static Action<int, int> GameLayerDisconnect;

	private static List<Action<byte[], int>> gameLayerPacketProcessing;

	private static int disconnectEvent;
	private static int joinEvent;
	private static int joinOtherEvent;

	// get a unique ID for packet types
	static public int RegisterPacketType(Action<byte[], int> processor)
	{
		gameLayerPacketProcessing.Add(processor);
		return gameLayerPacketProcessing.Count - 1;
	}

	// dummy to occupy slot 0, since that's reserved
	static private void DUMMYPACKET(byte[] data, int node)
	{
		return;
	}

    static public void StopNetworking()
    {
		for (int i = 0; i < maxClients; ++i)
		{
			if (ClientsUsed[i])
			{
				CloseConn(i);
				//PlayerNetworking.players[i] = new PlayerNetworking.NetPlayer();
			}
		}
        Sock.Close(0);
        Host = -1;
        ClientNode = -1;
        Sock = null;
    }

    static public void StartNetworking(int type, string ip = "", int port = 5029)
    {
        ClientsUsed = new bool[maxClients];
        // If our sock hasn't previously been set up, do initialization
        if (Sock == null)
        {
            /*if (File.Exists("Host.txt"))
            {
                Host = 1;
            }
            else
            {
                Host = 0;
            }
            Host = -1;*/
            Host = type;
            rng = new System.Random();
            ClientNode = -1;
            // Attempt to create a server socket on port 5029
            if (Host == 1)
            {
                localEndPoint = new IPEndPoint(IPAddress.Any, port);
                Sock = new Socket(IPAddress.Any.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
				// prevent the socket from being obliterated by dumb nonsense. why on god's green earth can a udp socket EVER return connection reset?
				const int SIO_UDP_CONNRESET = -1744830452;
				byte[] inValue = new byte[] { 0 };
				byte[] outValue = new byte[] { 0 };
				Sock.IOControl(SIO_UDP_CONNRESET, inValue, outValue);
				Sock.Bind(localEndPoint);
                Clients = new IPEndPoint[maxClients];
                ClientID = rng.Next();
                ClientNode = 0;
                // 0th client is always host
                ClientsUsed[0] = true;
                Sock.Blocking = false;
            }
            // Or we're a client, attempt to connect to server, using the IP from IP.txt
            if (Host == 0)
            {
				//string IP = System.IO.File.ReadAllText("IP.txt");
				//string IP = "24.250.159.94";
				try
				{
					//IPHostEntry iph = Dns.GetHostEntry(ip);
					//string IP = iph.AddressList[0];
					IPAddress IPAddr = IPAddress.Parse(ip);
					//if (iph.AddressList.Length > 0)
					//{
					//IPAddress IPAddr = iph.AddressList[0];
					localEndPoint = new IPEndPoint(IPAddr, port);
					Sock = new Socket(IPAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
					const int SIO_UDP_CONNRESET = -1744830452;
					byte[] inValue = new byte[] { 0 };
					byte[] outValue = new byte[] { 0 };
					Sock.IOControl(SIO_UDP_CONNRESET, inValue, outValue);
					// Send out our connection request packet
					byte[] ou = new byte[17];
					ClientNode = -1;
					Array.Copy(BitConverter.GetBytes(ClientNode), 0, ou, 4, 4);
					Utilj.AddToArray(netVer, ou, 13);
					Sock.SendTo(ou, localEndPoint);
					Sock.Blocking = false;
					//}
				}
				catch
				{
					Host = -1;
				}
            }
        }
        ImportantStack = new byte[maxClients, 8192][];
        ImportantStackPos = new int[maxClients];
        SockTimeout = new float[maxClients];
        PacketTimeout = new float[maxClients, 8192];
        ClientIDs = new int[maxClients];
        ImportantStackUsed = new bool[maxClients, 8192];
        ReceiveStackUsed = new bool[maxClients, 8192];
        ReceiveStackPos = new int[maxClients];
        ReceiveStack = new byte[maxClients, 8192][];
        ReceiveStackTypes = new int[maxClients, 8192];
        ReceiveStackSizes = new int[maxClients, 8192];
    }

    // Use this for initialization
    static public void Setup() {
		// Set up more variables
		gameLayerPacketProcessing = new List<Action<byte[], int>>();
        ClientsUsed = new bool[maxClients];
        ImportantStack = new byte[maxClients, 8192][];
        ImportantStackPos = new int[maxClients];
        SockTimeout = new float[maxClients];
        PacketTimeout = new float[maxClients, 8192];
        ClientIDs = new int[maxClients];
        ImportantStackUsed = new bool[maxClients, 8192];
        ReceiveStackUsed = new bool[maxClients, 8192];
        ReceiveStackPos = new int[maxClients];
        ReceiveStack = new byte[maxClients, 8192][];
        ReceiveStackTypes = new int[maxClients, 8192];
        ReceiveStackSizes = new int[maxClients, 8192];
		// packet type of 0; not used, hardcoded, etc
		RegisterPacketType(DUMMYPACKET);
		// packet types for various network features
		joinEvent = RegisterPacketType(SyncJoinRecv);
		joinOtherEvent = RegisterPacketType(PlayerJoinOthers);
		disconnectEvent = RegisterPacketType(DisconnectRecv);
    }

	static private void DisconnectRecv(byte[] packet, int node)
	{
		if (Host != 1)
		{
			int n = BitConverter.ToInt32(packet, 0);
			int reas = BitConverter.ToInt32(packet, 4);
			// if we're the ones being disconnected, then do that
			if (n == ClientNode)
			{
				CloseConn(ClientNode, reas);
			}
			else
			{
				GameLayerDisconnect(node, reas);
			}
		}
		else
		{
			int reas = BitConverter.ToInt32(packet, 4);
			CloseConn(node, reas);
		}
	}
	
	// Update is called once per frame
	static public void Update () {
        // Only execute if we're actually in multiplayer
        if (Host != -1 && Sock != null)
        {
            // We're using non-blocking sockets, so we need to check for available data
            while (Sock != null && Host != -1 && Sock.Available > 0)
            {
                // Receive said data
                byte[] packet = new byte[4096];
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                int packSize = Sock.ReceiveFrom(packet, ref ep);
				if (packSize < 13) continue;
                // Can't use custom structs and magical memory stuff in C#. I miss C.
				// I could probably reduce this to an int16, but oh well
                int RecvID = System.BitConverter.ToInt32(packet, 0);
                int RecvNode = System.BitConverter.ToInt32(packet, 4);
                int RecvType = System.BitConverter.ToInt16(packet, 11);
                // i'm legitimately confused here, this is a hacky as fuck fix
                if (Host != 1 && RecvType != 0)
				{
                    RecvNode = 0;
				}
                int RecvImportant = packet[8];
                int RecvImpID = System.BitConverter.ToInt16(packet, 9);
                // Copy our actual packet data to be readable over here
                Array.Copy(packet, 13, packet, 0, packSize - 13);
				packSize -= 13;
				Array.Resize<byte>(ref packet, packSize);
                if (RecvNode == -1)
                {
                    // If we're the host, then check if this is a connection packet. If it is, then log them
                    if (Host == 1)
                    {
						// first we check if they're banned
						string clIP = ((IPEndPoint)ep).Address.ToString();
						bool banned = false;
						for (int i2 = 0; i2 < banList.Count; ++i2)
						{
							if (clIP == banList[i2])
							{
								banned = true;
								i2 = banList.Count;
							}
						}
						// and if they're on the right version
						bool correctVer = false;
						if (packSize == 4 && BitConverter.ToInt32(packet, 0) == netVer)
						{
							correctVer = true;
						}
						if (!banned && correctVer)
						{
							//bool actuallyFound = false;
							for (int i = 0; i < maxClients; i++)
							{
								if (!ClientsUsed[i])
								{
									//actuallyFound = true;
									// Store their connection info, assign them a node and ID, and tell them their node, ID, and our ID
									Clients[i] = (IPEndPoint)ep;
									ClientIDs[i] = rng.Next();
									// keep giving us random if it returned 0
									while (ClientIDs[i] == 0)
									{
										ClientIDs[i] = rng.Next();
									}
									// Send out packet indicating that they connected (do before marking as used)
									Packet_Send(BitConverter.GetBytes(i), 4, joinOtherEvent, true);
									ClientsUsed[i] = true;
									byte[] o = new byte[17];
									// They get our ID from this, their node from the "node" here
									Array.Copy(BitConverter.GetBytes(ClientID), 0, o, 0, 4);
									Array.Copy(BitConverter.GetBytes(i), 0, o, 4, 4);
									// zero out irrelevant packet data
									o[8] = 0;
									/*o[13] = 0;
									o[14] = 0;
									o[15] = 0;
									o[16] = 0;*/
									// And put their ID inside the message
									Array.Copy(BitConverter.GetBytes(ClientIDs[i]), 0, o, 13, 4);
									Sock.SendTo(o, Clients[i]);
									// Sync our on-connection values, related to the game rather than the networking layer
									SyncJoinSend(i);
									SockTimeout[i] = 0;
									//Chat.LogMessage(String.Concat("A player has joined on node ", i.ToString()), true);
									i = maxClients;
								}
							} // TODO: else here, tell them the server's full
						}
                    }
                }
				// Not a connection packet
				else
				{
                    // If it's not an important packet, then parse it. Otherwise store it
                    if (RecvImportant == 0)
                    {
                        if (RecvID == ClientIDs[RecvNode] && ClientsUsed[RecvNode])
                        {
                            SockTimeout[RecvNode] = 0;
                            if (RecvType != 0)
                            {
                                // Read and apply data
                                ProcessPacket(packet, RecvType, RecvNode, packSize);
                            }
                        }
                        else
                        {
                            // Verify our connection packet from the host
                            if (RecvType == 0 && Host == 0)
                            {
                                ClientNode = RecvNode;
                                ClientIDs[0] = RecvID;
                                ClientID = BitConverter.ToInt32(packet, 0);
                                ClientsUsed[ClientNode] = true;
                                ClientsUsed[0] = true;
                            }
                        }
                    }
                    else
                    {
                        if (RecvID == ClientIDs[RecvNode] && ClientsUsed[RecvNode])
                        {
                            if (RecvImportant == 1)
                            {
                                SockTimeout[RecvNode] = 0;
                                // If it's important, store it and see if we can parse more
                                if (!ReceiveStackUsed[RecvNode, RecvImpID])
                                {
                                    // Only store this if it's deemed new enough
                                    ReceiveStack[RecvNode, RecvImpID] = packet;
                                    ReceiveStackUsed[RecvNode, RecvImpID] = true;
                                    ReceiveStackTypes[RecvNode, RecvImpID] = RecvType;
                                    ReceiveStackSizes[RecvNode, RecvImpID] = packSize;
                                }
                                // Tell the sender we got it
                                byte[] c = new byte[17];
                                Array.Copy(BitConverter.GetBytes(ClientID), 0, c, 0, 4);
                                if (Host == 0)
                                {
                                    Array.Copy(BitConverter.GetBytes(ClientNode), 0, c, 4, 4);
                                }
                                else
                                {
                                    Array.Copy(BitConverter.GetBytes(RecvNode), 0, c, 4, 4);
                                }
                                c[8] = 2;
                                Array.Copy(BitConverter.GetBytes(RecvImpID), 0, c, 9, 4);
                                if (Host == 1)
                                {
                                    Sock.SendTo(c, Clients[RecvNode]);
                                }
                                else
                                {
                                    Sock.SendTo(c, localEndPoint);
                                }
                                // parse data
                                while (ReceiveStackUsed[RecvNode, ReceiveStackPos[RecvNode]])
                                {
                                    // Iterate over all received important packets
                                    ProcessPacket(ReceiveStack[RecvNode, ReceiveStackPos[RecvNode]], ReceiveStackTypes[RecvNode, ReceiveStackPos[RecvNode]], RecvNode, ReceiveStackSizes[RecvNode, ReceiveStackPos[RecvNode]]);
                                    // Free up previous space
                                    int newpos = ReceiveStackPos[RecvNode];
                                    if (newpos < 4096)
                                    {
                                        newpos = 8192 - (4096 - newpos);
                                    }
                                    else
                                    {
                                        newpos -= 4096;
                                    }
                                    ReceiveStackUsed[RecvNode, newpos] = false;
                                    ReceiveStackPos[RecvNode] += 1;
                                    ReceiveStackPos[RecvNode] %= 8192;
                                }
                            }
                            if (RecvImportant == 2)
                            {
                                SockTimeout[RecvNode] = 0;
                                ImportantStackUsed[RecvNode, RecvImpID] = false;
                            }
                        }
                    }
                }
            }
            if (Host == 1)
            {
                // Update everyone's timeout
                for (int i = 0; i < maxClients; ++i)
                {
                    if (ClientsUsed[i])
                    {
                        // Re-send our important packets if deemed lost
                        int PStartPos = ImportantStackPos[i] - 4096;
                        if (PStartPos < 0)
                        {
                            PStartPos = 8192 + PStartPos;
                        }
                        while (PStartPos != ImportantStackPos[i])
                        {
                            if (ImportantStackUsed[i, PStartPos])
                            {
                                PacketTimeout[i, PStartPos] += Time.unscaledDeltaTime;
                                if (PacketTimeout[i, PStartPos] > 0.3f)
                                {
									PacketTimeout[i, PStartPos] = 0;
                                    Sock.SendTo(ImportantStack[i, PStartPos], Clients[i]);
                                }
                            }
                            PStartPos += 1;
                            PStartPos %= 8192;
                        }
                        SockTimeout[i] += Time.deltaTime;
                        if (SockTimeout[i] > 20.0f && i != 0)
                        {
                            // Eventually close socket here
                            CloseConn(i);
                        }
                    }
                }
            }
            else
            {
                if (ClientNode != -1)
                {
                    // Re-send our important packets if deemed lost
                    int PStartPos = ImportantStackPos[0] - 4096;
                    if (PStartPos < 0)
                    {
                        PStartPos = 8192 + PStartPos;
                    }
                    while (PStartPos != ImportantStackPos[0])
                    {
                        if (ImportantStackUsed[0, PStartPos])
                        {
                            PacketTimeout[0, PStartPos] += Time.deltaTime;
                            if (PacketTimeout[0, PStartPos] > 0.3f)
                            {
                                PacketTimeout[0, PStartPos] = 0;
                                Sock.SendTo(ImportantStack[0, PStartPos], localEndPoint);
                            }
                        }
                        PStartPos += 1;
                        PStartPos %= 8192;
                    }
                    SockTimeout[0] += Time.deltaTime;
                    if (SockTimeout[0] > 20.0f)
                    {
                        // Eventually close socket here
                        CloseConn(0);
                    }
                }
            }
            // Update AI position and such on clients
        }
	}

    static private void ProcessPacket(byte[] packet, int type, int node, int packSize)
    {
		// INVALID
		if (type < 0 || type > gameLayerPacketProcessing.Count)
		{
			return;
		}
		gameLayerPacketProcessing[type](packet, node);
    }

    static private void SyncJoinRecv(byte[] data, int node)
    {
        for (int i = 0; i < maxClients; ++i)
        {
            ClientsUsed[i] = BitConverter.ToBoolean(data, i);
            // create game object
            /*if (ClientsUsed[i])
            {
                PlayerMain.playerObjects[i] = Instantiate(Resources.Load<GameObject>("PlayerPrefab"));
                PlayerMain.playerObjects[i].GetComponent<PlayerMain>().playerType = 1;
            }*/
        }
		GameLayerJoinReceive();
    }

    static private void SyncJoinSend(int node)
    {
        byte[] ou = new byte[maxClients + 13];
        for (int i = 0; i < maxClients; i += 1)
        {
            ou[13 + i] = BitConverter.GetBytes(ClientsUsed[i])[0];
        }
        // Set up packet header
        Array.Copy(BitConverter.GetBytes(ClientID), 0, ou, 0, 4);
        Array.Copy(BitConverter.GetBytes(node), 0, ou, 4, 4);
        //int tmp = 3;
        Array.Copy(BitConverter.GetBytes((short)joinEvent), 0, ou, 11, 2);
        ou[8] = 1;
        // Copy our position in the stack to the packet, then update timeout and stack position
        Array.Copy(BitConverter.GetBytes((short)ImportantStackPos[node]), 0, ou, 9, 2);
        ImportantStackUsed[node, ImportantStackPos[node]] = true;
        ImportantStack[node, ImportantStackPos[node]] = ou;
        PacketTimeout[node, ImportantStackPos[node]] = 0;
        ImportantStackPos[node] += 1;
        ImportantStackPos[node] %= 8192;
        Sock.SendTo(ou, Clients[node]);
		// game layer set up
		GameLayerJoinSend(node);
	}

    static private void PlayerJoinOthers(byte[] data, int node)
    {
        if (node != ClientNode)
        {
			ClientsUsed[node] = true;
			GameLayerJoinOthers(node);
		}
    }

    public static void Packet_SendTo(byte[] data, int datasize, int type, int node, bool important = false)
    {
        if (Host == -1 || Sock == null)
        {
            return;
        }
        // Format packet header
        byte[] outt = new byte[datasize + 13];
        Array.Copy(BitConverter.GetBytes(ClientID), 0, outt, 0, 4);
        Array.Copy(BitConverter.GetBytes(ClientNode), 0, outt, 4, 4);
        // note if it's important
        if (important)
        {
            outt[8] = 1;
        }
        Array.Copy(BitConverter.GetBytes(type), 0, outt, 11, 2);
        // Copy our actual data over
        Array.Copy(data, 0, outt, 13, datasize);
        // If we're the host, then attempt to send out to the client. otherwise, we're only connected to the host, so we send to them regardless
        if (Host == 1)
        {
            //Array.Copy(BitConverter.GetBytes(node), 0, outt, 4, 4);
            // If it's an important packet, let's make note to re-send it as well as its ID
            if (important)
            {
                // Close connection if we're too far into the stack
                int oldpos = ImportantStackPos[node] - 4096;
                if (oldpos < 0)
                {
                    oldpos = 8192 + oldpos;
                }
                if (ImportantStackUsed[node, oldpos])
                {
                    // close connection
                    CloseConn(ClientNode);
                    return;
                }
                // Copy our position in the stack to the packet, then update timeout and stack position
                Array.Copy(BitConverter.GetBytes((short)ImportantStackPos[node]), 0, outt, 9, 2);
                ImportantStackUsed[node, ImportantStackPos[node]] = true;
                //ImportantStack[i,ImportantStackPos[i]] = outt;
                // Allocate new memory for the important stack
                ImportantStack[node, ImportantStackPos[node]] = new byte[datasize + 13];
                Array.Copy(outt, 0, ImportantStack[node, ImportantStackPos[node]], 0, datasize + 13);
                PacketTimeout[node, ImportantStackPos[node]] = 0;
                ImportantStackPos[node] += 1;
                ImportantStackPos[node] %= 8192;
            }
            Sock.SendTo(outt, Clients[node]);
        } else
        {
            // Haven't connected, abort
            if (0 == -1)
            {
                return;
            }
            // If it's an important packet, let's make note to re-send it as well as its ID
            if (important)
            {
                // Close connection if we're too far into the stack
                int oldpos = ImportantStackPos[0] - 4096;
                if (oldpos < 0)
                {
                    oldpos = 8192 + oldpos;
                }
                if (ImportantStackUsed[0, oldpos])
                {
                    // close connection
                    CloseConn(0);
                    return;
                }
                // Copy our position in the stack to the packet, then update timeout and stack position
                Array.Copy(BitConverter.GetBytes((short)ImportantStackPos[0]), 0, outt, 9, 2);
                ImportantStackUsed[0, ImportantStackPos[0]] = true;
                ImportantStack[0, ImportantStackPos[0]] = outt;
                PacketTimeout[0, ImportantStackPos[0]] = 0;
                ImportantStackPos[0] += 1;
                ImportantStackPos[0] %= 8192;
            }
            Sock.SendTo(outt, localEndPoint);
        }
    }

    public static void Packet_Send(byte[] data, int datasize, int type, bool important = false)
    {
        if (Host == -1 || Sock == null)
        {
            return;
        }
        // Format packet header
        byte[] outt = new byte[datasize + 13];
        Array.Copy(BitConverter.GetBytes(ClientID), 0, outt, 0, 4);
        Array.Copy(BitConverter.GetBytes(ClientNode), 0, outt, 4, 4);
        // note if it's important
        if (important)
        {
            outt[8] = 1;
        }
        Array.Copy(BitConverter.GetBytes((short)type), 0, outt, 11, 2);
        // Copy our actual data over
        Array.Copy(data, 0, outt, 13, datasize);
        // If we're the host, then attempt to send out to all clients. otherwise just send to the host
        if (Host == 1)
        {
            for (int i = 1; i < maxClients; i += 1)
            {
                if (ClientsUsed[i])
                {
                    //Array.Copy(BitConverter.GetBytes(i), 0, outt, 4, 4);
                    // If it's an important packet, let's make note to re-send it as well as its ID
                    if (important)
                    {
                        // Close connection if we're too far into the stack
                        int oldpos = ImportantStackPos[i] - 4096;
                        if (oldpos < 0)
                        {
                            oldpos = 8192 + oldpos;
                        }
                        if (ImportantStackUsed[i, oldpos])
                        {
                            // close connection
                            CloseConn(ClientNode);
                            continue;
                        }
                        // Copy our position in the stack to the packet, then update timeout and stack position
                        Array.Copy(BitConverter.GetBytes((short)ImportantStackPos[i]), 0, outt, 9, 2);
                        ImportantStackUsed[i, ImportantStackPos[i]] = true;
                        //ImportantStack[i,ImportantStackPos[i]] = outt;
                        // Allocate new memory for the important stack
                        ImportantStack[i, ImportantStackPos[i]] = new byte[datasize + 13];
                        Array.Copy(outt, 0, ImportantStack[i, ImportantStackPos[i]], 0, datasize + 13);
                        PacketTimeout[i, ImportantStackPos[i]] = 0;
                        ImportantStackPos[i] += 1;
                        ImportantStackPos[i] %= 8192;
                    }
                    Sock.SendTo(outt, Clients[i]);
                }
            }
        }
        else
        {
            // Haven't connected, abort
            if (ClientNode == -1)
            {
                return;
            }
            // If it's an important packet, let's make note to re-send it as well as its ID
            if (important)
            {
                // Close connection if we're too far into the stack
                int oldpos = ImportantStackPos[0] - 4096;
                if (oldpos < 0)
                {
                    oldpos = 8192 + oldpos;
                }
                if (ImportantStackUsed[0, oldpos])
                {
                    // close connection
                    CloseConn(0);
                    return;
                }
                // Copy our position in the stack to the packet, then update timeout and stack position
                Array.Copy(BitConverter.GetBytes(ImportantStackPos[0]), 0, outt, 9, 2);
                ImportantStackUsed[0, ImportantStackPos[0]] = true;
                ImportantStack[0, ImportantStackPos[0]] = outt;
                PacketTimeout[0, ImportantStackPos[0]] = 0;
                ImportantStackPos[0] += 1;
                ImportantStackPos[0] %= 8192;
            }
            Sock.SendTo(outt, localEndPoint);
        }
    }

	public static void CloseConn(int node, int reason = 0)
	{
		if (Host == 1 || node == ClientNode)
		{
			// If we're the host, then alert everyone else to their dc
			byte[] ou = new byte[8];
			Utilj.AddToArray(node, ou, 0);
			Utilj.AddToArray(reason, ou, 4);
			Packet_Send(ou, ou.Length, disconnectEvent, true);
		}
		ClientsUsed[node] = false;
		// iterate over our two stacks and free them
		for (int i = 0; i < 8192; i += 1)
		{
			ReceiveStackUsed[node, i] = false;
			ImportantStackUsed[node, i] = false;
		}
		ImportantStackPos[node] = 0;
		ReceiveStackPos[node] = 0;
		GameLayerDisconnect(node, reason);
	}
}
