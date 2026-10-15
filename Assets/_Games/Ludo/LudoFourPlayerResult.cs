using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensions;

/// <summary>
/// Result screen of an N-seat Ludo table (ludo-4-player plan §3.5): one row per seat with place, name, tokens home,
/// what happened to the seat and the coins. Built in code on top of the Ludo scene so it needs no new prefab; the font
/// is taken from the scene's own TMP text (no built-in assets in content that ships over the air).
/// Amounts follow rule A (winner takes the pot): the winner is shown the pot, the others their stake. The real credited
/// amounts come from the backend; the home balance refreshes when the panel opens.
/// </summary>
public class LudoFourPlayerResult : MonoBehaviour
{
    static LudoFourPlayerResult shown;

    public static void Show(string winnerId, string reason)
    {
        if (shown != null) return;
        var gui = GameGUIController.insta;
        var ludo = LudoNetworkManager.instance;
        if (gui == null || ludo == null) return;

        var go = new GameObject("LudoFourPlayerResult");
        shown = go.AddComponent<LudoFourPlayerResult>();
        shown.Build(gui, ludo, winnerId, reason);
        if (ApiAndRoomManager._instance != null) ApiAndRoomManager._instance.ModifyUserBalance();
    }

    TMP_FontAsset font;

    struct Row
    {
        public int seat, home, progress, state;
        public string id, name;
    }

    void Build(GameGUIController gui, LudoNetworkManager ludo, string winnerId, string reason)
    {
        var sceneText = FindFirstObjectByType<TextMeshProUGUI>();
        font = sceneText != null ? sceneText.font : TMP_Settings.defaultFontAsset;
        string myId = staticVariables.UserProfiledata != null ? staticVariables.UserProfiledata.user._id.ToString() : "";
        int stake = NetworkGameManager.Instance != null ? NetworkGameManager.Instance.Prize : staticVariables.currentPrize;
        bool gold = NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsGoldCoin;
        int pot = stake * Mathf.Max(2, ludo.seatCount);

        // Rows from the seat list + the board this phone shows.
        var rows = new List<Row>();
        for (int s = 0; s < ludo.seatList.Count; s++)
        {
            var info = ludo.seatList[s];
            int home = 0, progress = 0;
            if (gui.playerObjects != null && s < gui.playerObjects.Count && gui.playerObjects[s].pawns != null)
                foreach (var pawn in gui.playerObjects[s].pawns)
                {
                    int p = pawn.GetComponent<LudoPawnController>().CurrentPosition;
                    if (p >= LudoServerBoard.Home) home++;
                    if (p > 0) progress += p;
                }
            rows.Add(new Row { seat = s, id = info.playerId, name = string.IsNullOrEmpty(info.name) ? "Player " + (s + 1) : info.name,
                               home = home, progress = progress, state = info.state });
        }
        rows.Sort((a, b) =>
        {
            if (a.id == winnerId) return -1;
            if (b.id == winnerId) return 1;
            bool aOut = a.state == LudoNetworkManager.SeatLeft || a.state == LudoNetworkManager.SeatAbandoned;
            bool bOut = b.state == LudoNetworkManager.SeatLeft || b.state == LudoNetworkManager.SeatAbandoned;
            if (aOut != bOut) return aOut ? 1 : -1;
            int c = b.home.CompareTo(a.home);
            if (c == 0) c = b.progress.CompareTo(a.progress);
            return c != 0 ? c : a.seat.CompareTo(b.seat);
        });

        // Overlay canvas.
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        var dim = Panel(transform, "Dim", new Color(0, 0, 0, 0.75f));
        Stretch(dim, Vector2.zero, Vector2.one);

        var card = Panel(dim, "Card", new Color(0.08f, 0.10f, 0.22f, 0.97f));
        Anchor(card, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.80f));

        bool iWon = winnerId == myId;
        var title = Label(card, iWon ? "YOU WIN!" : "MATCH OVER", 64, iWon ? new Color(1f, 0.84f, 0.2f) : Color.white, TextAlignmentOptions.Center);
        Anchor(title, new Vector2(0, 0.86f), new Vector2(1, 0.98f));
        var sub = Label(card, reason, 30, new Color(0.8f, 0.85f, 1f), TextAlignmentOptions.Center);
        Anchor(sub, new Vector2(0.04f, 0.79f), new Vector2(0.96f, 0.86f));

        Color[] seatColours = { new Color(1f, 0.85f, 0.1f), new Color(0.2f, 0.8f, 0.3f), new Color(0.95f, 0.25f, 0.25f), new Color(0.25f, 0.5f, 1f) };
        string coin = gold ? "gold" : "silver";
        float top = 0.76f, h = 0.13f;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            bool winner = r.id == winnerId;
            var row = Panel(card, "Row" + i, r.id == myId ? new Color(1f, 1f, 1f, 0.14f) : new Color(1f, 1f, 1f, 0.06f));
            Anchor(row, new Vector2(0.03f, top - h * (i + 1) + 0.01f), new Vector2(0.97f, top - h * i));

            var dot = Panel(row, "Colour", seatColours[r.seat % 4]);
            Anchor(dot, new Vector2(0.01f, 0.2f), new Vector2(0.045f, 0.8f));

            Anchor(Label(row, (i + 1).ToString(), 44, winner ? new Color(1f, 0.84f, 0.2f) : Color.white, TextAlignmentOptions.Center),
                   new Vector2(0.05f, 0), new Vector2(0.14f, 1));
            Anchor(Label(row, r.name + (r.id == myId ? "  (you)" : ""), 34, Color.white, TextAlignmentOptions.MidlineLeft),
                   new Vector2(0.15f, 0.45f), new Vector2(0.70f, 1));
            Anchor(Label(row, $"{r.home}/4 home · {StateText(r.state, winner)}", 26, new Color(0.75f, 0.8f, 0.95f), TextAlignmentOptions.MidlineLeft),
                   new Vector2(0.15f, 0), new Vector2(0.70f, 0.48f));
            string amount = winner ? $"+{pot - stake}" : $"-{stake}";
            Anchor(Label(row, amount + " " + coin, 32, winner ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f), TextAlignmentOptions.MidlineRight),
                   new Vector2(0.70f, 0), new Vector2(0.98f, 1));
        }

        var note = Label(card, $"Pot {pot} {coin} · winner takes the pot", 26, new Color(0.7f, 0.75f, 0.9f), TextAlignmentOptions.Center);
        Anchor(note, new Vector2(0, 0.12f), new Vector2(1, 0.18f));

        var homeButton = Panel(card, "HomeButton", new Color(0.15f, 0.45f, 0.95f));
        Anchor(homeButton, new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.11f));
        Anchor(Label(homeButton, "HOME", 40, Color.white, TextAlignmentOptions.Center), Vector2.zero, Vector2.one);
        homeButton.gameObject.AddComponent<Button>().onClick.AddListener(GoHome);
    }

    static string StateText(int state, bool winner)
    {
        if (winner) return "winner";
        switch (state)
        {
            case LudoNetworkManager.SeatLeft: return "left";
            case LudoNetworkManager.SeatAbandoned: return "disconnected";
            case LudoNetworkManager.SeatFinished: return "finished";
            default: return "playing";
        }
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
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform r, Vector2 min, Vector2 max) => Anchor(r, min, max);

    static void Anchor(RectTransform r, Vector2 min, Vector2 max)
    {
        r.anchorMin = min;
        r.anchorMax = max;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}
