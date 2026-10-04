using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using System.Collections.Generic;
 

namespace Tanks.UI
{
	/// <summary>
	/// Multiplayer end game modal - announces winner and awards
	/// </summary>
	public class MultiplayerEndGameModal : EndGameModal
	{
		#region UI

		[SerializeField]
		protected Text m_AwardText, m_AwardAmountText;

		[SerializeField]
		protected EndGameCountDown m_CountDown;

		[SerializeField]
		protected NumberDisplay m_NumberDisplay, m_CurrencyDisplay;
		[SerializeField]
		protected ParticleSystem m_RewardParticleSystem;

		[SerializeField]
		protected GameObject m_RewardParent;

		//[SerializeField]


       // public ResultManagerForHorse winLoseHorse;
		#endregion

		//Anim delay
		[SerializeField]
		protected float m_DelayBeforeCurrencyAnim = 2.0f;

		
        LeaderboardElement localPlayer;

        //The scene in the menu to return to
       
        List<LeaderboardElement> leaderboardElement;
        

        private float m_DelayCounter = 0.0f;
		private bool m_IsDelaying = false, m_UsedEvent = false, m_HasAwardedCurrency = false;
		private int m_AwardCurrency, m_TotalCurrency = -1;


        public override void setLeadeBoarList(List<LeaderboardElement> leaderboardElement)
        {
            this.leaderboardElement = leaderboardElement;

            for (int i = 0; i < leaderboardElement.Count; ++i)
            {
                if (leaderboardElement[i].isLocal)
                {
                    localPlayer = leaderboardElement[i];
                }
            }
        }
        //Shows the modal, starts the count down and calls Setup()
        public override void ShowModel()
		{
			base.ShowModel();
			//m_RewardParent.SetActive(false);
			GetComponentInParent<Canvas>().worldCamera = Camera.main;
			m_CountDown.StartCountDown(10, FinishedCountDown);
			
			Setup();
            setData();
 

        }

        void setData()
        {
            MConstants.isToShowAd = true;
            PlayerDataController.instance.playerStats.multiplayerLevel += 1;
            PlayerDataController.instance.SaveData();
        }
        /// <summary>
        /// Setup this instance.
        /// </summary>
        protected virtual void Setup()
		{
			//cache game settings
			//If this is the server then the player rank is correct and the currency can be awarded immediately
            SetupAwardDisplay();
		}
        
		/// <summary>
		/// Awards the currency - only once
		/// </summary>
		

		/// <summary>
		/// Awards the currency and unsubscribe from the event
		/// </summary>
		

		//Sets up the award display
		private void SetupAwardDisplay()
		{
            if (m_HasAwardedCurrency)
            {
                return;

                    
            }
            m_HasAwardedCurrency = true;
            m_RewardParent.SetActive(true);
			m_AwardText.text = GetAwardText(localPlayer.rank);
			m_AwardCurrency =GetAwardAmount(localPlayer.rank);
			m_AwardAmountText.text = m_AwardCurrency.ToString();
            int rewardCash = Random.Range(500, 1000);
            int xp = (int)(300/localPlayer.rank);
            PlayerDataController.instance.playerStats.xpoints += xp;
            //Debug.Log("SetupAwardDisplay " + xp);
            
            if (PlayerDataController.instance.playerStats.xpoints > PlayerDataController.instance.playerStats.Rank * 1000)
            {
                MainMenuManager.isRankUp = true;
            }
            PlayerDataController.instance.playerStats.PlayerGold += m_AwardCurrency;
            PlayerDataController.instance.playerStats.PlayerCash += rewardCash;
            MConstants.isPlayerWin = false;
            if (localPlayer.rank <= 1)
            {
                MConstants.isPlayerWin = true;
            }
            if (MultiPlayerGame.isSinglePlayer)
            {
                if (localPlayer.rank <= 1)
                {
                    PlayerDataController.instance.playerStats.LastWinCount++;
                }
                else
                {
                    PlayerDataController.instance.playerStats.LastWinCount--;
                }
                "1".Show("Result");
                if (MConstants.isPlayerWin)
                {
                    "2".Show("Result");

                    //APIManager.instance.WinnerLossAIBet(staticVariables.UserProfiledata.user._id.ToString());
                  //  winLoseHorse.WinPlayer(true,staticVariables .UserProfiledata.user._id.ToString());
                }
                else
                {
                    "3".Show("Result");

                    // APIManager.instance.WinnerLossAIBet("ai");
                  //  winLoseHorse.WinPlayer(false, "ai");
                }
                
            }
            else
            {
                "Multiplayer Games hai".Show();
                MConstants.isPlayerWin.Show("value");
                if (MConstants.isPlayerWin)
                {
                "4".Show("Result");
                   // winLoseHorse.WinPlayer(true,staticVariables.UserProfiledata.user._id.ToString());
                }
                else
                {
                    "5".Show("Result");
                    "You Lose".Show("STATUS");

                 //   winLoseHorse.WinPlayer(false, staticVariables.UserProfiledata.user._id.ToString());

                }
            }
            PlayerDataController.instance.SaveData();
            if (m_TotalCurrency < 0)
            {
                m_TotalCurrency = PlayerDataController.instance.playerStats.PlayerGold;
            }

         
            m_CurrencyDisplay.GetComponent<Text>().text = m_TotalCurrency.ToString();
			m_IsDelaying = true;

			
		}

        public void WinStatus(string id)
        {

        }
       
        //Handles delayed amimation call
        private void Update()
		{
         
            if (m_IsDelaying)
			{
				m_DelayCounter += Time.deltaTime;
				if (m_DelayCounter >= m_DelayBeforeCurrencyAnim)
				{
					CurrencyAnimation();
					m_IsDelaying = false;
				}
			}
		}
		
		//Plays the currency animation - namely particles and the incremental number display
		private void CurrencyAnimation()
		{
			//m_CurrencyDisplay.SetTargetValue(m_TotalCurrency, m_TotalCurrency + m_AwardCurrency, 1.5f);
			m_RewardParticleSystem.Play();

			//UIAudioManager.s_Instance.PlayCoinSound();
		}

		//Fired by the countdown event
		private void FinishedCountDown()
		{
			CompleteGame();
			
		}

     
		public override void CompleteGame()
        {
            //Photon Removal PhotonNetwork.LeaveRoom();
        }




        protected int LeaderboardSort(LeaderboardElement player1, LeaderboardElement player2)
        {
            return player2.score - player1.score;
        }

        public virtual string GetAwardText(int rank)
        {
            string[] rankSuffix = new string[] { "st", "nd", "rd", "th","th" };
            return string.Format("Your Position {0}{1}", rank, rankSuffix[rank - 1]);
        }

        public virtual int GetAwardAmount(int rank)
        {
            return Mathf.FloorToInt(400 / Mathf.Pow(2f, (float)(rank - 1)));
        }
    }
}
