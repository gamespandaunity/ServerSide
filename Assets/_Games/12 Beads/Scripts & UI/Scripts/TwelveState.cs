using System;

namespace Twelve
{
    public enum PLAYERS : byte
    {
        EMPTY,
        PLAYER1,
        PLAYER2
    }

    public enum TwelveMatchPhase : byte
    {
        Waiting,
        Playing,
        Finished
    }

    public enum TwelveOutcome : byte
    {
        None,
        Player1,
        Player2,
        Draw
    }

    public enum TwelveMatchEndReason : byte
    {
        None,
        ScoreLimit,
        MatchTimeout,
        NoLegalMove,
        Forfeit
    }

    public enum TwelveMoveRejectReason : byte
    {
        None,
        MatchNotPlaying,
        InvalidActor,
        OutOfTurn,
        NodeOutOfRange,
        EmptyOrigin,
        NotOwner,
        DestinationOccupied,
        NotConnected,
        MissingEnemyMidpoint,
        ForcedPieceRequired,
        CaptureContinuationRequired,
        RepetitionBlocked
    }

    public readonly struct TwelveMove
    {
        public readonly byte pieceId;
        public readonly byte from;
        public readonly byte to;
        public readonly byte capturedNode;

        public bool IsCapture => capturedNode != TwelveBoardTopology.NoNode;

        public TwelveMove(byte pieceId, byte from, byte to, byte capturedNode)
        {
            this.pieceId = pieceId;
            this.from = from;
            this.to = to;
            this.capturedNode = capturedNode;
        }
    }

    public readonly struct TwelveMatchResult
    {
        public readonly bool ended;
        public readonly TwelveOutcome outcome;
        public readonly TwelveMatchEndReason reason;

        public TwelveMatchResult(bool ended, TwelveOutcome outcome, TwelveMatchEndReason reason)
        {
            this.ended = ended;
            this.outcome = outcome;
            this.reason = reason;
        }
    }

    public readonly struct TwelveTurnResult
    {
        public readonly PLAYERS expiredPlayer;
        public readonly PLAYERS nextPlayer;
        public readonly bool turnChanged;
        public readonly uint newRevision;
        public readonly TwelveMatchResult match;

        public TwelveTurnResult(PLAYERS expiredPlayer, PLAYERS nextPlayer, bool turnChanged,
            uint newRevision, TwelveMatchResult match)
        {
            this.expiredPlayer = expiredPlayer;
            this.nextPlayer = nextPlayer;
            this.turnChanged = turnChanged;
            this.newRevision = newRevision;
            this.match = match;
        }
    }

    public readonly struct TwelveApplyResult
    {
        public readonly bool applied;
        public readonly TwelveMoveRejectReason rejectReason;
        public readonly TwelveMove move;
        public readonly bool chainContinues;
        public readonly bool turnChanged;
        public readonly uint newRevision;
        public readonly TwelveMatchResult match;

        public TwelveApplyResult(bool applied, TwelveMoveRejectReason rejectReason, TwelveMove move,
            bool chainContinues, bool turnChanged, uint newRevision, TwelveMatchResult match)
        {
            this.applied = applied;
            this.rejectReason = rejectReason;
            this.move = move;
            this.chainContinues = chainContinues;
            this.turnChanged = turnChanged;
            this.newRevision = newRevision;
            this.match = match;
        }
    }

    public struct TwelveState
    {
        public byte[] cells;
        public PLAYERS turn;
        public byte player1Score;
        public byte player2Score;
        public byte forcedPieceId;
        public TwelveMatchPhase phase;
        public TwelveOutcome outcome;
        public TwelveMatchEndReason endReason;
        public uint revision;
        public int matchSecondsRemaining;
        public float turnSecondsRemaining;

        // Repetition data is part of the pure state but remains server/offline-only in networking.
        internal byte[] repetitionNodes;
        internal byte[] repetitionCounts;
        internal byte lastMovedPieceP1;
        internal byte lastMovedPieceP2;

        public TwelveState Clone()
        {
            TwelveState clone = this;
            clone.cells = cells != null ? (byte[])cells.Clone() : null;
            clone.repetitionNodes = repetitionNodes != null ? (byte[])repetitionNodes.Clone() : null;
            clone.repetitionCounts = repetitionCounts != null ? (byte[])repetitionCounts.Clone() : null;
            return clone;
        }
    }
}
