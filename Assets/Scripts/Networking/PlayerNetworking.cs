using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerNetworking
{
    public static PlayerNetworking[] netPlayers = new PlayerNetworking[NetworkingMain.maxClients];
    // confusing name? yes. descriptive of what it is? also yes. networking event for player events.
    public static int playerEventEvent;
    public static int playerEventRelayEvent;

    public static int playerNamesEvent;

    static Func<PlayerNetworking, byte[]>[] PlayerOutEvents = new Func<PlayerNetworking, byte[]>[128];
    static int[] PlayerOEventSizes = new int[128];
    static Action<PlayerNetworking, byte[]>[] PlayerInEvents = new Action<PlayerNetworking, byte[]>[128];
    static int[] PlayerIEventSizes = new int[128];
    static int PlayerOEvents;
    static int PlayerIEvents;

    public string name;
    public int id;
    public PlayerMain player;


    public static void Setup()
	{
        playerEventEvent = NetworkingMain.RegisterPacketType(EventProcessor);
        playerEventRelayEvent = NetworkingMain.RegisterPacketType(EventRelayProcessor);
        for (int i = 0; i < netPlayers.Length; ++i)
		{
            netPlayers[i] = new PlayerNetworking();
		}
	}


    // Adds event to fire
    static public int AddPlayerOutEvent(Func<PlayerNetworking, byte[]> function, int ArraySize)
    {
        PlayerOutEvents[PlayerOEvents] = function;
        PlayerOEventSizes[PlayerOEvents] = ArraySize;
        PlayerOEvents++;
        return PlayerOEvents - 1;
    }

    // Adds event to receive
    static public void AddPlayerInEvent(Action<PlayerNetworking, byte[]> function, int ArraySize)
    {
        PlayerInEvents[PlayerIEvents] = function;
        PlayerIEventSizes[PlayerIEvents] = ArraySize;
        PlayerIEvents++;
    }
    static public void FirePlayerEvent(int ev, PlayerNetworking pm, bool important = true)
    {
        //byte[] outarray = new byte[PlayerOEventSizes[ev] + 4];
        byte[] ou = PlayerOutEvents[ev](pm);
        byte[] outarray = new byte[ou.Length + 4];
        // Indicating kind of event
        Array.Copy(BitConverter.GetBytes(ev), 0, outarray, 0, 4);
        // Call function and add return value to out buff
        Array.Copy(ou, 0, outarray, 4, ou.Length);
        NetworkingMain.Packet_Send(outarray, outarray.Length, playerEventEvent, important);
    }

    static private void EventProcessor(byte[] packet, int node)
    {
        int packSize = packet.Length;
        int subType = BitConverter.ToInt32(packet, 0);
        byte[] newArr = new byte[packSize - 4];
        Array.Copy(packet, 4, newArr, 0, packSize - 4);
        PlayerInEvents[subType](netPlayers[node], newArr);
        if (NetworkingMain.Host == 1)
        {
            // relay to other players
            byte[] newOu = new byte[packSize + 4];
            Array.Copy(packet, 0, newOu, 4, packSize);
            Array.Copy(BitConverter.GetBytes(node), 0, newOu, 0, 4);
            NetworkingMain.Packet_Send(newOu, newOu.Length, playerEventRelayEvent, true);
        }
    }

    static private void EventRelayProcessor(byte[] packet, int node)
    {
        if (NetworkingMain.Host != 1)
        {
            int newNode = BitConverter.ToInt32(packet, 0);
            if (newNode != NetworkingMain.ClientNode)
            {
                int subType = BitConverter.ToInt32(packet, 4);
                byte[] newArr = new byte[packet.Length - 8];
                Array.Copy(packet, 8, newArr, 0, packet.Length - 8);
                PlayerInEvents[subType](netPlayers[newNode], newArr);
            }
        }
    }

    public void NetUpdate()
	{
        FirePlayerEvent(PlayerNetworkEvents.stepEvent, this, false);
	}
}
