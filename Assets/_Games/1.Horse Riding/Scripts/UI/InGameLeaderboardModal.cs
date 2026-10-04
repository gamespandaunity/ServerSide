using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Tanks.Utilities;

namespace Tanks.UI
{
	/// <summary>
	/// Modal to display leaderboard reflecting current in-game scores.
	/// </summary>
	public class InGameLeaderboardModal : Singleton<InGameLeaderboardModal>
	{

		[SerializeField]
		protected Text m_Heading;


		protected override void Awake()
		{
			base.Awake();
			Hide();
		}

		/// <summary>
		/// Displays the modal.
		/// </summary>
		/// <param name="text">Text to display as modal header.</param>
		public void Show(string text)
		{
			gameObject.SetActive(true);
			LazyLoad();
			m_Heading.text = text;
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		protected void LazyLoad()
		{
			

			
		}

	}
}