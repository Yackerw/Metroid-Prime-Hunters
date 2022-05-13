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
                int RecvImportant = packet[8];
                int RecvImpID = System.BitConverter.ToInt16(packet, 9);
                int RecvType = System.BitConverter.ToInt16(packet, 11);
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
                                ClientIDs[ClientNode] = RecvID;
                                ClientID = BitConverter.ToInt32(packet, 0);
                                ClientsUsed[ClientNode] = true;
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
                        if (SockTimeout[i] > 20.0f)
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
                    int PStartPos = ImportantStackPos[ClientNode] - 4096;
                    if (PStartPos < 0)
                    {
                        PStartPos = 8192 + PStartPos;
                    }
                    while (PStartPos != ImportantStackPos[ClientNode])
                    {
                        if (ImportantStackUsed[ClientNode, PStartPos])
                        {
                            PacketTimeout[ClientNode, PStartPos] += Time.deltaTime;
                            if (PacketTimeout[ClientNode, PStartPos] > 0.3f)
                            {
                                PacketTimeout[ClientNode, PStartPos] = 0;
                                Sock.SendTo(ImportantStack[ClientNode, PStartPos], localEndPoint);
                            }
                        }
                        PStartPos += 1;
                        PStartPos %= 8192;
                    }
                    SockTimeout[ClientNode] += Time.deltaTime;
                    if (SockTimeout[ClientNode] > 20.0f)
                    {
                        // Eventually close socket here
                        CloseConn(ClientNode);
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
        /*switch (type)
        {
            // case 1: normal, every frame sync
            case 1:
                {
                    PlayerNetworking.ReceivePlayerPosition(packet, node);
                    // Re-send data if host
                    if (Host == 1)
                    {
                        byte[] ou = new byte[packSize + 4];
                        Array.Copy(packet, 0, ou, 4, packSize);
                        Array.Copy(BitConverter.GetBytes(node), 0, ou, 0, 4);
                        Packet_Send(ou, ou.Length, 2, false);
                    }
                }
            break;
            // case 2: every frame sync being relayed
            case 2:
                {
                    // we won't be fooled by your trickery!
                    if (Host == 1)
                    {
                        return;
                    }
                    int n = BitConverter.ToInt32(packet, 0);
                    if (n != ClientNode)
                    {
                        byte[] posin = new byte[packSize - 4];
                        Array.Copy(packet, 4, posin, 0, packSize - 4);
                        PlayerNetworking.ReceivePlayerPosition(posin, n);
                    }
                }
            break;
            // case 3: game data from connection
            case 3:
                {
                    SyncJoinRecv(packet);
                }
            break;
            // case 4: other players connecting
            case 4:
                {
                    PlayerJoinOthers(BitConverter.ToInt32(packet, 0));
                }
                break;
            // case 5: disconnection
            case 5:
                {
                    if (Host != 1)
                    {
                        int n = BitConverter.ToInt32(packet, 0);
						int reas = BitConverter.ToInt32(packet, 4);
                        // if we're the ones being disconnected, then do that
                        if (n == ClientNode)
                        {
                            CloseConn(ClientNode, reas);
                        } else
                        {
							GameLayerDisconnect(node, reas);
                        }
                    } else
                    {
						int reas = BitConverter.ToInt32(packet, 4);
						CloseConn(node, reas);
                    }
                }
                break;
            // case 6: player event
            case 6:
                {
                    int subType = BitConverter.ToInt32(packet, 0);
                    byte[] newArr = new byte[packSize - 4];
                    Array.Copy(packet, 4, newArr, 0, packSize - 4);
                    PlayerNetworking.PlayerInEvents[subType](node, newArr);
                    if (Host == 1)
                    {
                        // relay to other players
                        byte[] newOu = new byte[packSize + 4];
                        Array.Copy(packet, 0, newOu, 4, packSize);
                        Array.Copy(BitConverter.GetBytes(node), 0, newOu, 0, 4);
                        Packet_Send(newOu, newOu.Length, 7);
                    }
                }
                break;
            // case 7: relayed player events
            case 7:
                {
                    if (Host != 1)
                    {
                        int newNode = BitConverter.ToInt32(packet, 0);
                        if (newNode != ClientNode)
                        {
                            int subType = BitConverter.ToInt32(packet, 4);
                            byte[] newArr = new byte[packet.Length - 8];
                            Array.Copy(packet, 8, newArr, 0, packet.Length - 8);
                            PlayerNetworking.PlayerInEvents[subType](newNode, newArr);
                        }
                    }
                }
                break;
            // case 8: object step
            case 8:
                {
                    if (Host != 1)
                    {
                        int id = BitConverter.ToInt32(packet, 0);
						if (id > 0 && JourneyNet.netObjects.Count > id && JourneyNet.netObjects[id] != null)
						{
							Array.Copy(packet, 4, packet, 0, packet.Length - 4);
							try
							{
								JourneyNet.netObjects[id].NetRecv(packet);
							} catch
							{
								// painful bandaid, but prevents things from breaking
							}
						}
                    }
                }
                break;
            // case 9: object create
            case 9:
                {
                    if (Host != 1)
                    {
                        NetLevel.LoadObject(packet, packSize);
                    }
                }
                break;
            // case 10: object specific network events
            case 10:
                {
                    int id = BitConverter.ToInt32(packet, 0);
                    int eventType = BitConverter.ToInt32(packet, 4);
                    byte[] restOfData;
                    if (packet.Length - 8 > 0)
                    {
                        restOfData = new byte[packet.Length - 8];
                        Array.Copy(packet, 8, restOfData, 0, restOfData.Length);
                    } else
                    {
                        restOfData = null;
                    }
                    if (JourneyNet.netObjects.Count > id && JourneyNet.netObjects[id] != null)
                    {
						JourneyNet.netObjects[id].NetEvent(restOfData, eventType);
                    }
                }
                break;
            // case 11: trash an object
            case 11:
                {
                    if (Host != 1 && JourneyManager.sceneType == 2)
                    {
                        int id = BitConverter.ToInt32(packet, 0);
						JourneyNet.netObjects[id].NetClose();
						JourneyNet.netObjects[id] = null;
                    }
                }
                break;
            // case 12: level load
            case 12:
                {
                    if (Host != 1 && !PlayerNetworking.players[0].sending)
                    {
                        string levelName;
                        Utilj.StringFromArray(packet, 1, out levelName);
						// reset netlevel stuff
						NetLevel.objectSize = new List<int>();
						NetLevel.objectStore = new List<byte[]>();
                        if (!Loading.LoadLevel(levelName))
						{
							// level doesn't exist, let's ask for it
							Packet_Send(new byte[1], 1, 14, true);
							// we failed, disconnect
							//CloseConn(ClientNode, 3);
						}
                    }
                }
                break;
            // case 13: syncing everyone's name on join
            case 13:
                {
                    if (Host != 1)
                    {
                        PlayerNetworking.ReceiveNames(packet, node);
                    }
                }
                break;
			// case 14: someone has requested our current level be sent to them
			case 14:
				{
					if (Host == 1)
					{
						PlayerNetworking.players[node].GZI = JourneyManager.stageArchive;
						PlayerNetworking.players[node].sending = true;
						PlayerNetworking.players[node].numPackets = PlayerNetworking.players[node].GZI.ms.Length / 256;
						if (PlayerNetworking.players[node].GZI.ms.Length % 256 != 0)
						{
							++PlayerNetworking.players[node].numPackets;
						}
						PlayerNetworking.players[node].received = new bool[PlayerNetworking.players[node].numPackets];
						// tell them how large it will be and to start checking for packets
						byte[] name = Utilj.StringToArray(PlayerNetworking.players[node].GZI.baseDir);
						byte[] ou = new byte[8 + name.Length];
						Utilj.AddToArray(PlayerNetworking.players[node].numPackets, ou, 0);
						Utilj.AddToArray(PlayerNetworking.players[node].GZI.ms.Length, ou, 4);
						Array.Copy(name, 0, ou, 8, name.Length);
						Packet_SendTo(ou, ou.Length, 15, node, true);
					}
				}
				break;
				// case 15: host is sending us a file, prepare to receive it
			case 15:
				{
					if (Host != 1)
					{
						PlayerNetworking.players[0].GZI = new ZIPUtilities.GZInfo();
						SceneManager.LoadScene("StageDownloading");
						int num = BitConverter.ToInt32(packet, 0);
						PlayerNetworking.players[0].numPackets = num;
						PlayerNetworking.players[0].received = new bool[num];
						PlayerNetworking.players[0].sending = true;
						// allocate space for the incoming archive
						PlayerNetworking.players[0].GZI.ms = new byte[BitConverter.ToInt32(packet, 4)];
						PlayerNetworking.players[0].GZI.fileDirs = new List<string>();
						PlayerNetworking.players[0].GZI.unCompFileSize = new List<int>();
						// and acquire the name
						string name;
						Utilj.StringFromArray(packet, 8, out name);
						PlayerNetworking.players[0].GZI.baseDir = name;
						// mark us as "in a menu"
						JourneyManager.sceneType = 0;
					}
				}
				break;
				// case 16: received part of a file from the host
			case 16:
				{
					if (Host != 1)
					{
						NetFileHandler.ReceivePacket(packet, node);
					}
				}
				break;
			// case 17: client has told us they received a part of a file
			case 17:
				{
					if (Host == 1)
					{
						int packNum = BitConverter.ToInt32(packet, 0);
						if (PlayerNetworking.players[node].sending && PlayerNetworking.players[node].numPackets > packNum && packNum >= 0)
						{
							PlayerNetworking.players[node].received[packNum] = true;
						}
					}
				}
				break;
			// case 18: host has told us all packets sent. Inform them we got the message, and to re-send dropped ones, or send the metadata if all have been sent successfully
			case 18:
				{
					if (Host != 1)
					{
						Packet_Send(new byte[1], 1, 19, true);
					}
				}
				break;
			// case 19: client has told us we're good to go to re-send packet data, or to send metadata
			case 19:
				{
					if (Host == 1)
					{
						NetFileHandler.HostFinish(node);
					}
				}
				break;
			// case 20: we are receiving metadata from the host about the file we have received
			case 20:
				{
					if (Host != 1 && PlayerNetworking.players[0].sending)
					{
						PlayerNetworking.players[0].GZI.unCompFileSize.Add(BitConverter.ToInt32(packet, 0));
						string fName;
						Utilj.StringFromArray(packet, 4, out fName);
						PlayerNetworking.players[0].GZI.fileDirs.Add(fName);
					}
				}
				break;
			// case 21: we've received everything necessary to create the file
			case 21:
				{
					if (Host != 1 && PlayerNetworking.players[0].sending)
					{
						ZIPUtilities.ZipToFolder(PlayerNetworking.players[0].GZI);
						// now free everything
						PlayerNetworking.players[0].GZI = new ZIPUtilities.GZInfo();
						PlayerNetworking.players[0].sending = false;
						PlayerNetworking.players[0].bytesReceived = 0;
					}
				}
				break;
			// case 22: we've received sync info on a certain sync object
			case 22:
				{
					if (Host != 1)
					{
						int id = BitConverter.ToInt32(packet, 0);
						// error proof
						if (id >= 0 && id < CullingManager.syncObjects.Count)
						{
							CullingManager.syncObjects[id].UpdateSync(packet, packSize);
						}
					}
				}
				break;
			// case 23: trigger activated
			case 23:
				{
					int id = BitConverter.ToInt32(packet, 0);
					if (id >= 0 && id < CullingManager.groups.Count)
					{
						GeneralTrigger gt = CullingManager.groups[id].gameObject.GetComponent<GeneralTrigger>();
						if (gt != null)
						{
							PlayerInteract.TriggerInteraction(gt.gameObject, PlayerMain.localPlayer.GetComponent<PlayerMain>(), true);
							if (Host == 1)
							{
								// relay it
								for (int i = 0; i < ClientsUsed.Length; ++i)
								{
									if (ClientsUsed[i] && i != node)
									{
										Packet_SendTo(packet, packet.Length, 23, i, true);
									}
								}
							}
						}
					}
				}
				break;
        }*/
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
            Array.Copy(BitConverter.GetBytes(node), 0, outt, 4, 4);
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
            if (ClientNode == -1)
            {
                return;
            }
            // If it's an important packet, let's make note to re-send it as well as its ID
            if (important)
            {
                // Close connection if we're too far into the stack
                int oldpos = ImportantStackPos[ClientNode] - 4096;
                if (oldpos < 0)
                {
                    oldpos = 8192 + oldpos;
                }
                if (ImportantStackUsed[ClientNode, oldpos])
                {
                    // close connection
                    CloseConn(ClientNode);
                    return;
                }
                // Copy our position in the stack to the packet, then update timeout and stack position
                Array.Copy(BitConverter.GetBytes((short)ImportantStackPos[ClientNode]), 0, outt, 9, 2);
                ImportantStackUsed[ClientNode, ImportantStackPos[ClientNode]] = true;
                ImportantStack[ClientNode, ImportantStackPos[ClientNode]] = outt;
                PacketTimeout[ClientNode, ImportantStackPos[ClientNode]] = 0;
                ImportantStackPos[ClientNode] += 1;
                ImportantStackPos[ClientNode] %= 8192;
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
            for (int i = 0; i < maxClients; i += 1)
            {
                if (ClientsUsed[i])
                {
                    Array.Copy(BitConverter.GetBytes(i), 0, outt, 4, 4);
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
                int oldpos = ImportantStackPos[ClientNode] - 4096;
                if (oldpos < 0)
                {
                    oldpos = 8192 + oldpos;
                }
                if (ImportantStackUsed[ClientNode, oldpos])
                {
                    // close connection
                    CloseConn(ClientNode);
                    return;
                }
                // Copy our position in the stack to the packet, then update timeout and stack position
                Array.Copy(BitConverter.GetBytes(ImportantStackPos[ClientNode]), 0, outt, 9, 2);
                ImportantStackUsed[ClientNode, ImportantStackPos[ClientNode]] = true;
                ImportantStack[ClientNode, ImportantStackPos[ClientNode]] = outt;
                PacketTimeout[ClientNode, ImportantStackPos[ClientNode]] = 0;
                ImportantStackPos[ClientNode] += 1;
                ImportantStackPos[ClientNode] %= 8192;
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
