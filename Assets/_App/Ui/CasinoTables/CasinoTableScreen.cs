using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GamesPanda.Tables;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The table screen of Teen Patti 5 / Poker 6, drawn entirely in code from CasinoTableArt so its layout and behaviour
/// ship as hot updates. It shows what the match server says (TableProtocol: the table state, my cards, results) and
/// sends only intents (sit with a ticket, act, leave). It never decides anything about money or cards.
/// Me at the bottom centre; the other seats round the table in seat order.
/// </summary>
public class CasinoTableScreen : MonoBehaviour
{
    public CasinoTableArt art;
    public RectTransform root;

    // ── state from the server ───────────────────────────────────────────────────────────────────
    TableProtocol.View view;
    float viewAt;
    int mySeat = -1;
    string myUser;
    int myHand;
    int[] myCards;
    TableProtocol.Result lastResult;
    float resultUntil;
    readonly Dictionary<string, int[]> revealed = new Dictionary<string, int[]>();
    bool leaving, closedShown;

    // ── widgets ─────────────────────────────────────────────────────────────────────────────────
    sealed class SeatUI
    {
        public RectTransform root;
        public Image ring, avatar, frame, dealer;
        public TextMeshProUGUI name, stack, bet, label;
        public RectTransform cards;
        public CanvasGroup group;
        public string avatarUser;
    }

    readonly List<SeatUI> seats = new List<SeatUI>();
    TextMeshProUGUI potText, messageText, headerText, logText, resultText, sideAskText;
    RectTransform board, actionBar, sideAskPanel, resultPanel, raisePanel;
    Slider raiseSlider;
    TextMeshProUGUI raiseValue;
    readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
    readonly Queue<string> log = new Queue<string>();
    readonly Dictionary<string, string> avatarUrl = new Dictionary<string, string>();
    readonly Dictionary<string, Sprite> avatarSprite = new Dictionary<string, Sprite>();

    bool Poker => view != null ? view.game == "poker" : CasinoTableSession.Table?.IsPoker ?? false;
    int SeatCount => view?.seats ?? CasinoTableSession.Table?.max_players ?? 5;

