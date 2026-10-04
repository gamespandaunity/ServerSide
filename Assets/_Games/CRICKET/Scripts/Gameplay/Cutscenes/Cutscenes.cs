using UnityEngine;
using Cricket;
public class Cutscenes : CutsceneManager
{
	public static Cutscenes instance;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		if (instance != this)
		{
			Object.DestroyImmediate(base.gameObject);
		}
	}

	private void onIntroEnded()
	{
		if (CurrentCutsceneName == "Intro" && Singleton<GameData>.instance != null)
		{
			Singleton<GameData>.instance.introCompleted();
		}
	}
}
