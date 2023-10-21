using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
	public int owner = -1;

	public Vector3 RandomPosition()
	{
		Vector3 vector = base.transform.position + Vector3.Scale(Random.insideUnitSphere, new Vector3(3f, 0f, 3f));
		Ray ray = new Ray(vector + Vector3.up * 3f, Vector3.down);
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo))
		{
			return hitInfo.point;
		}
		return vector;
	}
}
