using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Twelve
{
    public class MenuManagerTwelve : MonoBehaviour
{
    public static MenuManagerTwelve Instance;

    [Header("PopUps")]
    [SerializeField] GameObject LoadingPopup;
    [SerializeField] GameObject JoinFailedPopUp;
    [SerializeField] TextMeshProUGUI JoinFailedText;
    [SerializeField] GameObject backBtn;

    [Space]
    [Header("Menus")]
    [SerializeField] List<GameObject> Menus;
    public enum AllMenus
    {
        ModeSelectionScreen,
        AvatarScreen,
        LobbyScreen,
        MatchFixedScreen,

    }

    [SerializeField] private AllMenus CurrentMenu;
    private AllMenus PreviousMenu;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        PreviousMenu = CurrentMenu;
        DisableAllMenus();
        EnableMenu(CurrentMenu);

        SetBackBtn();

    }

    void DisableAllMenus()
    {
        foreach (GameObject menuObject in Menus)
        {
            menuObject.SetActive(false);
        }
    }

    public void ChangeState(AllMenus NewMenu)
    {
        DisableMenu(CurrentMenu);
        PreviousMenu = CurrentMenu;
        CurrentMenu = NewMenu;
        EnableMenu(NewMenu);

        SetBackBtn();

    }

    public void EnablePreviousMenu()
    {
        DisableMenu(CurrentMenu);
        CurrentMenu = PreviousMenu;
        EnableMenu(PreviousMenu);
        if (CurrentMenu.Equals(AllMenus.AvatarScreen))
        {
            PreviousMenu = AllMenus.ModeSelectionScreen;
        }
        else if (CurrentMenu.Equals(AllMenus.LobbyScreen))
        {
            PreviousMenu = AllMenus.AvatarScreen;
        }
        else if (CurrentMenu.Equals(AllMenus.MatchFixedScreen))
        {

            PreviousMenu = AllMenus.LobbyScreen;
        }
        else if (CurrentMenu.Equals(AllMenus.ModeSelectionScreen))
        {
            PreviousMenu = AllMenus.ModeSelectionScreen;

        }

        SetBackBtn();
    }

    private void EnableMenu(AllMenus menu)
    {
        Menus[(int)menu].SetActive(true);
    }

    private void DisableMenu(AllMenus menu)
    {
        Menus[(int)menu].SetActive(false);
    }

    

    void SetBackBtn()
    {
        if(CurrentMenu.Equals(AllMenus.ModeSelectionScreen))
        {
            backBtn.SetActive(false);
        }
        else 
        {
            backBtn.SetActive(true);
        }
    }


    // Popups
    public void SetLoadingPopUpState(bool state)
    {
        LoadingPopup.SetActive(state);
    }

    public void SetJoinFailedPopUpState(bool state, string errorMessage)
    {
        JoinFailedText.text = errorMessage;
        JoinFailedPopUp.SetActive(state);
    }

    // public void StartMultiPlayerGame()
    // {
    //     SceneManager.LoadScene(1);
    // }

    public void OnClickBack()
    {
            //Photon Removal    NetworkManagerTwelve.Instance.LeaveRoom();
            EnablePreviousMenu();
    }



}
}
