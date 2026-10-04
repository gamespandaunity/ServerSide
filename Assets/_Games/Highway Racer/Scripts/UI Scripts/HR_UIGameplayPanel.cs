using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Gameplay Panel")]
public class HR_UIGameplayPanel : MonoBehaviour {

   private HR_PlayerHandler player;
    public GameObject content;

    public Text score;
    public Text timeLeft;
    public Text combo;

    public Text speed;
    public Text distance;
    public Text highSpeed;
    public Text oppositeDirection;
    public Slider bombSlider;

    [Header("Wrong Way")]
    [Tooltip("Optional. Khali chhora jaye to runtime par is canvas ke neeche ek simple warning popup bana diya jata hai.")]
    public GameObject wrongWayPopup;
    [Tooltip("Warning par likha jane wala text.")]
    public string wrongWayMessage = "WRONG WAY!";

    private CanvasGroup wrongWayCanvasGroup;

    private Image comboMImage;
    private Vector2 comboDefPos;

    private Image highSpeedImage;
    private Vector2 highSpeedDefPos;

    private Image oppositeDirectionImage;
    private Vector2 oppositeDirectionDefPos;

    private Image timeAttackImage;

    private RectTransform bombRect;
    private Vector2 bombDefPos;

    // ✅ Multiplayer panel (generic, works for both offline and Mirror)
    private GameObject multiplayerPanel;
    private bool isSearchingForPlayer = false;

    private void Awake()
    {

        comboMImage = combo.GetComponentInParent<Image>();
        comboDefPos = comboMImage.rectTransform.anchoredPosition;
        highSpeedImage = highSpeed.GetComponentInParent<Image>();
        highSpeedDefPos = highSpeedImage.rectTransform.anchoredPosition;
        oppositeDirectionImage = oppositeDirection.GetComponentInParent<Image>();
        oppositeDirectionDefPos = oppositeDirectionImage.rectTransform.anchoredPosition;
        timeAttackImage = timeLeft.GetComponentInParent<Image>();
        bombRect = bombSlider.GetComponent<RectTransform>();
        bombDefPos = bombRect.anchoredPosition;

        // ✅ Find multiplayer panel if exists (optional)
        Transform mpPanel = transform.Find("MultiplayerPanel");
        if (mpPanel != null)
            multiplayerPanel = mpPanel.gameObject;

        SetupWrongWayPopup();

    }

    /// <summary>
    /// Wrong way warning. The two gameplay scenes each carry their own copy of this canvas, so
    /// the popup is built here rather than wired per scene. Assign wrongWayPopup in the inspector
    /// to use a hand made one instead.
    /// </summary>
    private void SetupWrongWayPopup()
    {

        if (!wrongWayPopup)
            wrongWayPopup = CreateWrongWayPopup();

        if (!wrongWayPopup)
            return;

        wrongWayCanvasGroup = wrongWayPopup.GetComponent<CanvasGroup>();

        if (!wrongWayCanvasGroup)
            wrongWayCanvasGroup = wrongWayPopup.AddComponent<CanvasGroup>();

        //	Warning must never eat the steering / brake touches sitting under it.
        wrongWayCanvasGroup.blocksRaycasts = false;
        wrongWayCanvasGroup.interactable = false;

        ApplyWrongWayMessage();

        wrongWayPopup.SetActive(false);

    }

    /// <summary>
    /// A popup placed by hand usually ships with an empty label, which shows up as a warning that
    /// activates but stays invisible. Fill it in here. Text already typed on the label wins, so
    /// custom wording is never overwritten.
    /// </summary>
    private void ApplyWrongWayMessage()
    {

        if (string.IsNullOrEmpty(wrongWayMessage))
            return;

        TMP_Text tmpLabel = wrongWayPopup.GetComponentInChildren<TMP_Text>(true);

        if (tmpLabel && string.IsNullOrEmpty(tmpLabel.text))
            tmpLabel.text = wrongWayMessage;

        Text label = wrongWayPopup.GetComponentInChildren<Text>(true);

        if (label && string.IsNullOrEmpty(label.text))
            label.text = wrongWayMessage;

    }

