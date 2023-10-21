using UnityEngine;

public class Helicopter : Vehicle
{
	private const float ROTOR_SPEED = 1000f;

	private const float ROTOR_SPEED_GAIN = 0.3f;

	private const float MAX_VOLUME = 0.5f;

	private const float BASE_DRAG = 0.04f;

	private const float ANGULAR_DRAG_ALONG_WIND_GAIN = 0.005f;

	private const float DRAG_BROADSIDE_WIND_GAIN = 0.005f;

	private const float ALONG_WIND_LIFT = 0.03f;

	public Transform rotor;

	private Renderer solidRotor;

	private Renderer blurredRotor;

	public float rotorForce = 5f;

	public float manouverability = 1f;

	public float counterForceMultiplier = 0.3f;

	private float rotorSpeed;

	protected override void Awake()
	{
		base.Awake();
		solidRotor = rotor.GetComponent<Renderer>();
		blurredRotor = rotor.GetChild(0).GetComponent<Renderer>();
		rigidbody.maxAngularVelocity = 1.5f;
	}

	private void Update()
	{
		audio.volume = rotorSpeed * 0.5f;
		audio.pitch = rotorSpeed;
		if (HasDriver())
		{
			rotorSpeed = Mathf.Clamp01(rotorSpeed + Time.deltaTime * 0.3f);
			if (base.transform.up.y < 0f)
			{
				Damage(Time.deltaTime * 30f);
			}
		}
		else
		{
			rotorSpeed = Mathf.Clamp01(rotorSpeed - Time.deltaTime * 0.3f);
		}
		bool flag = rotorSpeed > 0.8f;
		solidRotor.enabled = !flag;
		blurredRotor.enabled = flag;
		rotor.Rotate(Vector3.forward * 1000f * rotorSpeed * Time.deltaTime);
	}

	protected override void DriverEntered()
	{
		base.DriverEntered();
	}

	protected override void DriverExited()
	{
		base.DriverExited();
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		Vector3 normalized = (base.transform.forward + 0.15f * base.transform.up).normalized;
		float num = Vector3.Dot(normalized, rigidbody.velocity);
		float magnitude = Vector3.Cross(normalized, rigidbody.velocity).magnitude;
		rigidbody.angularDrag = 0.04f + num * 0.005f;
		rigidbody.drag = 0.04f + magnitude * 0.005f;
		rigidbody.AddForce(base.transform.up * num * 0.03f, ForceMode.Acceleration);
		if (HasDriver())
		{
			Vector4 vector = Vehicle.Clamp4(Driver().controller.HelicopterInput()) * rotorSpeed;
			float y = vector.y;
			Vector3 torque = new Vector3(vector.w, vector.x, 0f - vector.z) * manouverability;
			Vector3 normalized2 = (base.transform.up + base.transform.forward * 0.05f).normalized;
			Vector3 vector2 = Vector3.Project(normalized2, Vector3.up);
			normalized2 = (normalized2 - 0.05f * vector2).normalized;
			float t = Mathf.Clamp01(0f - Vector3.Dot(normalized2, rigidbody.velocity.normalized));
			float num2 = 1f + Mathf.Lerp(0f, counterForceMultiplier, t);
			rigidbody.AddForce(normalized2 * (y * rotorForce * num2 - Physics.gravity.y - 0.5f), ForceMode.Acceleration);
			rigidbody.AddRelativeTorque(torque, ForceMode.Acceleration);
		}
	}

	public override void Die()
	{
		base.Die();
		rotor.gameObject.SetActive(false);
		audio.Stop();
	}
}
