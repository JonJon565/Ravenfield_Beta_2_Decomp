using UnityEngine;

public class Rocket : ExplodingProjectile
{
	protected Light light;

	protected virtual void Awake()
	{
		light = GetComponent<Light>();
	}

	protected override void Start()
	{
		base.Start();
		ParticleSystem[] array = trailParticles;
		foreach (ParticleSystem particleSystem in array)
		{
			particleSystem.Play(false);
		}
	}

	protected override void Hit(Ray ray, RaycastHit hitInfo)
	{
		light.enabled = false;
		base.Hit(ray, hitInfo);
	}
}