    private GameObject CreateWrongWayPopup()
    {

        Canvas canvas = GetComponentInParent<Canvas>();

        if (!canvas)
            return null;

        //	Parented to the canvas, not to content, so it survives content being hidden.
        GameObject popup = new GameObject("Wrong Way Warning", typeof(RectTransform), typeof(Image));
        popup.transform.SetParent(canvas.transform, false);

        RectTransform popupRect = (RectTransform)popup.transform;
        popupRect.anchorMin = new Vector2(.5f, 1f);
        popupRect.anchorMax = new Vector2(.5f, 1f);
        popupRect.pivot = new Vector2(.5f, 1f);
        popupRect.anchoredPosition = new Vector2(0f, -140f);
        popupRect.sizeDelta = new Vector2(560f, 110f);

        Image background = popup.GetComponent<Image>();
        background.color = new Color(.7f, 0f, 0f, .8f);
        background.raycastTarget = false;

        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(popup.transform, false);

        RectTransform labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text labelText = label.GetComponent<Text>();
        labelText.text = wrongWayMessage;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        labelText.fontSize = 50;
        labelText.fontStyle = FontStyle.Bold;
        labelText.raycastTarget = false;

        //	Reusing a HUD font keeps the warning in the same style as the rest of the panel.
        if (score && score.font)
            labelText.font = score.font;
        else
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return popup;

    }

    /// <summary>
    /// Shows the warning while the player is heading back down the highway, and blinks it so it
    /// reads as a warning rather than another HUD readout.
    /// </summary>
    private void UpdateWrongWayWarning()
    {

        if (!wrongWayPopup)
            return;

        bool show = player && player.wrongWay;

        if (wrongWayPopup.activeSelf != show)
            wrongWayPopup.SetActive(show);

        if (show && wrongWayCanvasGroup)
            wrongWayCanvasGroup.alpha = .55f + Mathf.PingPong(Time.unscaledTime * 2f, .45f);

    }

    /// <summary>
    /// Shows the warning for two seconds from the component's context menu, so the popup itself
    /// can be checked without having to drive the wrong way first.
    /// </summary>
    [ContextMenu("Test Wrong Way Popup")]
    private void TestWrongWayPopup()
    {

        if (!wrongWayPopup)
        {

            Debug.LogWarning("⚠️ HR_UIGameplayPanel: No wrong way popup to test.");
            return;

        }

        StartCoroutine(TestWrongWayPopupDelayed());

    }

    private IEnumerator TestWrongWayPopupDelayed()
    {

        ApplyWrongWayMessage();
        wrongWayPopup.SetActive(true);

        yield return new WaitForSecondsRealtime(2f);

        wrongWayPopup.SetActive(false);

    }

