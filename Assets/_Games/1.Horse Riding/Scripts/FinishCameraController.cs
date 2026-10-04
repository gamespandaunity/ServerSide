using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class FinishCameraController : MonoBehaviour
{
    [FormerlySerializedAs("CamerasList")] public List<GameObject> FinishCameras;

    int currentIndex;
    //public SmoothLookAt smoothLook;
    private void OnEnable()
    {
        DemoGameManagers.Instance.HorsePhotonDemoController.thirdPersonCameraFollow.gameObject.SetActive(false);
        Time.timeScale = 0.5f;
        ActiveNextCamera();
        Invoke("FreezVehicle", 1.5f);
    }
    void ActiveNextCamera()
    {
        if (currentIndex >= FinishCameras.Count)
        {
            return;
        }
        for (int i = 0; i < FinishCameras.Count; i++)
        {
            if (currentIndex == i)
            {
                FinishCameras[i].SetActive(true);
            }
            else
            {
                FinishCameras[i].SetActive(false);

            }
        }
        currentIndex++;
        Invoke("ActiveNextCamera", 0.5f);

    }
    void FreezVehicle()
    {
        if (LevelsManager.instance.finishLineTarget .transform.root.GetComponent<Rigidbody>())
        {
            LevelsManager.instance.finishLineTarget .transform.root.GetComponent<Rigidbody>().isKinematic = true;
        }
        Time.timeScale = 1f;

    }

    //void OnTriggerEnter(Collider other)
    //{

    //    if (other.gameObject.CompareTag("PlayerCollider") && MConstants.isRaceOver)
    //    {
    //        gameObject.SetActive(false);
    //        nextCamera.SetActive(true);

    //    }
    //}
}
