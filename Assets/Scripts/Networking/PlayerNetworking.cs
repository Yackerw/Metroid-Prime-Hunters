using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerNetworking
{
    public static PlayerNetworking[] netPlayers = new PlayerNetworking[32];
    // confusing name? yes. descriptive of what it is? also yes. networking event for player events.
    public static int playerEventEvent;
    public static int playerEventRelayEvent;

    public static int playerNamesEvent;

    static Func<PlayerMain, byte[]>[] PlayerOutEvents = new Func<PlayerMain, byte[]>[128];
    static int[] PlayerOEventSizes = new int[128];
    static Action<int, byte[]>[] PlayerInEvents = new Action<int, byte[]>[128];
    static int[] PlayerIEventSizes = new int[128];
    static int PlayerOEvents;
    static int PlayerIEvents;

    public string name;
    public int id;



    // Adds event to fire
    static public int AddPlayerOutEvent(Func<PlayerMain, byte[]> function, int ArraySize)
    {
        PlayerOutEvents[PlayerOEvents] = function;
        PlayerOEventSizes[PlayerOEvents] = ArraySize;
        PlayerOEvents++;
        return PlayerOEvents - 1;
    }

    // Adds event to receive
    static public void AddPlayerInEvent(Action<int, byte[]> function, int ArraySize)
    {
        PlayerInEvents[PlayerIEvents] = function;
        PlayerIEventSizes[PlayerIEvents] = ArraySize;
        PlayerIEvents++;
    }
    static public void FirePlayerEvent(int ev, PlayerMain pm, bool important = true)
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
        PlayerInEvents[subType](node, newArr);
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
                PlayerInEvents[subType](newNode, newArr);
            }
        }
    }
}
