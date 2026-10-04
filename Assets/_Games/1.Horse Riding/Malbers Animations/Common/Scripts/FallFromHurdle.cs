using MalbersAnimations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallFromHurdle : MonoBehaviour
{
    //public Animal animal;
    //private void OnCollisionEnter(Collision other)
    //{
    //    if (other.transform.CompareTag("HurdleFall"))
    //    {
    //        if (animal != null && !transform.GetComponent<PlayerPositionController>().isAI && animal.AnimState != AnimTag.Locomotion)
    //        {
    //            //Debug.Log("collider sy collide hoa ha ");
    //            animal.SetAnimatorFallFromHurdle();
    //        }
    //    }
    //}
    //private void OnCollisionExit(Collision collision)
    //{
        
    //}
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Time.timeScale = 0.1f;
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            Time.timeScale = 1f;
        }
    }
    //public float jumpHeight = 5f;
    //public float jumpDistance = 2f;
    //public float raycastDistance = 5f;
    //private bool isActive = true;
    //void Update()
    //{

    //        if (Input.GetKeyDown(KeyCode.T))
    //        {
    //            Time.timeScale = 0.1f;
    //        }

    //        if (Input.GetKeyDown(KeyCode.Y))
    //        {
    //            Time.timeScale = 1f;
    //        }

    //    if (/*animal.AnimState == AnimTag.Jump &&*/ isActive)
    //    {
    //        RaycastHit hit;
    //        Vector3 forward = transform.TransformDirection(Vector3.forward) * raycastDistance;
    //        Debug.DrawRay(transform.position, forward, Color.green);
    //        if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance))
    //        {
    //            if (hit.collider.CompareTag("HurdleFall"))
    //            {
    //                isActive = false;
    //                Vector3 hurdlePosition = hit.collider.transform.position;
    //                Vector3 horsePosition = transform.position;

    //                float distanceToHurdle = Vector3.Distance(horsePosition, hurdlePosition);

    //                if (CanJumpOverHurdle(horsePosition, hurdlePosition, distanceToHurdle))
    //                {
    //                    //Debug.Log("Horse jump kr skta .");
    //                    Invoke("boolFalse", 5f);
    //                }
    //                else
    //                {
    //                    //Debug.Log("Horse jump ni kr skta ");

    //                    animal.SetAnimatorFallFromHurdle();
    //                    Invoke("boolFalse", 5f);
    //                }
    //            }
    //        }
    //    }
    //}

    //bool CanJumpOverHurdle(Vector3 horsePosition, Vector3 hurdlePosition, float distanceToHurdle)
    //{

    //    //0.2 , 2
    //    float requiredJumpHeight = hurdlePosition.y - horsePosition.y;
    //    float requiredJumpDistance = distanceToHurdle;
    //    //Debug.Log("Hurdle Position is " + hurdlePosition);
    //    //Debug.Log("requiredJumpHeight::" + requiredJumpHeight);
    //    //Debug.Log("distanceToHurdle::" + distanceToHurdle);
    //    if (requiredJumpHeight <= jumpHeight && requiredJumpDistance <= jumpDistance)
    //    {
    //        return true;
    //    }

    //    return false;
    //}
    //void boolFalse()
    //{
    //    isActive = true;
    //}
}
