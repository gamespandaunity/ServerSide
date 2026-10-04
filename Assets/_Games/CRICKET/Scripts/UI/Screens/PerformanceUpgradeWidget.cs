using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
using UnityEngine;
using Cricket;

public class PerformanceUpgradeWidget : Singleton<PerformanceUpgradeWidget>
{
	private bool PGTimerShow;

	private bool CGTimerShow;

	private bool AGTimerShow;

	private bool PGUpgradeShow;

	private bool CGUpgradeShow;

	private bool AGUpgradeShow;

	private GameObject sideScreen;

	private GameObject mainScreen;

	private GameObject tempMainScreen;

	private float sideScreenPos;

	private float mainScreenPos;

	public GameObject PGTimer;

	public GameObject PGUpgrade;

	public GameObject CGTimer;

	public GameObject CGUpgrade;

	public GameObject AGTimer;

	public GameObject AGUpgrade;

	public GameObject DefaultSlide;

	public GameObject PowerPanel;

	public GameObject ControlPanel;

	public GameObject AgilityPanel;

	private int[] XPmilestones = new int[10] { 0, 5000, 10000, 20000, 35000, 55000, 80000, 110000, 145000, 185000 };

	private string[] UpgradePrefs = new string[3] { "PowerUpgradeTimer", "ControlUpgradeTimer", "AgilityUpgradeTimer" };

	
}
