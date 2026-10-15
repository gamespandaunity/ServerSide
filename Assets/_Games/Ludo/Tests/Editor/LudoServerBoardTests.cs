using System.Collections.Generic;
using NUnit.Framework;

namespace LudoGame.Tests
{
    public class LudoServerBoardTests
    {
        // ─── 2 players: identical to the board that shipped before the 4-player work ───────────────────────────

        [Test]
        public void TwoPlayerGamesMatchThePreviousBoardMoveForMove()
        {
            for (int seed = 1; seed <= 300; seed++)
            {
                var now = new LudoServerBoard(0, seed);
                var before = new PreviousTwoPlayerBoard(0, seed);
                var pick = new System.Random(seed * 7919);
                for (int move = 0; move < 600; move++)
                {
                    int pl = now.Turn;
                    Assert.AreEqual(before.Turn, pl);
                    int steps = now.Roll(pl);
                    Assert.AreEqual(before.Roll(pl), steps, $"seed {seed} move {move}: dice");

                    var legal = now.LegalPawns(pl, steps);
                    CollectionAssert.AreEqual(before.LegalPawns(pl, steps), legal, $"seed {seed} move {move}: legal pawns");
                    if (legal.Count == 0)
                    {
                        now.Turn = before.Turn = 1 - pl;
                        continue;
                    }
                    int pawn = legal[pick.Next(legal.Count)];
                    var a = now.Apply(pl, pawn, steps);
                    var b = before.Apply(pl, pawn, steps);
                    Assert.AreEqual(b.To, a.To);
                    Assert.AreEqual(b.Captured, a.Captured, $"seed {seed} move {move}: capture");
                    Assert.AreEqual(b.ExtraRoll, a.ExtraRoll);
                    Assert.AreEqual(b.Won, a.Won);
                    for (int s = 0; s < 2; s++)
                    {
                        CollectionAssert.AreEqual(before.Pos[s], now.Pos[s], $"seed {seed} move {move}: positions of {s}");
                        Assert.AreEqual(before.CanEnterHome[s], now.CanEnterHome[s]);
                    }
                    Assert.AreEqual(before.Snapshot(0), now.Snapshot(0));
                    if (a.Won) break;
                    if (!a.ExtraRoll)
                    {
                        Assert.AreEqual(1 - pl, now.NextTurn(), "2 players: the next turn is the other player");
                        now.Turn = before.Turn = 1 - pl;
                    }
                }
            }
        }

        [Test]
        public void TwoPlayerBoardUsesOppositeCorners()
        {
            var b = new LudoServerBoard(0, 1);
            CollectionAssert.AreEqual(new[] { 0, 2 }, b.Corner);
            Assert.IsTrue(b.SameSquare(0, 30, 1, 4), "26 apart is the same square");
            Assert.IsTrue(b.SameSquare(1, 30, 0, 4));
            Assert.IsFalse(b.SameSquare(0, 30, 1, 17));
        }

        // ─── 4 players ──────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void FourPlayerSquaresLineUpThirteenPerCorner()
        {
            var b = new LudoServerBoard(0, 1, 4);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, b.Corner);
            // Seat 1's start square is seat 0's square 13; seat 3's start is seat 0's square 39.
            Assert.IsTrue(b.SameSquare(0, 13, 1, 0));
            Assert.IsTrue(b.SameSquare(0, 26, 2, 0));
            Assert.IsTrue(b.SameSquare(0, 39, 3, 0));
            // Seat 3's square 13 wraps to seat 0's start.
            Assert.IsTrue(b.SameSquare(3, 13, 0, 0));
            Assert.IsTrue(b.SameSquare(2, 30, 1, 43));
            Assert.IsFalse(b.SameSquare(0, 13, 2, 0));
            // The home column is no one's track.
            Assert.IsFalse(b.SameSquare(0, 52, 1, 39));
        }

        [Test]
        public void ASinglePawnOfAnyColourIsCaptured()
        {
            for (int victim = 1; victim < 4; victim++)
            {
                var b = Playing4();
                int square = 3;                                 // seat 0's square 3, not a safe square
                b.Pos[0][0] = 1;
                b.Pos[victim][2] = Mod(square - 13 * victim);   // the same square, in the victim's own path
                var r = b.Apply(0, 0, 2);
                Assert.AreEqual(1, r.Captures.Count, $"victim seat {victim}");
                Assert.AreEqual(victim, r.CapturedSeat);
                Assert.AreEqual(2, r.Captured);
                Assert.AreEqual(-1, b.Pos[victim][2]);
                Assert.IsTrue(b.CanEnterHome[0], "a capture opens the home column");
                Assert.IsTrue(r.ExtraRoll);
            }
        }

