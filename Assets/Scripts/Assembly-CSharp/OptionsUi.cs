using UnityEngine;
using UnityEngine.UI;

public class OptionsUi : MonoBehaviour
{
	public class Options
	{
		public const int HELICOPTER_TYPE_BATTLEFIELD = 0;

		public const int HELICOPTER_TYPE_ARMA = 1;

		public const int HELICOPTER_TYPE_CUSTOM = 2;

		public const int DIFFICULTY_EASY = 0;

		public const int DIFFICULTY_NORMAL = 1;

		public const int DIFFICULTY_HARD = 2;

		public float mouseSensitivity;

		public float sniperMultiplier;

		public float helicopterSensitivity;

		public bool mouseInvert;

		public bool hitmarkers;

		public bool heliInvertPitch;

		public bool heliInvertYaw;

		public bool heliInvertRoll;

		public bool heliInvertThrottle;

		public bool autoReload;

		public int helicopterType;

		public int difficulty;

		public static Options Load()
		{
			Options options = new Options();
			options.mouseSensitivity = PlayerPrefs.GetFloat("mouse sensitivity", 0.5f);
			options.sniperMultiplier = PlayerPrefs.GetFloat("sniper multiplier", 0.3f);
			options.mouseInvert = PlayerPrefs.GetInt("mouse invert", 0) == 1;
			options.helicopterType = PlayerPrefs.GetInt("helicopter type", 0);
			options.helicopterSensitivity = PlayerPrefs.GetFloat("helicopter sensitivity", 0.5f);
			options.heliInvertPitch = PlayerPrefs.GetInt("helicopter invert pitch", 1) == 1;
			options.heliInvertYaw = PlayerPrefs.GetInt("helicopter invert yaw", 0) == 1;
			options.heliInvertRoll = PlayerPrefs.GetInt("helicopter invert roll", 0) == 1;
			options.heliInvertThrottle = PlayerPrefs.GetInt("helicopter invert throttle", 1) == 1;
			options.hitmarkers = PlayerPrefs.GetInt("hitmarkers2", 1) == 1;
			options.autoReload = PlayerPrefs.GetInt("auto reload", 0) == 1;
			options.difficulty = PlayerPrefs.GetInt("difficulty", 1);
			return options;
		}

		public void Save()
		{
			PlayerPrefs.SetFloat("mouse sensitivity", instance.mouseSensitivity.value);
			PlayerPrefs.SetFloat("sniper multiplier", instance.sniperMultiplier.value);
			PlayerPrefs.SetInt("mouse invert", instance.mouseInvert.isOn ? 1 : 0);
			PlayerPrefs.SetInt("helicopter type", instance.helicopterType.value);
			PlayerPrefs.SetFloat("helicopter sensitivity", instance.helicopterSensitivity.value);
			PlayerPrefs.SetInt("helicopter invert pitch", instance.heliInvertPitch.isOn ? 1 : 0);
			PlayerPrefs.SetInt("helicopter invert yaw", instance.heliInvertYaw.isOn ? 1 : 0);
			PlayerPrefs.SetInt("helicopter invert roll", instance.heliInvertRoll.isOn ? 1 : 0);
			PlayerPrefs.SetInt("helicopter invert throttle", instance.heliInvertThrottle.isOn ? 1 : 0);
			PlayerPrefs.SetInt("hitmarkers2", instance.hitmarkers.isOn ? 1 : 0);
			PlayerPrefs.SetInt("auto reload", instance.autoReload.isOn ? 1 : 0);
			PlayerPrefs.SetInt("difficulty", instance.difficulty.value);
			PlayerPrefs.Save();
		}
	}

	public static OptionsUi instance;

	private static Options options;

	private Canvas canvas;

	public RawImage hitmarkerEffect;

	private AudioSource hitmarkerAudio;

	public Slider mouseSensitivity;

	public Slider sniperMultiplier;

	public Slider helicopterSensitivity;

	public Toggle mouseInvert;

	public Toggle heliInvertPitch;

	public Toggle heliInvertYaw;

	public Toggle heliInvertRoll;

	public Toggle heliInvertThrottle;

	public Toggle hitmarkers;

	public Toggle autoReload;

	public Dropdown helicopterType;

	public Dropdown difficulty;

	public static void Show()
	{
		if (instance != null)
		{
			instance.canvas.enabled = true;
		}
	}

	public static void Hide()
	{
		if (instance != null)
		{
			instance.canvas.enabled = false;
		}
	}

	public static Options GetOptions()
	{
		if (options == null)
		{
			options = Options.Load();
		}
		return options;
	}

	private void Awake()
	{
		if (instance != null)
		{
			Object.Destroy(instance.gameObject);
		}
		instance = this;
		Object.DontDestroyOnLoad(base.gameObject);
		canvas = GetComponent<Canvas>();
		hitmarkerAudio = hitmarkerEffect.GetComponent<AudioSource>();
		Load();
		Hide();
	}

	private void Load()
	{
		Show();
		options = Options.Load();
		mouseSensitivity.value = options.mouseSensitivity;
		sniperMultiplier.value = options.sniperMultiplier;
		mouseInvert.isOn = options.mouseInvert;
		helicopterType.value = options.helicopterType;
		helicopterSensitivity.value = options.helicopterSensitivity;
		heliInvertPitch.isOn = options.heliInvertPitch;
		heliInvertYaw.isOn = options.heliInvertYaw;
		heliInvertRoll.isOn = options.heliInvertRoll;
		heliInvertThrottle.isOn = options.heliInvertThrottle;
		hitmarkers.isOn = options.hitmarkers;
		autoReload.isOn = options.autoReload;
		difficulty.value = options.difficulty;
	}

	public void Cancel()
	{
		Load();
		Hide();
	}

	public void Save()
	{
		options.Save();
		Load();
		Hide();
	}

	public void ToggleHitmarker()
	{
		if (hitmarkers.isOn)
		{
			CancelInvoke();
			Invoke("HitmarkerOff", 0.2f);
			hitmarkerEffect.enabled = true;
			hitmarkerAudio.Play();
		}
	}

	private void HitmarkerOff()
	{
		hitmarkerEffect.enabled = false;
	}
}
