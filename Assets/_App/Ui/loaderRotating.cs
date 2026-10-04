using UnityEngine;

public class loaderRotating : MonoBehaviour
{

    public int rotationSpeed;

    private void OnEnable()
    {
        Invoke(nameof(close),4f);
    }
    public void close()
    {
        gameObject.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0, 0, -rotationSpeed*Time.deltaTime));
        
    }
}
