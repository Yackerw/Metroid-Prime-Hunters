using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager
{
    public enum Characters : int { None, Samus }
    static public void SpawnPlayer(PlayerMain.PlayerType type, Characters character, Vector3 position, PlayerNetworking owner)
	{
		GameObject playerObj = GameObject.Instantiate((GameObject)Resources.Load("Objects/Player/Player"));
		owner.player = playerObj.GetComponent<PlayerMain>();
		owner.player.character = character;
		if (type == PlayerMain.PlayerType.local)
		{
			// spawn the arm cannon as well if we're the local player
			GameObject armCannon = GameObject.Instantiate((GameObject)Resources.Load("Objects/Player/Cannons/" + character.ToString() + "Cannon"));
			owner.player.gunHolder = armCannon.transform;
			owner.player.armCannon = armCannon.transform.GetChild(0).gameObject;
		}
		owner.player.playerID = owner.id;
		owner.player.playerType = type;
	}
}
