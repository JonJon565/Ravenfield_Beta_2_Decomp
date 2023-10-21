using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
	public const float SPAWN_TIME = 16f;

	public GameObject prefab;

	private void Awake()
	{
		GetComponent<Renderer>().enabled = false;
	}

	private void Start()
	{
		SpawnVehicle();
	}

	private void SpawnVehicle()
	{
		Vehicle component = ((GameObject)Object.Instantiate(prefab, base.transform.position, base.transform.rotation)).GetComponent<Vehicle>();
		component.SetSpawner(this);
	}

	public void VehicleDied()
	{
		Invoke("SpawnVehicle", 16f);
	}
}
