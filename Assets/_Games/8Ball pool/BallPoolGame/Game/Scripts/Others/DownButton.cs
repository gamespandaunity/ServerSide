using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NetworkManagement
{
    public delegate void ButtonDownHandler(NetworkManagement.DownButton button);

    [System.Serializable]
    public class Sender
    {
        public string message;
        public Component sender;

        public void SendMessage()
        {
            sender.SendMessage(message);
        }
    }

    public class DownButton : MonoBehaviour, IPointerDownHandler
    {
        public event ButtonDownHandler OnDown;

        public List<Sender> senders;

        #region IPointerDownHandler implementation

        public void OnPointerDown(PointerEventData eventData)
        {
            foreach (Sender sender in senders)
            {
                sender.SendMessage();
            }
            if (OnDown != null)
            {
                OnDown(this);
            }
        }

        #endregion
    }
}
