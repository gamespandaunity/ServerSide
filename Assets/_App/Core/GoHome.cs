using UnityEngine;
using UnityEngine.SceneManagement;
public class GoHome : MonoBehaviour
{
    public void goHome()
    {
        SceneLoaderUtility.LoadScene("MainmenuScene");
    }
}
