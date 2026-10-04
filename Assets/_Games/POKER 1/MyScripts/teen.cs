using UnityEngine;
using DG.Tweening;
//

namespace POKER
{
    public class teen : MonoBehaviour
    {
        public CardProperty card0;
        public CardProperty card1;
        public CardProperty card2;

        private void Start()
        {
            PlayerCardsRankAndScoreCalc.CalculateRankAndScores(card0, card1, card2);
            print("Your rank is: " + PlayerCardsRankAndScoreCalc.Rank + "          Your Score: " + PlayerCardsRankAndScoreCalc.Scores);
        }

        //DOTweenAnimation anim;
        // Start is called before the first frame update
        //private void Start()
        //{
        //    //anim = GetComponent<DOTweenAnimation>();
        //}
        //public void PlayAnim()
        //{
        //    PlayAnimation(cardAnim, TargetObj, null);
        //}
        //public void PlayAnimation(Transform ObjToAnimate, GameObject targetPosition, GameObject ObjToActivate)
        //{
        //    //cardAnim.DOPlayAllById("1");

        //    ObjToAnimate.DOMove(targetPosition.transform.position, 0.5f, false).OnComplete(() => OnCompleteAnim(ObjToAnimate.gameObject, ObjToActivate));
        //    ObjToAnimate.DOLocalRotate(new Vector3(0, 0, 360), 0.5f, RotateMode.FastBeyond360);
        //}

        //void OnCompleteAnim(GameObject AnimatedObj, GameObject ObjToActivate)
        //{
        //    AnimatedObj.gameObject.SetActive(false);
        //    if (ObjToActivate != null)
        //        ObjToActivate.gameObject.SetActive(true);

        //}
    }
}