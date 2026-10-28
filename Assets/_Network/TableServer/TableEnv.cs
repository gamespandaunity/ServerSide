using System;
using System.Globalization;

/// <summary>
/// The settings of a Teen Patti 5 / Poker 6 table, read once from the Edgegap deployment ("Teen Patti and Poker
/// Tables", backend, 22 Oct 2026): GameId (3 = Teen Patti, 8 = Poker: the board the table plays), MaxPlayers, Tier,
/// Boot / SmallBlind / BigBlind (empty when not used by the game), BuyInMin / BuyInMax, CommissionPct (0 on silver),
/// CommissionCap (empty = none) and CloseAt (UTC). A deployment without MaxPlayers (or 2) is an ordinary 2-player
/// match: <see cref="IsTable"/> is false and nothing here applies.
/// </summary>
public static class TableEnv
{
    public const int TeenPattiBoard = 3, PokerBoard = 8;
    public const int TeenPattiTableGame = 60, PokerTableGame = 61;

    /// <summary>No hand starts this close to CloseAt (the backend refuses create-new-round then too).</summary>
    public static readonly TimeSpan NoNewHandBefore = TimeSpan.FromMinutes(3);

    static bool read;
    static int gameId, maxPlayers, commissionPct;
    static string tier;
    static long boot, smallBlind, bigBlind, buyInMin, buyInMax;
    static long? commissionCap;
    static DateTime closeAt;

    static void Read()
    {
        if (read) return;
        read = true;
        gameId = Int("GameId");
        // The table's own game id (60 Teen Patti 5, 61 Poker 6) instead of the board it plays: the table then ran the
        // 2-player scene with three people at it (28 Oct). Both mean the same table.
        if (gameId == TeenPattiTableGame) gameId = TeenPattiBoard;
        else if (gameId == PokerTableGame) gameId = PokerBoard;
        maxPlayers = Int("MaxPlayers");
        if (maxPlayers <= 2 && (Int("GameId") == TeenPattiTableGame || Int("GameId") == PokerTableGame))
            maxPlayers = Int("GameId") == PokerTableGame ? 6 : 5;
        tier = Env("Tier");
        boot = Long("Boot");
        smallBlind = Long("SmallBlind");
        bigBlind = Long("BigBlind");
        buyInMin = Long("BuyInMin");
        buyInMax = Long("BuyInMax");
        commissionPct = Int("CommissionPct");
        string cap = Env("CommissionCap");
        commissionCap = long.TryParse(cap, NumberStyles.Integer, CultureInfo.InvariantCulture, out long c) ? c : (long?)null;
        if (!DateTime.TryParse(Env("CloseAt"), CultureInfo.InvariantCulture,
                               DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out closeAt))
            closeAt = DateTime.MaxValue;
        // Once per server: what the deployment said, so a table that came up as a 2-player match shows why.
        UnityEngine.Debug.Log($"[TableEnv] GameId='{Env("GameId")}' MaxPlayers='{Env("MaxPlayers")}' Tier='{tier}' -> " +
                              (maxPlayers > 2 && (gameId == TeenPattiBoard || gameId == PokerBoard) ? "table" : "2-player match"));
    }

    static string Env(string name)
    {
        try { return Environment.GetEnvironmentVariable(name)?.Trim(); }
        catch (Exception) { return null; }
    }

    static int Int(string name) => int.TryParse(Env(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
    static long Long(string name) => long.TryParse(Env(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : 0;

    /// <summary>This server hosts a table (more than 2 seats) for Teen Patti or Poker.</summary>
    public static bool IsTable { get { Read(); return maxPlayers > 2 && (gameId == TeenPattiBoard || gameId == PokerBoard); } }
    public static bool IsTeenPatti { get { Read(); return IsTable && gameId == TeenPattiBoard; } }
    public static bool IsPoker { get { Read(); return IsTable && gameId == PokerBoard; } }

    public static int MaxPlayers { get { Read(); return maxPlayers; } }
    public static string Tier { get { Read(); return tier; } }
    public static long Boot { get { Read(); return boot; } }
    public static long SmallBlind { get { Read(); return smallBlind; } }
    public static long BigBlind { get { Read(); return bigBlind; } }
    public static long BuyInMin { get { Read(); return buyInMin; } }
    public static long BuyInMax { get { Read(); return buyInMax; } }
    public static int CommissionPct { get { Read(); return commissionPct; } }
    public static long? CommissionCap { get { Read(); return commissionCap; } }
    public static DateTime CloseAtUtc { get { Read(); return closeAt; } }

    /// <summary>Smallest stack that can play a hand: the boot (Teen Patti) or the big blind (Poker).</summary>
    public static long MinStackToPlay => IsPoker ? BigBlind : Boot;

    public static bool MayStartHand(DateTime utcNow) => utcNow < CloseAtUtc - NoNewHandBefore;

    /// <summary>Tests and local runs: use these values instead of the environment.</summary>
    public static void Override(int game, int seats, long bootOrSmall, long big, long minBuyIn, long maxBuyIn, int pct, DateTime closeUtc, string tierName = "test")
    {
        read = true;
        gameId = game; maxPlayers = seats; tier = tierName;
        if (game == PokerBoard) { smallBlind = bootOrSmall; bigBlind = big; boot = 0; }
        else { boot = bootOrSmall; smallBlind = bigBlind = 0; }
        buyInMin = minBuyIn; buyInMax = maxBuyIn; commissionPct = pct; commissionCap = null; closeAt = closeUtc;
    }

    public static string Describe()
    {
        Read();
        return IsTable
            ? $"table game {gameId}, {maxPlayers} seats, tier {tier}, " + (IsPoker ? $"blinds {smallBlind}/{bigBlind}" : $"boot {boot}") +
              $", buy-in {buyInMin}-{buyInMax}, commission {commissionPct}%{(commissionCap.HasValue ? " cap " + commissionCap : "")}, closes {closeAt:u}"
            : "not a table (2-player match)";
    }
}
