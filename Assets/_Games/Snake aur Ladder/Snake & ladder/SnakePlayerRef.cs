using Mirror;
using Snake_Ladder;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class SnakePlayerRef : NetworkBehaviour
{
    public bool Player1;
    public bool Player2;
    public static SnakePlayerRef ins;
    [SyncVar(hook = nameof(OnPlayerNameChanged))] public string PlayerName = "";
    public Text nameText;
    [SyncVar] public string playerID = "";
    [SyncVar] public Vector3 netPosition;
    [SyncVar] public Quaternion netRotation;

    private void Awake()
    {
        ins = this;

    }

    public bool IsMine;
    IEnumerator SetupPlayerData()
    {
        yield return new WaitUntil(() => !string.IsNullOrEmpty(playerID));
        IsMine = playerID == staticVariables.UserProfiledata.user._id.ToString();
        if (SnakeMirrorNetworkManager.IsMultiSeatTable)
        {
            // N seats: my seat from the server's seat list; tokens are moved by the server only.
            yield return new WaitUntil(() => SnakeMirrorNetworkManager.Instance.seatList.Count > 0);
            int seat = SnakeMirrorNetworkManager.Instance.MatchSeatIndex(playerID);
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && seat >= 0) sr.color = GameControllerNew.TokenTint(seat);
            if (IsMine)
            {
                GameControllerNew.myPlayerNumber = seat;
                GameControllerNew.instance.SetupPlayerProfiles();
            }
            yield break;
        }
        if (IsMine)
        {
            GameControllerNew.myPlayerNumber = NetworkGameManager.Instance.creatorData.playerId == playerID ? 0 : 1;
            PlayerName = staticVariables.UserProfiledata.user.first_name + " " +
                   staticVariables.UserProfiledata.user.last_name;
            CmdSetPlayerName(PlayerName);
            Player1 = GameControllerNew.myPlayerNumber == 0;
            Player2 = GameControllerNew.myPlayerNumber == 1;
            GameControllerNew.instance.SetupPlayerProfiles();
            if (Player1)
            {
                GameControllerNew.instance.Player1Soldier = transform;

                Debug.Log("✅ Player 1 reference set in GameController");
            }
            else
            {
                GameControllerNew.instance.Player2Soldier = transform;
                Debug.Log("✅ Player 2 reference set in GameController");
            }
        }
    }
    public override void OnStartClient()
    {
        base.OnStartClient();

        StartCoroutine(SetupPlayerData());

    }

    [Command]
    private void CmdSetPlayerName(string name)
    {
        PlayerName = name;
    }

    private void OnPlayerNameChanged(string oldName, string newName)
    {
        if (nameText != null)
            nameText.text = newName;
    }

}
