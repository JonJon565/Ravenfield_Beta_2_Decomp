using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
	public enum WeaponSlot
	{
		Primary = 0,
		Secondary = 1,
		Gear = 2,
		LargeGear = 3
	}

	[Serializable]
	public class WeaponEntry
	{
		public string name = "Weapon";

		public Sprite image;

		public GameObject prefab;

		public WeaponSlot slot;

		public bool hidden;
	}

	public class LoadoutSet
	{
		public WeaponEntry primary;

		public WeaponEntry secondary;

		public WeaponEntry gear1;

		public WeaponEntry gear2;

		public WeaponEntry gear3;

		public LoadoutSet()
		{
			primary = null;
			secondary = null;
			gear1 = null;
			gear2 = null;
			gear3 = null;
		}
	}

	public static WeaponManager instance;

	public List<WeaponEntry> weapons;

	private int sequenceIndex;

	private KeyCode[] secretSequence = new KeyCode[13]
	{
		KeyCode.G,
		KeyCode.I,
		KeyCode.M,
		KeyCode.M,
		KeyCode.E,
		KeyCode.T,
		KeyCode.H,
		KeyCode.E,
		KeyCode.G,
		KeyCode.O,
		KeyCode.O,
		KeyCode.D,
		KeyCode.S
	};

	private void Awake()
	{
		instance = this;
	}

	public static List<WeaponEntry> GetWeaponEntriesOfSlot(WeaponSlot slot)
	{
		List<WeaponEntry> list = new List<WeaponEntry>();
		foreach (WeaponEntry weapon in instance.weapons)
		{
			if (weapon.slot == slot)
			{
				list.Add(weapon);
			}
		}
		return list;
	}

	public static WeaponEntry EntryNamed(string name)
	{
		return instance.weapons.Find((WeaponEntry obj) => obj.name == name);
	}

	private void Update()
	{
		if (GameManager.instance.ingame)
		{
			return;
		}
		if (Input.GetKeyDown(secretSequence[sequenceIndex]))
		{
			if (sequenceIndex < secretSequence.Length - 1)
			{
				sequenceIndex++;
				return;
			}
			sequenceIndex = 0;
			ShowAllWeapons();
			GetComponent<AudioSource>().Play();
		}
		else if (Input.anyKeyDown)
		{
			sequenceIndex = 0;
		}
	}

	private void ShowAllWeapons()
	{
		foreach (WeaponEntry weapon in weapons)
		{
			weapon.hidden = false;
		}
	}
}
