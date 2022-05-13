using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*public class ObjectNetworking {

	static public int objStepEvent;
	static public int objCreateEvent;
	static public int objEventEvent;
	static public int objDestroyEvent;

	static public int netObjStepEvent;
	static public int netObjTriggerEvent;

	static public void Setup()
	{
		//objCreateEvent = NetworkingMain.RegisterPacketType(ObjectCreate);
		objStepEvent = NetworkingMain.RegisterPacketType(ObjectStep);
		objEventEvent = NetworkingMain.RegisterPacketType(ObjectEventProcessor);
		objDestroyEvent = NetworkingMain.RegisterPacketType(ObjectDestroy);
		//netObjStepEvent = NetworkingMain.RegisterPacketType(NetObjectStepProcessor);
	}

	/*static private void NetObjectStepProcessor(byte[] packet, int node)
	{
		if (NetworkingMain.Host != 1)
		{
			int id = BitConverter.ToInt32(packet, 0);
			// error proof
			if (id >= 0 && id < CullingManager.syncObjects.Count)
			{
				CullingManager.syncObjects[id].UpdateSync(packet, packet.Length);
			}
		}
	}*/

	/*static private void ObjectCreate(byte[] packet, int node)
	{
		if (NetworkingMain.Host != 1)
		{
			NetLevel.LoadObject(packet, packet.Length);
		}
	}*/

	/*static private void ObjectStep(byte[] packet, int node)
	{
		if (NetworkingMain.Host != 1)
		{
			int id = BitConverter.ToInt32(packet, 0);
			if (id > 0 && MetNet.netObjects.Count > id && MetNet.netObjects[id] != null)
			{
				Array.Copy(packet, 4, packet, 0, packet.Length - 4);
				try
				{
					MetNet.netObjects[id].NetRecv(packet);
				}
				catch
				{
					// painful bandaid, but prevents things from breaking
				}
			}
		}
	}

	static private void ObjectEventProcessor(byte[] packet, int node)
	{
		int id = BitConverter.ToInt32(packet, 0);
		int eventType = BitConverter.ToInt32(packet, 4);
		byte[] restOfData;
		if (packet.Length - 8 > 0)
		{
			restOfData = new byte[packet.Length - 8];
			Array.Copy(packet, 8, restOfData, 0, restOfData.Length);
		}
		else
		{
			restOfData = null;
		}
		if (MetNet.netObjects.Count > id && MetNet.netObjects[id] != null)
		{
			MetNet.netObjects[id].NetEvent(restOfData, eventType);
		}
	}

	static private void ObjectDestroy(byte[] packet, int node)
	{
		if (NetworkingMain.Host != 1 /*&& JourneyManager.sceneType == 2*///)
		/*{
			int id = BitConverter.ToInt32(packet, 0);
			MetNet.netObjects[id].NetClose();
			MetNet.netObjects[id] = null;
		}
	}
}
*/