using System;
using System.Collections.Generic;

/// <summary>
/// Seats of a match with more than 2 players (4-player Ludo, docs/multiplayer-plans/ludo-4-player.md §4.2).
/// The backend deploys such a match with MaxPlayers (missing = 2) and Seats = the players' user ids in seat order
/// (seat 0 = the creator). A connection gets the seat of the user it is — checked against Seats, never taken from
/// what the phone asks for — and a user who is not in Seats is refused. The join ticket (JoinTicketGate) already
/// disconnects a connection whose ticket belongs to another user.
///
/// With 2 players (MaxPlayers missing or 2) <see cref="Active"/> is false and nothing here is used: the
/// creator/joiner roles in NetworkGameManager stay exactly as they were.
/// </summary>
public static class MatchSeats
{
    public sealed class Table
    {
        public readonly int MaxPlayers;
        readonly string[] ids;
        readonly string[] names;
        readonly int[] connection;   // connectionId per seat, -1 = nobody connected

        public Table(int maxPlayers, string[] seatIds)
        {
            MaxPlayers = maxPlayers;
            ids = seatIds;
            names = new string[seatIds.Length];
            connection = new int[seatIds.Length];
            for (int s = 0; s < connection.Length; s++) connection[s] = NoConnection;
        }

        /// <summary>An empty seat. Not -1 or "below 0": KCP connection ids are hashes and are often negative (17 Oct: three
        /// of four players had negative ids, so the table counted 1/4 connected and the match never started).</summary>
        public const int NoConnection = int.MinValue;

        public int Count => ids.Length;

        /// <summary>More than 2 players and a seat list that fits: the N-player code paths are on.</summary>
        public bool Active => MaxPlayers > 2 && ids.Length == MaxPlayers;

        public string IdOf(int seat) => seat >= 0 && seat < ids.Length ? ids[seat] : null;
        public string NameOf(int seat) => seat >= 0 && seat < names.Length ? names[seat] : null;

        public int SeatOf(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return -1;
            for (int s = 0; s < ids.Length; s++) if (ids[s] == userId) return s;
            return -1;
        }

        public int SeatOfConnection(int connectionId)
        {
            if (connectionId == NoConnection) return -1;
            for (int s = 0; s < connection.Length; s++) if (connection[s] == connectionId) return s;
            return -1;
        }

        public bool IsConnected(int seat) => seat >= 0 && seat < connection.Length && connection[seat] != NoConnection;

        public int ConnectedCount
        {
            get
            {
                int n = 0;
                for (int s = 0; s < connection.Length; s++) if (connection[s] != NoConnection) n++;
                return n;
            }
        }

        public bool AllConnected => ConnectedCount == ids.Length;

        /// <summary>Connection <paramref name="connectionId"/> says it is <paramref name="userId"/>: give it that
        /// user's seat (a reconnect takes the seat over from the old connection). -1 = not a player of this match.</summary>
        public int Bind(int connectionId, string userId, string name)
        {
            int seat = SeatOf(userId);
            if (seat < 0) return -1;
            int old = SeatOfConnection(connectionId);
            if (old >= 0 && old != seat) connection[old] = NoConnection;   // the same connection claimed another seat before
            connection[seat] = connectionId;
            if (!string.IsNullOrEmpty(name)) names[seat] = name;
            return seat;
        }

        /// <summary>The connection dropped; returns the seat it held, or -1.</summary>
        public int Unbind(int connectionId)
        {
            int seat = SeatOfConnection(connectionId);
            if (seat >= 0) connection[seat] = NoConnection;
            return seat;
        }
    }

    /// <summary>Build the table from the deployment's MaxPlayers and Seats values ("10363,10194,…").</summary>
    public static Table Parse(string maxPlayers, string seats)
    {
        int max = 2;
        if (!string.IsNullOrWhiteSpace(maxPlayers) && int.TryParse(maxPlayers.Trim(), out int m)) max = m;
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(seats))
            foreach (string part in seats.Split(','))
            {
                string id = part.Trim();
                if (id.Length > 0 && !list.Contains(id)) list.Add(id);
            }
        return new Table(max, list.ToArray());
    }

    static Table current;

    /// <summary>This server's seats, read once from the environment.</summary>
    public static Table Current
    {
        get
        {
            if (current == null) current = Parse(Env("MaxPlayers"), Env("Seats"));
            return current;
        }
    }

    /// <summary>The match has more than 2 players.</summary>
    public static bool Active => Current.Active;

    /// <summary>Tests and local runs: use these seats instead of the environment.</summary>
    public static void Override(Table table) => current = table;

    static string Env(string name)
    {
        try { return Environment.GetEnvironmentVariable(name); }
        catch (Exception) { return null; }
    }
}
