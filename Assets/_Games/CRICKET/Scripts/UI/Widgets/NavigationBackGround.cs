
using Cricket;
using UnityEngine.Serialization;

public class NavigationBackGround : Singleton<NavigationBackGround>
{
	private bool isEscapeKeyPressedOnGround;

	[FormerlySerializedAs("disableDeviceBack")] public bool isDeviceBackDisabled;
}
