using UnityEngine;

public static class Extensions
{
	public static float ProjectionScalar(this Vector3 vector, Vector3 onto)
	{
		return Vector3.Dot(vector, onto) / Vector3.Dot(vector, onto);
	}

	public static Vector3 ToGround(this Vector3 v)
	{
		v.y = 0f;
		return v;
	}

	public static Vector3 ToLocalZGround(this Vector3 v)
	{
		v.z = 0f;
		return v;
	}
}
