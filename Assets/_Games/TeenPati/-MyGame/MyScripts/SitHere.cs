using TeenPattiGame;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityExtensions;


[RequireComponent(typeof(Button))]
public class SitHere : MonoBehaviour
{

    private static SitHere _instance;
    public static SitHere Instance
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.Find("Pos1").transform.GetChild(0).GetComponent<SitHere>();
            return _instance;
        }
    }




    private void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    public int positionToSit;

    private void OnEnable()
    {
        RegisterListener();
    }

    void RegisterListener()
    {
        Button local_button = GetComponent<Button>();
        local_button.onClick.RemoveAllListeners();
        local_button.onClick.AddListener(SetThisPositionByPlayer);
    }

    public void SetThisPositionByPlayer()
    {
        if (MatchHandler.isOffline())
        {

            if (LocalSettings.AI_StandUP)
            {
                Debug.LogError("Here we Go   1");
                LocalSettings.Show_Dialogue(TeenPattiGame.UIManager.Instance.dialogueBox, " Wait For AI ");
                return;
            }
        }
        //if (GameManager.Instance.position_availability[positionToSit].)

        if (LocalSettings.GetTotalChips() < LocalSettings.MinBetAmount)
        {
            if (!MatchHandler.isOffline())
            {
                if (TeenPattiGame.UIManager.Instance.GetMyPlayerInfo().this_photonView.isOwned)
                {
                    TeenPattiGame.UIManager.Instance.quickShop.SetActive(true);
                    TeenPattiGame.UIManager.Instance.GetMyPlayerInfo().StandUp();
                    return;
                }
            }
            else
            {
                TeenPattiGame.UIManager.Instance.quickShop.SetActive(true);
                TeenPattiGame.UIManager.Instance.GetMyPlayerInfo().StandUp();
                return;
            }

        }




        TeenPattiGame.UIManager uIManager = TeenPattiGame.UIManager.Instance;


        PositionsManager.Instance.SitHere(positionToSit);
        Invoke(nameof(RefreshAfterSomeTime), 0.5f);
        TeenPattiGame.GameManager.Instance.SitHereBtnStatus(false);
        PlayerStateManager.Instance.Amountobject.SetActive(true);
        PlayerStateManager.Instance.taptoSitHere.SetActive(false);
        PlayerStateManager.Instance.waitForNextRound.SetActive(false);
        TurnManagerOfflineTeenPatti.Instance.TurnOnBottomAmountAfterDelay();

    }




    public void SetThisForAIPositionByPlayer()
    {
        StartCoroutine(SetWaitForAIPositionByPlayer());

    }


    IEnumerator SetWaitForAIPositionByPlayer()
    {
        // Debug.LogError("Here we Go " + AI_coroutine);
        //if (GameManager.Instance.position_availability[positionToSit].)
        yield return new WaitForSeconds(Random.Range(3f, 11f));


        if (LocalSettings.AI_Amount < LocalSettings.MinBetAmount)
        {

            LocalSettings.AI_Amount = UnityEngine.Random.Range(50000, 100000);



        }


        TeenPattiGame.UIManager uIManager = TeenPattiGame.UIManager.Instance;


        PositionsManager.Instance.AISitHere(positionToSit);
        Invoke(nameof(RefreshAfterSomeTime), 0.5f);
        yield return new WaitForSeconds(1f);
        LocalSettings.AI_StandUP = false;
        TeenPattiGame.GameManager.Instance.SitHereBtnStatus(false);
    }



    void RefreshAfterSomeTime()
    {
        PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();
    }

}
