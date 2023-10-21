using System;
using UnityEngine;

public class Seat : MonoBehaviour
{
	public enum Type
	{
		Driver = 0,
		Pilot = 1,
		Gunner = 2,
		Passenger = 3
	}

	public const int LAYER = 11;

	public Vehicle vehicle;

	public Type type = Type.Passenger;

	public bool enclosed;

	public Vector3 exitOffset = Vector3.zero;

	public MountedWeapon weapon;

	[NonSerialized]
	public Actor occupant;

	public GameObject hud;

	public bool IsOccupied()
	{
		return occupant != null;
	}

	public void SetOccupant(Actor actor)
	{
		occupant = actor;
		if (HasMountedWeapon())
		{
			weapon.user = occupant;
		}
		if (!occupant.aiControlled && hud != null)
		{
			hud.SetActive(true);
		}
		vehicle.OccupantEntered(this);
	}

	public void OccupantLeft()
	{
		occupant = null;
		if (HasMountedWeapon())
		{
			weapon.StopFire();
			weapon.user = null;
		}
		if (hud != null)
		{
			hud.SetActive(false);
		}
		vehicle.OccupantLeft(this);
	}

	public bool CanUseWeapon()
	{
		return type == Type.Passenger;
	}

	public bool HasMountedWeapon()
	{
		return weapon != null;
	}
}
