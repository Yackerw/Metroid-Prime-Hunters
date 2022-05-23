using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
	public enum GameState : int { Multiplayer, Menu }
	static public GameState gameState;
	static public string currentLevel;

	static GameManager instance;
	int sceneLoaded;

	private void Awake()
	{
		instance = this;
		DontDestroyOnLoad(gameObject);
	}

	static public void ChangeLevel(string levelName)
	{
		// TODO: replace this with an asset bundle system later
		SceneManager.LoadSceneAsync(levelName);
		SceneManager.sceneLoaded += SceneFinishedLoading;
		currentLevel = levelName;
		if (NetworkingMain.Host == 1)
		{
			LevelNetworking.SyncLevel();
		}
		instance.sceneLoaded = 0;
		MetNet.netObjectsUsed = new List<bool>();
		MetNet.netObjects = new List<NetObj>();
	}

	static public void SceneFinishedLoading(Scene scene, LoadSceneMode lsm)
	{
		//SceneManager.UnloadSceneAsync("LoadingScene");
		SceneManager.SetActiveScene(scene);
		SceneManager.sceneLoaded -= SceneFinishedLoading;
		// i am in rage
		instance.sceneLoaded = 2;
		// spawn in players
		gameState = GameState.Multiplayer;
	}

	private void Update()
	{
		if (sceneLoaded > 0)
		{
			--sceneLoaded;
			if (sceneLoaded == 0 && gameState == GameState.Multiplayer)
			{
				// TODO: make all of this more proper in the future
				for (int i = 0; i < PlayerNetworking.netPlayers.Length; ++i)
				{
					if (NetworkingMain.ClientsUsed[i])
					{
						if (i == NetworkingMain.ClientNode)
						{
							PlayerManager.SpawnPlayer(PlayerMain.PlayerType.local, PlayerManager.Characters.Samus, Vector3.zero, PlayerNetworking.netPlayers[i]);
						}
						else
						{
							PlayerManager.SpawnPlayer(PlayerMain.PlayerType.network, PlayerManager.Characters.Samus, Vector3.zero, PlayerNetworking.netPlayers[i]);
						}
					}
				}
			}
		}
	}
}
