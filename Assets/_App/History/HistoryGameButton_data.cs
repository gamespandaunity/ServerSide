using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HistoryGameButton_data : MonoBehaviour
{
    private Image logo;
    private Button button;
     int Gameid;
    string url, category;
   
  


    private void OnEnable()
    {
        logo = this.transform.GetChild(0).gameObject.GetComponent<Image>();
  
       
    }
  
    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(()=>GameHistoryManager.instance.GamesHistoryWIthID(Gameid));
    }
    public void Init(game_detail detail)
    {
        Gameid = detail.game_id;
        url = detail.file_url;
        category = detail.category;
        GetGamesLogo(url, logo);
        ApiAndRoomManager._instance.LoadingObject.SetActive(false);
    }
    public string SplitUrl(string str)
    {
        if (str.Contains("public"))
        {

        string[] splittedform = str.Split("public");
        return splittedform[1];
        }
        else
        {
            return str;
        }
    }
    public void GetGamesLogo(string fileUrl, Image gameLogo)
    {
        string url = SplitUrl(fileUrl);
        ServerConnection.DownloadSprite(url
             , DownloadedTexture =>
             {
                 gameLogo.sprite = ConstantsData_M.ConvertTextureToSprite( DownloadedTexture);
             },
               Onfailed =>
               {
                   //Debug.Log("Failed To Load Image Main menu" + url + " with " + Onfailed);
               }
              );
    }

}
