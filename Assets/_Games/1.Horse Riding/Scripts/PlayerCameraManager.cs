using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityStandardAssets.Utility;

public class PlayerCameraManager : MonoBehaviour
{
    public static PlayerCameraManager Instance ;
    [FormerlySerializedAs("smoothFollowBackCamera")] public HorseSmoothFollow backCameraFollow;
    [FormerlySerializedAs("smoothFollowFrontCamera")] public HorseSmoothFollow frontCameraFollow;
    [FormerlySerializedAs("orbitCameraController")] public HorseCameraOrbit cameraOrbitController;

    int activeCameraIndex;
    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
    }

   // public void SetCamera(int cameraId)
    public void SwitchToCamera(int cameraId)
    {
        switch (cameraId)
        {
            case 0:
                //CAMERA_MODE.BACK
                backCameraFollow.enabled = true;
                frontCameraFollow.enabled = false;
                cameraOrbitController.enabled = false;

                break;
            case 1:
                //CAMERA_MODE.FRONT
                backCameraFollow.enabled = false;
                frontCameraFollow.enabled = true;
                cameraOrbitController.enabled = false;

                break;
            case 2:
                //CAMERA_MODE.ORBIT
                backCameraFollow.enabled = false;
                frontCameraFollow.enabled = false;
                cameraOrbitController.enabled = true;

                break;
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            activeCameraIndex++;
            activeCameraIndex %= 3;
            SwitchToCamera(activeCameraIndex);
        }
    }
   
   // public void SetTarget(PlayerPositionController player)
    public void AssignCameraTarget(PlayerPositionController player)
    {
        backCameraFollow.followTarget = player.cameraTargetBack;
        frontCameraFollow.followTarget = player.cameraTargetFront;
        cameraOrbitController.followTarget = player.cameraTargetOrbit;

    }
    
   // public void SetFrontCamera()
    public void ActivateFrontView()
    {
        DeactivateCameraFollow();
        var position = frontCameraFollow.followTarget.localPosition;
        position.z = 3;
        frontCameraFollow.followTarget.position = position;
        frontCameraFollow.enabled = true;
    }
  //  public void DisablePlayerCamera()
    public void DeactivateCameraFollow()
    {
        backCameraFollow.enabled = false;
        frontCameraFollow.enabled = false;
        cameraOrbitController.enabled = false;
    }
  
}
