using System;
using System.Collections.Generic;
using UnityEngine;

public class CapturePoint : SpawnPoint
{
	private const float UPDATE_RATE = 1f;

	private const float CAPTURE_RATE_PER_PERSON = 0.1f;

	public float captureRange = 10f;

	public bool canBeCaptured = true;

	private float control = 1f;

	private int pendingOwner;

	public Renderer flagRenderer;

	private void Start()
	{
		if (GameManager.instance.reverseMode)
		{
			if (owner == 0)
			{
				owner = 1;
			}
			else if (owner == 1)
			{
				owner = 0;
			}
		}
		SetOwner(owner);
		if (owner == -1)
		{
			if (GameManager.instance.assaultMode)
			{
				SetOwner(1);
			}
			else
			{
				control = 0f;
			}
		}
		InvokeRepeating("UpdateOwner", 1f, 1f);
	}

	private void UpdateOwner()
	{
		if (!canBeCaptured)
		{
			return;
		}
		List<Actor> list = ActorManager.AliveActorsInRange(base.transform.position, captureRange);
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		foreach (Actor item in list)
		{
			if (dictionary.ContainsKey(item.team))
			{
				Dictionary<int, int> dictionary2;
				Dictionary<int, int> dictionary3 = (dictionary2 = dictionary);
				int team;
				int key = (team = item.team);
				team = dictionary2[team];
				dictionary3[key] = team + 1;
			}
			else
			{
				dictionary.Add(item.team, 1);
			}
		}
		int num = -1;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < 2; i++)
		{
			if (dictionary.ContainsKey(i) && dictionary[i] > num3)
			{
				num = i;
				num2 = num3;
				num3 = dictionary[i];
			}
		}
		int num4 = num3 - num2;
		if (num != -1)
		{
			if (num != pendingOwner)
			{
				control -= (float)num4 * 0.1f;
				if (control <= 0f)
				{
					SetOwner(num);
					control = 0.01f;
				}
			}
			else
			{
				control = Mathf.Clamp01(control + (float)num4 * 0.1f);
				if (control == 1f && owner != pendingOwner)
				{
					SetOwner(pendingOwner);
				}
			}
		}
		flagRenderer.enabled = control > 0f;
		Vector3 localPosition = flagRenderer.transform.localPosition;
		localPosition.y = 0.1f + 1.7f * control;
		flagRenderer.transform.localPosition = localPosition;
	}

	private void SetOwner(int team)
	{
		int num = 0;
		int num2 = 0;
		switch (team)
		{
		case 0:
			num2++;
			break;
		case 1:
			num++;
			break;
		}
		if (team != owner)
		{
			if (owner == 0)
			{
				num2--;
			}
			else if (owner == 1)
			{
				num--;
			}
		}
		owner = team;
		pendingOwner = team;
		flagRenderer.material.color = ColorScheme.TeamColor(team);
		ScoreUi.AddFlag(num2, num);
		try
		{
			LoadoutUi.UpdateSpawnPointButtons();
		}
		catch (Exception)
		{
		}
	}
}
