using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Characters.FirstPerson;

public class IngameMenuUi : MonoBehaviour
{
	public static IngameMenuUi instance;

	private Canvas canvas;

	public static void Show()
	{
		instance.canvas.enabled = true;
		MouseLook.paused = true;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		Time.timeScale = 0f;
	}

	public static void Hide()
	{
		instance.canvas.enabled = false;
		MouseLook.paused = false;
		Time.timeScale = 1f;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	public static bool IsOpen()
	{
		return instance.canvas.enabled;
	}

	private void Awake()
	{
		instance = this;
		canvas = GetComponent<Canvas>();
		canvas.enabled = false;
		Hide();
	}

	public void Resume()
	{
		Hide();
	}

	public void Options()
	{
		OptionsUi.Show();
	}

	public void Menu()
	{
		MouseLook.paused = false;
		SceneManager.LoadScene(1);
	}

	public void Quit()
	{
		Application.Quit();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if (canvas.enabled)
			{
				Hide();
			}
			else
			{
				Show();
			}
		}
	}
}
