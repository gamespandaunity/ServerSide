using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tanks.UI
{
	//Base class for all modals
	public class Modal : MonoBehaviour
	{
        [FormerlySerializedAs("m_CanvasGroup")][SerializeField] protected CanvasGroup CanvasGroup;


        public virtual void CloseModal()
		{
			gameObject.SetActive(false);
		}

		public virtual void ShowModel()
		{
			gameObject.SetActive(true);
			EnableInteractivity();
		}

     

        protected virtual void EnableInteractivity()
		{
			if (CanvasGroup != null)
			{
				CanvasGroup.interactable = true;
			}
		}

		protected virtual void DisableInteractivity()
		{
			if (CanvasGroup != null)
			{
				CanvasGroup.interactable = false;
			}
		}
	}
}