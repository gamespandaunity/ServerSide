using System;

namespace Twelve
{
    public static class TwelveRulesEngine
    {
        public const byte NoPiece = byte.MaxValue;
        private const int PieceCount = 24;
        private const int HistoryLength = 4;
        private const byte ScoreToWin = 12;

        public static void CreateInitialState(ref TwelveState state)
        {
            state.cells = new byte[TwelveBoardTopology.CellCount];
            for (byte node = 0; node < 12; node++) state.cells[node] = (byte)(node + 1);
            for (byte node = 13; node < 25; node++) state.cells[node] = node;
            state.turn = PLAYERS.PLAYER1;
            state.player1Score = 0;
            state.player2Score = 0;
            state.forcedPieceId = NoPiece;
            state.phase = TwelveMatchPhase.Playing;
            state.outcome = TwelveOutcome.None;
            state.endReason = TwelveMatchEndReason.None;
            state.revision = 0;
            state.matchSecondsRemaining = 300;
            state.turnSecondsRemaining = 30f;
            state.repetitionNodes = new byte[PieceCount * HistoryLength];
            state.repetitionCounts = new byte[PieceCount];
            Array.Fill(state.repetitionNodes, TwelveBoardTopology.NoNode);
            state.lastMovedPieceP1 = NoPiece;
            state.lastMovedPieceP2 = NoPiece;
        }

        public static int GetLegalMoves(in TwelveState state, PLAYERS player, Span<TwelveMove> output)
        {
            if (!IsInitialized(state) || state.phase != TwelveMatchPhase.Playing ||
                (player != PLAYERS.PLAYER1 && player != PLAYERS.PLAYER2)) return 0;

            int totalBoardLegal = 0;
            int unrestricted = 0;
            Span<TwelveMove> boardLegal = stackalloc TwelveMove[128];
            Span<bool> repetitive = stackalloc bool[128];
            for (byte from = 0; from < TwelveBoardTopology.CellCount; from++)
            {
                byte piece = state.cells[from];
                if (Owner(piece) != player) continue;
                if (state.forcedPieceId != NoPiece && piece != state.forcedPieceId) continue;
                AddBoardLegalMoves(state, player, piece, from, boardLegal, repetitive,
                    ref totalBoardLegal, ref unrestricted);
            }

            bool allowRepetitionFallback = unrestricted == 0;
            int written = 0;
            for (int i = 0; i < totalBoardLegal && written < output.Length; i++)
            {
                if (repetitive[i] && !allowRepetitionFallback) continue;
                output[written++] = boardLegal[i];
            }
            return written;
        }

        public static int GetLegalMovesFrom(in TwelveState state, byte fromNode, Span<TwelveMove> output)
        {
            if (!IsInitialized(state) || fromNode >= TwelveBoardTopology.CellCount) return 0;
            PLAYERS owner = Owner(state.cells[fromNode]);
            if (owner == PLAYERS.EMPTY) return 0;

            Span<TwelveMove> allMoves = stackalloc TwelveMove[128];
            int count = GetLegalMoves(state, owner, allMoves);
            int written = 0;
            for (int i = 0; i < count && written < output.Length; i++)
                if (allMoves[i].from == fromNode) output[written++] = allMoves[i];
            return written;
        }

