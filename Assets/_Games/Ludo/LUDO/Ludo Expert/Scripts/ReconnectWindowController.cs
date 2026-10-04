using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LudoGame;

public class ReconnectWindowController : MonoBehaviour
{

    public GameObject window;

    void Start()
    {
        DontDestroyOnLoad(transform.gameObject);
        LudoGame.GameManager.Instance.reconnectingWindow = window;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void destroy()
    {
        if (this.gameObject != null)
            DestroyImmediate(this.gameObject);
    }
}