    // ── life cycle ──────────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (root == null) root = (RectTransform)transform;
        myUser = staticVariables.UserProfiledata?.user != null ? staticVariables.UserProfiledata.user._id.ToString() : null;
        if (CasinoTableSession.Table?.players != null)
            foreach (var p in CasinoTableSession.Table.players) avatarUrl[p.user_id] = p.file_url;
        Build();
        CasinoTableNet.ClientMessage += OnMessage;
        if (CasinoTableNet.LastState != null) OnMessage(CasinoTableNet.LastState);
        foreach (string m in CasinoTableNet.TakeEarly()) OnMessage(m);
        if (!NetworkServer.active) StartCoroutine(SitWhenConnected());
    }

    void OnDestroy() => CasinoTableNet.ClientMessage -= OnMessage;

    IEnumerator SitWhenConnected()
    {
        while (CasinoTableNet.Instance == null || !NetworkClient.isConnected || !NetworkClient.ready) yield return null;
        string ticket = CasinoTableSession.TakeTicket();
        if (!string.IsNullOrEmpty(ticket)) { SendTicket(ticket); yield break; }
        // A reconnect (the scene came back) or an editor test: a fresh ticket from the backend.
        if (CasinoTableSession.Active) CasinoTableSession.RefreshTicket(SendTicket, Toast);
        else Toast("Not seated at a table");
    }

    void SendTicket(string ticket)
    {
        string name = staticVariables.UserProfiledata?.user?.first_name ?? "";
        Send(new TableProtocol.In { t = "ticket", ticket = ticket, name = name });
    }

    void Send(TableProtocol.In msg)
    {
        if (CasinoTableNet.Instance == null) { Toast("Not connected"); return; }
        CasinoTableNet.Instance.Send(JsonConvert.SerializeObject(msg, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
    }

    void Act(string action, long amount = 0, bool yes = false) =>
        Send(new TableProtocol.In { t = "act", a = action, amount = amount, yes = yes, hand = view?.hand ?? 0 });

    // ── messages ────────────────────────────────────────────────────────────────────────────────

    void OnMessage(string json)
    {
        JObject j;
        try { j = JObject.Parse(json); }
        catch (JsonException) { return; }
        switch (j["t"]?.ToString())
        {
            case "state":
                view = j.ToObject<TableProtocol.View>();
                viewAt = Time.unscaledTime;
                if (view.hand != myHand) revealed.Clear();
                break;
            case "you":
                var you = j.ToObject<TableProtocol.Private>();
                mySeat = you.seat;
                myUser = you.user ?? myUser;
                break;
            case "hole":
                var hole = j.ToObject<TableProtocol.Private>();
                myHand = hole.hand;
                myCards = hole.cards;
                break;
            case "reveal":
                var rev = j.ToObject<TableProtocol.Private>();
                if (rev.otherUser != null) revealed[rev.otherUser] = rev.otherCards;
                Toast("Side-show: you see " + NameOf(rev.otherUser) + "'s cards");
                break;
            case "result":
                lastResult = j.ToObject<TableProtocol.Result>();
                resultUntil = Time.unscaledTime + 5f;
                foreach (var kv in lastResult.shown) revealed[kv.Key] = kv.Value;
                ShowResult();
                break;
            case "event":
                AddLog(j["text"]?.ToString());
                break;
            case "error":
                Toast(j["text"]?.ToString());
                break;
        }
        Render();
    }

    string NameOf(string user)
    {
        var p = view?.players.FirstOrDefault(x => x.user == user);
        return p == null ? user : string.IsNullOrEmpty(p.name) ? "Player " + (p.seat + 1) : p.name;
    }

    // ── layout ──────────────────────────────────────────────────────────────────────────────────

    static readonly Vector2[] Spots5 = { new Vector2(0, -330), new Vector2(-700, -80), new Vector2(-400, 300), new Vector2(400, 300), new Vector2(700, -80) };
    static readonly Vector2[] Spots6 = { new Vector2(0, -330), new Vector2(-680, -170), new Vector2(-560, 260), new Vector2(0, 360), new Vector2(560, 260), new Vector2(680, -170) };

    void Build()
    {
        var bg = Img("Background", root, art.background, Color.white);
        Stretch(bg.rectTransform);
        var table = Img("Table", root, art.table, art.table ? Color.white : new Color(0.1f, 0.35f, 0.2f));
        Place(table.rectTransform, Vector2.zero, new Vector2(1500, 760));
        table.preserveAspect = art.table != null;

        potText = Text("Pot", root, "", 40, TextAlignmentOptions.Center);
        Place(potText.rectTransform, new Vector2(0, 140), new Vector2(600, 60));
        messageText = Text("Message", root, "", 34, TextAlignmentOptions.Center);
        Place(messageText.rectTransform, new Vector2(0, -60), new Vector2(900, 60));
        headerText = Text("Header", root, "", 26, TextAlignmentOptions.Right);
        Place(headerText.rectTransform, new Vector2(700, 500), new Vector2(480, 50));
        logText = Text("Log", root, "", 22, TextAlignmentOptions.TopLeft);
        Place(logText.rectTransform, new Vector2(-640, 430), new Vector2(560, 140));
        logText.color = new Color(1, 1, 1, 0.75f);

        board = new GameObject("Board", typeof(RectTransform)).GetComponent<RectTransform>();
        board.SetParent(root, false);
        Place(board, new Vector2(0, 40), new Vector2(620, 130));

        var spots = SeatCount == 6 ? Spots6 : Spots5;
        for (int i = 0; i < spots.Length; i++) seats.Add(BuildSeat(i, spots[i]));

        BuildActions();
        BuildSideAsk();
        BuildResult();

        var leave = Button("Leave", root, "LEAVE", () => AskLeave());
        Place((RectTransform)leave.transform, new Vector2(-840, 490), new Vector2(180, 70));
        var topUp = Button("TopUp", root, "TOP UP", () => TopUp());
        Place((RectTransform)topUp.transform, new Vector2(-640, 490), new Vector2(180, 70));
    }

    SeatUI BuildSeat(int slot, Vector2 at)
    {
        var s = new SeatUI();
        s.root = new GameObject("Seat" + slot, typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        s.root.SetParent(root, false);
        Place(s.root, at, new Vector2(260, 260));
        s.group = s.root.GetComponent<CanvasGroup>();
        s.ring = Img("Timer", s.root, art.timerRing, new Color(1f, 0.85f, 0.2f));
        Place(s.ring.rectTransform, new Vector2(0, 20), new Vector2(170, 170));
        s.ring.type = Image.Type.Filled;
        s.ring.fillMethod = Image.FillMethod.Radial360;
        s.ring.fillOrigin = (int)Image.Origin360.Top;
        s.avatar = Img("Avatar", s.root, art.avatar, Color.white);
        Place(s.avatar.rectTransform, new Vector2(0, 20), new Vector2(130, 130));
        s.frame = Img("Frame", s.root, art.seatFrame, art.seatFrame ? Color.white : new Color(0, 0, 0, 0));
        Place(s.frame.rectTransform, new Vector2(0, 20), new Vector2(160, 160));
        var bar = Img("NameBar", s.root, art.nameBar, art.nameBar ? Color.white : new Color(0, 0, 0, 0.55f));
        Place(bar.rectTransform, new Vector2(0, -80), new Vector2(240, 70));
        s.name = Text("Name", s.root, "", 24, TextAlignmentOptions.Center);
        Place(s.name.rectTransform, new Vector2(0, -66), new Vector2(230, 30));
        s.stack = Text("Stack", s.root, "", 24, TextAlignmentOptions.Center);
        Place(s.stack.rectTransform, new Vector2(0, -94), new Vector2(230, 30));
        s.stack.color = new Color(1f, 0.85f, 0.3f);
        s.label = Text("Label", s.root, "", 22, TextAlignmentOptions.Center);
        Place(s.label.rectTransform, new Vector2(0, 112), new Vector2(240, 30));
        s.bet = Text("Bet", s.root, "", 24, TextAlignmentOptions.Center);
        // Bets sit between the seat and the middle of the table.
        Vector2 toCentre = -at.normalized * 150f;
        Place(s.bet.rectTransform, toCentre + new Vector2(0, 20), new Vector2(200, 34));
        s.dealer = Img("Dealer", s.root, art.dealer, art.dealer ? Color.white : new Color(1, 1, 1, 0.9f));
        Place(s.dealer.rectTransform, new Vector2(95, 85), new Vector2(46, 46));
        s.cards = new GameObject("Cards", typeof(RectTransform)).GetComponent<RectTransform>();
        s.cards.SetParent(s.root, false);
        Place(s.cards, slot == 0 ? new Vector2(0, 180) : new Vector2(0, 20) + toCentre * 0.55f, new Vector2(240, 110));
        return s;
    }

    void BuildActions()
    {
        actionBar = new GameObject("Actions", typeof(RectTransform)).GetComponent<RectTransform>();
        actionBar.SetParent(root, false);
        Place(actionBar, new Vector2(520, -470), new Vector2(860, 110));
        var h = actionBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 14; h.childAlignment = TextAnchor.MiddleRight; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = false;
        string[] names = { "see", "pack", "chaal", "raise", "show", "sideshow", "fold", "check", "call", "allin" };
        foreach (string n in names)
        {
            string action = n;
            var b = Button(n, actionBar, n.ToUpperInvariant(), () => OnAction(action));
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 150; le.preferredHeight = 90;
            buttons[n] = b;
        }

        raisePanel = new GameObject("RaisePanel", typeof(RectTransform)).GetComponent<RectTransform>();
        raisePanel.SetParent(root, false);
        Place(raisePanel, new Vector2(520, -360), new Vector2(560, 80));
        var bgr = Img("Bg", raisePanel, art.panel, art.panel ? Color.white : new Color(0, 0, 0, 0.6f));
        Stretch(bgr.rectTransform);
        raiseSlider = new GameObject("Slider", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>();
        var srt = (RectTransform)raiseSlider.transform;
        srt.SetParent(raisePanel, false);
        Place(srt, new Vector2(-70, 0), new Vector2(380, 40));
        var track = Img("Track", srt, null, new Color(1, 1, 1, 0.3f));
        Stretch(track.rectTransform);
        var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(srt, false); Stretch(fillArea);
        var fill = Img("Fill", fillArea, null, new Color(1f, 0.8f, 0.2f));
        Stretch(fill.rectTransform);
        raiseSlider.fillRect = fill.rectTransform;
        var handleArea = new GameObject("Handle Area", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(srt, false); Stretch(handleArea);
        var handle = Img("Handle", handleArea, art.chip, Color.white);
        handle.rectTransform.sizeDelta = new Vector2(48, 48);
        raiseSlider.handleRect = handle.rectTransform;
        raiseSlider.targetGraphic = handle;
        raiseSlider.wholeNumbers = true;
        raiseSlider.onValueChanged.AddListener(_ => raiseValue.text = Money(RaiseAmount()));
        raiseValue = Text("Value", raisePanel, "", 30, TextAlignmentOptions.Center);
        Place(raiseValue.rectTransform, new Vector2(200, 0), new Vector2(160, 60));
        raisePanel.gameObject.SetActive(false);
    }

    void BuildSideAsk()
    {
        sideAskPanel = new GameObject("SideShowAsk", typeof(RectTransform)).GetComponent<RectTransform>();
        sideAskPanel.SetParent(root, false);
        Place(sideAskPanel, new Vector2(0, -200), new Vector2(720, 200));
        var bg = Img("Bg", sideAskPanel, art.panel, art.panel ? Color.white : new Color(0, 0, 0, 0.8f));
        Stretch(bg.rectTransform);
        sideAskText = Text("Text", sideAskPanel, "", 30, TextAlignmentOptions.Center);
        Place(sideAskText.rectTransform, new Vector2(0, 45), new Vector2(680, 70));
        var yes = Button("Accept", sideAskPanel, "ACCEPT", () => Act("answer", 0, true));
        Place((RectTransform)yes.transform, new Vector2(-130, -45), new Vector2(220, 80));
        var no = Button("Decline", sideAskPanel, "DECLINE", () => Act("answer", 0, false));
        Place((RectTransform)no.transform, new Vector2(130, -45), new Vector2(220, 80));
        sideAskPanel.gameObject.SetActive(false);
    }

    void BuildResult()
    {
        resultPanel = new GameObject("Result", typeof(RectTransform)).GetComponent<RectTransform>();
        resultPanel.SetParent(root, false);
        Place(resultPanel, new Vector2(0, 200), new Vector2(900, 150));
        var bg = Img("Bg", resultPanel, art.panel, art.panel ? Color.white : new Color(0, 0, 0, 0.75f));
        Stretch(bg.rectTransform);
        resultText = Text("Text", resultPanel, "", 30, TextAlignmentOptions.Center);
        Stretch(resultText.rectTransform);
        resultPanel.gameObject.SetActive(false);
    }

    // ── drawing ─────────────────────────────────────────────────────────────────────────────────

    void Update()
    {
        if (view == null) return;
        float elapsed = Time.unscaledTime - viewAt;
        foreach (var p in view.players)
        {
            var ui = SeatFor(p.seat);
            if (ui == null) continue;
            bool onTurn = view.turn == p.seat && (view.phase == "playing" || view.phase == "sideshow");
            ui.ring.enabled = onTurn;
            if (onTurn && view.turnTotal > 0) ui.ring.fillAmount = Mathf.Clamp01((view.turnLeft - elapsed) / view.turnTotal);
        }
        if (resultPanel.gameObject.activeSelf && Time.unscaledTime > resultUntil) resultPanel.gameObject.SetActive(false);
        if (view.phase == "waiting" && view.nextHandIn > 0)
            messageText.text = $"Next hand in {Mathf.CeilToInt(Mathf.Max(0, view.nextHandIn - elapsed))}";
    }

    SeatUI SeatFor(int seat)
    {
        int n = seats.Count;
        if (seat < 0 || seat >= n) return null;
        int slot = mySeat >= 0 ? (seat - mySeat + n) % n : seat;
        return seats[slot];
    }

    void Render()
    {
        if (view == null) return;
        if (mySeat < 0 && myUser != null) mySeat = view.players.FirstOrDefault(p => p.user == myUser)?.seat ?? -1;

        foreach (var ui in seats) { ui.group.alpha = 0.35f; ui.name.text = "Empty"; ui.stack.text = ""; ui.bet.text = ""; ui.label.text = ""; ui.dealer.enabled = false; ui.ring.enabled = false; Clear(ui.cards); }
        bool handOn = view.phase == "playing" || view.phase == "sideshow" || view.phase == "result";
        foreach (var p in view.players)
        {
            var ui = SeatFor(p.seat);
            if (ui == null) continue;
            bool me = p.seat == mySeat;
            ui.group.alpha = p.state == "packed" || p.state == "away" || !p.connected ? 0.55f : 1f;
            ui.name.text = me ? "You" : string.IsNullOrEmpty(p.name) ? "Player " + (p.seat + 1) : p.name;
            ui.stack.text = Money(p.stack);
            ui.bet.text = p.bet > 0 ? Money(p.bet) : "";
            ui.dealer.enabled = view.dealer == p.seat && handOn;
            ui.label.text = Label(p);
            SetAvatar(ui, p.user);
            if (!handOn || p.state == "waiting" || p.state == "away" && p.bet == 0) continue;
            int count = Poker ? 2 : 3;
            int[] faces = me && myHand == view.hand ? myCards : revealed.TryGetValue(p.user, out var r) ? r : null;
            DrawCards(ui.cards, faces, count, me ? 1f : 0.62f, p.state == "packed");
        }

        potText.text = view.pot > 0 ? "Pot " + Money(view.pot) : "";
        Clear(board);
        if (Poker && view.board != null)
            for (int i = 0; i < view.board.Length; i++) Card(board, view.board[i], new Vector2(-240 + i * 120, 0), 1f);
        if (view.phase != "waiting" || view.nextHandIn <= 0) messageText.text = view.phase == "closed" ? "The table is closed" : view.message ?? "";
        headerText.text = (Poker ? $"Blinds {view.smallBlind}/{view.bigBlind}" : $"Boot {view.boot}") + (view.hand > 0 ? $"   Hand #{view.hand}" : "");

        RenderActions();
        RenderSideAsk();

        if (view.phase == "closed" && !closedShown) { closedShown = true; StartCoroutine(GoHomeLater("The table has closed. Your chips are back in your wallet.")); }
        if (leaving && mySeat >= 0 && view.players.All(p => p.seat != mySeat)) StartCoroutine(GoHomeLater(null));
    }

    string Label(TableProtocol.SeatView p)
    {
        switch (p.state)
        {
            case "packed": return Poker ? "Folded" : "Packed";
            case "allin": return "All-in";
            case "away": return "Away";
            case "leaving": return "Leaving";
            case "waiting": return view.phase == "playing" ? "Next hand" : "";
            default: return Poker ? "" : p.seen ? "Seen" : "Blind";
        }
    }

    TableProtocol.SeatView Me => view?.players.FirstOrDefault(p => p.seat == mySeat);

    void RenderActions()
    {
        var me = Me;
        bool inHand = me != null && (me.state == "in");
        bool myTurn = inHand && view.phase == "playing" && view.turn == mySeat;
        foreach (var b in buttons.Values) b.gameObject.SetActive(false);
        raisePanel.gameObject.SetActive(false);
        if (!inHand) return;
        int playersIn = view.players.Count(p => p.state == "in" || p.state == "allin");
        if (!Poker)
        {
            long cost = me.seen ? view.stake * 2 : view.stake;
            Show("see", !me.seen, "SEE");
            Show("pack", myTurn, "PACK");
            Show("chaal", myTurn, (me.seen ? "CHAAL " : "BLIND ") + Money(cost));
            Show("raise", myTurn, "RAISE " + Money(cost * 2));
            Show("show", myTurn && playersIn == 2, "SHOW");
            Show("sideshow", myTurn && playersIn >= 3 && me.seen, "SIDE SHOW");
        }
        else
        {
            long toCall = Math.Max(0, view.currentBet - me.bet);
            long maxTo = me.bet + me.stack;
            Show("fold", myTurn, "FOLD");
            Show("check", myTurn && toCall == 0, "CHECK");
            Show("call", myTurn && toCall > 0, "CALL " + Money(Math.Min(toCall, me.stack)));
            bool canRaise = myTurn && maxTo > view.currentBet;
            Show("raise", canRaise && maxTo > view.minRaiseTo, (view.currentBet == 0 ? "BET" : "RAISE"));
            Show("allin", canRaise, "ALL-IN " + Money(me.stack));
            if (canRaise && maxTo > view.minRaiseTo)
            {
                raisePanel.gameObject.SetActive(true);
                long min = view.minRaiseTo;
                if ((long)raiseSlider.minValue != min || (long)raiseSlider.maxValue != maxTo)
                {
                    raiseSlider.minValue = min;
                    raiseSlider.maxValue = maxTo;
                    raiseSlider.value = min;
                }
                raiseValue.text = Money(RaiseAmount());
            }
        }
    }

    long RaiseAmount()
    {
        long bb = Math.Max(1, view?.bigBlind ?? 1);
        long v = (long)raiseSlider.value;
        long max = (long)raiseSlider.maxValue;
        return v >= max ? max : Math.Max((long)raiseSlider.minValue, v / bb * bb);
    }

    void Show(string key, bool on, string text)
    {
        if (!buttons.TryGetValue(key, out var b)) return;
        b.gameObject.SetActive(on);
        if (on) b.GetComponentInChildren<TextMeshProUGUI>().text = text;
    }

    void RenderSideAsk()
    {
        bool asked = view.phase == "sideshow" && view.sideShowTo == mySeat && mySeat >= 0;
        sideAskPanel.gameObject.SetActive(asked);
        if (asked)
        {
            var from = view.players.FirstOrDefault(p => p.seat == view.sideShowFrom);
            sideAskText.text = $"{(from != null ? NameOf(from.user) : "A player")} asks for a side-show";
        }
    }

    void OnAction(string action)
    {
        if (action == "raise" && Poker) { Act("raise", RaiseAmount()); return; }
        Act(action);
    }

    void ShowResult()
    {
        if (lastResult == null) return;
        var parts = new List<string>();
        foreach (var w in lastResult.players.Where(p => p.won > 0).OrderByDescending(p => p.won))
        {
            string hand = lastResult.hands.TryGetValue(w.user, out var h) && !string.IsNullOrEmpty(h) ? " - " + Pretty(h) : "";
            parts.Add($"{(w.user == myUser ? "You" : NameOf(w.user))} won {Money(w.won)}{hand}");
        }
        resultText.text = string.Join("\n", parts);
        resultPanel.gameObject.SetActive(parts.Count > 0);
    }

    static string Pretty(string category) => System.Text.RegularExpressions.Regex.Replace(category, "(?<=[a-z])(?=[A-Z])", " ");

    // ── leaving, top-up ─────────────────────────────────────────────────────────────────────────

    void AskLeave()
    {
        if (PopupMessageManager.instance != null)
            PopupMessageManager.instance.ShowConfirmPanel("Leave the table? Your chips go back to your wallet after this hand.", DoLeave, "LEAVE", true, true);
        else DoLeave();
    }

    void DoLeave()
    {
        leaving = true;
        CasinoTableSession.Leave(error => { if (error != null) Toast(error); });
        if (Me == null || Me.state != "in") StartCoroutine(GoHomeLater(null, 1.5f));
    }

    void TopUp()
    {
        var t = CasinoTableSession.Table;
        var me = Me;
        if (t == null || me == null) return;
        long room = t.buy_in_max - me.stack;
        if (room <= 0) { Toast("Your stack is already at the maximum."); return; }
        long amount = Math.Min(room, t.buy_in_min);
        CasinoTableSession.TopUp(amount, SendTicket, Toast);
    }

    IEnumerator GoHomeLater(string message, float delay = 3f)
    {
        if (!string.IsNullOrEmpty(message)) Toast(message);
        yield return new WaitForSecondsRealtime(delay);
        CasinoTableSession.Clear();
        CasinoTableSession.ForgetLastTable();
        if (MirrorNetwork.Instance != null) MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.UserRequested;
        if (NetworkClient.active) NetworkManager.singleton.StopClient();
        SceneRouter.LoadScene("Home");
    }

    // ── small helpers ───────────────────────────────────────────────────────────────────────────

    void AddLog(string line)
    {
        if (string.IsNullOrEmpty(line)) return;
        foreach (var p in view?.players ?? new List<TableProtocol.SeatView>())
            if (!string.IsNullOrEmpty(p.user)) line = line.Replace(p.user, p.user == myUser ? "You" : NameOf(p.user));
        log.Enqueue(line);
        while (log.Count > 4) log.Dequeue();
        logText.text = string.Join("\n", log);
    }

    void Toast(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        AddLog(text);
        ConstantsData_M.Log("[Table] " + text);
    }

    void SetAvatar(SeatUI ui, string user)
    {
        if (ui.avatarUser == user) return;
        ui.avatarUser = user;
        ui.avatar.sprite = art.avatar;
        if (string.IsNullOrEmpty(user)) return;
        if (avatarSprite.TryGetValue(user, out var s)) { ui.avatar.sprite = s; return; }
        if (!avatarUrl.TryGetValue(user, out var url) || string.IsNullOrEmpty(url)) return;
        ServerConnection.DownloadSprite("/" + url, tex =>
        {
            if (tex == null || this == null) return;
            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            avatarSprite[user] = sp;
            if (ui.avatarUser == user) ui.avatar.sprite = sp;
        });
    }

    void DrawCards(RectTransform parent, int[] faces, int count, float scale, bool dim)
    {
        float step = 70 * scale;
        for (int i = 0; i < count; i++)
        {
            int face = faces != null && i < faces.Length ? faces[i] : -1;
            var img = Card(parent, face, new Vector2((i - (count - 1) / 2f) * step, 0), scale);
            if (dim) img.color = new Color(1, 1, 1, 0.5f);
        }
    }

    Image Card(RectTransform parent, int index, Vector2 at, float scale)
    {
        Sprite sprite = index >= 0 && index < art.cardFaces.Length && art.cardFaces[index] != null ? art.cardFaces[index] : art.cardBack;
        var img = Img("Card", parent, sprite, Color.white);
        img.preserveAspect = true;
        Place(img.rectTransform, at, new Vector2(100, 140) * scale);
        return img;
    }

    static void Clear(RectTransform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }

    static string Money(long v) => v >= 100000 ? (v / 1000f).ToString("0.#") + "K" : v.ToString("N0");

    Image Img(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = sprite == null && color == Color.white ? new Color(1, 1, 1, 0) : color;
        img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI Text(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        if (art.font != null) t.font = art.font;
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        t.enableWordWrapping = true;
        return t;
    }

    Button Button(string name, Transform parent, string label, Action onClick)
    {
        var img = Img(name, parent, art.button, art.button ? Color.white : new Color(0.85f, 0.55f, 0.1f));
        img.raycastTarget = true;
        img.type = art.button != null && art.button.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(() => onClick());
        var t = Text("Label", img.transform, label, 26, TextAlignmentOptions.Center);
        Stretch(t.rectTransform);
        t.fontStyle = FontStyles.Bold;
        return b;
    }

    static void Place(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
