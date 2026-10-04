using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FalConnectionEventListener : MonoBehaviour
{
    // Start is called before the first frame update
    public void OnClickOk()
    {
        gameObject.SetActive(false);
    }
}
