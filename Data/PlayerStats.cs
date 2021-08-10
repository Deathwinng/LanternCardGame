using System;

namespace LanternCardGame.Data
{
    public class PlayerStats
    {
        public DateTime RegistrationDate { get; set; }

        public int GamesFinished { get; set; }

        public int GamesLeft { get; set; }

        public int GamesWon { get; set; }

        public int GamesPlacedLast { get; set; }
    }
}