        [Test]
        public void TwoPawnsOfOneColourAreABlock()
        {
            var b = Playing4();
            b.Pos[0][0] = 1;
            b.Pos[2][0] = Mod(3 - 26);
            b.Pos[2][1] = Mod(3 - 26);
            var r = b.Apply(0, 0, 2);
            Assert.AreEqual(0, r.Captures.Count);
            Assert.AreEqual(Mod(3 - 26), b.Pos[2][0]);
            Assert.IsFalse(b.CanEnterHome[0]);
        }

        [Test]
        public void OneLandingCanCaptureTwoColours()
        {
            var b = Playing4();
            b.Pos[0][0] = 1;
            b.Pos[1][3] = Mod(3 - 13);
            b.Pos[3][1] = Mod(3 - 39);
            var r = b.Apply(0, 0, 2);
            Assert.AreEqual(2, r.Captures.Count);
            Assert.AreEqual(-1, b.Pos[1][3]);
            Assert.AreEqual(-1, b.Pos[3][1]);
        }

        [Test]
        public void NothingIsCapturedOnASafeSquare()
        {
            var b = Playing4();
            b.Pos[0][0] = 4;
            b.Pos[1][0] = Mod(8 - 13);   // seat 0's square 8 is a star
            var r = b.Apply(0, 0, 4);
            Assert.AreEqual(0, r.Captures.Count);
            Assert.AreEqual(Mod(8 - 13), b.Pos[1][0]);
        }

        [Test]
        public void TheHomeColumnStaysLockedUntilACapture()
        {
            var b = Playing4();
            b.Pos[0][0] = 48;
            Assert.IsFalse(b.CanMove(0, 0, 4), "no capture yet: may not pass square 50");
            Assert.IsTrue(b.CanMove(0, 0, 2));
            b.CanEnterHome[0] = true;
            Assert.IsTrue(b.CanMove(0, 0, 4));
            b.Pos[0][0] = 52;
            Assert.IsFalse(b.CanMove(0, 0, 6), "never past home");
            Assert.IsTrue(b.CanMove(0, 0, 4), "exactly home");
        }

        [Test]
        public void TurnsGoRoundAndSkipPlayersWhoAreNotPlaying()
        {
            var b = Playing4();
            b.Turn = 0;
            Assert.AreEqual(1, b.NextTurn());
            b.Turn = 3;
            Assert.AreEqual(0, b.NextTurn());

            b.Leave(1);
            b.Turn = 0;
            Assert.AreEqual(2, b.NextTurn(), "a seat that left is skipped");

            b.State[2] = LudoServerBoard.SeatState.Finished;
            Assert.AreEqual(3, b.NextTurn(), "a finished seat is skipped");

            b.Leave(3);
            Assert.AreEqual(0, b.NextTurn(), "only the player on turn is left");
            Assert.AreEqual(1, b.PlayingCount);
        }

        [Test]
        public void ALeaverFreesTheBoard()
        {
            var b = Playing4();
            b.Pos[1][0] = 10;
            b.Pos[1][1] = 10;
            b.Leave(1);
            CollectionAssert.AreEqual(new[] { -1, -1, -1, -1 }, b.Pos[1]);
            Assert.IsFalse(b.CanMove(1, 0, 6), "a seat that left cannot move");

            // Its old block no longer stops anyone: a pawn of seat 1 left over (not possible after Leave) is ignored.
            b.Pos[0][0] = 1;
            b.Pos[1][2] = Mod(3 - 13);
            var r = b.Apply(0, 0, 2);
            Assert.AreEqual(0, r.Captures.Count, "pawns of a seat that left are never captured");
        }

        [Test]
        public void TheFirstFourHomeWinsAndIsRecorded()
        {
            var b = Playing4();
            b.CanEnterHome[2] = true;
            b.Pos[2] = new[] { LudoServerBoard.Home, LudoServerBoard.Home, LudoServerBoard.Home, 54 };
            var r = b.Apply(2, 3, 2);
            Assert.IsTrue(r.ReachedHome);
            Assert.IsTrue(r.Won);
            Assert.IsFalse(r.ExtraRoll);
            Assert.AreEqual(LudoServerBoard.SeatState.Finished, b.State[2]);
            CollectionAssert.AreEqual(new[] { 2 }, b.FinishOrder);
        }

        [Test]
        public void RankingPutsFinishedFirstThenTokensHomeThenProgressThenLeavers()
        {
            var b = Playing4();
            b.State[3] = LudoServerBoard.SeatState.Finished;
            b.FinishOrder.Add(3);
            b.Pos[0] = new[] { LudoServerBoard.Home, 10, -1, -1 };   // 1 home
            b.Pos[1] = new[] { 40, 40, 40, -1 };                      // 0 home, progress 120
            b.Pos[2] = new[] { 5, -1, -1, -1 };
            b.Leave(2);
            CollectionAssert.AreEqual(new[] { 3, 0, 1, 2 }, b.Ranking());
        }

