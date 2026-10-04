using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CutsceneManager : MonoBehaviour
{
	public delegate void camEvent();

	[FormerlySerializedAs("cutsceneCam")] public Camera cutSceneCam;

	[FormerlySerializedAs("currentCutscene")] public string CurrentCutsceneName;

	[FormerlySerializedAs("cutscenes")] public List<Cutscene> AllCutscenes = new List<Cutscene>();

	private Animation cameraAnimation;

	private Cutscene activeCutscene;

	public event camEvent CutsceneEnded;

	private void Start()
	{
		for (int i = 0; i < AllCutscenes.Count; i++)
		{
			AllCutscenes[i].Init();
		}
		cameraAnimation = cutSceneCam.GetComponent<Animation>();
	}

	public void PlayCutscene(string name)
	{
		for (int i = 0; i < AllCutscenes.Count; i++)
		{
			if (AllCutscenes[i].name == name)
			{
				cutSceneCam.enabled = true;
				CurrentCutsceneName = name;
				activeCutscene = AllCutscenes[i];
				AllCutscenes[i].PlayCutscene();
				if (cameraAnimation != null)
				{
					cameraAnimation.Play(name);
				}
			}
		}
	}

	public void EndCutscene()
	{
		if (cameraAnimation != null)
		{
			cameraAnimation.Stop();
		}
		if (activeCutscene != null)
		{
			activeCutscene.Stop();
		}
		cutSceneCam.enabled = false;
		CurrentCutsceneName = string.Empty;
		if (this.CutsceneEnded != null)
		{
			this.CutsceneEnded();
		}
	}

	public void PlayCutsceneObjectAnimation(string cutsceneName, string objectName, string animationName)
	{
		for (int i = 0; i < AllCutscenes.Count; i++)
		{
			if (AllCutscenes[i].name == base.name)
			{
				AllCutscenes[i].PlayObjectAnimation(objectName, animationName);
			}
		}
	}
}
