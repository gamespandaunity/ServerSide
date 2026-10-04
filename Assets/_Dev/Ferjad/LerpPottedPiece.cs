using System.Collections;
using UnityEngine;

public class LerpPottedPiece : MonoBehaviour
{
    
    
    private float time;
    [SerializeField] private float speed;
    private Vector3 destinationPos;
    void Start()
    {
        time = 0;
        destinationPos = new Vector3(-6.5f, 2.5f , 0);
        StartCoroutine(MoveToHome());
    }
    IEnumerator MoveToHome()
    {
            float distance = Vector3.Distance(destinationPos, transform.position);
        time += speed / distance * Time.deltaTime;
        while (true)
        {
            if (Vector3.Distance(destinationPos, transform.position) > 0.3f)
            {

            transform.position = Vector3.Lerp(transform.position, destinationPos, time );
            }
            else
            {
                Destroy( gameObject );
            }

            yield return null;
            
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
