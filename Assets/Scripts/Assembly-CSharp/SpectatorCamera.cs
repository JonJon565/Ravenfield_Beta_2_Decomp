using UnityEngine;

public class SpectatorCamera : MonoBehaviour
{
	public static SpectatorCamera instance;

	private void Awake()
	{
		instance = this;
	}

	private void Update()
	{
		float num = 20f;
		if (Input.GetKey(KeyCode.LeftShift))
		{
			num = 80f;
		}
		base.transform.position += (base.transform.forward * Input.GetAxis("Vertical") + base.transform.right * Input.GetAxis("Horizontal")) * num * Time.deltaTime;
		Vector3 eulerAngles = base.transform.eulerAngles;
		eulerAngles += new Vector3(0f - Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0f) * 3f;
		base.transform.eulerAngles = eulerAngles;
		if (Input.GetKeyDown(KeyCode.L))
		{
			ScreenCapture.CaptureScreenshot("screenshot.png", 3);
		}
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			if (Time.timeScale < 1f)
			{
				Time.timeScale = 1f;
			}
			else
			{
				Time.timeScale = 0.1f;
			}
		}
	}
}
