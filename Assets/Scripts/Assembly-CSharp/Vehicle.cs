using System;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
	private const float EXPLODE_TIME = 0.3f;

	private const float CLEANUP_TIME = 15f;

	private const float CRASH_SPEED_THRESHOLD = 2f;

	public const int LAYER = 12;

	private const float AUTO_DAMAGE_START_TIME = 20f;

	private const float AUTO_DAMAGE_PERIOD = 2f;

	private const float AUTO_DAMAGE_PERCENT = 0.05f;

	private const float RAM_MIN_SPEED = 3f;

	public Actor.TargetType targetType = Actor.TargetType.Unarmored;

	public Seat[] seats;

	[NonSerialized]
	public bool claimedBySquad;

	[NonSerialized]
	public bool stuck;

	public float maxHealth = 1000f;

	public float crashDamageMultiplier;

	public bool directJavelinPath;

	private float health;

	[NonSerialized]
	public bool dead;

	[NonSerialized]
	public Rigidbody rigidbody;

	private VehicleSpawner spawner;

	public ParticleSystem damageParticles;

	public ParticleSystem deathParticles;

	public Transform blockSensor;

	protected AudioSource audio;

	public AudioClip explosionClip;

	public AudioSource impactAudio;

	[NonSerialized]
	public Collider[] colliders;

	private Vector3 blockSensorOrigin;

	private Action cannotRamAction = new Action(0.5f);

	private Action crashDamageCooldown = new Action(0.2f);

	public bool HasDriver()
	{
		return seats[0].IsOccupied();
	}

	public Actor Driver()
	{
		return seats[0].occupant;
	}

	protected virtual void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
		audio = GetComponent<AudioSource>();
		ActorManager.RegisterVehicle(this);
		health = maxHealth;
		colliders = GetComponentsInChildren<Collider>();
		if (HasBlockSensor())
		{
			blockSensorOrigin = blockSensor.transform.localPosition;
		}
		cannotRamAction.Start();
	}

	private void CheckRam()
	{
		Vector3 vector = rigidbody.velocity * Time.fixedDeltaTime;
		RaycastHit[] array = rigidbody.SweepTestAll(vector.normalized, vector.magnitude);
		RaycastHit[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			RaycastHit raycastHit = array2[i];
			if (raycastHit.collider.gameObject.layer == 8)
			{
				Hitbox component = raycastHit.collider.GetComponent<Hitbox>();
				if (component.RigidbodyHit(rigidbody, raycastHit.point) && HasDriver() && !Driver().aiControlled)
				{
					IngameUi.Hit();
				}
			}
		}
	}

	protected virtual void FixedUpdate()
	{
		if (rigidbody.velocity.magnitude < 3f)
		{
			cannotRamAction.Start();
		}
		if (cannotRamAction.TrueDone())
		{
			CheckRam();
		}
	}

	public void OccupantEntered(Seat seat)
	{
		if (seat == seats[0])
		{
			DriverEntered();
		}
		CancelInvoke("AutoDamage");
	}

	public void OccupantLeft(Seat seat)
	{
		if (seat == seats[0])
		{
			DriverExited();
		}
		if (IsEmpty())
		{
			InvokeRepeating("AutoDamage", 20f, 2f);
		}
	}

	private void AutoDamage()
	{
		Damage(maxHealth * 0.05f);
	}

	protected virtual void DriverEntered()
	{
	}

	protected virtual void DriverExited()
	{
	}

	public void Damage(float amount)
	{
		health -= amount;
		if (health < 0f && !dead)
		{
			Die();
		}
		if (health < 0.5f * maxHealth)
		{
			damageParticles.Play();
		}
	}

	public virtual void Die()
	{
		dead = true;
		if (spawner != null)
		{
			spawner.VehicleDied();
		}
		ActorManager.DropVehicle(this);
		Seat[] array = seats;
		foreach (Seat seat in array)
		{
			if (seat.IsOccupied())
			{
				Actor occupant = seat.occupant;
				occupant.LeaveSeat();
				if (seat.enclosed)
				{
					occupant.Damage(200f, 200f, true, base.transform.position, Vector3.forward, Vector3.up * 10f);
				}
				else
				{
					occupant.Damage(0f, 200f, true, base.transform.position, Vector3.forward, Vector3.up * 10f);
				}
			}
			seat.gameObject.SetActive(false);
		}
		rigidbody.WakeUp();
		base.enabled = false;
		Invoke("Cleanup", 15f);
		Invoke("Explode", 0.3f);
	}

	private void OnCollisionEnter(Collision c)
	{
		float num = Mathf.Abs(Vector3.Dot(c.relativeVelocity, c.contacts[0].normal));
		if (crashDamageCooldown.TrueDone() && num > 2f && c.collider.gameObject.layer != 8 && c.collider.gameObject.layer != 10)
		{
			float amount = c.relativeVelocity.magnitude * crashDamageMultiplier;
			Damage(amount);
			crashDamageCooldown.Start();
			impactAudio.transform.position = c.contacts[0].point;
			impactAudio.pitch *= UnityEngine.Random.Range(0.9f, 1.1f);
			impactAudio.Play();
		}
	}

	private void Explode()
	{
		rigidbody.WakeUp();
		rigidbody.AddForce((UnityEngine.Random.insideUnitSphere + Vector3.up) * 2000f, ForceMode.Impulse);
		rigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * 500f, ForceMode.Impulse);
		deathParticles.Play();
		audio.Stop();
		audio.pitch = 1f;
		audio.volume = 1f;
		audio.PlayOneShot(explosionClip);
	}

	private void Cleanup()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public Vector3 Velocity()
	{
		return rigidbody.velocity;
	}

	public Vector3 LocalVelocity()
	{
		return base.transform.worldToLocalMatrix.MultiplyVector(Velocity());
	}

	public void SetSpawner(VehicleSpawner spawner)
	{
		this.spawner = spawner;
	}

	protected static Vector2 Clamp2(Vector2 v)
	{
		return new Vector2(Mathf.Clamp(v.x, -1f, 1f), Mathf.Clamp(v.y, -1f, 1f));
	}

	protected static Vector4 Clamp4(Vector4 v)
	{
		return new Vector4(Mathf.Clamp(v.x, -1f, 1f), Mathf.Clamp(v.y, -1f, 1f), Mathf.Clamp(v.z, -1f, 1f), Mathf.Clamp(v.w, -1f, 1f));
	}

	public int EmptySeats()
	{
		int num = 0;
		Seat[] array = seats;
		foreach (Seat seat in array)
		{
			if (!seat.IsOccupied())
			{
				num++;
			}
		}
		return num;
	}

	public bool IsEmpty()
	{
		Seat[] array = seats;
		foreach (Seat seat in array)
		{
			if (seat.IsOccupied())
			{
				return false;
			}
		}
		return true;
	}

	public bool HasBlockSensor()
	{
		return blockSensor != null;
	}

	public int BlockTest(Collider[] outColliders, float extrapolationTime, int mask)
	{
		float num = Mathf.Max(0.1f, LocalVelocity().z * extrapolationTime);
		Vector3 v = blockSensorOrigin;
		v.z += num / 2f;
		Vector3 localScale = blockSensor.localScale;
		localScale.z = num;
		Vector3 vector = base.transform.localToWorldMatrix.MultiplyPoint(v);
		blockSensor.transform.position = vector;
		blockSensor.transform.localScale = localScale;
		return Physics.OverlapBoxNonAlloc(vector, localScale / 2f, outColliders, blockSensor.rotation, mask);
	}
}
