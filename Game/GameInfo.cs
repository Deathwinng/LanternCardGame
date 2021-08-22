using LanternCardGame.Game.Cards;
using System.Collections.Generic;

namespace LanternCardGame.Game
{
    public class GameInfo
    {
        private readonly string gameId;
        private readonly string currentTurnPlayerUsername;
        private readonly IDictionary<string, int> playersCardsNumber;
        private readonly IDictionary<string, int> playersPoints;
        private readonly int deckCardsLeft;
        private readonly Card discardDeckTopCard;
        private readonly int roundsPlayed;
        private readonly int rotationsPerRoundsPlayed;

        public GameInfo()
        {
            this.gameId = string.Empty;
            this.currentTurnPlayerUsername = string.Empty;
            this.playersCardsNumber = new Dictionary<string, int>();
            this.playersPoints = new Dictionary<string, int>();
            this.deckCardsLeft = 0;
            this.discardDeckTopCard = null;
            this.roundsPlayed = 0;
            this.rotationsPerRoundsPlayed = 0;
        }

        public GameInfo(
            string gameId,
            string currentTurnPlayerUsername,
            IDictionary<string, int> playersCardsNumber,
            IDictionary<string, int> playersPoints,
            int deckCardsLeft,
            Card discardDeckTopCard,
            int roundsPlayed,
            int rotationsPerRoundsPlayed)
        {
            this.gameId = gameId;
            this.currentTurnPlayerUsername = currentTurnPlayerUsername;
            this.playersCardsNumber = playersCardsNumber;
            this.playersPoints = playersPoints;
            this.deckCardsLeft = deckCardsLeft;
            this.discardDeckTopCard = discardDeckTopCard;
            this.roundsPlayed = roundsPlayed;
            this.rotationsPerRoundsPlayed = rotationsPerRoundsPlayed;
        }

        public string GameId => this.gameId;

        public string CurrentTurnPlayerUsername => this.currentTurnPlayerUsername;

        public IDictionary<string, int> PlayersCardsNumber => this.playersCardsNumber;

        public IDictionary<string, int> PlayersPoints => this.playersPoints;

        public int DeckCardsLeft => this.deckCardsLeft;

        public Card DiscardDeckTopCard => this.discardDeckTopCard;

        public int RoundsPlayed => this.roundsPlayed;

        public int RotationsPerRoundsPlayed => this.rotationsPerRoundsPlayed;
    }
}