        public static TwelveApplyResult TryApplyMove(ref TwelveState state, PLAYERS actor, byte from, byte to)
        {
            EnsureInitialized(ref state);
            TwelveMove emptyMove = new TwelveMove(NoPiece, from, to, TwelveBoardTopology.NoNode);
            if (state.phase != TwelveMatchPhase.Playing)
                return Rejected(TwelveMoveRejectReason.MatchNotPlaying, emptyMove, state);
            if (actor != PLAYERS.PLAYER1 && actor != PLAYERS.PLAYER2)
                return Rejected(TwelveMoveRejectReason.InvalidActor, emptyMove, state);
            if (state.turn != actor)
                return Rejected(TwelveMoveRejectReason.OutOfTurn, emptyMove, state);
            if (from >= TwelveBoardTopology.CellCount || to >= TwelveBoardTopology.CellCount)
                return Rejected(TwelveMoveRejectReason.NodeOutOfRange, emptyMove, state);

            byte piece = state.cells[from];
            TwelveMove move = new TwelveMove(piece, from, to, TwelveBoardTopology.NoNode);
            if (piece == 0) return Rejected(TwelveMoveRejectReason.EmptyOrigin, move, state);
            if (Owner(piece) != actor) return Rejected(TwelveMoveRejectReason.NotOwner, move, state);
            if (state.cells[to] != 0) return Rejected(TwelveMoveRejectReason.DestinationOccupied, move, state);
            if (state.forcedPieceId != NoPiece && piece != state.forcedPieceId)
                return Rejected(TwelveMoveRejectReason.ForcedPieceRequired, move, state);

            byte capturedNode;
            if (!TwelveBoardTopology.TryResolve(from, to, out capturedNode))
                return Rejected(TwelveMoveRejectReason.NotConnected, move, state);
            move = new TwelveMove(piece, from, to, capturedNode);
            if (state.forcedPieceId != NoPiece && !move.IsCapture)
                return Rejected(TwelveMoveRejectReason.CaptureContinuationRequired, move, state);
            if (move.IsCapture && (Owner(state.cells[capturedNode]) == PLAYERS.EMPTY ||
                Owner(state.cells[capturedNode]) == actor))
                return Rejected(TwelveMoveRejectReason.MissingEnemyMidpoint, move, state);

            Span<TwelveMove> legalMoves = stackalloc TwelveMove[128];
            int legalCount = GetLegalMoves(state, actor, legalMoves);
            bool found = false;
            for (int i = 0; i < legalCount; i++)
            {
                if (legalMoves[i].from == from && legalMoves[i].to == to)
                {
                    move = legalMoves[i];
                    found = true;
                    break;
                }
            }
            if (!found) return Rejected(TwelveMoveRejectReason.RepetitionBlocked, move, state);

            bool wasChainContinuation = state.forcedPieceId != NoPiece;
            state.cells[from] = 0;
            state.cells[to] = piece;
            if (move.IsCapture)
            {
                state.cells[move.capturedNode] = 0;
                if (actor == PLAYERS.PLAYER1) state.player1Score++;
                else state.player2Score++;
            }

            if (!wasChainContinuation) RecordHistory(ref state, actor, piece, to);

            bool chainContinues = move.IsCapture && HasUnrestrictedCapture(state, actor, piece, to);
            bool turnChanged = false;
            if (chainContinues)
                state.forcedPieceId = piece;
            else
            {
                state.forcedPieceId = NoPiece;
                state.turn = Opponent(actor);
                turnChanged = true;
            }

            TwelveMatchResult match = default;
            if (state.player1Score >= ScoreToWin)
                match = Finish(ref state, TwelveOutcome.Player1, TwelveMatchEndReason.ScoreLimit);
            else if (state.player2Score >= ScoreToWin)
                match = Finish(ref state, TwelveOutcome.Player2, TwelveMatchEndReason.ScoreLimit);
            else if (turnChanged && !HasAnyLegalMove(state, state.turn))
                match = Finish(ref state, OutcomeFor(actor), TwelveMatchEndReason.NoLegalMove);

            if (match.ended)
            {
                state.forcedPieceId = NoPiece;
                chainContinues = false;
            }
            state.revision++;
            return new TwelveApplyResult(true, TwelveMoveRejectReason.None, move, chainContinues,
                turnChanged, state.revision, match);
        }

        public static TwelveTurnResult ExpireTurn(ref TwelveState state)
        {
            EnsureInitialized(ref state);
            PLAYERS expired = state.turn;
            if (state.phase != TwelveMatchPhase.Playing ||
                (expired != PLAYERS.PLAYER1 && expired != PLAYERS.PLAYER2))
                return new TwelveTurnResult(expired, state.turn, false, state.revision, default);

            state.forcedPieceId = NoPiece;
            state.turn = Opponent(expired);
            TwelveMatchResult match = default;
            if (!HasAnyLegalMove(state, state.turn))
                match = Finish(ref state, OutcomeFor(expired), TwelveMatchEndReason.NoLegalMove);
            state.revision++;
            return new TwelveTurnResult(expired, state.turn, true, state.revision, match);
        }

        public static TwelveMatchResult ExpireMatch(ref TwelveState state)
        {
            EnsureInitialized(ref state);
            if (state.phase == TwelveMatchPhase.Finished)
                return new TwelveMatchResult(true, state.outcome, state.endReason);
            TwelveOutcome outcome = state.player1Score == state.player2Score
                ? TwelveOutcome.Draw
                : state.player1Score > state.player2Score ? TwelveOutcome.Player1 : TwelveOutcome.Player2;
            return Finish(ref state, outcome, TwelveMatchEndReason.MatchTimeout);
        }

        public static bool HasAnyLegalMove(in TwelveState state, PLAYERS player)
        {
            Span<TwelveMove> one = stackalloc TwelveMove[1];
            return GetLegalMoves(state, player, one) > 0;
        }

        public static PLAYERS Owner(byte pieceId)
        {
            if (pieceId >= 1 && pieceId <= 12) return PLAYERS.PLAYER1;
            if (pieceId >= 13 && pieceId <= 24) return PLAYERS.PLAYER2;
            return PLAYERS.EMPTY;
        }

        private static void AddBoardLegalMoves(in TwelveState state, PLAYERS player, byte piece,
            byte from, Span<TwelveMove> moves, Span<bool> repetitive, ref int count, ref int unrestricted)
        {
            ReadOnlySpan<TwelveTopologyEdge> nodeEdges = TwelveBoardTopology.GetEdges(from);
            bool chain = state.forcedPieceId != NoPiece;
            for (int i = 0; i < nodeEdges.Length && count < moves.Length; i++)
            {
                TwelveTopologyEdge edge = nodeEdges[i];
                if (!chain && state.cells[edge.adjacent] == 0)
                    AddCandidate(state, piece, from, edge.adjacent, TwelveBoardTopology.NoNode,
                        moves, repetitive, ref count, ref unrestricted);

                if (edge.landing == TwelveBoardTopology.NoNode || state.cells[edge.landing] != 0)
                    continue;
                PLAYERS middleOwner = Owner(state.cells[edge.adjacent]);
                if (middleOwner == PLAYERS.EMPTY || middleOwner == player) continue;
                AddCandidate(state, piece, from, edge.landing, edge.adjacent,
                    moves, repetitive, ref count, ref unrestricted);
            }
        }

