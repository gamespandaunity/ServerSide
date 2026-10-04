using UnityEngine;
namespace Snake_Ladder
{
    public class GamePlayEventListener : MonoBehaviour
    {
        //public GameObject avatarPrefab
        public GameObject GamePlay;
        private void OnEnable()
        {
           // NetworkManager.Instance.InstantiateAvatarIn_GP();
          //  NetworkManager.Instance.StartGame += CheckConnectivity;
          //  NetworkManager.Instance.GameEnded += GoToMenue;
            // NetworkManager.Instance.TurnChanged += ChangeTurn;
            GamePlay.SetActive(true);
            GameController.instance.SetTurnAndBoard();
        }
        private void OnDisable()
        {
            //GamePlay.SetActive(false);
         //   NetworkManager.Instance.StartGame -= CheckConnectivity;
            // NetworkManager.Instance.TurnChanged -= ChangeTurn;
        //    NetworkManager.Instance.GameEnded -= GoToMenue;
        }
        void ChangeTurn()
        {
            GameController.instance.ChangeTurn();
        }
        public void OnClickMainMenue()
        {
            Debug.Log("Going to main Menue");
            GoToMenue();
        }
        void CheckConnectivity()
        {
         //   NetworkManager.Instance.OnMasterClientDisconnected();
        }
        void GoToMenue()
        {
            MenuManager.Instance.ChangeState(MenuManager.AllMenus.ModeSelectionScreen);
        }
    }
}