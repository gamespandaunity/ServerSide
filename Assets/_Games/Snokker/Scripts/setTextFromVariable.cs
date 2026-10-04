using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class setTextFromVariable : MonoBehaviour
{
	public GameObject targetGameObject;

	public string targetScript;

	public string targetVariable;

	private void OnEnable()
	{
		Type type = targetGameObject.GetComponent(targetScript).GetType();
		FieldInfo field = type.GetField(targetVariable);
		GetComponent<Text>().text = string.Concat(field.GetValue(targetGameObject.GetComponent(targetScript)), string.Empty);
	}
}
