using UnityEngine;

public class ballBLickPositionInCylinder : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Transform ballBlick;
    
    float radius;
    
    void Start()

    {
        radius  = GetComponent<SphereCollider>().radius;

        //ballBlick = Transform.Instantiate(ballBlick) as Transform;
        ballBlick.transform.parent = null;
        Invoke(nameof(RemoveObjectWhenStopMoving), 10f);

    }

    // Update is called once per frame
    void Update()
    {
        ballBlick.position = new Vector3(transform.position.x, 1f * radius, transform.position.z);
    }
    private void RemoveObjectWhenStopMoving()
    {
       ballBlick.gameObject.SetActive(false);
    }
     
}
