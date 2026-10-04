using System.Collections;
using System.Collections.Generic;
using DG.Tweening.Core;
 
using UnityEngine;

public class WinFxController : MonoBehaviour
{
    public GameObject WinFx;
    public static bool LevelFailed;
    public static bool LevelComplete;

    void OnEnable()
    {
        LevelFailed = false;
        LevelComplete = false;
    }
    
    void Start()
    {
        WinFx.SetActive(false);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        //Photon Removal  if (other.gameObject.transform.root.CompareTag("Player") && other.gameObject.transform.root.GetComponent<PhotonView>() != null && other.gameObject.transform.root.GetComponent<PhotonView>().IsMine && !LevelFailed)
        {
            LevelComplete = true;
            //Debug.Log("Level Complete");
        }
        //Photon Removal else
        {
            if (other.gameObject.transform.root.GetComponent<PlayerPowerController>().isAI && !LevelComplete)
            {
                LevelFailed = true;
                //Debug.Log("Level Failed");
            }
        }
        
        StartCoroutine(TriggerDelay(other));
    }

    private IEnumerator TriggerDelay(Collider other)
    {
        yield return new WaitForSeconds(0.5f);
        //Photon Removal if (other.gameObject.transform.root.CompareTag("Player") && other.gameObject.transform.root.GetComponent<PhotonView>() != null && other.gameObject.transform.root.GetComponent<PhotonView>().IsMine)
        {
            StartCoroutine(FreezeAll(other.gameObject.transform.root.GetComponent<Rigidbody>()));
            StartCoroutine(ShowWinFx());
        }
    }

    private IEnumerator ShowWinFx()
    {
        // set front camera
        PlayerCameraManager.Instance.SwitchToCamera(1);
        yield return new WaitForSeconds(0.1f);
        HorseMobileButton.Instance.DisableControls();
        yield return new WaitForSeconds(0.1f);
        WinFx.SetActive(true);
        PlayerCameraManager.Instance.DeactivateCameraFollow();
    }

    private IEnumerator FreezeAll(Rigidbody body)
    {
        HorseMobileButton.Instance.DeactivateNitro();
        yield return new WaitForSeconds(1f);
        body.constraints = RigidbodyConstraints.FreezeAll;
    }
}
