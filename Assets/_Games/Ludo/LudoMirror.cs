using UnityEngine;
using Mirror;

public class LudoMirror : MonoBehaviour
{
    private void OnDisable()
    {
        MirrorNetwork.OnDirectWinWithoutInternet -= DirectResult;
    }

    private void OnEnable()
    {
        MirrorNetwork.OnDirectWinWithoutInternet += DirectResult;
    }

    public void DirectResult(bool result)
    {
        if (result)
        {
            // Victory(staticVariables.UserProfiledata.user._id, "Opponent has disconnected. You are declared the winner.");
            if (ResultManager.GameSpawnedFinished == false)
            {
                ResultManager.GameSpawnedFinished = true;
                GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                if (enemyPrefab != null)
                {
                    "1".Show();
                    // Spawn at position (0,0,0)
                    var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                    gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, staticVariables.UserProfiledata.user._id.ToString());
                }
                else
                {
                    Debug.LogError("WinLose GameManager prefab not found!");
                }
            }
        }
        else
        {
            if (ResultManager.GameSpawnedFinished == false)
            {
                ResultManager.GameSpawnedFinished = true;
                GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                if (enemyPrefab != null)
                {
                    "1".Show();
                    // Spawn at position (0,0,0)
                    var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                    gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(false, staticVariables.UserProfiledata.user._id.ToString());
                }
                else
                {
                    Debug.LogError("WinLose GameManager prefab not found!");
                }
            }
        }

    }
}
