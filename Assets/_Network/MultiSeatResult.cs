using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Result screen of a table with more than 2 seats (4-player Ludo, 2–4-player Snake &amp; Ladder): one row per seat with
/// place, colour, name, a game detail ("3/4 home", "square 64"), what happened to the seat and the coins. Built in code
/// on top of the game scene so it needs no prefab; the font is taken from the scene's own TMP text (no built-in assets
/// in content that ships over the air). Rule used: the winner takes the pot (the winner is shown the pot minus their
/// own stake, the others lose their stake); the real credited amounts come from the backend, and the home balance
/// refreshes when the panel opens.
/// </summary>
public class MultiSeatResult : MonoBehaviour
{
    public struct Row
    {
        public int seat;
        public string id;
        public string name;
        /// <summary>Game detail, e.g. "3/4 home" or "square 64".</summary>
        public string detail;
        /// <summary>"winner", "playing", "left", "disconnected", "out (AFK)".</summary>
        public string state;
    }

    static MultiSeatResult shown;

    public static readonly Color[] SeatColours =
    {
        new Color(1f, 0.85f, 0.1f), new Color(0.2f, 0.8f, 0.3f), new Color(0.95f, 0.25f, 0.25f), new Color(0.25f, 0.5f, 1f)
    };

    /// <summary>Rows best first (the winner first). Shown once per match.</summary>
    public static void Show(IList<Row> rows, string winnerId, string reason, Color[] seatColours = null)
    {
        if (shown != null || rows == null || rows.Count == 0) return;
        var go = new GameObject("MultiSeatResult");
        shown = go.AddComponent<MultiSeatResult>();
        shown.Build(rows, winnerId, reason, seatColours ?? SeatColours);
        ResultManager.isGameFinished = true;
        if (ApiAndRoomManager._instance != null) ApiAndRoomManager._instance.ModifyUserBalance();
    }

    TMP_FontAsset font;

    void Build(IList<Row> rows, string winnerId, string reason, Color[] seatColours)
    {
        var sceneText = FindFirstObjectByType<TextMeshProUGUI>();
        font = sceneText != null ? sceneText.font : TMP_Settings.defaultFontAsset;
        string myId = staticVariables.UserProfiledata != null ? staticVariables.UserProfiledata.user._id.ToString() : "";
        int stake = NetworkGameManager.Instance != null ? NetworkGameManager.Instance.Prize : staticVariables.currentPrize;
        bool gold = NetworkGameManager.Instance != null ? NetworkGameManager.Instance.IsGoldCoin : staticVariables.isgoldcoins;
        int pot = stake * Mathf.Max(2, rows.Count);

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        var dim = Panel(transform, "Dim", new Color(0, 0, 0, 0.75f));
        Anchor(dim, Vector2.zero, Vector2.one);

        var card = Panel(dim, "Card", new Color(0.08f, 0.10f, 0.22f, 0.97f));
        Anchor(card, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.80f));

        bool iWon = winnerId == myId;
        Anchor(Label(card, iWon ? "YOU WIN!" : "MATCH OVER", 64, iWon ? new Color(1f, 0.84f, 0.2f) : Color.white, TextAlignmentOptions.Center),
               new Vector2(0, 0.86f), new Vector2(1, 0.98f));
        Anchor(Label(card, reason, 30, new Color(0.8f, 0.85f, 1f), TextAlignmentOptions.Center),
               new Vector2(0.04f, 0.79f), new Vector2(0.96f, 0.86f));

        string coin = gold ? "gold" : "silver";
        float top = 0.76f, h = 0.13f;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            bool winner = r.id == winnerId;
            var row = Panel(card, "Row" + i, r.id == myId ? new Color(1f, 1f, 1f, 0.14f) : new Color(1f, 1f, 1f, 0.06f));
            Anchor(row, new Vector2(0.03f, top - h * (i + 1) + 0.01f), new Vector2(0.97f, top - h * i));

            Anchor(Panel(row, "Colour", seatColours[Mathf.Abs(r.seat) % seatColours.Length]), new Vector2(0.01f, 0.2f), new Vector2(0.045f, 0.8f));
            Anchor(Label(row, (i + 1).ToString(), 44, winner ? new Color(1f, 0.84f, 0.2f) : Color.white, TextAlignmentOptions.Center),
                   new Vector2(0.05f, 0), new Vector2(0.14f, 1));
            Anchor(Label(row, r.name + (r.id == myId ? "  (you)" : ""), 34, Color.white, TextAlignmentOptions.MidlineLeft),
                   new Vector2(0.15f, 0.45f), new Vector2(0.70f, 1));
            Anchor(Label(row, string.IsNullOrEmpty(r.detail) ? r.state : $"{r.detail} · {r.state}", 26, new Color(0.75f, 0.8f, 0.95f), TextAlignmentOptions.MidlineLeft),
                   new Vector2(0.15f, 0), new Vector2(0.70f, 0.48f));
            Anchor(Label(row, (winner ? $"+{pot - stake}" : $"-{stake}") + " " + coin, 32,
                         winner ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f), TextAlignmentOptions.MidlineRight),
                   new Vector2(0.70f, 0), new Vector2(0.98f, 1));
        }

        Anchor(Label(card, $"Pot {pot} {coin} · winner takes the pot", 26, new Color(0.7f, 0.75f, 0.9f), TextAlignmentOptions.Center),
               new Vector2(0, 0.12f), new Vector2(1, 0.18f));

        var homeButton = Panel(card, "HomeButton", new Color(0.15f, 0.45f, 0.95f));
        Anchor(homeButton, new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.11f));
        Anchor(Label(homeButton, "HOME", 40, Color.white, TextAlignmentOptions.Center), Vector2.zero, Vector2.one);
        homeButton.gameObject.AddComponent<Button>().onClick.AddListener(GoHome);
    }

    void GoHome()
    {
        shown = null;
        MatchRoster.Clear();
        if (NetworkClient.active) NetworkManager.singleton.StopClient();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
    }

    void OnDestroy()
    {
        if (shown == this) shown = null;
    }

    RectTransform Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    RectTransform Label(Transform parent, string text, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return go.GetComponent<RectTransform>();
    }

    static void Anchor(RectTransform r, Vector2 min, Vector2 max)
    {
        r.anchorMin = min;
        r.anchorMax = max;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}
