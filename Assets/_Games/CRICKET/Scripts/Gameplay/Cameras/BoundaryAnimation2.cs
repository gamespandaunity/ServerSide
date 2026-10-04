namespace Cricket 
{
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BoundaryAnimation2 : Singleton<BoundaryAnimation2>
{
	public Image result;

	public Sprite[] resultSprites;

	public GameObject Holder;

	public Transform[] bigFlareRef;

	public Transform[] bigFlare;

	public Transform[] smallFlare;

	public Transform[] smallFlareRef;

	private TweenCallback AnimationCompletedCallback;

	private void Start()
	{
		AnimationCompletedCallback = delegate
		{
			HideMe();
		};
	}

	private void ResetTransition()
	{
		int num = 0;
		for (num = 0; num < bigFlare.Length; num++)
		{
			bigFlare[num].localPosition = bigFlareRef[num].localPosition;
			smallFlare[num].localPosition = smallFlareRef[num].localPosition;
		}
		result.transform.DOLocalRotate(new Vector3(0f, 180f, 0f), 0f).SetUpdate(isIndependentUpdate: true);
		result.transform.localScale = new Vector3(0f, 1.5f, 1.5f);
	}

	public void PanelTransition()
	{
		ResetTransition();
		Sequence sequence = DOTween.Sequence();
		sequence.Insert(0f, result.transform.DOLocalRotate(new Vector3(0f, 0f, 0f), 0.3f));
		sequence.Insert(0f, result.transform.DOScaleX(1.5f, 0.4f));
		sequence.Insert(0.35f, result.transform.DOScale(1.65f, 4.2f));
		sequence.InsertCallback(2.5f, AnimationCompletedCallback);
		sequence.SetUpdate(isIndependentUpdate: true);
		sequence.SetLoops(1);
	}

	public void ShowMe(int index)
	{
		// TWO independent paths show this banner on the BOWLING side, and both now fire for the same
		// delivery:
		//   1. RpcBallOutcome -> UpdateCurrentBallFromNetwork -> UpdateCurrentBall -> the scoring switch ->
		//      InitAnimation -> AnimationScreen.StartAnimation -> here.
		//   2. the explicit fallback at the tail of that same RpcBallOutcome, added when path 1 was believed
		//      to drop the banner on the bowling side.
		// The 30-07 ritu_mp logs settle it: InitAnimation=34 and StartAnimation ENTER=34, but ShowMe=62 —
		// ~28 extra shows, and per boundary the sequence is one StartAnimation ENTER followed by TWO ShowMe
		// (then two HideMe). That is the tester's "no ball py 4 ho to 4 ka panel do dfa ata hai", and on the
		// last ball of an over the second show lands after the over-end scorecards ("phir 4 ka panel aya").
		// Suppressing a re-show while the panel is ALREADY up collapses exactly the duplicate and nothing
		// else: path 2 runs after path 1 in the same RPC, so the Holder is still active; a genuine second
		// boundary always has a HideMe in between, and if path 1 ever really does drop the banner the
		// fallback still works because the Holder is inactive.
		if (Holder != null && Holder.activeSelf)
		{
			ConstantsData_M.MpLog($"[BoundaryBanner] ShowMe SUPPRESSED — panel already up (duplicate show for the same delivery) type={(index == 1 ? "SIX" : "FOUR")}");
			return;
		}
		ConstantsData_M.MpLog($"[BoundaryBanner] ShowMe type={(index == 1 ? "SIX" : "FOUR")} state={(Singleton<GroundController>.instance != null ? Singleton<GroundController>.instance.currentActionState : -99)}");
		result.sprite = resultSprites[index];
		Holder.SetActive(value: true);
		PanelTransition();
	}

	public void HideMe()
	{
		// Paired with the ShowMe log above so a show-without-hide is visible in the tester log.
		ConstantsData_M.MpLog("[BoundaryBanner] HideMe — panel torn down.");
		Holder.SetActive(value: false);
		ResetTransition();
	}
}

}