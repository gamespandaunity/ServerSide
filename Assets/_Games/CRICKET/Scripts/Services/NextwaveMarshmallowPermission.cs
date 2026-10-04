using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using Cricket;
public class NextwaveMarshmallowPermission : MonoBehaviour
{
    public static NextwaveMarshmallowPermission instance;

    private int INITIAL_PERMISSIONS_REQUEST_CODE = 100;

    private int SINGLE_PERMISSIONS_REQUEST_CODE = 200;

    public AndroidJavaObject objNextwavePermission;

    public AndroidJavaObject objNative;

    public AndroidJavaObject playerActivityContext;

   
    private void Awake()
    {
        Object.DontDestroyOnLoad(base.gameObject);
        instance = this;
    }

    private void Start()
    {
        initTheNativeAndroid();
    }

    public void initTheNativeAndroid()
    {
        //if (!isMarshMallow())
        //{
        moveToNextScene();


    }

    public void moveToNextScene()
    {
        if (Singleton<LoadingScreen>.instance != null)
        {
            Singleton<LoadingScreen>.instance.LoadingBarAnim();
        }
    }

}
