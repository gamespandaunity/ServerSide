
using Mirror;
using UnityEngine;

public class holesTrigger : MonoBehaviour
{
    public mainScript mainScriptScript;

    public bool onlyForSound;

    private void Start()
    {
        if (mainScriptScript == null)
            mainScriptScript = GameObject.FindAnyObjectByType<mainScript>();
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.GetComponent<Collider>().CompareTag("ballTag"))
        {
            if (!onlyForSound)
            {
                if (SnokerNetwork.IsMultiplayer && NetworkServer.active)
                {
                    int ballnumber = int.Parse(collision.GetComponent<Collider>().name);
                    mainScriptScript.holesTriggerOnEnter(ballnumber, mainScriptScript._SnokerGameManager.GetCurrentTargetAsInt());
                }
                else if (!SnokerNetwork.IsMultiplayer)
                {
                    mainScriptScript.holesTriggerOnEnter(int.Parse(collision.GetComponent<Collider>().name), mainScriptScript._SnokerGameManager.GetCurrentTargetAsInt());
                }

            }
            else
            {
                mainScriptScript.holesSoundTriggerOnEnter();
            }
        }

    }
}
