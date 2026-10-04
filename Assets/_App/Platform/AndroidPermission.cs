using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class AndroidPermission : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            askRequst();
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
        }

    }


    void askRequst()//call this function to ask request
    {
        if (Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {

        }
        else
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
            callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
            callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS", callbacks);
        }

    }
    internal void PermissionCallbacks_PermissionDeniedAndDontAskAgain(string permissionName)
    {
        //Debug.Log($"{permissionName} PermissionDeniedAndDontAskAgain");
    }

    internal void PermissionCallbacks_PermissionGranted(string permissionName)
    {
        //Debug.Log($"{permissionName} PermissionCallbacks_PermissionGranted");
    }

    internal void PermissionCallbacks_PermissionDenied(string permissionName)
    {
        //Debug.Log($"{permissionName} PermissionCallbacks_PermissionDenied");
    }
}
