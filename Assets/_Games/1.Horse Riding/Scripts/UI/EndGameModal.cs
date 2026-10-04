using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Tanks.UI
{
	/// <summary>
	/// Base class for summary modals that are displayed at the end of a game.
	/// </summary>
	public class EndGameModal : Modal
	{
		//Internal reference to continue button.
		[SerializeField]
		protected Button m_ContinueButton;

		//Internal reference to the title box for this modal.
		[SerializeField]
		protected Text m_TitleTextbox;

		

		/// <summary>
		/// Displays this modal.
		/// </summary>
		public override void ShowModel()
		{
			base.ShowModel();

			//Endgame modals have particle systems attached, which means they must exist in camera space. Attach this modal to the main camera and ensure its depth is correct.
			Canvas parentCanvas = GetComponentInParent<Canvas>();

			parentCanvas.worldCamera = Camera.main;
			parentCanvas.planeDistance = 32;
		}

		

		public virtual void SetEndMessage(string message)
		{
		}

        public virtual void setLeadeBoarList(List<LeaderboardElement> leaderboardElement)
        {

        }
        /// <summary>
        /// Continue button event. Signals the rulesprocessor to end the game and closes the modal.
        /// </summary>
        public virtual void OnContinueClick()
		{
			CompleteGame();
			CloseModal();
		}

        /// <summary>
		/// Handles the game being complete (including the transitions)
		/// </summary>
		public virtual void CompleteGame()
        {
        }
        /// <summary>
        /// Sets the title text.
        /// </summary>
        /// <param name="titleText">Modal title text.</param>
        protected void SetTitleText(string titleText)
		{
			if (m_TitleTextbox != null)
			{
				m_TitleTextbox.text = titleText;
			}
		}
	}
}
