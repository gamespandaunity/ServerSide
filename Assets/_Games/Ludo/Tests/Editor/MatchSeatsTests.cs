using NUnit.Framework;

namespace LudoGame.Tests
{
    public class MatchSeatsTests
    {
        [TearDown]
        public void ResetSeats() => MatchSeats.Override(MatchSeats.Parse(null, null));

        [Test]
        public void TwoPlayerDeploymentsLeaveSeatsOff()
        {
            Assert.IsFalse(MatchSeats.Parse(null, null).Active, "no MaxPlayers = 2 players");
            Assert.IsFalse(MatchSeats.Parse("2", "10363,10194").Active);
            Assert.IsFalse(MatchSeats.Parse("4", "10363,10194,77").Active, "a seat list that does not fill 4 seats is ignored");
            Assert.IsFalse(MatchSeats.Parse("abc", "1,2,3,4").Active);
        }

        [Test]
        public void SeatsFollowTheDeploymentOrder()
        {
            var t = MatchSeats.Parse("4", " 10363, 10194 ,501,502 ");
            Assert.IsTrue(t.Active);
            Assert.AreEqual(4, t.Count);
            Assert.AreEqual(0, t.SeatOf("10363"));
            Assert.AreEqual(2, t.SeatOf("501"));
            Assert.AreEqual("502", t.IdOf(3));
            Assert.AreEqual(-1, t.SeatOf("999"));
            Assert.IsNull(t.IdOf(4));
        }

        [Test]
        public void DuplicateIdsAreDropped()
        {
            var t = MatchSeats.Parse("4", "1,2,2,3");
            Assert.AreEqual(3, t.Count);
            Assert.IsFalse(t.Active);
        }

        [Test]
        public void AConnectionGetsTheSeatOfItsUserNotOfItsArrivalOrder()
        {
            var t = MatchSeats.Parse("4", "1,2,3,4");
            Assert.AreEqual(3, t.Bind(10, "4", "Dee"));
            Assert.AreEqual(0, t.Bind(11, "1", "Ann"));
            Assert.AreEqual(3, t.SeatOfConnection(10));
            Assert.AreEqual("Dee", t.NameOf(3));
            Assert.AreEqual(2, t.ConnectedCount);
            Assert.IsFalse(t.AllConnected);
        }

        [Test]
        public void AUserWithoutASeatIsRefused()
        {
            var t = MatchSeats.Parse("4", "1,2,3,4");
            Assert.AreEqual(-1, t.Bind(10, "99", "Mallory"));
            Assert.AreEqual(0, t.ConnectedCount);
        }

        [Test]
        public void AReconnectTakesTheSeatBack()
        {
            var t = MatchSeats.Parse("4", "1,2,3,4");
            t.Bind(10, "3", null);
            Assert.AreEqual(2, t.Unbind(10));
            Assert.IsFalse(t.IsConnected(2));
            Assert.AreEqual(2, t.Bind(20, "3", null), "new connection, same seat");
            Assert.AreEqual(2, t.SeatOfConnection(20));
            Assert.AreEqual(-1, t.SeatOfConnection(10));
            Assert.AreEqual(-1, t.Unbind(10), "the old connection holds nothing");
        }

        [Test]
        public void AConnectionHoldsOneSeat()
        {
            var t = MatchSeats.Parse("4", "1,2,3,4");
            t.Bind(10, "1", null);
            t.Bind(10, "2", null);
            Assert.IsFalse(t.IsConnected(0), "claiming another user moves the connection, it does not hold both");
            Assert.AreEqual(1, t.SeatOfConnection(10));
        }

        [Test]
        public void EverySeatConnected()
        {
            var t = MatchSeats.Parse("4", "1,2,3,4");
            for (int i = 0; i < 4; i++) t.Bind(100 + i, (i + 1).ToString(), null);
            Assert.IsTrue(t.AllConnected);
            MatchSeats.Override(t);
            Assert.IsTrue(MatchSeats.Active);
        }

        [Test]
        public void NegativeConnectionIdsCountAsConnected()
        {
            // The ids KCP gave the four players of room 014aafc4-ca0e-1 (17 Oct): three are negative.
            var t = MatchSeats.Parse("4", "10498,10499,10215,10363");
            Assert.AreEqual(3, t.Bind(-875039474, "10363", "Wasi khan1"));
            Assert.AreEqual(0, t.Bind(-1061978277, "10498", "donald trump"));
            Assert.AreEqual(2, t.Bind(-875039476, "10215", "Player Champ"));
            Assert.AreEqual(1, t.Bind(1765878189, "10499", "Narendra Modi"));
            Assert.AreEqual(4, t.ConnectedCount);
            Assert.IsTrue(t.AllConnected);
            Assert.AreEqual(0, t.SeatOfConnection(-1061978277));
            Assert.IsTrue(t.IsConnected(3));

            Assert.AreEqual(0, t.Unbind(-1061978277));
            Assert.AreEqual(3, t.ConnectedCount);
            Assert.IsFalse(t.IsConnected(0));
            Assert.AreEqual(-1, t.SeatOfConnection(-1061978277));
        }
    }
}