        [Test]
        public void SnapshotV2CarriesEverySeat()
        {
            var b = Playing4();
            b.Turn = 2;
            b.PendingSteps = 5;
            b.Pos[1][0] = 12;
            b.CanEnterHome[3] = true;
            b.Leave(0);
            Assert.AreEqual("v2;4;2;5;-1;-1;-1;-1;12;-1;-1;-1;-1;-1;-1;-1;-1;-1;-1;-1;0;0;0;1;2;0;0;0;", b.SnapshotV2());
        }

        [Test]
        public void OnlyTwoToFourPlayers()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new LudoServerBoard(0, 1, 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new LudoServerBoard(0, 1, 5));
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, new LudoServerBoard(0, 1, 3).Corner);
        }

        static LudoServerBoard Playing4() => new LudoServerBoard(0, 42, 4);

        static int Mod(int a) => ((a % 52) + 52) % 52;

        /// <summary>The 2-player board exactly as it was before seats were added (commit de8aec109), kept as the
        /// reference the current board must reproduce.</summary>
        class PreviousTwoPlayerBoard
        {
            const int Home = 56, LastTrack = 50, TrackLength = 52, Opposite = 26;
            static readonly HashSet<int> Safe = new HashSet<int> { 0, 8, 13, 21, 26, 34, 39, 47 };
            public readonly int[][] Pos = { new[] { -1, -1, -1, -1 }, new[] { -1, -1, -1, -1 } };
            public readonly bool[] CanEnterHome = new bool[2];
            public int Turn;
            readonly int[] diceIndex = new int[2], nextSix = new int[2], sixStreak = new int[2];
            readonly System.Random rng;

            public PreviousTwoPlayerBoard(int firstTurn, int seed)
            {
                Turn = firstTurn;
                rng = new System.Random(seed);
                nextSix[0] = rng.Next(3, 8);
                nextSix[1] = rng.Next(3, 8);
            }

            public int Roll(int pl)
            {
                int steps = rng.Next(1, 7);
                if (steps == 6 || diceIndex[pl] == nextSix[pl]) { nextSix[pl] = rng.Next(4, 8); steps = 6; diceIndex[pl] = 0; }
                if (steps == 6) { sixStreak[pl]++; if (sixStreak[pl] == 2) { steps = rng.Next(1, 6); sixStreak[pl] = 0; } }
                else sixStreak[pl] = 0;
                diceIndex[pl]++;
                return steps;
            }

            bool CanMove(int pl, int pawn, int steps)
            {
                int from = Pos[pl][pawn];
                if (from < 0) return steps == 6;
                if (from >= Home) return false;
                int to = from + steps;
                if (to > Home) return false;
                if (to > LastTrack && !CanEnterHome[pl]) return false;
                return true;
            }

            public List<int> LegalPawns(int pl, int steps)
            {
                var list = new List<int>();
                for (int p = 0; p < 4; p++) if (CanMove(pl, p, steps)) list.Add(p);
                return list;
            }

            public struct Result { public int To, Captured; public bool ExtraRoll, Won; }

            public Result Apply(int pl, int pawn, int steps)
            {
                int from = Pos[pl][pawn];
                var r = new Result { Captured = -1, To = from < 0 ? 0 : from + steps };
                Pos[pl][pawn] = r.To;
                if (r.To <= LastTrack && !Safe.Contains(r.To))
                {
                    int opp = 1 - pl, count = 0, which = -1;
                    for (int q = 0; q < 4; q++)
                    {
                        int op = Pos[opp][q];
                        if (op >= 0 && op <= LastTrack && (((r.To - op) % TrackLength) + TrackLength) % TrackLength == Opposite) { count++; which = q; }
                    }
                    if (count == 1) { Pos[opp][which] = -1; r.Captured = which; CanEnterHome[pl] = true; }
                }
                bool home = r.To == Home;
                r.Won = true;
                for (int q = 0; q < 4; q++) if (Pos[pl][q] != Home) r.Won = false;
                r.ExtraRoll = !r.Won && (steps == 6 || r.Captured >= 0 || home);
                return r;
            }

            public string Snapshot(int creatorPl)
            {
                int joinerPl = 1 - creatorPl;
                var sb = new System.Text.StringBuilder();
                for (int q = 0; q < 4; q++) sb.Append(Pos[creatorPl][q]).Append(';');
                for (int q = 0; q < 4; q++) sb.Append(Pos[joinerPl][q]).Append(';');
                sb.Append(Turn).Append(';').Append(CanEnterHome[creatorPl] ? 1 : 0).Append(';').Append(CanEnterHome[joinerPl] ? 1 : 0).Append(';');
                return sb.ToString();
            }
        }
    }
}
