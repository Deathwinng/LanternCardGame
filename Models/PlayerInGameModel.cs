using LanternCardGame.Game.Cards;

namespace LanternCardGame.Models
{
    public class PlayerInGameModel : PlayerModel
    {
        public PlayerInGameModel(
            string id,
            string username)
            : base(id, username)
        {
        }

        public PlayerDeck Deck { get; set; }
    }
}
