using System;
using UnityEngine;
using UnityEngine.Audio;

public class GameManager : MonoBehaviour
{
	public const int MENU_LEVEL_INDEX = 1;

	public static GameManager instance;

	[NonSerialized]
	public bool ingame;

	public GameObject loadoutUiPrefab;

	public bool reverseMode;

	public bool assaultMode;

	public int victoryPoints = 200;

	public AudioMixerGroup fpMixerGroup;

	private float gameStartTime;

	private void Awake()
	{
		instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	private void Start()
	{
		OnLevelWasLoaded(Application.loadedLevel);
	}

	private void OnLevelWasLoaded(int level)
	{
		if (IngameLevel(level))
		{
			StartGame();
		}
		else
		{
			ingame = false;
		}
	}

	private bool IngameLevel(int level)
	{
		return level > 1;
	}

	private void StartGame()
	{
		ingame = true;
		UnityEngine.Object.Instantiate(loadoutUiPrefab);
		ActorManager.instance.StartGame();
		CoverManager.instance.StartGame();
		DecalManager.instance.StartGame();
		gameStartTime = Time.time;
	}

	public float ElapsedGameTime()
	{
		return Time.time - gameStartTime;
	}
}
