using UnityEngine;

public class shortsliderMovement : MonoBehaviour
{
    
    public Vector3 targetPosition;
    public Vector3 startingPosition;
    public float speed = 1.5f;
    //private void OnEnable()
    //{
    //    StartCoroutine(MoveObjectToTarget());
    //}
    //IEnumerator MoveObjectToTarget()
    //{
    //    float startTime = Time.time;
    //    float journeyLength = Vector3.Distance(startingPosition, targetPosition);

    
    //    while (true)
    //    {
          
    //        float distCovered = (Time.time - startTime) * speed;

    //        float fractionOfJourney = distCovered / journeyLength;

    //        transform.loa = Vector3.Lerp(startingPosition, targetPosition, fractionOfJourney);

           
    //        if (fractionOfJourney >= 1.0f)
    //        {
    //            //Debug.Log("Object has reached the target position.");
    //            break; 
    //        }
    //        yield return null;
    //    }
    //}

}
