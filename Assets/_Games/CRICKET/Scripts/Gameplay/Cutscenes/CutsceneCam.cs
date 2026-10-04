using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(Animation))]
public class CutsceneCam : MonoBehaviour
{
	[FormerlySerializedAs("_cam")] [SerializeField]
	private Camera gameCamera;

	[FormerlySerializedAs("isPlaying")] public bool isGamePlaying;

	private void Start()
	{
		gameCamera = GetComponent<Camera>();
		Cutscenes.instance.cutSceneCam = gameCamera;
	}

	public void AnimationStarted()
	{
		gameCamera.enabled = true;
		isGamePlaying = true;
	}

	public void AnimationEnded()
	{
		Cutscenes.instance.EndCutscene();
		isGamePlaying = false;
	}
}
