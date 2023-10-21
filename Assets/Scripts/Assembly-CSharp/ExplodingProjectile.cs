using System;
using UnityEngine;

public class ExplodingProjectile : Projectile
{
	[Serializable]
	public class ExplosionConfiguration
	{
		public float damage = 300f;

		public float balanceDamage = 300f;

		public float force = 500f;

		public float damageRange = 6f;

		public AnimationCurve damageFalloff;

		public float balanceRange = 9f;

		public AnimationCurve balanceFalloff;
	}

	private const float CLEANUP_TIME = 10f;

	public ExplosionConfiguration explosionConfiguration;

	public float smokeTime = 8f;

	public Renderer[] renderers;

	public ParticleSystem[] trailParticles;

	public ParticleSystem[] impactParticles;

	private void Awake()
	{
		ParticleSystem[] array = trailParticles;
		foreach (ParticleSystem particleSystem in array)
		{
			particleSystem.Play(false);
		}
	}

	protected override void Hit(Ray ray, RaycastHit hitInfo)
	{
		base.transform.position = hitInfo.point;
		bool flag = false;
		if (hitInfo.collider.gameObject.layer == 12)
		{
			Vehicle componentInParent = hitInfo.collider.gameObject.GetComponentInParent<Vehicle>();
			flag = !componentInParent.dead;
			componentInParent.Damage(Damage());
		}
		if ((Explode(hitInfo.point, hitInfo.normal) || flag) && !source.aiControlled)
		{
			IngameUi.Hit();
		}
	}

	protected virtual bool Explode(Vector3 position, Vector3 up)
	{
		bool result = ActorManager.Explode(position, explosionConfiguration);
		base.transform.rotation = Quaternion.LookRotation(up);
		base.enabled = false;
		Renderer[] array = renderers;
		foreach (Renderer renderer in array)
		{
			renderer.enabled = false;
		}
		ParticleSystem[] array2 = trailParticles;
		foreach (ParticleSystem particleSystem in array2)
		{
			particleSystem.Stop();
		}
		ParticleSystem[] array3 = impactParticles;
		foreach (ParticleSystem particleSystem2 in array3)
		{
			particleSystem2.Play();
		}
		AudioSource component = GetComponent<AudioSource>();
		component.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
		component.Play();
		Invoke("StopSmoke", smokeTime);
		return result;
	}

	private void StopSmoke()
	{
		ParticleSystem[] array = impactParticles;
		foreach (ParticleSystem particleSystem in array)
		{
			particleSystem.Stop();
		}
		Invoke("Cleanup", 10f);
	}

	private void Cleanup()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}
}
