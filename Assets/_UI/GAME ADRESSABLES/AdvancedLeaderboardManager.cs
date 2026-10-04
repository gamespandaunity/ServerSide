// using UnityEngine;
// using Firebase;
// using Firebase.Database;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using System;
// using UnityEngine.UI;
// using TMPro;

// public class AdvancedLeaderboardManager : MonoBehaviour
// {
//     [Header("Firebase Configuration")]
//     [SerializeField] private string databaseURL = "YOUR_FIREBASE_DATABASE_URL";
//     [SerializeField] private int maxScores = 10;
//     [SerializeField] private int scoreUpdateInterval = 30; // seconds

//     [Header("UI References")]
//     [SerializeField] private Transform contentParent;
//     [SerializeField] private GameObject leaderboardEntryPrefab;
//     [SerializeField] private TMP_Text statusText;
//     [SerializeField] private Button refreshButton;
//     [SerializeField] private TMP_Text lastUpdateText;

//     [Header("Filtering Options")]
//     [SerializeField] private TMP_Dropdown timeframeDropdown;
//     [SerializeField] private TMP_Dropdown regionDropdown;

//     private DatabaseReference databaseReference;
//     private const string LEADERBOARD_KEY = "leaderboard";
//     private bool isInitialized = false;
//     private DateTime lastUpdateTime;
//     private Dictionary<string, PlayerScore> cachedScores = new Dictionary<string, PlayerScore>();
//     private System.Threading.CancellationTokenSource updateCancellationToken;

//     [System.Serializable]
//     public class PlayerScore
//     {
//         public string playerName;
//         public int score;
//         public string userId;
//         public string region;
//         public string deviceId;
//         public long timestamp;
//         public Dictionary<string, int> achievements;
//         public int gamesPlayed;
//         public float averageScore;
//         public string lastPlayedDate;
//         public int rank;
//         public bool isValid = true;

//         public PlayerScore(string playerName, int score, string userId, string region = "global")
//         {
//             this.playerName = playerName;
//             this.score = score;
//             this.userId = userId;
//             this.region = region;
//             this.deviceId = SystemInfo.deviceUniqueIdentifier;
//             this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//             this.achievements = new Dictionary<string, int>();
//             this.gamesPlayed = 1;
//             this.averageScore = score;
//             this.lastPlayedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
//         }
//     }

//     private void Awake()
//     {
//         InitializeFirebase();
//         if (refreshButton != null)
//             refreshButton.onClick.AddListener(async () => await RefreshLeaderboard());

//         if (timeframeDropdown != null)
//             timeframeDropdown.onValueChanged.AddListener(async (value) => await RefreshLeaderboard());

//         if (regionDropdown != null)
//             regionDropdown.onValueChanged.AddListener(async (value) => await RefreshLeaderboard());
//     }

//     private async void InitializeFirebase()
//     {
//         try
//         {
//             FirebaseApp.DefaultInstance.Options.DatabaseUrl = new System.Uri(databaseURL);
//             databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
//             isInitialized = true;
//             UpdateStatus("Firebase initialized successfully");
//             await RefreshLeaderboard();
//             StartAutomaticUpdates();
//         }
//         catch (Exception ex)
//         {
//             UpdateStatus($"Firebase initialization failed: {ex.Message}");
//             //Constants_M.Log($"Firebase initialization error: {ex}");
//         }
//     }

//     private void StartAutomaticUpdates()
//     {
//         updateCancellationToken = new System.Threading.CancellationTokenSource();
//         _ = AutomaticUpdateRoutine();
//     }

//     private async Task AutomaticUpdateRoutine()
//     {
//         while (!updateCancellationToken.Token.IsCancellationRequested)
//         {
//             await Task.Delay(scoreUpdateInterval * 1000);
//             if (!updateCancellationToken.Token.IsCancellationRequested)
//             {
//                 await RefreshLeaderboard();
//             }
//         }
//     }

//     public async Task SubmitScore(string playerName, int score, string userId, string region = "global")
//     {
//         if (!isInitialized)
//         {
//             UpdateStatus("Firebase not initialized");
//             return;
//         }

//         try
//         {
//             // Check for existing score
//             var existingScore = await GetPlayerScore(userId);
//             PlayerScore newScore;

