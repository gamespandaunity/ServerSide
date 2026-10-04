using UnityEngine;

public class Cue3DSet : MonoBehaviour
{
    public void Set()
    {
        GameObject cue3D = GameObject.Find("Cue3D");
        if(cue3D)
        {
            name = "Cue3DObject";
            transform.parent = cue3D.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
