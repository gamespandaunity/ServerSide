using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class WithdrawManager : MonoBehaviour
{
    public GameObject History_All;
    private HistoryManager _historyManager;
    public string GoToBackPanelName;
    private void OnEnable()
    {
        if (Borderspanel.UnselectCoin != null)
        {
            Borderspanel.UnselectCoin(false);
        }
        if (Borderspanel.logoHandler != null)
        {
            Borderspanel.logoHandler(false);
        }
        HomeMenuManager.instance.backButtonHeader.onClick.RemoveAllListeners();
        HomeMenuManager.instance.backButtonHeader.onClick.AddListener(Back);
    }
    private void Start()
    {
        _historyManager = History_All.GetComponent<HistoryManager>();
    }
    public void ReturnBtn()
    {
        SceneLoaderUtility.LoadScene("MainmenuScene");
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void Back()
    {
        foreach (GameObject gb in ScreenNavigotor_Custom.Instance.ScreenGameObjects)
        {
            gb.SetActive(gb.name.Equals(GoToBackPanelName));
        }


    }

    public void WithdrawHistoryBtn()
    {
        //SceneLoaderUtility.LoadScene("WithdrawHistoryScene");
        UIMainMenManager.instance.openPanelByName("GameHistory");
        _historyManager.historyButtons[1].GetComponent<Button>().onClick.Invoke();

        SoundManagerMain.instance.ClickSoundPlay();
    }

    public void ApplyWithdrawBtn()
    {
        SceneLoaderUtility.LoadScene("ApplyWithdrawScene");
        UIMainMenManager.instance.openPanelByName("ApplyWithdrawScene");
        SoundManagerMain.instance.ClickSoundPlay();
    }
}