    private void OnEnable()
    {

        HR_GamePlayHandler.OnPlayerSpawned += HR_PlayerHandler_OnPlayerSpawned;
        HR_GamePlayHandler.OnPlayerDied += HR_PlayerHandler_OnPlayerDied;

        // ✅ Also listen to network manager event (safe even if offline)
        if (HR_NetworkManager.Instance != null)
            HR_NetworkManager.OnPlayerSpawned += HR_PlayerHandler_OnPlayerSpawned;

    }
    [SerializeField] private GameObject speakerObj;
    [SerializeField] private GameObject micObj;
    private void Start()
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            if (staticVariables.gameFeatures.isAgora)
            {
                speakerObj.SetActive(true);
                micObj.SetActive(true);
            }
            else
            {
                speakerObj.SetActive(false);
                micObj.SetActive(false);
            }
        }
        // ✅ Handle multiplayer panel visibility
        if (multiplayerPanel)
        {

            if (PlayerPrefs.GetInt("Multiplayer", 0) == 1)
                multiplayerPanel.SetActive(true);
            else
                multiplayerPanel.SetActive(false);

        }

        // ✅ Start searching for player if not found yet
        if (player == null)
        {
         //   StartCoroutine(FindLocalPlayer());
        }

    }

    // ✅ Smart player finder - works for both offline and multiplayer
    private IEnumerator FindLocalPlayer()
    {
        if (isSearchingForPlayer) yield break;

        isSearchingForPlayer = true;
        Debug.Log("🔍 HR_UIGameplayPanel: Searching for local player...");

        yield return new WaitForSeconds(0.5f);

        int attempts = 0;
        while (player == null && attempts < 20)
        {
            // Method 1: Check HR_GamePlayHandler (works for offline)
            if (HR_GamePlayHandler.Instance != null && HR_GamePlayHandler.Instance.player != null)
            {
                player = HR_GamePlayHandler.Instance.player;
                content.SetActive(true);
                Debug.Log("✅ HR_UIGameplayPanel: Found player via HR_GamePlayHandler");
                isSearchingForPlayer = false;
                yield break;
            }

            // Method 2: Check if multiplayer mode (only if Mirror is active)
            if (PlayerPrefs.GetInt("Multiplayer", 0) == 1)
            {
                var networkCars = FindObjectsOfType<HighwayCarNetwork>();
                foreach (var car in networkCars)
                {
                    if (car.isOwned)
                    {
                        var handler = car.GetComponent<HR_PlayerHandler>();
                        if (handler != null)
                        {
                            player = handler;
                        //    content.SetActive(true);

                            // Update GamePlayHandler too
                            if (HR_GamePlayHandler.Instance != null)
                                HR_GamePlayHandler.Instance.player = handler;

                            Debug.Log("✅ HR_UIGameplayPanel: Found player via HighwayCarNetwork");
                            isSearchingForPlayer = false;
                            yield break;
                        }
                    }
                }
            }

            attempts++;
            yield return new WaitForSeconds(0.5f);
        }

        if (player == null)
        {
            Debug.LogWarning("⚠️ HR_UIGameplayPanel: Could not find local player after 10 seconds");
        }

        isSearchingForPlayer = false;
    }

    private void HR_PlayerHandler_OnPlayerSpawned(HR_PlayerHandler _player)
    {

        player = _player;
       // content.SetActive(true);
        Debug.Log("✅ HR_UIGameplayPanel: Player assigned via event");

    }

    private void HR_PlayerHandler_OnPlayerDied(HR_PlayerHandler _player, int[] scores)
    {

        // Only hide the HUD when the LOCAL player dies. In AI mode the bot
        // opponent (HR_GamePlayHandler.player2) also fires OnPlayerDied when
        // it crashes — without this guard the human player's HUD, including
        // the left/right steering controls, gets hidden mid-race the moment
        // the AI opponent has its first crash. That's why the bug only
        // surfaces in AI mode (the other modes have no player2 to die).
        if (HR_GamePlayHandler.Instance != null
            && HR_GamePlayHandler.Instance.player != null
            && _player != HR_GamePlayHandler.Instance.player)
        {
            return;
        }

        player = null;
        content.SetActive(false);

    }

    private void Update()
    {

        //	Runs before the player null check below so the warning also clears on death / lost reference.
        UpdateWrongWayWarning();

        // ✅ Auto-recovery if player reference lost
        if (!player)
        {
            if (!isSearchingForPlayer && HR_GamePlayHandler.Instance != null)
            {
                player = HR_GamePlayHandler.Instance.player;
                if (player != null)
                {
                    
                    //   content.SetActive(true);
                    Debug.Log("🔧 HR_UIGameplayPanel: Recovered player reference");
                }
            }
            return;
        }

        if (player.combo > 1)
            comboMImage.rectTransform.anchoredPosition = Vector2.Lerp(comboMImage.rectTransform.anchoredPosition, comboDefPos, Time.deltaTime * 5f);
        else
            comboMImage.rectTransform.anchoredPosition = Vector2.Lerp(comboMImage.rectTransform.anchoredPosition, new Vector2(comboDefPos.x - 500, comboDefPos.y), Time.deltaTime * 5f);

        if (player.highSpeedCurrent > .1f)
            highSpeedImage.rectTransform.anchoredPosition = Vector2.Lerp(highSpeedImage.rectTransform.anchoredPosition, highSpeedDefPos, Time.deltaTime * 5f);
        else
            highSpeedImage.rectTransform.anchoredPosition = Vector2.Lerp(highSpeedImage.rectTransform.anchoredPosition, new Vector2(highSpeedDefPos.x + 500, highSpeedDefPos.y), Time.deltaTime * 5f);

        if (player.opposideDirectionCurrent > .1f)
            oppositeDirectionImage.rectTransform.anchoredPosition = Vector2.Lerp(oppositeDirectionImage.rectTransform.anchoredPosition, oppositeDirectionDefPos, Time.deltaTime * 5f);
        else
            oppositeDirectionImage.rectTransform.anchoredPosition = Vector2.Lerp(oppositeDirectionImage.rectTransform.anchoredPosition, new Vector2(oppositeDirectionDefPos.x - 500, oppositeDirectionDefPos.y), Time.deltaTime * 5f);

        if (HR_GamePlayHandler.Instance.mode == HR_GamePlayHandler.Mode.TimeAttack)
        {

            if (!timeLeft.gameObject.activeSelf)
                timeAttackImage.gameObject.SetActive(true);

        }
        else
        {

            if (timeLeft.gameObject.activeSelf)
                timeAttackImage.gameObject.SetActive(false);

        }

        if (HR_GamePlayHandler.Instance.mode == HR_GamePlayHandler.Mode.Bomb)
        {

            if (!bombSlider.gameObject.activeSelf)
                bombSlider.gameObject.SetActive(true);

        }
        else
        {

            if (bombSlider.gameObject.activeSelf)
                bombSlider.gameObject.SetActive(false);

        }

        if (player.bombTriggered)
            bombRect.anchoredPosition = Vector2.Lerp(bombRect.anchoredPosition, bombDefPos, Time.deltaTime * 5f);
        else
            bombRect.anchoredPosition = Vector2.Lerp(bombRect.anchoredPosition, new Vector2(bombDefPos.x - 500, bombDefPos.y), Time.deltaTime * 5f);

    }

    private void LateUpdate()
    {

        if (!player)
            return;

        // ✅ Safe text updates with null checks
        if (score) score.text = player.score.ToString("F0");
        if (speed) speed.text = player.speed.ToString("F0");
        if (distance) distance.text = (player.distance).ToString("F2");
        if (highSpeed) highSpeed.text = player.highSpeedCurrent.ToString("F1");
        if (oppositeDirection) oppositeDirection.text = player.opposideDirectionCurrent.ToString("F1");
        if (timeLeft) timeLeft.text = player.timeLeft.ToString("F1");
        if (combo) combo.text = player.combo.ToString();

        if (HR_GamePlayHandler.Instance.mode == HR_GamePlayHandler.Mode.Bomb && bombSlider)
            bombSlider.value = player.bombHealth / 100f;

    }

    private void OnDisable()
    {

    //    HR_GamePlayHandler.OnPlayerSpawned -= HR_PlayerHandler_OnPlayerSpawned;
     //   HR_GamePlayHandler.OnPlayerDied -= HR_PlayerHandler_OnPlayerDied;

     //   if (HR_NetworkManager.Instance != null)
      //      HR_NetworkManager.OnPlayerSpawned -= HR_PlayerHandler_OnPlayerSpawned;

    }


}