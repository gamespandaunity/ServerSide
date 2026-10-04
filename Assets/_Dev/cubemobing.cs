using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cubemobing : MonoBehaviour
{
    public List<Transform> objectsToCollect;
    public float moveSpeed = 5f;
    public float pickDistance = 1f;

    private Vector3 startPosition;
    private int currentIndex = 0;
    private bool isMoving = false;
    private bool hasObject = false;
    private Transform pickedObject;
    private Vector3 objectStartPosition;

    void Start()
    {
        startPosition = transform.position;
        if (objectsToCollect.Count > 0)
        {
            StartCoroutine(MoveToNextObject());
        }
    }

    IEnumerator MoveToNextObject()
    {
        while (currentIndex < objectsToCollect.Count)
        {
            Transform targetObject = objectsToCollect[currentIndex];
            objectStartPosition = targetObject.position;

            isMoving = true;
            while (Vector3.Distance(transform.position, targetObject.position) > pickDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetObject.position, moveSpeed * Time.deltaTime);
                yield return null;
            }

            pickedObject = targetObject;
            pickedObject.SetParent(transform);
            hasObject = true;

            while (Vector3.Distance(transform.position, startPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, startPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }

            pickedObject.SetParent(null);
            pickedObject.position = startPosition;
            hasObject = false;
            pickedObject = null;

            currentIndex++;
            yield return new WaitForSeconds(0.5f);
        }

        isMoving = false;
        Debug.Log("Finished collecting all objects!");
    }
}
