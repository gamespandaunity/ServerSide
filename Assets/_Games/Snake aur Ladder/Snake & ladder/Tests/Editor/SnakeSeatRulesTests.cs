using NUnit.Framework;

namespace Snake_Ladder.Tests
{
    public class SnakeSeatRulesTests
    {
        [Test]
        public void TwoSeatsAlternate()
        {
            var r = new SnakeSeatRules(2, 1);
            Assert.AreEqual(0, r.NextTurn());
            r.Turn = 0;
            Assert.AreEqual(1, r.NextTurn());
        }

        [Test]
        public void TurnsGoRoundAndSkipSeatsThatAreOut()
        {
            var r = new SnakeSeatRules(4, 0);
            Assert.AreEqual(1, r.NextTurn());
            r.Out(1, SnakeSeatRules.State.Left);
            Assert.AreEqual(2, r.NextTurn());
            r.Turn = 3;
            Assert.AreEqual(0, r.NextTurn(), "wraps to seat 0");
            r.Out(0, SnakeSeatRules.State.Abandoned);
            Assert.AreEqual(2, r.NextTurn());
        }

        [Test]
        public void ThreeSeatsWork()
        {
            var r = new SnakeSeatRules(3, 2);
            Assert.AreEqual(0, r.NextTurn());
            r.Turn = 0;
            Assert.AreEqual(1, r.NextTurn());
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SnakeSeatRules(1, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SnakeSeatRules(5, 0));
        }

        [Test]
        public void ThreeMissesInARowPutASeatOut()
        {
            var r = new SnakeSeatRules(4, 0);
            Assert.IsFalse(r.Miss(2));
            Assert.IsFalse(r.Miss(2));
            r.Rolled(2);
            Assert.AreEqual(0, r.Missed[2], "a roll starts the count again");
            Assert.IsFalse(r.Miss(2));
            Assert.IsFalse(r.Miss(2));
            Assert.IsTrue(r.Miss(2), "third miss in a row");
            Assert.AreEqual(SnakeSeatRules.State.Eliminated, r.States[2]);
            Assert.IsFalse(r.Miss(2), "a seat that is out does not miss again");
        }

        [Test]
        public void TheLastSeatStillPlayingIsFound()
        {
            var r = new SnakeSeatRules(4, 0);
            Assert.AreEqual(-1, r.LastPlaying());
            r.Out(0, SnakeSeatRules.State.Left);
            r.Out(1, SnakeSeatRules.State.Abandoned);
            Assert.AreEqual(-1, r.LastPlaying());
            r.Out(3, SnakeSeatRules.State.Eliminated);
            Assert.AreEqual(2, r.LastPlaying());
            Assert.AreEqual(1, r.PlayingCount);
            Assert.IsFalse(r.Out(3, SnakeSeatRules.State.Left), "already out");
        }

        [Test]
        public void TimeOutGoesToTheFurthestToken()
        {
            var r = new SnakeSeatRules(4, 0);
            r.Moved(0, 40);
            r.Moved(1, 72);
            r.Moved(2, 55);
            Assert.AreEqual(1, r.Leader());
        }

        [Test]
        public void ATieGoesToWhoeverGotThereFirst()
        {
            var r = new SnakeSeatRules(4, 0);
            r.Moved(3, 64);
            r.Moved(1, 64);
            Assert.AreEqual(3, r.Leader(), "seat 3 reached 64 first");
            r.Moved(3, 30);
            r.Moved(3, 64);
            Assert.AreEqual(1, r.Leader(), "seat 3 left 64 and came back later");
        }

        [Test]
        public void ANeverMovedTieGoesToTheLowerSeat()
        {
            var r = new SnakeSeatRules(3, 0);
            Assert.AreEqual(0, r.Leader());
            r.Out(0, SnakeSeatRules.State.Left);
            Assert.AreEqual(1, r.Leader());
        }

        [Test]
        public void SeatsThatAreOutNeverLead()
        {
            var r = new SnakeSeatRules(4, 0);
            r.Moved(2, 99);
            r.Moved(0, 10);
            r.Out(2, SnakeSeatRules.State.Left);
            Assert.AreEqual(0, r.Leader());
        }
    }
}
