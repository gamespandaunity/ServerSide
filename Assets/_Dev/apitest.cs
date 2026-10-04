using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class apitest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine( ServerConnection.GetApiRequest("https://api.freebackgroundremover.net/bets/winner/fe644f5/user/5"));
       StartCoroutine(ServerConnection.GetApiRequest("https://api.freebackgroundremover.net/bets/winner/fe644f5/user/5"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
