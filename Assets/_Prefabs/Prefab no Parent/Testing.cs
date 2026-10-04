using UnityEngine;
using DG.Tweening;

public class Testing : MonoBehaviour
{
    public Transform endV;
    public float jumpP;
    public int numJump;
    public float duration;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.DOJump(endV.position, jumpP, numJump, duration);
    }
}
