using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayManager : MonoBehaviour
{
    public List<GameObject> ChampionShipAssets;
    public List<GameObject> BaseAssets;
    // Start is called before the first frame update
    void Start()
    {
        HideSceneAssets(BaseAssets);
        HideSceneAssets(ChampionShipAssets);
        
        if (MultiPlayerGame.isChampion)
        {
            ActivateSceneAssets(ChampionShipAssets);
        }
        else
        {
            ActivateSceneAssets(BaseAssets);
        }
    }

    private void ActivateSceneAssets(List<GameObject> Assets)
    {
        foreach (var Asset in Assets)
        {
            Asset.SetActive(true);
        }
    }
    
    private void HideSceneAssets(List<GameObject> Assets)
    {
        foreach (var Asset in Assets)
        {
            Asset.SetActive(false);
        }
    }
}
