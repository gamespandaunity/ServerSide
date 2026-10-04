using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Snake_Ladder;
using Mirror;
namespace Twelve
{
    public class SetGamePlayAvatarTwelve : NetworkBehaviour
{
    public TMP_Text playerNameText;
    public Image Avatar;
    public Image imageToFill;
    public float fillDuration = 10f;
    public Tween imageFillTween;
    private void OnEnable()
    {
        NetworkManagerTwelve.Instance.TurnChanged += TimerAnimation;
    }
    private void OnDisable()
    {
        NetworkManagerTwelve.Instance.TurnChanged -= TimerAnimation;
    }
    // Start is called before the first frame update
    void Start()
    {
            //Avatar.sprite = ReferenceManager.Instance.Avatars[GameManager.instance.AvatarId];
            //playerNameText.text = GameManager.instance.UserName;
            //Wasi  if (photonView.IsMine)
        if (isLocalPlayer)
        {
           //Wasi   transform.SetParent(FusionGameReferenceManager.Instance.Player1AvatarGP);
            transform.localPosition = Vector3.zero;
            Debug.Log("me top pe show hnga");
        }
        else
        {
           //Wasi   transform.SetParent(FusionGameReferenceManager.Instance.Player2AvatarGP);
            transform.localPosition = Vector3.zero;
            Debug.Log("me bottom pe show hnga");
        }
 
    }
    void SetAvatarInfo(string name, int index)
    {
      //Wasi    Avatar.sprite = FusionGameReferenceManager.Instance.Avatars[index];
        playerNameText.text = name;
    }
    public void BroadCastGamePlayUI(string name, int index)
    {
        Debug.Log("Broadcasting");
        // The obsolete avatar RPC was removed; current player data is owned by the match manager.
        SetAvatarInfo(name, index);
        //Wasi photonView.RPC(nameof(setAvatarInfo), RpcTarget.AllBuffered, name, index);
    }
    void TimerAnimation()
    {
        if(GamePlayControllerTwelve.currentPlayerTurn== PLAYERS.PLAYER1)
            {
        imageToFill.fillAmount = 0f;
        imageFillTween =  imageToFill.DOFillAmount(1f, fillDuration).SetEase(Ease.Linear)
            .OnComplete(() => OnFillComplete());
        }
        else
        {
            imageFillTween.Kill();
        }
    }

    // Function to call when the fill animation completes
    void OnFillComplete()
    {
        GameControllerTwelve.instance.SwitchPlayerTurn();
    }

}
}
