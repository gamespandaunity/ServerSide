using UnityEngine;

public class HandPositionSetter : MonoBehaviour
{
    public Transform cueballPosition;
    private void OnEnable()
    {
        transform.position = new Vector3(cueballPosition.position.x,transform.position.y, cueballPosition.position.z);
    }
}
