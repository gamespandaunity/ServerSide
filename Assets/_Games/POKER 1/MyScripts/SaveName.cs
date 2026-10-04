//
//using Photon.Pun;
using TMPro;
using UnityEngine;

namespace POKER
{
    public class SaveName : MonoBehaviour
    {
        public TMP_InputField inputField;
        public GameObject Crossbtn;

        // Savenameinstance
        private static SaveName instance;
        public static SaveName Instance
        {
            get
            {
                if (instance == null)
                    instance = FindObjectOfType<SaveName>();
                return instance;
            }
        }

        // Start is called before the first frame update
        void Awake()
        {
            instance = this;

        }

        private void OnEnable()
        {
            Crossbtn.SetActive(PlayerPrefs.HasKey("name") ? true : false);

        }

        public void SaveMyName()
        {
            bool containsOnlySpaces = IsOnlySpaces(inputField.text);
            if (containsOnlySpaces)
                return;
            if (inputField.text != "")
            {
                string name = inputField.text;
                //PlayerPrefs.SetString("name", name);
                //gameObject.SetActive(false);
                //PhotonNetwork.LocalPlayer.NickName = name; //+ Random.Range(100, 1000);
                //PlayerPrefs.SetString("name", PhotonNetwork.LocalPlayer.NickName);
               UpdateFrames.Instance.NameText.text = PlayerPrefs.GetString("name");



            }
        }

        private bool IsOnlySpaces(string text)
        {
            foreach (char c in text)
            {
                if (c != ' ')
                {
                    return false; // Found a non-space character
                }
            }
            return true; // Only spaces found
        }
    }
}