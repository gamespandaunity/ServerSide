using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class MirrorCallbacks : NetworkBehaviour
{

    public static MirrorCallbacks Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnStopClient()
    {

        "OnStopClient Mirror Callback".Show();

    }
}