        private static void AddCandidate(in TwelveState state, byte piece, byte from, byte to,
            byte captured, Span<TwelveMove> moves, Span<bool> repetitive, ref int count, ref int unrestricted)
        {
            bool isRepetitive = IsRepetitive(state, piece, to);
            moves[count] = new TwelveMove(piece, from, to, captured);
            repetitive[count] = isRepetitive;
            count++;
            if (!isRepetitive) unrestricted++;
        }

        private static bool HasUnrestrictedCapture(in TwelveState state, PLAYERS player,
            byte piece, byte from)
        {
            ReadOnlySpan<TwelveTopologyEdge> nodeEdges = TwelveBoardTopology.GetEdges(from);
            for (int i = 0; i < nodeEdges.Length; i++)
            {
                TwelveTopologyEdge edge = nodeEdges[i];
                if (edge.landing == TwelveBoardTopology.NoNode || state.cells[edge.landing] != 0)
                    continue;
                PLAYERS middleOwner = Owner(state.cells[edge.adjacent]);
                if (middleOwner != PLAYERS.EMPTY && middleOwner != player &&
                    !IsRepetitive(state, piece, edge.landing)) return true;
            }
            return false;
        }

        private static bool IsRepetitive(in TwelveState state, byte piece, byte destination)
        {
            if (piece < 1 || piece > PieceCount || state.repetitionCounts[piece - 1] < HistoryLength)
                return false;
            int offset = (piece - 1) * HistoryLength;
            return state.repetitionNodes[offset] == state.repetitionNodes[offset + 2] &&
                   state.repetitionNodes[offset + 1] == state.repetitionNodes[offset + 3] &&
                   state.repetitionNodes[offset] == destination;
        }

        private static void RecordHistory(ref TwelveState state, PLAYERS actor, byte piece, byte node)
        {
            byte previous = actor == PLAYERS.PLAYER1 ? state.lastMovedPieceP1 : state.lastMovedPieceP2;
            if (previous != NoPiece && previous != piece) ClearHistory(ref state, previous);
            if (actor == PLAYERS.PLAYER1) state.lastMovedPieceP1 = piece;
            else state.lastMovedPieceP2 = piece;

            int pieceIndex = piece - 1;
            int offset = pieceIndex * HistoryLength;
            int count = state.repetitionCounts[pieceIndex];
            if (count < HistoryLength)
            {
                state.repetitionNodes[offset + count] = node;
                state.repetitionCounts[pieceIndex]++;
                return;
            }
            for (int i = 0; i < HistoryLength - 1; i++)
                state.repetitionNodes[offset + i] = state.repetitionNodes[offset + i + 1];
            state.repetitionNodes[offset + HistoryLength - 1] = node;
        }

        private static void ClearHistory(ref TwelveState state, byte piece)
        {
            if (piece < 1 || piece > PieceCount) return;
            int pieceIndex = piece - 1;
            int offset = pieceIndex * HistoryLength;
            state.repetitionCounts[pieceIndex] = 0;
            for (int i = 0; i < HistoryLength; i++)
                state.repetitionNodes[offset + i] = TwelveBoardTopology.NoNode;
        }

        private static TwelveMatchResult Finish(ref TwelveState state, TwelveOutcome outcome,
            TwelveMatchEndReason reason)
        {
            state.phase = TwelveMatchPhase.Finished;
            state.outcome = outcome;
            state.endReason = reason;
            state.forcedPieceId = NoPiece;
            return new TwelveMatchResult(true, outcome, reason);
        }

        private static TwelveApplyResult Rejected(TwelveMoveRejectReason reason, TwelveMove move,
            in TwelveState state)
        {
            return new TwelveApplyResult(false, reason, move, false, false, state.revision, default);
        }

        private static PLAYERS Opponent(PLAYERS player)
        {
            return player == PLAYERS.PLAYER1 ? PLAYERS.PLAYER2 : PLAYERS.PLAYER1;
        }

        private static TwelveOutcome OutcomeFor(PLAYERS player)
        {
            return player == PLAYERS.PLAYER1 ? TwelveOutcome.Player1 : TwelveOutcome.Player2;
        }

        private static bool IsInitialized(in TwelveState state)
        {
            return state.cells != null && state.cells.Length == TwelveBoardTopology.CellCount &&
                   state.repetitionNodes != null && state.repetitionNodes.Length == PieceCount * HistoryLength &&
                   state.repetitionCounts != null && state.repetitionCounts.Length == PieceCount;
        }

        private static void EnsureInitialized(ref TwelveState state)
        {
            if (!IsInitialized(state)) CreateInitialState(ref state);
        }
    }
}
