using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class toggleOnEnabled : MonoBehaviour
{
	public GameObject targetGameObject;

	public string targetScript;

	public string targetVariable;

	private Image graphic;

	private void Awake()
	{
		graphic = base.transform.Find("Background").GetComponent<Image>();
		GetComponent<Toggle>().onValueChanged.AddListener(delegate(bool value)
		{
			OnValueChanged(value);
		});
	}

	private void OnEnable()
	{
		Type type = targetGameObject.GetComponent(targetScript).GetType();
		FieldInfo field = type.GetField(targetVariable);
		GetComponent<Toggle>().isOn = (bool)field.GetValue(targetGameObject.GetComponent(targetScript));
	}

	private void OnValueChanged(bool value)
	{
		if (graphic != null)
		{
			graphic.enabled = !value;
		}
	}
}
