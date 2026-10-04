namespace CarRace 
{
    using Mirror;
    using UnityEngine;
public class ModeSelector : MonoBehaviour
{
    [SerializeField] RCC_SceneManager rCC_SceneManager;
    [SerializeField] GameObject[] OnlineGameObjects;
    [SerializeField] GameObject[] OfflineGameObjects;
        [SerializeField] private GameObject RaceTrack;
    void Awake()
    {
            RaceTrack.SetActive(true);
            //   PlayerSelections.GameMode = 1;
            if (NetworkServer.active || NetworkClient.active)
            {
                PlayerSelections.GameMode = 1;
            }
            if (PlayerSelections.GameMode == 0) //Offline Mode
            {
                rCC_SceneManager.registerFirstVehicleAsPlayer = true;
                rCC_SceneManager.loadCustomizationAtFirst = false;
                foreach (GameObject oN in OnlineGameObjects)
                    Destroy(oN);
                foreach (GameObject oF in OfflineGameObjects)
                    oF.SetActive(true);
            }
            else
            {
                rCC_SceneManager.registerFirstVehicleAsPlayer = false;
                rCC_SceneManager.loadCustomizationAtFirst = true;
                foreach (GameObject oF in OfflineGameObjects)
                    Destroy(oF);
                //Online Game Objects are already activated, so not needed to be activated again
            }
    }

}

}