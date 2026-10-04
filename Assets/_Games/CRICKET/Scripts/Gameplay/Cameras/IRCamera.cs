using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class IRCamera : Singleton<IRCamera>
{
	[FormerlySerializedAs("IRMat")] public Material infraredMaterial;

	[FormerlySerializedAs("intensity")] [Range(0f, 1f)]
	public float intensityLevel;

	[FormerlySerializedAs("_realTime")] [SerializeField]
	private bool isRealTime;

	[FormerlySerializedAs("_excludeLayers")] [SerializeField]
	private LayerMask excludedLayers;

	private Camera mainCamera;

	private Camera temporaryCamera;

	private GameObject temporaryObject;

	public void OnEnable()
	{
		Singleton<GroundController>.instance.SaveJerseyColor();
	}

	public void OnDisable()
	{
		Singleton<GroundController>.instance.RevertJerseyColor();
	}

	private void Start()
	{
		if (!isRealTime)
		{
			infraredMaterial.SetFloat(Shader.PropertyToID("_Intensity"), intensityLevel);
		}
	}

	private void OnRenderImage(RenderTexture _source, RenderTexture _destination)
	{
		if (infraredMaterial != null)
		{
			if (isRealTime)
			{
				infraredMaterial.SetFloat(Shader.PropertyToID("_Intensity"), intensityLevel);
			}
			Graphics.Blit(_source, _destination, infraredMaterial);
		}
		else
		{
			Graphics.Blit(_source, _destination);
			ConstantsData_M.Log("IR Material not assigned. Disabling IR effect");
			base.enabled = false;
		}
	}

	public void ChangeColor()
	{
	}

	public void RevertColor()
	{
	}
}
