using System;
using NUnit.Framework;

namespace Twelve.Tests
{
    public class TwelveRulesTests
    {
        [Test]
        public void InitialStatePreservesPieceIdentityAndHasFourOpeningMoves()
        {
            TwelveState state = default;
            TwelveRulesEngine.CreateInitialState(ref state);

            Assert.AreEqual(25, state.cells.Length);
            for (int node = 0; node < 12; node++) Assert.AreEqual(node + 1, state.cells[node]);
            Assert.AreEqual(0, state.cells[12]);
            for (int node = 13; node < 25; node++) Assert.AreEqual(node, state.cells[node]);

            TwelveMove[] moves = new TwelveMove[64];
            Assert.AreEqual(4, TwelveRulesEngine.GetLegalMoves(state, PLAYERS.PLAYER1, moves));
        }

        [Test]
        public void HumanCaptureIsNotGloballyForced()
        {
            TwelveState state = EmptyPlayingState(PLAYERS.PLAYER1);
            state.cells[0] = 1;
            state.cells[1] = 13;
            state.cells[5] = 2;

            TwelveMove[] moves = new TwelveMove[64];
            int count = TwelveRulesEngine.GetLegalMoves(state, PLAYERS.PLAYER1, moves);

            Assert.IsTrue(Contains(moves, count, 0, 2, true));
            Assert.IsTrue(Contains(moves, count, 5, 6, false));
        }

        [Test]
        public void CaptureChainForcesSamePieceAndOnlyCaptureContinuations()
        {
            TwelveState state = EmptyPlayingState(PLAYERS.PLAYER1);
            state.cells[0] = 1;
            state.cells[1] = 13;
            state.cells[3] = 14;
            state.cells[5] = 2;

            TwelveApplyResult first = TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER1, 0, 2);
            Assert.IsTrue(first.applied);
            Assert.IsTrue(first.chainContinues);
            Assert.AreEqual(1, state.forcedPieceId);
            Assert.AreEqual(PLAYERS.PLAYER1, state.turn);

            TwelveMove[] moves = new TwelveMove[64];
            int count = TwelveRulesEngine.GetLegalMoves(state, PLAYERS.PLAYER1, moves);
            Assert.AreEqual(1, count);
            Assert.AreEqual(2, moves[0].from);
            Assert.AreEqual(4, moves[0].to);
            Assert.IsTrue(moves[0].IsCapture);

            TwelveApplyResult wrongPiece = TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER1, 5, 6);
            Assert.IsFalse(wrongPiece.applied);
            Assert.AreEqual(TwelveMoveRejectReason.ForcedPieceRequired, wrongPiece.rejectReason);

            TwelveApplyResult second = TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER1, 2, 4);
            Assert.IsTrue(second.applied);
            Assert.IsFalse(second.chainContinues);
            Assert.AreEqual(PLAYERS.PLAYER2, state.turn);
            Assert.AreEqual(2, state.player1Score);
        }

        [Test]
        public void RepetitionBlocksWhenAlternativeExistsAndFallsBackWhenItDoesNot()
        {
            TwelveState state = EmptyPlayingState(PLAYERS.PLAYER1);
            state.cells[0] = 1;
            state.cells[23] = 13;
            RepeatBackAndForth(ref state);

            TwelveApplyResult blocked = TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER1, 0, 1);
            Assert.IsFalse(blocked.applied);
            Assert.AreEqual(TwelveMoveRejectReason.RepetitionBlocked, blocked.rejectReason);

            for (int i = 0; i < state.cells.Length; i++) state.cells[i] = 13;
            state.cells[0] = 1;
            state.cells[1] = 0;
            state.turn = PLAYERS.PLAYER1;
            TwelveApplyResult fallback = TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER1, 0, 1);
            Assert.IsTrue(fallback.applied, "A-B-A-B must be allowed when no unrestricted move exists.");
        }

        [Test]
        public void MovingDifferentPieceClearsPreviousPieceHistory()
        {
            TwelveState state = EmptyPlayingState(PLAYERS.PLAYER1);
            state.cells[0] = 1;
            state.cells[23] = 13;
            RepeatBackAndForth(ref state);

            state.cells[5] = 2;
            state.turn = PLAYERS.PLAYER1;
            Assert.IsTrue(TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER1, 5, 6).applied);
            state.turn = PLAYERS.PLAYER1;
            Assert.IsTrue(TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER1, 0, 1).applied);
        }

        [Test]
        public void ScoreLimitAndMatchTimeoutProduceExpectedOutcomes()
        {
            TwelveState scoreState = EmptyPlayingState(PLAYERS.PLAYER1);
            scoreState.player1Score = 11;
            scoreState.cells[0] = 1;
            scoreState.cells[1] = 13;
            TwelveApplyResult winningCapture = TwelveRulesEngine.TryApplyMove(ref scoreState, PLAYERS.PLAYER1, 0, 2);
            Assert.IsTrue(winningCapture.match.ended);
            Assert.AreEqual(TwelveOutcome.Player1, winningCapture.match.outcome);
            Assert.AreEqual(TwelveMatchEndReason.ScoreLimit, winningCapture.match.reason);

            TwelveState drawState = EmptyPlayingState(PLAYERS.PLAYER1);
            drawState.player1Score = 4;
            drawState.player2Score = 4;
            Assert.AreEqual(TwelveOutcome.Draw, TwelveRulesEngine.ExpireMatch(ref drawState).outcome);

            TwelveState p2State = EmptyPlayingState(PLAYERS.PLAYER1);
            p2State.player1Score = 3;
            p2State.player2Score = 4;
            Assert.AreEqual(TwelveOutcome.Player2, TwelveRulesEngine.ExpireMatch(ref p2State).outcome);
        }

        [Test]
        public void ExpireTurnClearsForcedPieceAndChecksNextPlayerMobility()
        {
            TwelveState state = EmptyPlayingState(PLAYERS.PLAYER1);
            state.cells[0] = 1;
            state.forcedPieceId = 1;

            TwelveTurnResult result = TwelveRulesEngine.ExpireTurn(ref state);

            Assert.IsTrue(result.turnChanged);
            Assert.AreEqual(TwelveRulesEngine.NoPiece, state.forcedPieceId);
            Assert.IsTrue(result.match.ended);
            Assert.AreEqual(TwelveOutcome.Player1, result.match.outcome);
            Assert.AreEqual(TwelveMatchEndReason.NoLegalMove, result.match.reason);
        }

        private static TwelveState EmptyPlayingState(PLAYERS turn)
        {
            TwelveState state = default;
            TwelveRulesEngine.CreateInitialState(ref state);
            Array.Clear(state.cells, 0, state.cells.Length);
            state.turn = turn;
            return state;
        }

        private static void RepeatBackAndForth(ref TwelveState state)
        {
            Assert.IsTrue(TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER1, 0, 1).applied);
            Assert.IsTrue(TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER2, 23, 24).applied);
            Assert.IsTrue(TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER1, 1, 0).applied);
            Assert.IsTrue(TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER2, 24, 23).applied);
            Assert.IsTrue(TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER1, 0, 1).applied);
            Assert.IsTrue(TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER2, 23, 24).applied);
            Assert.IsTrue(TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER1, 1, 0).applied);
            Assert.IsTrue(TwelveRulesEngine.TryApplyMove(ref state, PLAYERS.PLAYER2, 24, 23).applied);
        }

        private static bool Contains(TwelveMove[] moves, int count, byte from, byte to, bool capture)
        {
            for (int i = 0; i < count; i++)
                if (moves[i].from == from && moves[i].to == to && moves[i].IsCapture == capture) return true;
            return false;
        }
    }
}
