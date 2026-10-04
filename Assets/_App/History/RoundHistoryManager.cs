using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundHistoryManager : MonoBehaviour
{
    public string transactionId,    coins,  created_at, first_player_invest,    second_player_invest;
    public Transform dataHolder;
    public GameObject ParentObj;
    public void Init(List<RoundHistoryModel> data)
    {
        ParentObj.SetActive(true);

            GameObject gb = Resources.Load<GameObject>("TeenPattiTemp_Prefab");
            for(int i = 0; i < data.Count; i++)
            {
                GameObject History = Instantiate(gb , dataHolder);
                History.GetComponent<TeenPattiRoundHistory_Template>().Init(data[i]);     
            }
           

        
    }
}
