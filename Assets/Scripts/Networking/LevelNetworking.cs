using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelNetworking
{
	static int syncLevelEvent;
    static public void Setup()
	{
		syncLevelEvent = NetworkingMain.RegisterPacketType(ReceiveLevelSync);
	}

	static public void SyncLevel()
	{
		byte[] data = Utilj.StringToArray(GameManager.currentLevel);
		NetworkingMain.Packet_Send(data, data.Length, syncLevelEvent, true);
	}
	static public void SyncLevelTo(int node)
	{
		byte[] data = Utilj.StringToArray(GameManager.currentLevel);
		NetworkingMain.Packet_SendTo(data, data.Length, syncLevelEvent, node, true);
	}

	static public void ReceiveLevelSync(byte[] data, int node)
	{
		if (NetworkingMain.Host != 1)
		{
			string levelName;
			Utilj.StringFromArray(data, 0, out levelName);
			GameManager.ChangeLevel(levelName);
		}
	}
}
