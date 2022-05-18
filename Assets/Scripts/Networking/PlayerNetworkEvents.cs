using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNetworkEvents
{

	static public int stepEvent;
    static public void Setup()
	{
		stepEvent = PlayerNetworking.AddPlayerOutEvent(SyncPosition, 12);
		PlayerNetworking.AddPlayerInEvent(SyncPositionReceive, 12);
	}

	static public byte[] SyncPosition(PlayerNetworking pm)
	{
		// error handling
		if (pm.player == null)
		{
			return new byte[12];
		}
		byte[] ou = new byte[12];
		Utilj.AddToArray(pm.player.transform.position, ou, 0);
		return ou;
	}

	static public void SyncPositionReceive(PlayerNetworking pm, byte[] inarr)
	{
		if (pm.player == null)
		{
			return;
		}
		pm.player.transform.position = Utilj.ReadVector3Array(inarr, 0);
	}
}
