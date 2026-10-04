
using Mirror;
using UnityEngine;
using UnityEngine.Events;
namespace BEKStudio
{
    public class PuckHole : NetworkBehaviour
{
    public int index;
    public Transform ring;
    public Transform targetPosition;
    public RectTransform topLeftPos;
    public RectTransform topRightPos;

    private bool isSwitchingMaster;

    private void OnEnable()
    {
        Invoke(nameof(Delay),1f);
    }

    void Delay()
    { 
        GameController.Instance.Event_SwitchingMasterClient += OnSwitchingMasterClient;
        GameController.Instance.Event_MasterClientSwithced += OnMasterClientSwitched;
    }

    private void OnDisable()
    {
        GameController.Instance.Event_SwitchingMasterClient -= OnSwitchingMasterClient;
        GameController.Instance.Event_MasterClientSwithced -= OnMasterClientSwitched;
    }

    void OnSwitchingMasterClient() => isSwitchingMaster = true;

    void OnMasterClientSwitched() => isSwitchingMaster = false;

    public void PutPuckOnPuckHole(GameObject puck)
    {
        //Meaning this happen when the states are syncing 
        if (isSwitchingMaster) return;

        LeanTween.move(puck, targetPosition.position, 0.2f).setOnComplete(() => OnComplete(puck));
    }

    void OnComplete(GameObject puck)
    {
        ring.localScale = Vector3.zero;
        LeanTween.scale(ring.gameObject, Vector3.one, 0.5f).setOnComplete(() =>
        {
            ring.localScale = Vector3.zero;
        });

        string puckTag = puck.tag;
        SpriteRenderer spriteRenderer = puck.GetComponent<SpriteRenderer>();
        Color color = spriteRenderer.color;
        color.a = 0f;
        spriteRenderer.color = color;

        GameObject puckToAnimate;

        if (puckTag == "White")
        {
            puckToAnimate = GameController.Instance.WhitePool.Retrieve();
            puckToAnimate.transform.position = puck.transform.position;
            puckToAnimate.SetActive(true);
            LeanTween.move(puckToAnimate, topLeftPos.position, 0.8f).setOnComplete(() =>
            {
                color.a = 0f;
                spriteRenderer.color = color;
                GameController.Instance.WhitePool.Restore(puckToAnimate);
                GameController.Instance.IncScoreOnCollected(true);
                //puck.GetComponent<PhotonTransformView>().enabled = true;
            });
        }
        else if (puckTag == "Black")
        {
            puckToAnimate = GameController.Instance.BlackPool.Retrieve();
            puckToAnimate.transform.position = puck.transform.position;
            puckToAnimate.SetActive(true);
            LeanTween.move(puckToAnimate, topRightPos.position, 0.8f).setOnComplete(() =>
            {
                color.a = 0f;
                spriteRenderer.color = color;
                GameController.Instance.BlackPool.Restore(puckToAnimate);
                GameController.Instance.IncScoreOnCollected(false);
                //puck.GetComponent<PhotonTransformView>().enabled = true;
            });
        }

        else
        {
            puckToAnimate = GameController.Instance.redPool.Retrieve();
            puckToAnimate.transform.position = puck.transform.position;
            puckToAnimate.SetActive(true);
            LeanTween.move(puckToAnimate, GameController.Instance.masterClientTag == "White" ? topLeftPos.position : topRightPos.position, 0.8f).setOnComplete(() =>
            {
                if (GameController.Instance.masterClientTag == "White")
                {
                    GameController.Instance.leftRedPuckIcon.SetActive(true);
                }
                else
                {
                    GameController.Instance.rightRedPuckIcon.SetActive(true);
                }
                color.a = 0f;
                spriteRenderer.color = color;
                GameController.Instance.redPool.Restore(puckToAnimate);
                //puck.GetComponent<PhotonTransformView>().enabled = true;
            });
        }
    }

    public void PutStrikerOnPuckHole(GameObject striker, UnityAction callback)
    {
        LeanTween.move(striker, targetPosition.position, 0.3f).setOnComplete(() =>
        {
            callback?.Invoke();
        });
    }
}
}
