namespace CarRace 
{
using System.Collections;
using UnityEngine;
using Michsky.LSS;
public class SplashScreen : MonoBehaviour
{
    [SerializeField] LoadingScreenManager loadingScreenManager;
    [SerializeField] Animator loadingScreenAnimator;

    IEnumerator Start()
    {
        loadingScreenAnimator.enabled = true;
        yield return new WaitForSeconds(3.3f);
        loadingScreenManager.LoadScene("Main Menu");
    }
}

}