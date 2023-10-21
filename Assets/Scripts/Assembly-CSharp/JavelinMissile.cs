using System;
using UnityEngine;

public class JavelinMissile : Rocket
{
	private const float TURN_SPEED = 300f;

	private const float TARGET_ALTITUDE = 200f;

	private const float ACCELERATION = 80f;

	private const float DIVE_DISTANCE = 50f;

	public float ejectSpeed = 10f;

	[NonSerialized]
	public Vector3 targetPoint;

	[NonSerialized]
	public Transform target;

	private bool diving;

	private bool thrustEnabled;

	private Action thrustStartAction = new Action(0.5f);

	protected override void Start()
	{
		base.Start();
		velocity = base.transform.forward * ejectSpeed + source.Velocity() * 0.9f;
		thrustStartAction.Start();
		light.enabled = false;
		ParticleSystem[] array = trailParticles;
		foreach (ParticleSystem particleSystem in array)
		{
			particleSystem.Stop();
		}
	}

	protected override void Update()
	{
		if (thrustStartAction.TrueDone())
		{
			if (!thrustEnabled)
			{
				light.enabled = true;
				Debug.Log(trailParticles.Length);
				ParticleSystem[] array = trailParticles;
				foreach (ParticleSystem particleSystem in array)
				{
					particleSystem.Play(false);
				}
				thrustEnabled = true;
			}
			Vector3 vector = ((!(target == null)) ? target.position : targetPoint);
			Vector3 vector2 = vector - base.transform.position;
			Vector3 vector3;
			if (!diving)
			{
				vector2.y = 0f;
				float value = 200f - base.transform.position.y;
				vector3 = (vector2.normalized + Vector3.up * Mathf.Clamp(value, 0f, 1.5f)).normalized * configuration.speed;
				diving = vector2.magnitude < 50f;
			}
			else
			{
				vector3 = vector2.normalized * configuration.speed;
			}
			velocity = Vector3.MoveTowards(velocity, vector3, 80f * Time.deltaTime);
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, Quaternion.LookRotation(velocity), 300f * Time.deltaTime);
		}
		base.Update();
	}

	public void ForceDirectMode()
	{
		diving = true;
	}
}
