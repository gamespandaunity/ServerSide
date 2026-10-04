using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AssemblyCSharp;
using LudoGame;

public class EditProfileController : MonoBehaviour
{

    public GameObject changeName;
    public GameObject gridView;
    public GameObject buttonPrefab;

    private string avatarIndex;

    public GameObject PlayerNameMain;
    public GameObject PlayerAvatarMain;

    private StaticGameVariablesController staticController;

    private List<GameObject> buttons = new List<GameObject>();
    // Use this for initialization
    void Start()
    {

        avatarIndex = LudoGame.GameManager.Instance.myPlayerData.GetAvatarIndex();

        staticController = GameObject.Find("StaticGameVariablesContainer").GetComponent<StaticGameVariablesController>();
        changeName.GetComponent<InputField>().text = LudoGame.GameManager.Instance.nameMy;

        if (LudoGame.GameManager.Instance.facebookAvatar != null)
        {
            GameObject button = Instantiate(buttonPrefab);
            button.GetComponent<ProfilePictureController>().picture.GetComponent<Image>().sprite = LudoGame.GameManager.Instance.facebookAvatar;
            button.transform.SetParent(gridView.transform, false);

            GameObject border = button.GetComponent<ProfilePictureController>().frame;
            if (LudoGame.GameManager.Instance.myPlayerData.GetAvatarIndex().Equals("fb"))
            {
                border.GetComponent<Image>().color = Color.green;
            }

            string index = "fb";
            button.GetComponent<Button>().onClick.RemoveAllListeners();
            button.GetComponent<Button>().onClick.AddListener(() => ClickButton(index, border));

            buttons.Add(border);
        }



        for (int i = 0; i < staticController.avatars.Length; i++)
        {
            GameObject button = Instantiate(buttonPrefab);
            button.GetComponent<ProfilePictureController>().picture.GetComponent<Image>().sprite = staticController.avatars[i];
            button.transform.SetParent(gridView.transform, false);

            GameObject border = button.GetComponent<ProfilePictureController>().frame;
            if (LudoGame.GameManager.Instance.myPlayerData.GetAvatarIndex().Equals(i + ""))
            {
                border.GetComponent<Image>().color = Color.green;
            }

            string index = "" + i;
            button.GetComponent<Button>().onClick.RemoveAllListeners();
            button.GetComponent<Button>().onClick.AddListener(() => ClickButton(index, border));

            buttons.Add(border);
        }
    }

    public void ClickButton(string avatarIndex, GameObject border)
    {
        this.avatarIndex = avatarIndex;

        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].GetComponent<Image>().color = Color.white;

        }
        border.GetComponent<Image>().color = Color.green;
    }

    public void Save()
    {


        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add(MyPlayerData.AvatarIndexKey, avatarIndex);
        data.Add(MyPlayerData.PlayerName, changeName.GetComponent<InputField>().text);

        LudoGame.GameManager.Instance.myPlayerData.UpdateUserData(data);


        PlayerNameMain.GetComponent<Text>().text = changeName.GetComponent<InputField>().text;
        LudoGame.GameManager.Instance.nameMy = changeName.GetComponent<InputField>().text;

        //call api to update player name
        string url = StaticStrings.baseURL+"update-users-name.php?playfab_id="+LudoGame.GameManager.Instance.playfabManager.PlayFabId+"&player_name="+LudoGame.GameManager.Instance.nameMy;
        WWW www = new WWW(url);
        StartCoroutine(updateUserName(www));


        if (avatarIndex.Equals("fb"))
        {
            PlayerAvatarMain.GetComponent<Image>().sprite = LudoGame.GameManager.Instance.facebookAvatar;
            LudoGame.GameManager.Instance.avatarMy = LudoGame.GameManager.Instance.facebookAvatar;
        }
        else
        {
            PlayerAvatarMain.GetComponent<Image>().sprite = staticController.avatars[int.Parse(avatarIndex)];
            LudoGame.GameManager.Instance.avatarMy = staticController.avatars[int.Parse(avatarIndex)];
        }



        gameObject.SetActive(false);
    }

    IEnumerator updateUserName(WWW www)
     {
         yield return www;

         // check for errors
         if (www.error == null)
         {
            Debug.Log("WWW Result!: " + www.text);// contains all the data sent from the server
         }
         else{
            Debug.Log("WWW Failed!: " + www.text);// contains all the data sent from the server
         }
     }
    // Update is called once per frame
    void Update()
    {

    }
}
