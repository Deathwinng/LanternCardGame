using System;
using System.ComponentModel.DataAnnotations;

namespace LanternCardGame.Data
{
    public class PlayerStats
    {
        public PlayerStats()
        {
            this.Id = Guid.NewGuid().ToString();
            this.RegistrationDate = DateTime.UtcNow;
            this.GamesStarted = 0;
            this.GamesFinished = 0;
            this.GamesLeft = 0;
            this.GamesWon = 0;
            this.GamesPlacedLast = 0;
        }

        [Key]
        public string Id { get; set; }

        public DateTime RegistrationDate { get; set; }

        [Range(0, int.MaxValue)]
        public int GamesStarted { get; set; }

        [Range(0, int.MaxValue)]
        public int GamesFinished { get; set; }

        [Range(0, int.MaxValue)]
        public int GamesLeft { get; set; }

        [Range(0, int.MaxValue)]
        public int GamesWon { get; set; }

        [Range(0, int.MaxValue)]
        public int GamesPlacedLast { get; set; }

        public virtual ApplicationUser User { get; set; }
    }
}