//             if (existingScore != null)
//             {
//                 // Update existing score
//                 existingScore.score = Math.Max(existingScore.score, score);
//                 existingScore.gamesPlayed++;
//                 existingScore.averageScore = ((existingScore.averageScore * (existingScore.gamesPlayed - 1)) + score) / existingScore.gamesPlayed;
//                 existingScore.lastPlayedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
//                 newScore = existingScore;
//             }
//             else
//             {
//                 newScore = new PlayerScore(playerName, score, userId, region);
//             }

//             string json = JsonUtility.ToJson(newScore);

//             await databaseReference.Child(LEADERBOARD_KEY)
//                 .Child(userId)
//                 .SetRawJsonValueAsync(json);

//             await UpdateTopScores();
//             UpdateStatus("Score submitted successfully");
//         }
//         catch (Exception ex)
//         {
//             UpdateStatus($"Error submitting score: {ex.Message}");
//             //Constants_M.Log($"Score submission error: {ex}");
//         }
//     }

//     private async Task UpdateTopScores()
//     {
//         try
//         {
//             var timeframe = timeframeDropdown != null ? timeframeDropdown.value : 0;
//             var region = regionDropdown != null ? regionDropdown.options[regionDropdown.value].text : "global";

//             var snapshot = await databaseReference.Child(LEADERBOARD_KEY)
//                 .OrderByChild("score")
//                 .GetValueAsync();

//             List<PlayerScore> allScores = new List<PlayerScore>();
//             long cutoffTime = GetTimeframeCutoff(timeframe);

//             foreach (var childSnapshot in snapshot.Children.Reverse())
//             {
//                 PlayerScore score = JsonUtility.FromJson<PlayerScore>(childSnapshot.GetRawJsonValue());

//                 // Apply filters
//                 if (score.timestamp >= cutoffTime &&
//                     (region == "global" || score.region == region) &&
//                     ValidateScore(score))
//                 {
//                     score.rank = allScores.Count + 1;
//                     allScores.Add(score);
//                 }
//             }

//             var topScores = allScores.Take(maxScores).ToList();
//             await SaveTopScores(topScores, timeframe, region);

//             cachedScores = topScores.ToDictionary(s => s.userId, s => s);
//             lastUpdateTime = DateTime.Now;
//             UpdateLastUpdateTimeUI();
//         }
//         catch (Exception ex)
//         {
//             UpdateStatus($"Error updating top scores: {ex.Message}");
//             //Constants_M.Log($"Top scores update error: {ex}");
//         }
//     }

//     private bool ValidateScore(PlayerScore score)
//     {
//         // Basic validation
//         if (score.score < 0 || score.score > 999999999) return false;
//         if (string.IsNullOrEmpty(score.userId)) return false;
//         if (score.timestamp > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return false;

//         // Add more validation rules as needed
//         return true;
//     }

//     private async Task SaveTopScores(List<PlayerScore> topScores, int timeframe, string region)
//     {
//         var key = $"topScores_{timeframe}_{region}";
//         await databaseReference.Child(key)
//             .SetRawJsonValueAsync(JsonUtility.ToJson(new { scores = topScores }));
//     }

//     public async Task<List<PlayerScore>> GetTopScores()
//     {
//         try
//         {
//             var timeframe = timeframeDropdown != null ? timeframeDropdown.value : 0;
//             var region = regionDropdown != null ? regionDropdown.options[regionDropdown.value].text : "global";
//             var key = $"topScores_{timeframe}_{region}";

//             var snapshot = await databaseReference.Child(key).GetValueAsync();

//             if (snapshot.Exists)
//             {
//                 string json = snapshot.GetRawJsonValue();
//                 var wrapper = JsonUtility.FromJson<PlayerScoreWrapper>(json);
//                 return wrapper.scores.ToList();
//             }
//         }
//         catch (Exception ex)
//         {
//             UpdateStatus($"Error getting top scores: {ex.Message}");
//             //Constants_M.Log($"Get top scores error: {ex}");
//         }

//         return new List<PlayerScore>();
//     }

//     public async Task<PlayerScore> GetPlayerScore(string userId)
//     {
//         try
//         {
//             var snapshot = await databaseReference.Child(LEADERBOARD_KEY)
//                 .Child(userId)
//                 .GetValueAsync();

