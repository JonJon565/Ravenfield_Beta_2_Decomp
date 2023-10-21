using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Javelin : ScopedWeapon
{
	private const float MAX_DISTANCE = 1000f;

	private const int TARGET_LOS_MASK = 1;

	private const int TARGET_LAYER_MASK = 5377;

	private const float LOCK_ON_DOT = 0.95f;

	public Transform pointSampler;

	public RawImage lockImage;

	public Texture2D lockingTexture;

	public Texture2D lockedTexture;

	private Vehicle target;

	private Action lockOnAction = new Action(2f);

	private Action lockOnStayAction = new Action(1f);

	public override void Unholster()
	{
		base.Unholster();
		if (ammo == 0)
		{
			ReloadDone();
		}
	}

	protected override Projectile SpawnProjectile(Vector3 direction)
	{
		Projectile projectile = base.SpawnProjectile(direction);
		JavelinMissile javelinMissile = (JavelinMissile)projectile;
		Ray ray = new Ray(pointSampler.position, pointSampler.forward);
		if (target != null)
		{
			javelinMissile.target = target.transform;
			if (target.HasDriver() && target.directJavelinPath)
			{
				javelinMissile.ForceDirectMode();
			}
		}
		return projectile;
	}

	protected override void Update()
	{
		base.Update();
		if (aiming)
		{
			Vehicle vehicle = FindTarget();
			if (vehicle == target && target != null && HasLock())
			{
				lockOnStayAction.Start();
			}
			if (vehicle != target && lockOnStayAction.TrueDone())
			{
				target = vehicle;
				if (target != null)
				{
					lockOnAction.Start();
				}
				else
				{
					lockOnAction.Stop();
				}
			}
		}
		else
		{
			target = null;
			lockOnAction.Stop();
		}
		if (user != null && !user.aiControlled)
		{
			if (IsLocking())
			{
				lockImage.enabled = true;
				lockImage.rectTransform.position = Camera.main.WorldToScreenPoint(target.transform.position);
			}
			else
			{
				lockImage.enabled = false;
			}
			if (HasLock())
			{
				lockImage.texture = lockedTexture;
			}
			else
			{
				lockImage.texture = lockingTexture;
			}
		}
	}

	public override void Fire(Vector3 direction, bool useMuzzleDirection)
	{
		base.Fire(direction, useMuzzleDirection);
		Reload();
	}

	public override bool CanFire()
	{
		Debug.Log("Has lock? " + HasLock());
		Debug.Log("Reloading? " + !reloading);
		Debug.Log("HasLoadedAmmo? " + HasLoadedAmmo());
		Debug.Log("not holding fire?" + !holdingFire);
		Debug.Log("not on cooldown? " + !CoolingDown());
		return base.CanFire() && HasLock();
	}

	private bool IsLocking()
	{
		return target != null;
	}

	private bool HasLock()
	{
		return target != null && lockOnAction.Done();
	}

	private Vehicle FindTarget()
	{
		List<Vehicle> sortedTargets = GetSortedTargets();
		foreach (Vehicle item in sortedTargets)
		{
			Vector3 direction = item.transform.position - base.transform.position;
			if (Vector3.Dot(direction.normalized, configuration.muzzle.forward) > 0.95f)
			{
				Ray ray = new Ray(MuzzlePosition(), direction);
				if (!Physics.Raycast(ray, direction.magnitude, 1))
				{
					return item;
				}
			}
		}
		return null;
	}

	private List<Vehicle> GetSortedTargets()
	{
		List<Vehicle> list = new List<Vehicle>(ActorManager.instance.vehicles);
		if (user.IsSeated())
		{
			list.Remove(user.seat.vehicle);
		}
		Dictionary<Vehicle, bool> isEnemy = new Dictionary<Vehicle, bool>();
		foreach (Vehicle item in list)
		{
			if (item.HasDriver())
			{
				isEnemy.Add(item, item.Driver().team != user.team);
			}
			else
			{
				isEnemy.Add(item, false);
			}
		}
		list.Sort((Vehicle x, Vehicle y) => (isEnemy[x] != isEnemy[y]) ? isEnemy[y].CompareTo(isEnemy[x]) : Vector3.Distance(base.transform.position, x.transform.position).CompareTo(Vector3.Distance(base.transform.position, y.transform.position)));
		return list;
	}
}
