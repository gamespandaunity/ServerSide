using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using UnityEngine.UI;
using Cricket;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GetPlayerPrefs : Singleton<GetPlayerPrefs>
{
    [FormerlySerializedAs("Marshmallow_Perm")]
    public GameObject marshmallowPermanent;


    private int pointsForWin = 4;

    private int pointsForDraw = 2;

    public void CheckIfNewUser()
    {
        if (!ObscuredPrefs.HasKey("newUser"))
        {
            ObscuredPrefs.SetInt("newUser", 0);
            CONTROLLER.newUser = true;
        }
    }

    public void SetChaseTargetLevels()
    {
        for (int i = 0; i < CONTROLLER.TargetRangeArray.Length; i++)
        {
            CONTROLLER.MainLevelCompletedArray[i] = 0;
        }
        if (ObscuredPrefs.HasKey("CTMainArray"))
        {
            int @int = ObscuredPrefs.GetInt("CTMainArray");
            for (int i = 0; i <= @int; i++)
            {
                CONTROLLER.MainLevelCompletedArray[i] = 1;
            }
        }
        for (int i = 0; i < 5; i++)
        {
            CONTROLLER.SubLevelCompletedArray[i] = 0;
            if (i < CONTROLLER.CTSubLevelCompleted)
            {
                CONTROLLER.SubLevelCompletedArray[i] = 1;
            }
        }
    }

    public void GetChaseTargetLevelDetails()
    {
        CONTROLLER.TargetRangeArray[0] = "14-16$18-20$22-24$24-26$28-30";
        CONTROLLER.TargetRangeArray[1] = "40-50$60-70$80-90$100-120$120-150";
        CONTROLLER.TargetRangeArray[2] = "70-90$100-120$125-140$150-170$180-200";
        CONTROLLER.TargetRangeArray[3] = "120-140$145-160$165-190$195-220$225-260";
        CONTROLLER.TargetRangeArray[4] = "180-200$210-230$235-260$265-280$285-320";
        CONTROLLER.TargetRangeArray[5] = "300-330$340-370$380-410$420-450$460-500";
        if (ObscuredPrefs.HasKey("ChaseTargetLevelDetail"))
        {
            string @string = ObscuredPrefs.GetString("ChaseTargetLevelDetail");
            string[] array = @string.Split("|"[0]);
            CONTROLLER.CTLevelCompleted = int.Parse(array[0]);
            CONTROLLER.CTSubLevelCompleted = int.Parse(array[1]);
        }
        else
        {
            CONTROLLER.CTLevelCompleted = 0;
            CONTROLLER.CTSubLevelCompleted = 0;
        }
        if (ObscuredPrefs.HasKey("ctWonMatch"))
        {
            CONTROLLER.CTWonMatch = ObscuredPrefs.GetInt("ctWonMatch");
        }
        else
        {
            CONTROLLER.CTWonMatch = 1;
        }
        SetChaseTargetLevels();
    }

    public void SetArrayForChaseTarget()
    {
        if (!ObscuredPrefs.HasKey("ChaseTargetLevelDetail"))
        {
            return;
        }
        string @string = ObscuredPrefs.GetString("ChaseTargetLevelDetail");
        string[] array = @string.Split("|"[0]);
        int num = int.Parse(array[0]);
        int num2 = int.Parse(array[1]);
        if (CONTROLLER.CTCurrentPlayingMainLevel <= num && CONTROLLER.MainLevelCompletedArray[CONTROLLER.CTCurrentPlayingMainLevel] == 1)
        {
            for (int i = 0; i < 5; i++)
            {
                CONTROLLER.SubLevelCompletedArray[i] = 1;
            }
            return;
        }
        for (int i = 0; i < 5; i++)
        {
            CONTROLLER.SubLevelCompletedArray[i] = 0;
            if (i < num2)
            {
                CONTROLLER.SubLevelCompletedArray[i] = 1;
            }
        }
    }





    public void savePlayerDetails(int teamARank, int teamAscore, float teamAballFaced, int teamBRank, int teamBscore, float teamBballFaced, string[] pointsTable)
    {
        float num = 0f;
        float num2 = 0f;
        string text = pointsTable[teamARank];
        string text2 = pointsTable[teamBRank];
        string[] array = text.Split("&"[0]);
        string[] array2 = text2.Split("&"[0]);
        int num3 = teamAscore;
        float num4 = teamAballFaced;
        int num5 = teamBscore;
        float num6 = teamBballFaced;
        teamAscore = num3 + int.Parse(array[7]);
        teamAballFaced += float.Parse(array[8]);
        int num7 = num5 + int.Parse(array[9]);
        float num8 = num6 + float.Parse(array[10]);
        teamBscore = num5 + int.Parse(array2[7]);
        teamBballFaced += float.Parse(array2[8]);
        int num9 = num3 + int.Parse(array2[9]);
        float num10 = num4 + float.Parse(array2[10]);
        array[7] = string.Empty + teamAscore;
        array[8] = string.Empty + teamAballFaced;
        array[9] = string.Empty + num7;
        array[10] = string.Empty + num8;
        array2[7] = string.Empty + teamBscore;
        array2[8] = string.Empty + teamBballFaced;
        array2[9] = string.Empty + num9;
        array2[10] = string.Empty + num10;
        num = float.Parse(string.Empty + teamAscore) / float.Parse(string.Empty + teamAballFaced) - float.Parse(string.Empty + num7) / float.Parse(string.Empty + num8);
        num2 = float.Parse(string.Empty + teamBscore) / float.Parse(string.Empty + teamBballFaced) - float.Parse(string.Empty + num9) / float.Parse(string.Empty + num10);
        if (CONTROLLER.tournamentType == "PAK")
        {
            //PointsWin = 2;
            //PointsDraw = 1;
        }
        else
        {
            pointsForWin = 4;
            pointsForDraw = 2;
        }
        if (num3 > num5)
        {
            int num11 = int.Parse(array[0]) + 1;
            array[0] = string.Empty + num11;
            num11 = int.Parse(array[1]) + 1;
            array[1] = string.Empty + num11;
            num11 = int.Parse(array[5]) + pointsForWin;
            array[5] = string.Empty + num11;
            if (num > 0f)
            {
                array[6] = "+" + num.ToString("F3");
            }
            else if (num < 0f)
            {
                array[6] = string.Empty + num.ToString("F3");
            }
            else
            {
                array[6] = "0";
            }
            text = (pointsTable[teamARank] = string.Join("&", array));
            num11 = int.Parse(array2[0]) + 1;
            array2[0] = string.Empty + num11;
            num11 = int.Parse(array2[2]) + 1;
            array2[2] = string.Empty + num11;
            if (num2 > 0f)
            {
                array2[6] = "+" + num2.ToString("F3");
            }
            else if (num2 < 0f)
            {
                array2[6] = string.Empty + num2.ToString("F3");
            }
            else
            {
                array2[6] = "0";
            }
            text2 = (pointsTable[teamBRank] = string.Join("&", array2));
        }
        else if (num5 > num3)
        {
            int num11 = int.Parse(array[0]) + 1;
            array[0] = string.Empty + num11;
            num11 = int.Parse(array[2]) + 1;
            array[2] = string.Empty + num11;
            if (num > 0f)
            {
                array[6] = "+" + num.ToString("F3");
            }
            else if (num < 0f)
            {
                array[6] = string.Empty + num.ToString("F3");
            }
            else
            {
                array[6] = "0";
            }
            text = (pointsTable[teamARank] = string.Join("&", array));
            num11 = int.Parse(array2[0]) + 1;
            array2[0] = string.Empty + num11;
            num11 = int.Parse(array2[1]) + 1;
            array2[1] = string.Empty + num11;
            num11 = int.Parse(array2[5]) + pointsForWin;
            array2[5] = string.Empty + num11;
            if (num2 > 0f)
            {
                array2[6] = "+" + num2.ToString("F3");
            }
            else if (num2 < 0f)
            {
                array2[6] = string.Empty + num2.ToString("F3");
            }
            else
            {
                array2[6] = "0";
            }
            text2 = (pointsTable[teamBRank] = string.Join("&", array2));
        }
        else if (num3 == num5)
        {
            int num11 = int.Parse(array[0]) + 1;
            array[0] = string.Empty + num11;
            num11 = int.Parse(array[3]) + 1;
            array[3] = string.Empty + num11;
            num11 = int.Parse(array[5]) + pointsForDraw;
            array[5] = string.Empty + num11;
            if (num > 0f)
            {
                array[6] = "+" + num.ToString("F3");
            }
            else if (num < 0f)
            {
                array[6] = string.Empty + num.ToString("F3");
            }
            else
            {
                array[6] = "0";
            }
            text = (pointsTable[teamARank] = string.Join("&", array));
            num11 = int.Parse(array2[0]) + 1;
            array2[0] = string.Empty + num11;
            num11 = int.Parse(array2[3]) + 1;
            array2[3] = string.Empty + num11;
            num11 = int.Parse(array2[5]) + pointsForDraw;
            array2[5] = string.Empty + num11;
            if (num2 > 0f)
            {
                array2[6] = "+" + num2.ToString("F3");
            }
            else if (num2 < 0f)
            {
                array2[6] = string.Empty + num2.ToString("F3");
            }
            else
            {
                array2[6] = "0";
            }
            text2 = (pointsTable[teamBRank] = string.Join("&", array2));
        }
        string text3 = string.Empty;
        for (int i = 0; i < CONTROLLER.TeamList.Length; i++)
        {
            text3 = text3 + pointsTable[i] + "|";
        }

    }

    public string[] setPointsTable(int teamCount, string[] pointsTable)
    {
        int num = 0;
        string text = string.Empty;
        string[] array = new string[10];
        pointsTable = new string[teamCount];

        if (num == 1)
        {
            array = text.Split("|"[0]);
            for (int i = 0; i < teamCount; i++)
            {
                pointsTable[i] = array[i];
            }
        }
        else
        {
            text = string.Empty;
            for (int i = 0; i < teamCount; i++)
            {
                pointsTable[i] = "0&0&0&0&0&0&0&0&0&0&0";
                text = text + pointsTable[i] + "|";
            }
        }
        return pointsTable;
    }



    protected void Start()
    {

        CONTROLLER.SceneIsLoading = false;
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            OnLanguagesClicked();
            //AchievementTable.InitAchievementValues();
            KitTable.InitKitValues();
            CONTROLLER.CanRetrieveAchievements = true;
            CONTROLLER.fromPreloader = true;
            float num = 1.33333337f;
            float num2 = Screen.width;
            float num3 = Screen.height;
            float num4 = num2 / num3;
            CONTROLLER.xOffSet = (num - num4) * 600f / 2f;
            CONTROLLER.gameCompleted = false;

            if (!PlayerPrefs.HasKey("LoftMeterAdded"))
            {
                for (int i = 0; i < 7; i++)
                {
                    CONTROLLER.PlayModeSelected = i;
                    AutoSave.DeleteFile();
                }
                PlayerPrefs.SetInt("LoftMeterAdded", 1);
                CONTROLLER.PlayModeSelected = 0;
            }

            CheckIfNewUser();
        }
        if (CONTROLLER.TeamList == null)
        {
            InitializeGame();
            GetTeamList();
            getSettingsList();
        }

    }

    public void InitializeGame()
    {
        CONTROLLER.Overs[0] = 3;
        CONTROLLER.Overs[1] = 5;
        CONTROLLER.Overs[2] = 10;
        CONTROLLER.Overs[3] = 20;
        CONTROLLER.Overs[4] = 30;
        CONTROLLER.Overs[5] = 50;
        CONTROLLER.Overs[6] = 15;
        CONTROLLER.Overs[7] = 30;
        CONTROLLER.Overs[8] = 60;
        CONTROLLER.Overs[9] = 90;
    }

    public void getSettingsList()
    {
        if (ObscuredPrefs.HasKey("Settings"))
        {
            string @string = ObscuredPrefs.GetString("Settings");
            string[] array = @string.Split("|"[0]);
            CONTROLLER.bgMusicVal = int.Parse(array[0]);
            CONTROLLER.ambientVal = int.Parse(array[1]);
            CONTROLLER.menuBgVolume = float.Parse(array[2]);
            CONTROLLER.sfxVolume = float.Parse(array[3]);
            CONTROLLER.tutorialToggle = 0;
        }
        else
        {
            CONTROLLER.bgMusicVal = 1;
            CONTROLLER.ambientVal = 1;
            CONTROLLER.menuBgVolume = 1f;
            CONTROLLER.sfxVolume = 1f;
            CONTROLLER.tutorialToggle = 0;
            CONTROLLER.PlayerMode = false;
            SavePlayerPrefs.SetSettingsList();
        }
        if (CONTROLLER.sndController != null)
        {
            CONTROLLER.sndController.bgMusicToggle();
            CONTROLLER.sndController.ambientToggle();
        }
    }


    public void GetTeamList()
    {
        if (PlayerPrefs.HasKey("GDPRClicked") && Application.loadedLevelName == "Preloader")
        {
            marshmallowPermanent.gameObject.SetActive(value: true);
        }
    }


    public void OnLanguagesClicked()
    {
        LocalizationData.localizationInstance.loadTheSelectedLanguageFromResources();
    }

    public void LocalizationOKButton()
    {
        if (ManageScene.activeSceneName() == "Preloader")
        {
            PlayerPrefs.SetInt("Localization", 1);
            Invoke("ActivatePermissions", 0.3f);
        }
        else if (ManageScene.activeSceneName() == "MainMenu" || ManageScene.activeSceneName() == "Ground")
        {
            Singleton<SettingsPageTWO>.instance.LocalizationHolder.SetActive(value: false);
            Singleton<SettingsPageTWO>.instance.Holder.SetActive(value: true);
        }
    }

    private void ActivatePermissions()
    {
        marshmallowPermanent.gameObject.SetActive(value: true);
    }



    public void XMLLoaded()
    {
    }

    public void SetTeamList()
    {
        SavePlayerPrefs.SetTeamList();
    }

    public void LoadMenuScene()
    {
        Singleton<LoadingPanelTransition>.instance.PanelTransition1("MainMenu");
    }


}
