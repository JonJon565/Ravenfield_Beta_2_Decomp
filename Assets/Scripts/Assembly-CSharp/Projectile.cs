using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
	[Serializable]
	public class Configuration
	{
		public float speed = 300f;

		public float impactForce = 200f;

		public float lifetime = 2f;

		public float damage = 70f;

		public float balanceDamage = 60f;

		public float impactDecalSize = 0.2f;

		public bool piercing;
	}

	private const int LEVEL_LAYER = 0;

	private const int RAGDOLL_LAYER = 10;

	private const int HIT_MASK = -2049;

	private const float PIERCING_RANGE = 2f;

	public Configuration configuration;

	protected Vector3 velocity = Vector3.zero;

	protected float expireTime;

	[NonSerialized]
	public Actor source;

	protected virtual void Start()
	{
		velocity = base.transform.forward * configuration.speed;
		expireTime = Time.time + configuration.lifetime;
		ActorManager.RegisterProjectile(this);
	}

	protected virtual void Update()
	{
		if (Time.time > expireTime)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		velocity += Physics.gravity * Time.deltaTime;
		Vector3 delta = velocity * Time.deltaTime;
		Travel(delta);
	}

	protected virtual void Travel(Vector3 delta)
	{
		Ray ray = new Ray(base.transform.position, delta.normalized);
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo, delta.magnitude * 2f, -2049))
		{
			Hit(ray, hitInfo);
			if (hitInfo.collider.gameObject.layer == 0)
			{
				SpawnDecal(hitInfo);
			}
		}
		else
		{
			base.transform.position += delta;
		}
	}

	protected virtual void Hit(Ray ray, RaycastHit hitInfo)
	{
		if (hitInfo.collider.CompareTag("Piercable"))
		{
			Collider collider = hitInfo.collider;
			collider.enabled = false;
			Ray ray2 = new Ray(hitInfo.point, ray.direction);
			RaycastHit hitInfo2;
			if (Physics.Raycast(ray2, out hitInfo2, 2f, -2049))
			{
				hitInfo = hitInfo2;
			}
			collider.enabled = true;
		}
		if (hitInfo.collider.gameObject.layer == 8 || hitInfo.collider.gameObject.layer == 10)
		{
			Hitbox component = hitInfo.collider.GetComponent<Hitbox>();
			if (component.parent == source)
			{
				base.transform.position = hitInfo.point + velocity.normalized * 0.2f;
			}
			else if (component.ProjectileHit(this, hitInfo.point) && !source.aiControlled)
			{
				IngameUi.Hit();
			}
		}
		Rigidbody attachedRigidbody = hitInfo.collider.attachedRigidbody;
		if (attachedRigidbody != null)
		{
			attachedRigidbody.AddForceAtPosition(velocity.normalized * configuration.impactForce, hitInfo.point, ForceMode.Impulse);
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	protected virtual void SpawnDecal(RaycastHit hitInfo)
	{
		DecalManager.AddDecal(hitInfo.point, hitInfo.normal, configuration.impactDecalSize, DecalManager.DecalType.Impact);
	}

	public virtual float Damage()
	{
		return configuration.damage;
	}

	public virtual float BalanceDamage()
	{
		return configuration.balanceDamage;
	}
}
