using UnityEngine;
using System.Collections;

namespace Tanks.UI
{
    /// <summary>
    /// Model an element in the leaderboard
    /// </summary>
    public class LeaderboardElement
    {
        private readonly string m_Description;

        public string description
        {
            get { return m_Description; }
        }

        public Color m_Color;

        public Color color
        {
            get { return m_Color; }
        }

        private readonly int m_Score;

        public int score
        {
            get { return m_Score; }
        }

        private int m_Rank;

        public int rank
        {
            get { return m_Rank; }
        }

        public bool isLocal;

        public string m_countryFlag;
        public int m_awatarId;

        public void SetLocal(bool isLocal)
        {
            this.isLocal = isLocal;
        }
        public void SetRank(int pRank)
        {
            m_Rank = pRank;
        }

        public LeaderboardElement(string description, Color color, int score, string flag, int awatarid)
        {
            this.m_Description = description;
            this.m_Color = color;
            this.m_Score = score;
            m_countryFlag = flag;
            m_awatarId = awatarid;

        }
    }
}