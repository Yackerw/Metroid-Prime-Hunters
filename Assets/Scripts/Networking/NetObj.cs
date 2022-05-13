using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

// DON'T USE THIS. THIS IS ONLY MEANT TO BE A BASE SCRIPT, WITH EXAMPLE FUNCTIONS.
/*public class NetObj : MonoBehaviour
{

    public int id = -1;
	public int parent = -1;

	public void CheckParent()
	{
		if (transform.parent != null)
		{
			NetObj no = transform.parent.GetComponent<NetObj>();
			if (no != null)
			{
                parent = no.id;
			}
		}
	}

    // Use this for initialization
    public bool NetStart()
    {
		CheckParent();
        if (id == -1)
        {
            if (NetworkingMain.Host == 1)
            {
                //id = MetNet.RegisterObject(this);
            }
            // orphaned object on client, nuke
            if (NetworkingMain.Host == 0 && id == -1)
            {
                Destroy(gameObject);
                return false;
            }
        }
        return true;
    }

    public virtual byte[] NetStep()
    {
        return null;
        // I recommend returning null here if the player is far away from the object, to save bandwidth
    }

    // closes networking; strongly recommend running base.netclose
    public virtual void NetClose()
    {
        if (NetworkingMain.Host == 1)
        {
			MetNet.RemoveObject(id);
        }
        return;
    }

    // step receive
    public virtual void NetRecv(byte[] data)
    {
        return;
    }

    /// <summary>
    /// Used to fire networking events. data is input, type is the event type
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    public void FireEvent(byte[] data, int type)
    {
        byte[] ou;
        if (data != null)
        {
            ou = new byte[data.Length + 8];
            Array.Copy(data, 0, ou, 8, data.Length);
        }
        else
        {
            ou = new byte[8];
        }
        Array.Copy(BitConverter.GetBytes(id), 0, ou, 0, 4);
        Array.Copy(BitConverter.GetBytes(type), 0, ou, 4, 4);
        NetworkingMain.Packet_Send(ou, ou.Length, ObjectNetworking.objEventEvent, true);
    }

    // receives networking events
    public virtual void NetEvent(byte[] data, int type)
    {
        return;
    }

    // used to sync variables on spawn
    public virtual byte[] GetSpawnVariables()
    {
        return null;
    }

    // receives the output of GetSpawnVariables
    public virtual void SetSpawnVariables(byte[] input)
    {
        return;
    }

    // used to get the asset name; change to match the asset bundle
    public virtual string GetAssetName()
    {
        return "";
    }

    // distance from player to sync. set to 0 to ignore
    public virtual float GetSyncDistance()
    {
        return 0;
    }
}

*/