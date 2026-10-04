 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DumyLobbyPlayerList : MonoBehaviour {

    public List<GameObject> DataContainer;

    public GameObject PlayerLIstObject;
    int index = 0;
    private void OnDisable()
    {

    }
    public void DrawDummyPlayerList()
    {
        MultiPlayerGame.isSinglePlayer = true;
       // MultiPlayerGame.setAIData();
        foreach (Transform child in PlayerLIstObject.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        // MConstants.NO_AI_CAR = Random.Range(2, 4);
        //populateName();
        index = 0;
        Invoke("setData", 0.25f);


    }

    void setData()
    {
        if (index < MultiPlayerGame.NO_AI_CAR)
        {
            GameObject entry = Instantiate(MainMenuManager.Instance.multiLobbyPanel.PlayerListEntryPrefab);
            entry.transform.SetParent(PlayerLIstObject.transform);
            entry.transform.localScale = Vector3.one;
            //entry.GetComponent<Photon.Pun.Demo.Asteroids.PlayerListEntry>().isDummy = true;                                                                                //Photon Removal
            //entry.GetComponent<Photon.Pun.Demo.Asteroids.PlayerListEntry>().Initialize(index * 2505, MultiPlayerGame.TempNamesList[index].aiName);
            //entry.GetComponent<Photon.Pun.Demo.Asteroids.PlayerListEntry>().SetCountry(MultiPlayerGame.TempNamesList[index].aiCountry);
            //entry.GetComponent<Photon.Pun.Demo.Asteroids.PlayerListEntry>().SetPlayerAwatar(MultiPlayerGame.TempNamesList[index].awatarID);


            index++;
            Invoke("setData",0.25f);
        }


    }
    public void populateName()
    {
        //MultiPlayerGame.TempNamesList = new List<string>();

        //List<string> playersNamesList = new List<string>(MultiPlayerGame.AI_NamesList);
        //for (int i = 0; i < 6; i++)
        //{
        //    MultiPlayerGame.TempNamesList.Add(getRandomeName(playersNamesList));
        //}
    }

    public string getRandomeName(List<string> playersNamesList)
    {
        string name = playersNamesList[Random.Range(0, playersNamesList.Count)];
        playersNamesList.Remove(name);
        return name;
    }

}
