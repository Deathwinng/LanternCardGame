using LanternCardGame.Game.Cards;
using System.Collections.Generic;

namespace LanternCardGame.Models
{
    public class EndRoundPlayerCards
    {
        public string PlayerUsername { get; set; }

        public ICollection<Card> Cards { get; set; }

        public List<List<int>> CardsPairGroupIds { get; set; }
    }
}
