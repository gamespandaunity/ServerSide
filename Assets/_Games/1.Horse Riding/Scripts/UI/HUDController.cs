using UnityEngine;
using UnityEngine.UI;
using Tanks.Utilities;
using System;
using Tanks.UI;
using UnityEngine.SceneManagement;
 
/// <summary>
/// Responsible for displaying and updating all elements of the HUD during gameplay.
/// </summary>
public class HUDController : Singleton<HUDController>
	{

    //References to HUD-specific audioclips and internal audiosource
    [Header("Pickup Audio")]
    [SerializeField]
    protected AudioClip m_CurrencyPickupSound;
    [SerializeField]
    protected AudioClip m_PickupSound;
    private AudioSource m_AudioSource;
    private AudioClip m_QueuedSound;
    //Internal references to tank control scripts for update purposes.
    private PlayerManager m_TankManager;
    public AnnouncerModal announcerModal;
    //References and variables for the info text that appears in the centre of the HUD when a player collects a pickup.
    [Header("Pickup info")]
    [SerializeField]
    protected Text m_PickupInfoText;
    [SerializeField]
    protected float m_PickupInfoTimeout = 2f;
    [SerializeField]
    protected float m_PickupFadeStartTime = 1.5f;
    private float m_NextPickupInfoTimeout;
    private float m_PickupFadeTime;
    private Color m_BaseInfoTextColor;

    protected void Start()
    {
        m_BaseInfoTextColor = m_PickupInfoText.color;
        

        m_AudioSource = GetComponent<AudioSource>();

        //if (HudMenuManager.instance != null && MConstants.CurrentGameMode != MConstants.GAME_MODES.MULTI_PLAYER)
        //{
        //    HudMenuManager.instance.onPickupCollected += OnPickupTextChanged;
        //    HudMenuManager.instance.onCurrencyChanged += UpdatePickUpCurrency;
        //}
    }

    protected void Update()
    {
        if (m_PickupInfoText.gameObject.activeSelf)
        {
            if (Time.time >= m_PickupFadeTime)
            {
                float fadeTime = m_NextPickupInfoTimeout - m_PickupFadeTime;

                m_PickupInfoText.color = Color.Lerp(new Color(m_BaseInfoTextColor.r, m_BaseInfoTextColor.g, m_BaseInfoTextColor.b, 0f), m_BaseInfoTextColor, (m_NextPickupInfoTimeout - Time.time) / fadeTime);
            }

            if (Time.time >= m_NextPickupInfoTimeout)
            {
                m_PickupInfoText.gameObject.SetActive(false);
            }
            if (m_QueuedSound != null)
            {
                PlayInterfaceAudio(m_QueuedSound);
                m_QueuedSound = null;
            }
        }       
    }

    //This method is subscribed to the player's tank's currency change event, and plays the relevant sound when fired
    private void UpdatePickUpCurrency(int currency)
    {
        if (currency > 0)
        {
            PlayInterfaceAudio(m_CurrencyPickupSound);
            //HudMenuManager.instance.updateCurrency(currency);
        }
    }

    //This method is subscribed to the player's tank's pickup message change event, and makes the pickup text visible with the correct item name when fired
    private void OnPickupTextChanged(string itemName)
    {
        m_PickupInfoText.gameObject.SetActive(true);
        m_PickupInfoText.color = m_BaseInfoTextColor;
        m_PickupInfoText.text = itemName + " collected.";

        m_QueuedSound = m_PickupSound;
        if (m_QueuedSound != null)
        {
            PlayInterfaceAudio(m_QueuedSound);
            m_QueuedSound = null;
        }
        m_NextPickupInfoTimeout = Time.time + m_PickupInfoTimeout;
        m_PickupFadeTime = Time.time + m_PickupFadeStartTime;
    }

    //This method is called during setup, and subscribes all the HUD's listener methods to the local player's tank
    public void InitHudPlayer(PlayerManager playerTank)
    {
        m_TankManager = playerTank;

        //HudMenuManager.instance.MAX_HEALTH = m_TankManager.health.maxHP;
        playerTank.onMissileChanged += UpdateAmmo;


        m_TankManager.onPickupCollected += OnPickupTextChanged;
        m_TankManager.onCurrencyChanged += UpdatePickUpCurrency;

    }
    public void BackToMainMenue()
    {
        //Photon Removal PhotonNetwork.LeaveRoom();

    }
    //This method is subscribed to the player's tank's ammo quantity change events, and enables/disables the HUD overlay and changes its value when fired
    private void UpdateAmmo(int newAmmo)
    {
       // //Debug.Log("UpdateAmmo");

    }

    public void ShootMissile()
    {
        m_TankManager.FireMissile();
    }
    //We unsubscribe all listeners from the player's tank when this object is destroyed
    protected override void OnDestroy()
    {
       

        if (m_TankManager != null)
        {
            m_TankManager.onMissileChanged -= UpdateAmmo;

            m_TankManager.onPickupCollected -= OnPickupTextChanged;
            m_TankManager.onCurrencyChanged -= UpdatePickUpCurrency;
        }

        //if (HudMenuManager.instance!= null && MConstants.CurrentGameMode != MConstants.GAME_MODES.MULTI_PLAYER)
        //{
        //    HudMenuManager.instance.onPickupCollected -= OnPickupTextChanged;
        //    HudMenuManager.instance.onCurrencyChanged -= UpdatePickUpCurrency;
        //}
        base.OnDestroy();
    }

    //This method is used to play dedicated HUD audio effects via the HUD audiosource.
    public void PlayInterfaceAudio(AudioClip soundEffect)
    {
        m_AudioSource.PlayOneShot(soundEffect);
    }
}
