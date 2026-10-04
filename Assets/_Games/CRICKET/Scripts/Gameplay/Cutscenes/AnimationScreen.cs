using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class AnimationScreen : Singleton<AnimationScreen>
{
	public Sprite[] numberSprites;

	public Image number;

	public GameObject animObj;

	public GameObject animRed1;

	public GameObject animRed2;

	public Image Red1;

	public Image Red2;

	private int animType;

	private Transform _transform;

	protected void Awake()
	{
		Hide(boolean: true);
	}

	public void StartAnimation(int type)
	{
		// The 30-07 logs show ShowMe firing MORE often than InitAnimation inside a single visible window
		// (e.g. one InitAnimation type=0 followed by two ShowMe state=4 with no hide between), which matches
		// the tester's "pehle 4 ka panel aya, phir scorecards, phir 4 ka panel aya dono sides py". ShowMe has
		// exactly one caller — this method — and this method has exactly two call sites, both mutually
		// exclusive branches of InitAnimation, so the counts should match and they do not. Log the entry so
		// the next round shows unambiguously whether StartAnimation is entered twice or ShowMe is reached by
		// some other route.
		ConstantsData_M.MpLog($"[BoundaryBanner] StartAnimation ENTER type={type} pendingStartSA={IsInvoking("StartSA")}");
		// CancelInvoke first: this Invoke was unguarded, so a second entry stacked a second StartSA and the
		// pair of StopAnimation coroutines could show/hide the panel out of order. Re-arming a single timer is
		// the correct behaviour either way — the newest animation owns the teardown.
		CancelInvoke("StartSA");
		Singleton<BoundaryAnimation2>.instance.ShowMe(type);
		Invoke("StartSA", 2.5f);
	}

	public void StartSA()
	{
		StartCoroutine(StopAnimation());
	}

	public IEnumerator StopAnimation()
	{
		bool isOverStepBall = Singleton<GroundController>.instance.getOverStepBall();
		if (CONTROLLER.gameMode == "WPL" && (animType == 0 || animType == 1) && !isOverStepBall)
		{
			yield return new WaitForSeconds(2f);
		}
		if (isOverStepBall && (animType == 0 || animType == 1))
		{
			StartCoroutine(Singleton<GroundController>.instance.updateBoundaryBall());
		}
		else
		{
			Singleton<GameData>.instance.AnimationCompleted();
		}
	}

	public void Hide(bool boolean)
	{
		if (boolean)
		{
			animObj.SetActive(value: false);
		}
		else
		{
			animObj.SetActive(value: true);
		}
	}
}
