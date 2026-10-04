using UnityEngine;

public static class ExtensionMethods
{
	public static void SetPositionX(this Transform transform, float x)
	{
		transform.position = new Vector3(x, transform.position.y, transform.position.z);
	}

	public static void SetPositionY(this Transform transform, float y)
	{
		transform.position = new Vector3(transform.position.x, y, transform.position.z);
	}

	public static void SetPositionZ(this Transform transform, float z)
	{
		transform.position = new Vector3(transform.position.x, transform.position.y, z);
	}

	public static void AddPosition(this Transform transform, Vector3 offset)
	{
		transform.position += offset;
	}

	public static void AddPosition(this Transform transform, float x, float y, float z)
	{
		transform.position += new Vector3(x, y, z);
	}

	public static void SetLocalPositionX(this Transform transform, float x)
	{
		transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
	}

	public static void SetLocalPositionY(this Transform transform, float y)
	{
		transform.localPosition = new Vector3(transform.localPosition.x, y, transform.localPosition.z);
	}

	public static void SetLocalPositionZ(this Transform transform, float z)
	{
		transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, z);
	}

	public static void SetEulerAnglesX(this Transform transform, float x)
	{
		transform.eulerAngles = new Vector3(x, transform.eulerAngles.y, transform.eulerAngles.z);
	}

	public static void SetEulerAnglesY(this Transform transform, float y)
	{
		transform.eulerAngles = new Vector3(transform.eulerAngles.x, y, transform.eulerAngles.z);
	}

	public static void SetEulerAnglesZ(this Transform transform, float z)
	{
		transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, z);
	}

	public static void SetLocalEulerAnglesX(this Transform transform, float x)
	{
		transform.localEulerAngles = new Vector3(x, transform.localEulerAngles.y, transform.localEulerAngles.z);
	}

	public static void SetLocalEulerAnglesY(this Transform transform, float y)
	{
		transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, y, transform.localEulerAngles.z);
	}

	public static void SetLocalEulerAnglesZ(this Transform transform, float z)
	{
		transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, z);
	}

	public static void SetRotationX(this Transform transform, float x)
	{
		transform.rotation *= Quaternion.Euler(x, transform.rotation.y, transform.rotation.z);
	}

	public static void SetRotationY(this Transform transform, float y)
	{
		transform.rotation *= Quaternion.Euler(transform.rotation.x, y, transform.rotation.z);
	}

	public static void SetRotationZ(this Transform transform, float z)
	{
		transform.rotation *= Quaternion.Euler(transform.rotation.x, transform.rotation.y, z);
	}

	public static void AddRotation(this Transform transform, Vector3 offset)
	{
		transform.rotation *= Quaternion.Euler(offset);
	}

	public static void AddRotation(this Transform transform, float x, float y, float z)
	{
		transform.rotation *= Quaternion.Euler(new Vector3(x, y, z));
	}

	public static void SetScaleX(this Transform transform, float x)
	{
		transform.localScale = new Vector3(x, transform.localScale.y, transform.localScale.z);
	}

	public static void SetScaleY(this Transform transform, float y)
	{
		transform.localScale = new Vector3(transform.localScale.x, y, transform.localScale.z);
	}

	public static void SetScaleZ(this Transform transform, float z)
	{
		transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, z);
	}

	public static void AddScale(this Transform transform, Vector3 offset)
	{
		transform.localScale += offset;
	}

	public static void AddScale(this Transform transform, float x, float y, float z)
	{
		transform.localScale += new Vector3(x, y, z);
	}

	public static void SetPositionX(this Rigidbody rigidbody, float x)
	{
		rigidbody.position = new Vector3(x, rigidbody.position.y, rigidbody.position.z);
	}

	public static void SetPositionY(this Rigidbody rigidbody, float y)
	{
		rigidbody.position = new Vector3(rigidbody.position.x, y, rigidbody.position.z);
	}

	public static void SetPositionZ(this Rigidbody rigidbody, float z)
	{
		rigidbody.position = new Vector3(rigidbody.position.x, rigidbody.position.y, z);
	}

	public static void SetAnchoredPositionX(this RectTransform transform, float x)
	{
		transform.anchoredPosition = new Vector2(x, transform.anchoredPosition.y);
	}

	public static void SetAnchoredPositionY(this RectTransform transform, float y)
	{
		transform.anchoredPosition = new Vector2(transform.anchoredPosition.x, y);
	}

	public static void SetSizeDeltaX(this RectTransform transform, float x)
	{
		transform.sizeDelta = new Vector2(x, transform.sizeDelta.y);
	}

	public static void SetSizeDeltaY(this RectTransform transform, float y)
	{
		transform.sizeDelta = new Vector2(transform.sizeDelta.x, y);
	}
}
