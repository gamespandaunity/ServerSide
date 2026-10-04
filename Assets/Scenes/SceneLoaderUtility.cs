using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoaderUtility
{
    public static void LoadScene(string sceneName)
    {
        // Get the stack trace to determine the calling method and class
       // //Debug.Log($"SceneLoaderUtility.LoadScene called from:here :" + sceneName);
        SceneManager.LoadScene(sceneName);
    }  
    public static void LoadScene(int sceneName)
    {
     //   //Debug.Log($"SceneLoaderUtility.LoadScene called from:here :" + sceneName);
        SceneManager.LoadScene(sceneName);
    } 
    public static void LoadScene(string sceneName,LoadSceneMode mode)
    {
        // Get the stack trace to determine the calling method and class
     //   //Debug.Log($"SceneLoaderUtility.LoadScene called from:here :"+sceneName);
        SceneManager.LoadScene(sceneName,mode);
    }
}
