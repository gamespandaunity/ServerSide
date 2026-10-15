using Mirror;

/// <summary>
/// A game that runs a table of more than 2 seats (4-player Ludo, 2–4-player Snake & Ladder). The shared networking
/// (MirrorPlayerPrefab, MirrorNetwork, NetworkGameManager) reports seat events to whichever game is active, so the
/// 2-player paths ("the other player wins") never run on such a table.
/// </summary>
public interface IMultiSeatTable
{
    /// <summary>Server: a connection took seat <paramref name="seat"/>.</summary>
    void ServerSeatConnected(int seat);

    /// <summary>Server: the connection holding <paramref name="seat"/> dropped.</summary>
    void ServerSeatDisconnected(int seat);

    /// <summary>Server: the player on <paramref name="seat"/> pressed Leave or quit.</summary>
    void ServerSeatLeft(int seat, string why);

    /// <summary>Phones and server: this table has more than 2 seats.</summary>
    bool IsMultiSeat { get; }
}

public static class MultiSeatTable
{
    /// <summary>The active game's table, registered by the game while its scene runs.</summary>
    public static IMultiSeatTable Current;

    public static void Register(IMultiSeatTable table) => Current = table;

    public static void Unregister(IMultiSeatTable table)
    {
        if (Current == table) Current = null;
    }

    /// <summary>Phones and server: the game on screen runs a table of more than 2 seats.</summary>
    public static bool IsMultiSeat => Current != null && Current.IsMultiSeat;

    public static void ServerSeatConnected(int seat)
    {
        if (NetworkServer.active && MatchSeats.Active) Current?.ServerSeatConnected(seat);
    }

    public static void ServerSeatDisconnected(int seat)
    {
        if (NetworkServer.active && MatchSeats.Active) Current?.ServerSeatDisconnected(seat);
    }

    /// <summary>Server: a Leave / quit on an N-seat table. Returns true when the table handled it, so the 2-player
    /// leave settlement must not run.</summary>
    public static bool ServerTakesSeatLeave(NetworkConnectionToClient sender, DisconnectReason reason)
    {
        if (!NetworkServer.active || !MatchSeats.Active) return false;
        if (reason != DisconnectReason.UserRequested && reason != DisconnectReason.ApplicationQuit) return true;
        int seat = sender == null ? -1 : MatchSeats.Current.SeatOfConnection(sender.connectionId);
        if (Current == null || seat < 0)
        {
            UnityEngine.Debug.LogWarning($"[Seats] Leave from conn={sender?.connectionId} ignored (seat {seat}, table {(Current == null ? "not ready" : "ready")}).");
            return true;
        }
        Current.ServerSeatLeft(seat, reason == DisconnectReason.UserRequested ? "pressed Leave" : "quit the app");
        return true;
    }
}
