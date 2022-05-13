using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class MetNet : MonoBehaviour
{

	public static List<NetObj> netObjects = new List<NetObj>();

	static int netObjInd = 0;

	public static string IP = "";

	void Awake()
	{
		NetworkingMain.GameLayerJoinOthers = OtherJoinRecv;
		NetworkingMain.GameLayerJoinSend = JoinSend;
		NetworkingMain.GameLayerJoinReceive = JoinRecv;
		NetworkingMain.GameLayerDisconnect = Disconnect;
		DontDestroyOnLoad(gameObject);
		PlayerNetworking.Setup();
		ObjectNetworking.Setup();
	}

	static private void JoinSend(int node)
	{
		// tell them what level we're on
		//NetLevel.SyncLevelChange(node);
		//PlayerNetworking.SyncNames(node);
		// sync all objects
		for (int i = 0; i < netObjects.Count; i++)
		{
			if (netObjects[i] != null)
			{
				SyncObject(netObjects[i], netObjects[i].id, node);
			}
		}
		// do misc set up here
		// setup default name
		PlayerNetworking.netPlayers[node].name = "Player";
		PlayerNetworking.netPlayers[node].id = node;
	}

	static private void JoinRecv()
	{
		//PlayerNetworking.FirePlayerEvent(PlayerNetworkEvents.nameEvent, null);
	}

	static private void OtherJoinRecv(int node)
	{
		// create new joiners object
		PlayerNetworking.netPlayers[node].name = "Player";
	}

	static string LogDisconnectReason(int reason)
	{
		string discreas = "Lost connection to server";
		switch (reason)
		{
			case 1:
				{
					discreas = "Kicked by admin";
				}
				break;
			case 2:
				{
					discreas = "Banned by admin";
				}
				break;
			case 3:
				{
					discreas = "Server loaded mod you lack";
				}
				break;
			case 4:
				{
					discreas = "Player has quit the game";
				}
				break;
		}
		return discreas;
	}

	static private void Disconnect(int node, int reason)
	{
		
	}

	public static void SyncObject(NetObj obj, int id, int node = -1)
	{
		string asset = obj.GetAssetName();
		byte[] syncVars = obj.GetSpawnVariables();
		byte[] ou;
		if (syncVars != null)
		{
			ou = new byte[4 + asset.Length + 1 + 32 + syncVars.Length];
			Array.Copy(syncVars, 0, ou, asset.Length + 5 + 32, syncVars.Length);
		}
		else
		{
			ou = new byte[4 + asset.Length + 1 + 32];
		}
		Array.Copy(BitConverter.GetBytes(id), 0, ou, 0, 4);
		Array.Copy(System.Text.Encoding.ASCII.GetBytes(asset), 0, ou, 4, asset.Length);
		ou[4 + asset.Length] = 0;
		Transform tr = obj.gameObject.transform;
		Array.Copy(BitConverter.GetBytes(tr.position.x), 0, ou, asset.Length + 5, 4);
		Array.Copy(BitConverter.GetBytes(tr.position.y), 0, ou, asset.Length + 5 + 4, 4);
		Array.Copy(BitConverter.GetBytes(tr.position.z), 0, ou, asset.Length + 5 + 8, 4);
		Array.Copy(BitConverter.GetBytes(tr.rotation.x), 0, ou, asset.Length + 5 + 12, 4);
		Array.Copy(BitConverter.GetBytes(tr.rotation.y), 0, ou, asset.Length + 5 + 16, 4);
		Array.Copy(BitConverter.GetBytes(tr.rotation.z), 0, ou, asset.Length + 5 + 20, 4);
		Array.Copy(BitConverter.GetBytes(tr.rotation.w), 0, ou, asset.Length + 5 + 24, 4);
		Array.Copy(BitConverter.GetBytes(obj.parent), 0, ou, asset.Length + 5 + 28, 4);
		if (node == -1)
		{
			NetworkingMain.Packet_Send(ou, ou.Length, ObjectNetworking.objCreateEvent, true);
		}
		else
		{
			NetworkingMain.Packet_SendTo(ou, ou.Length, ObjectNetworking.objCreateEvent, node, true);
		}
	}

	// used by host
	public static int RegisterObject(NetObj no)
	{
		if (NetworkingMain.Host != 1) return -1;
		netObjects.Add(no);
		netObjInd++;
		SyncObject(no, netObjInd - 1);
		return netObjInd - 1;
	}

	// used by client
	public static int AddObject(NetObj no, int id)
	{
		if (id > netObjects.Capacity)
		{
			netObjects.AddRange(Enumerable.Repeat(new NetObj(), id - netObjects.Capacity));
		}
		netObjects[id] = no;
		return id;
	}

	public static void RemoveObject(int id)
	{
		netObjects[id] = null;
		if (NetworkingMain.Host == 1)
		{
			NetworkingMain.Packet_Send(BitConverter.GetBytes(id), 4, ObjectNetworking.objDestroyEvent, true);
		}
	}

	// maintain object sync
	private void FixedUpdate()
	{
		if (NetworkingMain.Host == 1)
		{
			// sync objects
			for (int i = 0; i < netObjects.Count; i++)
			{
				if (netObjects[i] != null)
				{
					byte[] retval = netObjects[i].NetStep();
					if (retval != null && retval.Length != 0)
					{
						Array.Resize<byte>(ref retval, retval.Length + 4);
						Array.Copy(retval, 0, retval, 4, retval.Length - 4);
						Array.Copy(BitConverter.GetBytes(i), 0, retval, 0, 4);
						float dist = netObjects[i].GetSyncDistance();
						if (dist <= 0)
						{
							NetworkingMain.Packet_Send(retval, retval.Length, ObjectNetworking.objStepEvent);
						}
						else
						{
							dist *= dist;
							// TODO: update to account for spectating
							/*for (int i2 = 0; i2 < NetworkingMain.ClientsUsed.Length; ++i2)
							{
								GameObject po = PlayerMain.playerObjects[i2];
								if (po != null && NetworkingMain.ClientsUsed[i2])
								{
									// get distance
									float dist2 = Vector3.SqrMagnitude(netObjects[i].transform.position - po.transform.position);
									if (dist2 < dist)
									{
										NetworkingMain.Packet_SendTo(retval, retval.Length, ObjectNetworking.objStepEvent, i2);
									}
								}
							}*/
						}
					}
				}
			}

			// sync custom networked objects
			// should really change this part to a different function
			/*for (int i = 0; i < CullingManager.syncObjects.Count; ++i)
			{
				if (CullingManager.syncObjects[i] != null && CullingManager.syncObjects[i].gameObject.activeInHierarchy)
				{
					GameObject so = CullingManager.syncObjects[i].gameObject;
					SyncObject soc = CullingManager.syncObjects[i];
					Animator anim = null;
					Rigidbody rb = so.GetComponent<Rigidbody>();
					int ouSize = 0;
					byte fields = 0;
					// prepare the array of the proper size to sync information
					if (soc.syncPosition)
					{
						ouSize += 12;
						fields |= 1;
					}
					if (soc.syncRotation)
					{
						ouSize += 16;
						fields |= 1 << 1;
					}
					if (soc.syncForce && rb != null)
					{
						ouSize += 12;
						fields |= 1 << 2;
					}
					if (soc.syncDirForce && rb != null)
					{
						ouSize += 12;
						fields |= 1 << 3;
					}
					string animName = null;
					if (soc.syncAnimation)
					{
						ouSize += 4;
						fields |= 1 << 4;
						anim = so.GetComponent<Animator>();
						AnimatorClipInfo[] aa = anim.GetCurrentAnimatorClipInfo(0);
						if (anim != null && aa.Length != 0 && (aa[0].clip.name != soc.prevAnim || soc.syncConstant))
						{
							animName = anim.GetCurrentAnimatorClipInfo(0)[0].clip.name;
							ouSize += animName.Length + 1;
							fields |= 1 << 5;
						}
					}
					// now set up the array
					byte[] ou = new byte[ouSize + 5];
					Utilj.AddToArray(i, ou, 0);
					ou[4] = fields;
					int offs = 5;
					// position
					if ((fields & 1) != 0)
					{
						Utilj.AddToArray(so.transform.position, ou, offs);
						offs += 12;
					}
					// rotation
					if ((fields & (1 << 1)) != 0)
					{
						Utilj.AddToArray(so.transform.rotation, ou, offs);
						offs += 16;
					}
					// force
					if ((fields & (1 << 2)) != 0)
					{
						Utilj.AddToArray(rb.velocity, ou, offs);
						offs += 12;
					}
					// rotational force
					if ((fields & (1 << 3)) != 0)
					{
						Utilj.AddToArray(rb.angularVelocity, ou, offs);
						offs += 12;
					}
					// animation
					if ((fields & (1 << 4)) != 0)
					{
						Utilj.AddToArray(anim.GetCurrentAnimatorStateInfo(0).normalizedTime, ou, offs);
						offs += 4;
						if ((fields & (1 << 5)) != 0)
						{
							Array.Copy(Utilj.StringToArray(animName), 0, ou, offs, animName.Length + 1);
						}
					}
					soc.prevAnim = animName;
					// now sync it out
					for (int i2 = 0; i2 < NetworkingMain.ClientsUsed.Length; ++i2)
					{
						GameObject po = PlayerMain.playerObjects[i2];
						if (po != null && NetworkingMain.ClientsUsed[i2])
						{
							// get distance
							float dist = Vector3.SqrMagnitude(so.transform.position - po.transform.position);
							// sync if player is close enough, or animation has been updated
							if (dist <= soc.syncDistance * soc.syncDistance || ((fields & (1 << 5)) != 0 && !soc.syncConstant))
							{
								// mark it as important if an animation has been updated, as well
								NetworkingMain.Packet_SendTo(ou, ou.Length, ObjectNetworking.netObjStepEvent, i2, (fields & (1 << 5)) != 0 && !soc.syncConstant);
							}
						}
					}
				}
			}*/
		}
	}

	// networked update
	private void Update()
	{
		NetworkingMain.Update();
	}
}