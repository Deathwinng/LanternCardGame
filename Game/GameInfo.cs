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
        private readonly Card emptyDeckTopCard;
        private readonly int roundsPlayed;
        private readonly int rotationsPerRoundsPlayed;

        public GameInfo()
        {
            gameId = string.Empty;
            currentTurnPlayerUsername = string.Empty;
            playersCardsNumber = new Dictionary<string, int>();
            playersPoints = new Dictionary<string, int>();
            deckCardsLeft = 0;
            emptyDeckTopCard = null;
            roundsPlayed = 0;
            rotationsPerRoundsPlayed = 0;
        }

        public GameInfo(
            string gameId,
            string currentTurnPlayerUsername,
            IDictionary<string, int> playersCardsNumber,
            IDictionary<string, int> playersPoints,
            int deckCardsLeft,
            Card emptyDeckTopCard,
            int roundsPlayed,
            int rotationsPerRoundsPlayed)
        {
            this.gameId = gameId;
            this.currentTurnPlayerUsername = currentTurnPlayerUsername;
            this.playersCardsNumber = playersCardsNumber;
            this.playersPoints = playersPoints;
            this.deckCardsLeft = deckCardsLeft;
            this.emptyDeckTopCard = emptyDeckTopCard;
            this.roundsPlayed = roundsPlayed;
            this.rotationsPerRoundsPlayed = rotationsPerRoundsPlayed;
        }

        public string GameId => gameId;

        public string CurrentTurnPlayerUsername => currentTurnPlayerUsername;

        public IDictionary<string, int> PlayersCardsNumber => playersCardsNumber;

        public IDictionary<string, int> PlayersPoints => playersPoints;

        public int DeckCardsLeft => deckCardsLeft;

        public Card EmptyDeckTopCard => emptyDeckTopCard;

        public int RoundsPlayed => roundsPlayed;

        public int RotationsPerRoundsPlayed => rotationsPerRoundsPlayed;
    }
}
