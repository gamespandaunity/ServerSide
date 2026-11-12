using UnityEngine;

/// <summary>
/// Keeps detached light visuals (beams, glows) glued to the car every frame.
/// They live outside the car hierarchy so RCC camera auto-focus (which measures all car renderers)
/// only sees the real car body.
/// </summary>
[DefaultExecutionOrder(-50)]
public class CarLightFollow : MonoBehaviour
{
    public Transform target;

    void LateUpdate()
    {
        if (target == null) { Destroy(gameObject); return; }
        transform.SetPositionAndRotation(target.position, target.rotation);
    }
}
