using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
	private const string NEWS_URL = "https://docs.google.com/document/export?format=txt&id=12lhDOtsf3FUBGm-UGuNdVV1_3qju_ODtQvuuS1f7xp0";

	public static bool displayedNews;

	public GameObject menuContent;

	public GameObject newsContent;

	public Text newsText;

	public Toggle assaultModeToggle;

	public Toggle reverseModeToggle;

	public InputField victoryScoreInput;

	public InputField numberActorsInput;

	public InputField respawnTimeInput;

	private void Start()
	{
		menuContent.SetActive(false);
		newsContent.SetActive(true);
		if (displayedNews)
		{
			ShowMenu();
			return;
		}
		StartCoroutine(LoadNews());
		displayedNews = true;
	}

	public void StartLevel(string levelName)
	{
		SaveGameSettings();
		Application.LoadLevel(levelName);
	}

	private void SaveGameSettings()
	{
		GameManager.instance.assaultMode = assaultModeToggle.isOn;
		GameManager.instance.reverseMode = reverseModeToggle.isOn;
		int result;
		if (int.TryParse(victoryScoreInput.text, out result))
		{
			GameManager.instance.victoryPoints = result;
		}
		int result2;
		if (int.TryParse(numberActorsInput.text, out result2))
		{
			ActorManager.instance.maxActors = result2;
		}
		int result3;
		if (int.TryParse(respawnTimeInput.text, out result3))
		{
			ActorManager.instance.spawnTime = result3;
		}
	}

	public void OpenOptions()
	{
		OptionsUi.Show();
	}

	public void ShowMenu()
	{
		newsContent.SetActive(false);
		menuContent.SetActive(true);
	}

	private IEnumerator LoadNews()
	{
		WWW newsRequest = new WWW("https://docs.google.com/document/export?format=txt&id=12lhDOtsf3FUBGm-UGuNdVV1_3qju_ODtQvuuS1f7xp0");
		yield return newsRequest;
		if (newsRequest.error == null)
		{
			ParseNews(newsRequest.text);
		}
		else
		{
			WriteNews("Could not load news: \n\r" + newsRequest.error);
		}
	}

	private void ParseNews(string html)
	{
		try
		{
			WriteNews(html);
		}
		catch (Exception ex)
		{
			WriteNews("Could not load news: \n\r" + ex.Message);
		}
	}

	private void WriteNews(string text)
	{
		newsText.text = "\n\r" + text + "\n\r";
		newsText.rectTransform.anchoredPosition = newsText.rectTransform.anchoredPosition + Vector2.right * 3f;
	}

	public void OpenTwitter()
	{
		Application.OpenURL("http://twitter.com/SteelRaven7");
	}
}
