using UnityEngine;

public class BallRotateLoader : MonoBehaviour
{
    public int rotationSpeed;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0, rotationSpeed * Time.deltaTime, 0));
    }
}
