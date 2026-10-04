using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientHisory : MonoBehaviour
{
    //public Transform Content;
    //public MerchantHistory ClientHistory;

    //private void OnEnable()
    //{
    //    GetMerchantHistory();
    //}
    //[Button]
    //public void GetMerchantHistory()
    //{
    //    if (ClientHistory.data.Count > 0)
    //    {
    //        Content.Clear();
    //        ClientHistory = new MerchantHistory();
    //    }
    //    APIManager.instance.GetClientHistoryApi(OnSuccess =>
    //    {
    //        ClientHistory = JsonUtility.FromJson<MerchantHistory>(OnSuccess);
    //        if (ClientHistory.data.Count > 0)
    //        {
    //            var MerchantDetailPrefab = Resources.Load<GameObject>("MerchantHistoryPrefab");
    //            for (int i = 0; i < ClientHistory.data.Count; i++)
    //            {
    //                var merchantObject = Instantiate(MerchantDetailPrefab, Content.transform);
    //                merchantObject.GetComponent<MErchantHistoryDetail>().Init(ClientHistory.data[i]);
    //            }
    //        }
    //    });
    //}
}
