using UnityEngine;
using System.Collections;
using Tanks.Utilities;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace Tanks.UI
{
	/// <summary>
	/// This class controls a generic modal object used for generic status popups in the UI.
	/// </summary>
	public class AnnouncerModal : Singleton<AnnouncerModal>
	{
        [FormerlySerializedAs("m_Body")][SerializeField] protected Text bodyText;
        [FormerlySerializedAs("m_Heading")][SerializeField] protected Text headingText;


        protected override void Awake()
		{
			base.Awake();
			gameObject.SetActive(false);
		}

		public void ShowMessage(string heading, string body)
		{
			gameObject.SetActive(true);
			if (this.bodyText != null)
			{
				this.bodyText.text = body;
			}

			if (this.headingText != null)
			{
				this.headingText.text = heading;
			}	
		}

		public void HideMessage()
		{
			gameObject.SetActive(false);
		}
	}
}