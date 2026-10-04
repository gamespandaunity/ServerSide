using NetworkManagement;

namespace BallPool.Mechanics
{
    public class ReplayManager
    {
        private int GetBallsDataCount()
        {
            return DataManager.GetIntData("BallsDataCount");
        }
        public Impulse GetImpulse(int number)
        {
            return DataManager.ImpulseFromString(DataManager.GetStringData("Impulse_" + number));
        }
        public void SaveImpulse(Impulse impulse)
        {
            int number = GetReplayDataCount();
            DataManager.SetStringData("Impulse_" + number, DataManager.ImpulseToString(impulse));
        }
        public void AddReplayDataCount()
        {
            int number = GetReplayDataCount();
            DataManager.SetIntData("ReplayDataCount", number + 1);
        }
        public int GetReplayDataCount()
        {
            return DataManager.GetIntData("ReplayDataCount");
        }

        public void SaveReplay(int ballId, string date)
        {
            int number = GetReplayDataCount();
            int savedBallId = DataManager.GetIntData("BallsDataCount");
            if (ballId > savedBallId)
            {
                DataManager.SetIntData("BallsDataCount", ballId);
            }
            DataManager.SetStringData("ReplayData_" + ballId + "_" + number, date);
        }
        public string GetReplay(int ballId, int number)
        {
            return DataManager.GetStringData("ReplayData_" + ballId + "_" + number);
        }

        public void DeleteReplayData()
        {
            int replayDataCount = GetReplayDataCount();
            //int ballsDataCount = PlayerPrefs.GetInt("BallsDataCount");
            for (int number = 0; number < replayDataCount; number++)
            {
                for (int ballId = 0; ballId < 16; ballId++)
                {
                    DataManager.DeleteKeyData("ReplayData_" + ballId + "_" + number);
                }
                DataManager.DeleteKeyData("Impulse_" + number);
            }
            DataManager.DeleteKeyData("ReplayDataCount");
        }
    }
}
