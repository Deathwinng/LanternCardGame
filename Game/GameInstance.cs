using LanternCardGame.Extensions;
using LanternCardGame.Game.Cards;
using LanternCardGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace LanternCardGame.Game
{
    public class GameInstance
    {
        private readonly IList<PlayerModel> players;
        private string currentTurnPlayerId;
        private string startingRoudTurnPlayerId;
        private PlayerTurnAllowedMoves currentTurnPlayerAllowedMoves;
        private readonly int maxPoints;
        private readonly Timer turnTimer;
        private readonly Timer arragingTimer;
        private readonly bool timersEnabled;
        private int roundsPlayed;
        private int rotationsPerRoundsPlayed;
        private readonly IDictionary<string, PlayerDeck> playersDecks;
        private readonly IDictionary<string, int> playersPoints;
        private readonly IDictionary<string, int> playersLastRoundPoints;
        private readonly IDictionary<string, bool> playersReady;
        private Deck deck;
        private EmptyDeck emptyDeck;

        public GameInstance(
            string gameId,
            ICollection<PlayerModel> players,
            int maxPoints,
            int secondsPerTurn)
        {
            this.GameId = gameId;
            this.players = players.Shuffle().ToList();
            this.maxPoints = maxPoints;
            this.timersEnabled = secondsPerTurn > 0;
            if (this.timersEnabled)
            {
                this.turnTimer = new Timer(secondsPerTurn * 1000)
                {
                    AutoReset = false
                };
                this.arragingTimer = new Timer(secondsPerTurn * 1000)
                {
                    AutoReset = false
                };
            }

            this.deck = new Deck();
            this.emptyDeck = new EmptyDeck();
            this.currentTurnPlayerId = this.players.First().Id;
            this.startingRoudTurnPlayerId = this.currentTurnPlayerId;
            this.rotationsPerRoundsPlayed = 0;
            this.playersDecks = new Dictionary<string, PlayerDeck>(this.players.Count);
            this.playersPoints = new Dictionary<string, int>(this.players.Count);
            this.playersLastRoundPoints = new Dictionary<string, int>(this.players.Count);
            this.playersReady = new Dictionary<string, bool>(this.players.Count);
            this.roundsPlayed = 0;
            this.rotationsPerRoundsPlayed = 0;
            foreach (var player in this.players)
            {
                this.playersDecks.Add(player.Id, new PlayerDeck());
                this.playersReady.Add(player.Id, false);
                this.playersPoints.Add(player.Id, 0);
            }
        }

        public string GameId { get; set; }

        public string CurrentTurnPlayerId => this.currentTurnPlayerId;

        public string CurrentTurnPlayerUsername => this.players.First(x => x.Id == this.currentTurnPlayerId).Username;

        public PlayerTurnAllowedMoves CurrentPlayerTurnAllowedMoves => this.currentTurnPlayerAllowedMoves;

        public int DeckCardsLeft => this.deck.CardsLeft;

        public int MaxPoints => this.maxPoints;

        public bool AreMaxPointsReached => this.playersPoints.Any(kvp => kvp.Value >= this.maxPoints);

        public bool AllPlayersReady => this.playersReady.All(kvp => kvp.Value == true);

        public int NumberOfPlayersReady => this.playersReady.Where(kvp => kvp.Value == true).Count();

        public bool AllPlayersLeft => this.players.Count == 0;

        public int RoundsPlayed => this.roundsPlayed;

        public int RotationsPerRoundsPlayed => this.rotationsPerRoundsPlayed;

        public bool RoundOver { get; set; }

        public string RoundWinner { get; set; }

        public ICollection<PlayerModel> Players => this.players;

        public int PlayersCount => this.players.Count;

        public Card DrawCard(int cardId)
        {
            var card = this.deck.allCards.FirstOrDefault(x => x.Id == cardId);
            this.deck.allCards.Remove(card);
            return card;
        }

        public ICollection<Card> SeeAllCardsInDeck()
        {
            return this.deck.allCards;
        }

        public void PutCardInDeck(string playerId, Card card)
        {
            this.deck.allCards.Add(card);
            this.RemoveCardFromPlayerDeck(playerId, card.Id);
        }

        public void GiveJokerToPlayer(string playerId)
        {
            this.DoesPlayerExists(playerId);
            var joker = this.deck.allCards.FirstOrDefault(x => x.CardType == CardType.Joker);
            this.GetPlayerDeck(playerId).AddCardToDeck(joker);
            this.deck.allCards.Remove(joker);
        }

        public void PlayerReady(string playerId)
        {
            this.DoesPlayerExists(playerId);
            this.playersReady[playerId] = true;
        }

        public void ResetPlayersReady()
        {
            foreach (var kvp in this.playersReady)
            {
                this.playersReady[kvp.Key] = false;
            }
        }

        public void StartGame()
        {
            for (int i = 0; i < 9; i++)
            {
                foreach (var player in players)
                {
                    var card = this.deck.GetNextCard();
                    this.GetPlayerDeck(player.Id).AddCardToDeck(card);
                }
            }

            this.emptyDeck.AddCard(this.deck.GetNextCard());
        }

        public void Restart()
        {
            foreach (var player in players)
            {
                this.playersPoints[player.Id] = 0;
            }

            this.StartNewRound();
            this.roundsPlayed = 0;
            this.rotationsPerRoundsPlayed = 0;
        }

        public void StartNewRound()
        {
            this.roundsPlayed++;
            this.currentTurnPlayerId = this.startingRoudTurnPlayerId;
            this.SetNextPlayerTurn();
            this.rotationsPerRoundsPlayed = 0;
            this.startingRoudTurnPlayerId = this.currentTurnPlayerId;
            this.RoundWinner = null;
            this.RoundOver = false;
            this.deck = new Deck();
            this.emptyDeck = new EmptyDeck();
            foreach (var player in players)
            {
                this.playersDecks[player.Id] = new PlayerDeck();
            }

            this.ResetPlayersReady();
            this.StartGame();
        }

        public ICollection<Card> AddCardToPlayerDeck(string playerId, Card card)
        {
            this.DoesPlayerExists(playerId);
            var playerdeck = this.GetPlayerDeck(playerId);
            playerdeck.AddCardToDeck(card);
            return playerdeck.AllCards;
        }

        public Card RemoveCardFromPlayerDeck(string playerId, int cardId)
        {
            this.DoesPlayerExists(playerId);
            var playerdeck = this.GetPlayerDeck(playerId);
            var card = playerdeck.GetCardFromDeck(cardId);
            return card;
        }

        public ICollection<Card> GetPlayerCards(string playerId)
        {
            this.DoesPlayerExists(playerId);
            return this.GetPlayerDeck(playerId).AllCards;
        }

        public int GetNumberOfPlayerCards(string playerId)
        {
            this.DoesPlayerExists(playerId);
            return this.GetPlayerDeck(playerId).AllCards.Count;
        }

        public void SetCurrentPlayerAlowedMoves(PlayerTurnAllowedMoves allowedMoves)
        {
            this.currentTurnPlayerAllowedMoves = allowedMoves;
        }

        public Card DrawNextCard() => this.deck.GetNextCard();

        public Card PeekEmptyDeckNextCard() => this.emptyDeck.PeekNextCard();

        public Card DrawEmptyDeckNextCard() => this.emptyDeck.GetNextCard();

        public void PutCardInEmptyDeck(Card card)
        {
            this.emptyDeck.AddCard(card);
        }

        public string SetNextPlayerTurn()
        {
            var player = this.players.First(x => x.Id == this.currentTurnPlayerId);
            var index = this.players.IndexOf(player);
            if (index < this.players.Count - 1)
            {
                this.currentTurnPlayerId = this.players[index + 1].Id;
            }
            else
            {
                this.currentTurnPlayerId = this.players[0].Id;
                this.rotationsPerRoundsPlayed++;
            }

            return this.currentTurnPlayerId;
        }

        public int AddToPlayerPoints(string playerId, int points)
        {
            this.DoesPlayerExists(playerId);
            this.playersPoints[playerId] += points;
            this.playersLastRoundPoints[playerId] = points;
            return this.playersPoints[playerId];
        }

        public int SubstractFromPlayerPoints(string playerId, int points)
        {
            this.DoesPlayerExists(playerId);
            this.playersPoints[playerId] -= points;
            this.playersLastRoundPoints[playerId] = -points;
            return this.playersPoints[playerId];
        }

        public int GetPlayerPoints(string playerId)
        {
            this.DoesPlayerExists(playerId);
            return this.playersPoints[playerId];
        }

        public IDictionary<string, int> GetAllPlayerPoints()
        {
            return this.playersPoints;
        }

        public IDictionary<string, int> GetLastRoundGainedPlayerPoints()
        {
            return this.playersLastRoundPoints;
        }

        public ICollection<Card> RearrangePlayerCards(string playerId, ICollection<Card> cards)
        {
            this.DoesPlayerExists(playerId);
            return this.GetPlayerDeck(playerId).RearrangeCards(cards);
        }

        public void PlayerLeftGame(string playerId)
        {
            this.DoesPlayerExists(playerId);
            var player = this.players.First(x => x.Id == playerId);
            this.players.Remove(player);
        }

        public void RestartTurnTimer()
        {
            if (!this.timersEnabled)
            {
                return;
            }

            this.turnTimer.Stop();
            this.turnTimer.Start();
        }

        public void StopTurnTimer()
        {
            if (!this.timersEnabled)
            {
                return;
            }

            this.turnTimer.Stop();
        }

        public void DisposeTurnTimer()
        {
            this.turnTimer?.Dispose();
        }

        public void AddTurnTimerElapsedEvent(ElapsedEventHandler eventHandler)
        {
            if (!this.timersEnabled)
            {
                return;
            }

            this.turnTimer.Elapsed += eventHandler;
        }

        public void RemoveTurnTimerElapsedEvent(ElapsedEventHandler eventHandler)
        {
            if (!this.timersEnabled)
            {
                return;
            }

            this.turnTimer.Elapsed -= eventHandler;
        }

        public void RestartArragingTimer()
        {
            if (!this.timersEnabled)
            {
                return;
            }

            this.arragingTimer.Stop();
            this.arragingTimer.Start();
        }

        public void StopArragingTimer()
        {
            if (!this.timersEnabled)
            {
                return;
            }

            this.arragingTimer.Stop();
        }

        public void DisposeArragingTimer()
        {
            this.arragingTimer?.Dispose();
        }

        public void AddArragingTimerElapsedEvent(ElapsedEventHandler eventHandler)
        {
            if (!this.timersEnabled)
            {
                return;
            }

            this.arragingTimer.Elapsed += eventHandler;
        }

        public void RemoveArragingTimerElapsedEvent(ElapsedEventHandler eventHandler)
        {
            if (!this.timersEnabled)
            {
                return;
            }

            this.arragingTimer.Elapsed -= eventHandler;
        }

        private PlayerDeck GetPlayerDeck(string playerId)
        {
            this.DoesPlayerExists(playerId);
            return this.playersDecks[playerId];
        }

        private void DoesPlayerExists(string playerId)
        {
            if (!this.players.Any(x => x.Id == playerId))
            {
                throw new Exception("Player doesn't exist.");
            }
        }
    }
}
