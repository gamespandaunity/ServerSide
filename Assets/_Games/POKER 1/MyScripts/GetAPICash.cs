//
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
//using System.Security.Policy;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
//using WebSocketSharp;

namespace POKER
{
    [RequireComponent(typeof(TMP_Text))]

    public class GetAPICash : MonoBehaviour
    {
        TMP_Text this_text;

        // Start is called before the first frame update
        void OnEnable()
        {
            this_text = GetComponent<TMP_Text>();
            this_text.text = LocalSettings.Rs(LocalSettings.GetTotalChips());
            //RestAPILuqman.Instance.GetChips(UpdateThisText);
        }

        public void UpdateThisText(BigInteger cash)
        {
            //   this_text.text = LocalSettings.Rs(cash);
            // networkkkkkkkkkkkkkkkkkkk
            // Should uncomment to get network cash
            //LocalSettings.SetTotalChips(cash);
        }

    }
}