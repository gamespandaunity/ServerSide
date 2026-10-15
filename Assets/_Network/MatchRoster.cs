using System.Collections.Generic;

/// <summary>
/// The players of a match with more than 2 seats (4-player Ludo, docs/multiplayer-plans/ludo-4-player.md §3.1), in seat
/// order. The lobby sets <see cref="ExpectedSeats"/> when it joins such a room; the game scene then fills
/// <see cref="Players"/> from the match server's seat list (LudoNetworkManager.seatList), which is the one every phone
/// agrees on. staticVariables.OpponetProfile stays the "next seat" so code shared with 2-player games keeps working.
/// </summary>
public static class MatchRoster
{
    public struct Player
    {
        public int seat;
        public string userId;
        public string name;
        public string avatarUrl;
        public int state;
        public bool connected;
    }

    /// <summary>Seats of the match being joined; 0 or 2 = a normal 2-player match.</summary>
    public static int ExpectedSeats;

    public static readonly List<Player> Players = new List<Player>();

    // Avatars the lobby already knows (room list / invites), by user id.
    static readonly Dictionary<string, string> avatarUrls = new Dictionary<string, string>();

    public static bool IsMultiSeat => ExpectedSeats > 2;

    /// <summary>A normal match (or none): forget everything.</summary>
    public static void Clear()
    {
        ExpectedSeats = 0;
        Players.Clear();
        avatarUrls.Clear();
    }

    /// <summary>The lobby is joining a room with <paramref name="seats"/> seats.</summary>
    public static void Expect(int seats)
    {
        Clear();
        ExpectedSeats = seats;
    }

    public static void RememberAvatar(string userId, string url)
    {
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(url)) avatarUrls[userId] = url;
    }

    public static void SetFromSeatList(IList<LudoNetworkManager.SeatInfo> seats)
    {
        Players.Clear();
        for (int i = 0; i < seats.Count; i++)
        {
            var s = seats[i];
            avatarUrls.TryGetValue(s.playerId ?? "", out string url);
            Players.Add(new Player
            {
                seat = s.seat,
                userId = s.playerId,
                name = string.IsNullOrEmpty(s.name) ? "Player " + (s.seat + 1) : s.name,
                avatarUrl = url,
                state = s.state,
                connected = s.connected,
            });
        }
        if (seats.Count > 2) ExpectedSeats = seats.Count;
    }

    /// <summary>My seat, or -1.</summary>
    public static int SeatOf(string userId)
    {
        for (int i = 0; i < Players.Count; i++) if (Players[i].userId == userId) return Players[i].seat;
        return -1;
    }
}