//             if (snapshot.Exists)
//             {
//                 return JsonUtility.FromJson<PlayerScore>(snapshot.GetRawJsonValue());
//             }
//         }
//         catch (Exception ex)
//         {
//             //Constants_M.Log($"Get player score error: {ex}");
//         }

//         return null;
//     }

//     public async Task UpdatePlayerAchievement(string userId, string achievementId, int progress)
//     {
//         try
//         {
//             var playerScore = await GetPlayerScore(userId);
//             if (playerScore != null)
//             {
//                 if (playerScore.achievements == null)
//                     playerScore.achievements = new Dictionary<string, int>();

//                 playerScore.achievements[achievementId] = progress;

//                 await databaseReference.Child(LEADERBOARD_KEY)
//                     .Child(userId)
//                     .SetRawJsonValueAsync(JsonUtility.ToJson(playerScore));
//             }
//         }
//         catch (Exception ex)
//         {
//             UpdateStatus($"Error updating achievement: {ex.Message}");
//             //Constants_M.Log($"Achievement update error: {ex}");
//         }
//     }

//     public async Task RefreshLeaderboard()
//     {
//         if (!isInitialized) return;

//         UpdateStatus("Refreshing leaderboard...");
//         await UpdateTopScores();
//         await DisplayLeaderboard();
//         UpdateStatus("Leaderboard refreshed");
//     }

//     public async Task DisplayLeaderboard()
//     {
//         if (contentParent == null) return;

//         try
//         {
//             var topScores = await GetTopScores();

//             foreach (Transform child in contentParent)
//             {
//                 Destroy(child.gameObject);
//             }

//             foreach (var score in topScores)
//             {
//                 if (leaderboardEntryPrefab != null)
//                 {
//                     var entry = Instantiate(leaderboardEntryPrefab, contentParent);
//                     var entryUI = entry.GetComponent<LeaderboardEntryUI>();
//                     if (entryUI != null)
//                     {
//                         entryUI.SetValues(score);
//                     }
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             UpdateStatus($"Error displaying leaderboard: {ex.Message}");
//             //Constants_M.Log($"Display leaderboard error: {ex}");
//         }
//     }

//     private void UpdateStatus(string message)
//     {
//         if (statusText != null)
//             statusText.text = message;
//         //Debug.Log($"Leaderboard status: {message}");
//     }

//     private void UpdateLastUpdateTimeUI()
//     {
//         if (lastUpdateText != null)
//             lastUpdateText.text = $"Last Updated: {lastUpdateTime.ToString("HH:mm:ss")}";
//     }

//     private long GetTimeframeCutoff(int timeframeIndex)
//     {
//         var now = DateTimeOffset.UtcNow;
//         switch (timeframeIndex)
//         {
//             case 0: // All time
//                 return 0;
//             case 1: // Today
//                 return now.AddDays(-1).ToUnixTimeSeconds();
//             case 2: // This week
//                 return now.AddDays(-7).ToUnixTimeSeconds();
//             case 3: // This month
//                 return now.AddMonths(-1).ToUnixTimeSeconds();
//             default:
//                 return 0;
//         }
//     }

//     private void OnDestroy()
//     {
//         if (updateCancellationToken != null)
//         {
//             updateCancellationToken.Cancel();
//             updateCancellationToken.Dispose();
//         }
//     }

//     [System.Serializable]
//     private class PlayerScoreWrapper
//     {
//         public PlayerScore[] scores;
//     }
// }

// [System.Serializable]
// public class LeaderboardEntryUI : MonoBehaviour
// {
//     [SerializeField] private TMP_Text rankText;
//     [SerializeField] private TMP_Text nameText;
//     [SerializeField] private TMP_Text scoreText;
//     [SerializeField] private TMP_Text regionText;
//     [SerializeField] private TMP_Text lastPlayedText;
//     [SerializeField] private Image playerAvatar;

//     public void SetValues(AdvancedLeaderboardManager.PlayerScore score)
//     {
//         if (rankText != null) rankText.text = $"#{score.rank}";
//         if (nameText != null) nameText.text = score.playerName;
//         if (scoreText != null) scoreText.text = score.score.ToString("N0");
//         if (regionText != null) regionText.text = score.region;
//         if (lastPlayedText != null) lastPlayedText.text = score.lastPlayedDate;
//         // Add avatar loading logic here if needed
//     }
// }