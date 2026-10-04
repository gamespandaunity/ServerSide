using UnityEngine.Serialization;

namespace Cricket 
{
using UnityEngine;
using UnityEngine.UI;

public class PreloaderScreen : Singleton<PreloaderScreen>
{
	[FormerlySerializedAs("LoadingTxt")] public Text loadingText;

	[FormerlySerializedAs("RotateGo")] public GameObject rotatingGameObject;

	private float rotationSpeedInDegrees = 150f;

	protected void Awake()
	{
		CONTROLLER.CurrentMenu = string.Empty;
	}

	public void UpdateLoadingTxt(string str)
	{
		loadingText.text = str;
	}

	protected void Update()
	{
		if (rotatingGameObject != null)
		{
			rotatingGameObject.transform.localEulerAngles = new Vector3(rotatingGameObject.transform.localEulerAngles.x, rotatingGameObject.transform.localEulerAngles.y, rotatingGameObject.transform.localEulerAngles.z - rotationSpeedInDegrees * Time.deltaTime);
		}
	}
}

}