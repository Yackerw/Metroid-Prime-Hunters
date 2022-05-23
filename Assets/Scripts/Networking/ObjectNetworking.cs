using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ObjectNetworking {

	static public int objStepEvent;
	static public int objCreateEvent;
	static public int objEventEvent;
	static public int objDestroyEvent;

	static public int netObjStepEvent;
	static public int netObjTriggerEvent;

	static public void Setup()
	{
		objCreateEvent = NetworkingMain.RegisterPacketType(ObjectCreate);
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

	static private void ObjectCreate(byte[] packet, int node)
	{
		// TODO: check for loading, wait for it to finish before creating the objects
		if (NetworkingMain.Host != 1)
		{
			// create object
			int objId = BitConverter.ToInt32(packet, 0);
			string assetToCreate;
			int baseOffs = Utilj.StringFromArray(packet, 4, out assetToCreate);
			Vector3 objPos = Utilj.ReadVector3Array(packet, baseOffs);
			Quaternion objRot = Utilj.ReadQuaternionArray(packet, baseOffs + 12);
			int parent = BitConverter.ToInt32(packet, baseOffs + 12 + 16);
			// now create the actual thing and register it to the array
			GameObject newObj = GameObject.Instantiate((GameObject)Resources.Load("Objects/" + assetToCreate));
			NetObj no = newObj.GetComponentInChildren<NetObj>();
			newObj.transform.position = objPos;
			newObj.transform.rotation = objRot;
			no.id = objId;
			no.parent = parent;
			if (parent != -1 && MetNet.netObjects[parent] != null)
			{
				newObj.transform.parent = MetNet.netObjects[parent].transform;
			}
			while (objId >= MetNet.netObjects.Count)
			{
				MetNet.netObjects.Add(null);
				MetNet.netObjectsUsed.Add(false);
			}
			MetNet.netObjects[objId] = no;
			MetNet.netObjectsUsed[objId] = true;
			if (packet.Length - (baseOffs + 32) != 0)
			{
				byte[] objSpawnVariables = new byte[packet.Length - (baseOffs + 32)];
				Array.Copy(packet, baseOffs + 32, objSpawnVariables, 0, packet.Length - (baseOffs + 32));
				no.SetSpawnVariables(packet);
			}
		}
	}

	static private void ObjectStep(byte[] packet, int node)
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
		if (NetworkingMain.Host != 1 /*&& JourneyManager.sceneType == 2*/)
		{
			int id = BitConverter.ToInt32(packet, 0);
			if (MetNet.netObjects[id] != null)
			{
				MetNet.netObjects[id].NetClose();
				MetNet.netObjects[id] = null;
			}
			MetNet.netObjectsUsed[id] = false;
		}
	}
}
