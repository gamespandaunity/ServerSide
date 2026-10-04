using UnityEngine;
using System.Collections;
using UnityEngine.SocialPlatforms;

public class SocialManager : MonoBehaviour {
	public static SocialManager instance;
	#region PUBLIC_VAR
	[HideInInspector]
	public string leaderboard;
	#endregion
	#region DEFAULT_UNITY_CALLBACKS
	void Awake(){
		instance = this;
	}
	void Start ()
	{
		leaderboard = LeaderBoard.leaderboard_rank;
		// recommended for debugging:
		//PlayGamesPlatform.DebugLogEnabled = true;

		// Activate the Google Play Games platform
		//PlayGamesPlatform.Activate ();
		LogIn ();
		DontDestroyOnLoad (this);
	}
	#endregion
	#region BUTTON_CALLBACKS
	/// <summary>
	/// Login In Into Your Google+ Account
	/// </summary>
	public void LogIn ()
	{
		Social.localUser.Authenticate ((bool success) =>
			{
				if (success) {
					Debug.Log ("Login Sucess");
				} else {
					Debug.Log ("Login failed");
				}
			});
	}
	/// <summary>
	/// Shows All Available Leaderborad
	/// </summary>
	public void OnShowLeaderBoard ()
	{
		//PlayGamesPlatform.Activate ();
		//LogIn ();
		////        Social.ShowLeaderboardUI (); // Show all leaderboard
		//((PlayGamesPlatform)Social.Active).ShowLeaderboardUI (leaderboard); // Show current (Active) leaderboard
	}
	/// <summary>
	/// Adds Score To leader board
	/// </summary>
	public void OnAddScoreToLeaderBorad (int score)
	{
		if (Social.localUser.authenticated) {
			Social.ReportScore (score, leaderboard, (bool success) =>
				{
					if (success) {
						Debug.Log ("Update Score Success");

					} else {
						Debug.Log ("Update Score Fail");
					}
				});
		}
	}
	/// <summary>
	/// On Logout of your Google+ Account
	/// </summary>
	public void OnLogOut ()
	{
		//((PlayGamesPlatform)Social.Active).SignOut ();
	}
	#endregion
}
