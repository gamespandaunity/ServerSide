using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class DailyRewardManager : MonoBehaviour
{
    public GameObject loader;
    public GameObject Timer , NextRewardPopUp;
    public Text alertText;
    public Text messageTxt;
    public DailyRewardHistory historyResponse;

  [Tooltip("set days to get data")]  public string noDays = "";
    public static DailyRewardManager instance;
    public DailyRewardsFetch[] days;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
   
    }
    
    public void OnEnable()
    {
        if (Borderspanel.ChangeTitle != null)
        {
            Borderspanel.ChangeTitle("");
        }
        if (Borderspanel.UnselectCoin != null)
        {
            Borderspanel.UnselectCoin(false);
        }
        if (Borderspanel.logoHandler != null)
        {
            Borderspanel.logoHandler(false);
        }

    }
    public void backBtn()
    {
        SceneLoaderUtility.LoadScene("MainmenuScene");
    }
}
