using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Result of an N-seat Ludo table (ludo-4-player plan §3.5): rows from the seat list and the board this phone shows,
/// drawn by the shared MultiSeatResult panel (place, colour, name, tokens home, left/disconnected, coins).
/// </summary>
public static class LudoFourPlayerResult
{
    public static void Show(string winnerId, string reason)
    {
        var gui = GameGUIController.insta;
        var ludo = LudoNetworkManager.instance;
        if (gui == null || ludo == null) return;

        var rows = new List<(MultiSeatResult.Row row, int home, int progress, bool gone)>();
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
            bool gone = LudoNetworkManager.IsOut(info.state);
            rows.Add((new MultiSeatResult.Row
            {
                seat = s,
                id = info.playerId,
                name = string.IsNullOrEmpty(info.name) ? "Player " + (s + 1) : info.name,
                detail = $"{home}/4 home",
                state = info.playerId == winnerId ? "winner"
                      : info.state == LudoNetworkManager.SeatLeft ? "left"
                      : info.state == LudoNetworkManager.SeatAbandoned ? "disconnected"
                      : info.state == LudoNetworkManager.SeatKicked ? "removed" : "playing",
            }, home, progress, gone));
        }
        rows.Sort((a, b) =>
        {
            if (a.row.id == winnerId) return -1;
            if (b.row.id == winnerId) return 1;
            if (a.gone != b.gone) return a.gone ? 1 : -1;
            int c = b.home.CompareTo(a.home);
            if (c == 0) c = b.progress.CompareTo(a.progress);
            return c != 0 ? c : a.row.seat.CompareTo(b.row.seat);
        });
        MultiSeatResult.Show(rows.ConvertAll(r => r.row), winnerId, reason);
    }
}
