using System;
using UnityEngine;

public class SceneryCamera : MonoBehaviour
{
	public static SceneryCamera instance;

	[NonSerialized]
	public Camera camera;

	private void Awake()
	{
		instance = this;
		camera = GetComponent<Camera>();
	}
}
