using LanternCardGame.Game;
using LanternCardGame.Game.Cards;
using LanternCardGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LanternCardGame.Services
{
    public class GameService
    {
        private readonly ICollection<GameInstance> gameInstances;
        private readonly GameRoomsService roomService;
        private readonly OnlinePlayersService playersService;
        private readonly NotifyService notifyService;
        private readonly object balanceLock = new object();

        public GameService(
            GameRoomsService roomService,
            OnlinePlayersService playersService,
            NotifyService subscriptionService)
        {
            this.gameInstances = new HashSet<GameInstance>();
            this.roomService = roomService;
            this.playersService = playersService;
            this.notifyService = subscriptionService;
        }

        public ICollection<PlayerModel> GetPlayers(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).Players;
        }

        public string GetCurrentTurnPlayerId(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).CurrentTurnPlayerId;
        }

        public PlayerTurnAllowedMoves GetCurrentPlayerTurnAllowedMoves(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).CurrentPlayerTurnAllowedMoves;
        }

        public void StartGame(string gameId)
        {
            var gameInstance = this.CreateNewGameInstance(gameId);
            gameInstance.StartGame();
            this.notifyService.InvokeByGroup(gameId, "GameStarting");
        }

        public bool AllPlayersReady(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).AllPlayersReady;
        }

        public void PlayerReady(string gameId, string playerId)
        {
            lock (this.balanceLock)
            {
                this.DoesGameInstanceExists(gameId);
                var gameInstance = this.GetGameInstance(gameId);
                gameInstance.PlayerReady(playerId);
                var playerInstanceId = this.playersService.GetPlayerById(playerId).InstanceId;
                this.notifyService.InvokeByPlayer(playerInstanceId, "GetPlayerCards");
                if (gameInstance.AllPlayersReady)
                {
                    var allowedMoves = new PlayerTurnAllowedMoves(true, true, false, false);
                    gameInstance.SetCurrentPlayerAlowedMoves(allowedMoves);
                    this.UpdateGameInfo(gameId);
                    var currentPlayerId = gameInstance.CurrentTurnPlayerId;
                    var currentPlayerInstanceId = this.playersService.GetPlayerById(currentPlayerId).InstanceId;
                    notifyService.InvokeByGroup(gameId, "AllPlayersReady");
                    notifyService.InvokeByPlayer(currentPlayerInstanceId, "MyTurn");
                    gameInstance.ResetPlayersReady();
                }
            }
        }

        public void RoundOverPlayerReady(string gameId, string playerId)
        {
            lock (this.balanceLock)
            {
                this.DoesGameInstanceExists(gameId);
                var gameInstance = this.GetGameInstance(gameId);
                gameInstance.PlayerReady(playerId);
                gameInstance.AddToPlayerPoints(playerId, this.CalculatePlayerPoints(gameId, playerId));
                if (gameInstance.AllPlayersReady)
                {
                    if (gameInstance.IsMaxPointsReached)
                    {
                        this.UpdateGameInfo(gameId);
                        this.notifyService.InvokeByGroup(gameId, "GameOver");
                        gameInstance.ResetPlayersReady();
                        notifyService.InvokeByGroup(gameId, "NumberOfReplayReadyPlayers");

                        return;
                    }

                    gameInstance.StartNewRound();
                    this.notifyService.InvokeByGroup(gameId, "NewRoundStarting");
                    var currentPlayerId = gameInstance.CurrentTurnPlayerId;
                    var allowedMoves = new PlayerTurnAllowedMoves(true, true, false, false);
                    gameInstance.SetCurrentPlayerAlowedMoves(allowedMoves);
                    var playerInstanceId = playersService.GetPlayerById(currentPlayerId).InstanceId;
                    this.notifyService.InvokeByPlayer(playerInstanceId, "MyTurn");
                }
            }
        }

        public void GameOverPlayerReplay(string gameId, string playerId)
        {
            lock (this.balanceLock)
            {
                this.DoesGameInstanceExists(gameId);
                var gameInstance = this.GetGameInstance(gameId);
                gameInstance.PlayerReady(playerId);
                notifyService.InvokeByGroup(gameId, "NumberOfReplayReadyPlayers");
                if (gameInstance.AllPlayersReady)
                {
                    gameInstance.Restart();
                    var allowedMoves = new PlayerTurnAllowedMoves(true, true, false, false);
                    gameInstance.SetCurrentPlayerAlowedMoves(allowedMoves);
                    gameInstance.ResetPlayersReady();
                    var currentPlayerId = gameInstance.CurrentTurnPlayerId;
                    var currentPlayerInstanceId = this.playersService.GetPlayerById(currentPlayerId).InstanceId;
                    notifyService.InvokeByGroup(gameId, "NewRoundStarting");
                    notifyService.InvokeByPlayer(currentPlayerInstanceId, "MyTurn");
                    this.UpdateGameInfo(gameId);
                }
            }
        }

        public GameInfo GetGameInfo(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            var gameInstance = this.GetGameInstance(gameId);
            var playerCardsNumbers = new Dictionary<string, int>();
            var playersPoints = new Dictionary<string, int>();
            foreach (var player in gameInstance.Players)
            {
                playerCardsNumbers.Add(player.Username, gameInstance.GetNumberOfPlayerCards(player.Id));
                playersPoints.Add(player.Username, gameInstance.GetPlayerPoints(player.Id));
            }

            return new GameInfo(
                    gameId,
                    gameInstance.CurrentTurnPlayerUsername,
                    playerCardsNumbers,
                    playersPoints,
                    gameInstance.DeckCardsLeft,
                    gameInstance.PeekEmptyDeckNextCard(),
                    gameInstance.RoundsPlayed,
                    gameInstance.RotationsPerRoundsPlayed);
        }

        public int GetNumberOfPlayersReady(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).NumberOfPlayersReady;
        }

        public ICollection<Card> GetPlayerCards(string gameId, string playerId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).GetPlayerCards(playerId);
        }

        public int GetNumberOfPlayerCards(string gameId, string playerId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).GetNumberOfPlayerCards(playerId);
        }

        public int GetDeckCardsLeft(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).DeckCardsLeft;
        }

        public ICollection<Card> PlayerDrawNextCard(string gameId, string playerId)
        {
            this.DoesGameInstanceExists(gameId);
            this.IsPlayerAllowed(gameId, playerId);
            var gameInstance = this.GetGameInstance(gameId);
            if (!gameInstance.CurrentPlayerTurnAllowedMoves.DrawFromDeck || gameInstance.RoundOver)
            {
                throw new Exception("Move not allowed.");
            }

            var nextCard = gameInstance.DrawNextCard();
            var playerCards = gameInstance.AddCardToPlayerDeck(playerId, nextCard);
            var allowedMoves = new PlayerTurnAllowedMoves(false, false, true, false);
            gameInstance.SetCurrentPlayerAlowedMoves(allowedMoves);
            var playerInstanceId = playersService.GetPlayerById(gameInstance.CurrentTurnPlayerId).InstanceId;
            this.notifyService.InvokeByPlayer(playerInstanceId, "MyTurn");
            this.UpdateGameInfo(gameId);
            return playerCards;
        }

        public ICollection<Card> PlayerDrawEmptyDeckNextCard(string gameId, string playerId)
        {
            this.DoesGameInstanceExists(gameId);
            this.IsPlayerAllowed(gameId, playerId);
            var gameInstance = this.GetGameInstance(gameId);
            if (!gameInstance.CurrentPlayerTurnAllowedMoves.DrawFromEmptyDeck || gameInstance.RoundOver)
            {
                throw new Exception("Move not allowed.");
            }

            var nextCard = gameInstance.DrawEmptyDeckNextCard();
            var playerCards = gameInstance.AddCardToPlayerDeck(playerId, nextCard);
            var allowedMoves = new PlayerTurnAllowedMoves(false, false, true, false);
            gameInstance.SetCurrentPlayerAlowedMoves(allowedMoves);
            var pairGroups = this.GetPairGroups(playerCards.ToList());
            this.CheckIfPlayerCanLightUp(gameId, playerId, pairGroups);
            var playerInstanceId = this.playersService.GetPlayerById(gameInstance.CurrentTurnPlayerId).InstanceId;
            this.notifyService.InvokeByPlayer(playerInstanceId, "MyTurn");
            this.UpdateGameInfo(gameId);
            return playerCards;
        }

        public ICollection<Card> PlayerPutCardInEmptyDeck(string gameId, string playerId, int cardId)
        {
            lock (this.balanceLock)
            {
                this.DoesGameInstanceExists(gameId);
                this.IsPlayerAllowed(gameId, playerId);
                var gameInstance = this.GetGameInstance(gameId);
                if (!gameInstance.CurrentPlayerTurnAllowedMoves.PutToEmptyDeck || gameInstance.RoundOver)
                {
                    throw new Exception("Move not allowed.");
                }
                if (!gameInstance.GetPlayerCards(playerId).Any(x => x.Id == cardId))
                {
                    throw new Exception("Card not in player's deck.");
                }

                var card = gameInstance.RemoveCardFromPlayerDeck(playerId, cardId);
                gameInstance.PutCardInEmptyDeck(card);
                var drawDeckAllowed = gameInstance.DeckCardsLeft > 0;
                var allowedMoves = new PlayerTurnAllowedMoves(drawDeckAllowed, true, false, false);
                gameInstance.SetCurrentPlayerAlowedMoves(allowedMoves);
                var playerCards = gameInstance.GetPlayerCards(playerId);
                var pairGroups = this.GetPairGroups(playerCards.ToList());
                var canLightUp = this.CheckIfPlayerCanLightUp(gameId, playerId, pairGroups);

                if (canLightUp)
                {
                    var player = this.playersService.GetPlayerById(playerId);
                    gameInstance.RoundWinner = player.Username;
                    this.notifyService.InvokeByPlayer(player.InstanceId, "RoundWon");
                    this.notifyService.InvokeByGroupExcept(gameId, player.InstanceId, "RoundOver");
                    gameInstance.PlayerReady(playerId);
                    gameInstance.SubstractFromPlayerPoints(playerId, 10);
                    gameInstance.RoundOver = true;
                }
                else if (!drawDeckAllowed)
                {
                    this.notifyService.InvokeByGroup(gameId, "RoundOver");
                    gameInstance.RoundOver = true;
                }
                else
                {
                    var nextPlayerId = gameInstance.SetNextPlayerTurn();
                    var playerInstanceId = playersService.GetPlayerById(nextPlayerId).InstanceId;
                    this.notifyService.InvokeByPlayer(playerInstanceId, "MyTurn");
                }

                this.UpdateGameInfo(gameId);
                return playerCards;
            }
        }

        public void EndRound(string gameId)
        {
            this.notifyService.InvokeByGroup(gameId, "RoundOver");
            this.GetGameInstance(gameId).RoundOver = true;
        }

        public List<List<Card>> PlayerArrangeCards(string gameId, string playerId, ICollection<Card> cards)
        {
            this.DoesGameInstanceExists(gameId);
            var gameInstance = this.GetGameInstance(gameId);
            gameInstance.RearrangePlayerCards(playerId, cards);
            var pairGroups = this.GetPairGroups(cards.ToList());
            if (!gameInstance.RoundOver)
            {
                this.CheckIfPlayerCanLightUp(gameId, playerId, pairGroups);
                if (this.GetCurrentTurnPlayerId(gameId) == playerId)
                {
                    var playerInstanceId = playersService.GetPlayerById(playerId).InstanceId;
                    this.notifyService.InvokeByPlayer(playerInstanceId, "MyTurn");
                }
            }

            return pairGroups;
        }

        public int CalculatePlayerPoints(string gameId, string playerId)
        {
            this.DoesGameInstanceExists(gameId);
            var playerCards = this.GetGameInstance(gameId).GetPlayerCards(playerId).ToList();
            var pairGroups = this.GetPairGroups(playerCards.ToList()).Where(x => x.Count >= 3).SelectMany(x => x);
            var points = playerCards.Except(pairGroups).Where(x => x.CardType != CardType.Joker).Sum(x => (int)x.CardType);
            return points;
        }

        public string GetRoundWinner(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).RoundWinner;
        }

        public Card PeekEmptyDeckNextCard(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).PeekEmptyDeckNextCard();
        }

        public void LeaveGame(string gameId, string playerId)
        {
            lock (this.balanceLock)
            {
                this.DoesGameInstanceExists(gameId);
                this.roomService.RemovePlayerFromRoom(playerId, gameId, false);
                var gameInstance = this.GetGameInstance(gameId);
                gameInstance.PlayerLeftGame(playerId);
                this.notifyService.InvokeByGroup(gameId, "PlayerLeft");
                if (gameInstance.AllPlayersLeft)
                {
                    this.gameInstances.Remove(gameInstance);
                }
            }
        }

        public void DropGame(string gameId)
        {
            lock (this.balanceLock)
            {
                this.DoesGameInstanceExists(gameId);
                this.notifyService.InvokeByGroup(gameId, "GameDropped");
                this.roomService.DeleteRoom(gameId);
                var gameInstance = this.GetGameInstance(gameId);
                this.gameInstances.Remove(gameInstance);
            }
        }

        public ICollection<Card> SeeAllCardsInDeck(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).SeeAllCardsInDeck();
        }

        public Card GetCard(string gameId, int cardId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).DrawCard(cardId);
        }

        public void PutCardInPlayerDeck(string gameId, string playerId, Card card)
        {
            this.DoesGameInstanceExists(gameId);
            this.GetGameInstance(gameId).AddCardToPlayerDeck(playerId, card);
        }

        public void PlayerPutCardInDeck(string gameId, string playerId, Card card)
        {
            this.DoesGameInstanceExists(gameId);
            this.GetGameInstance(gameId).PutCardInDeck(playerId, card);
        }

        public List<List<Card>> GetPairGroups(List<Card> cards, int startIndex = 0)
        {
            var groups = new List<List<Card>>();
            return this.GetPairGroupsHelper(cards, startIndex, groups);
        }

        public List<List<Card>> GetPairGroupsHelper(List<Card> cards, int startIndex, List<List<Card>> groups)
        {
            var group1 = new List<Card>
            {
                cards[startIndex]
            };
            var index1 = startIndex + 1;
            for (int i = index1; i < cards.Count; i++)
            {
                var counter = 0;
                for (int k = 0; k < group1.Count; k++)
                {
                    if (cards[i].CardType == CardType.Joker)
                    {
                        group1.Add(cards[i]);
                        index1++;
                        break;
                    }
                    if (group1[k].CardType == cards[i].CardType ||
                        group1[k].CardType == CardType.Joker)
                    {
                        counter++;
                    }
                    if (k == group1.Count - 1 && k == counter - 1)
                    {
                        group1.Add(cards[i]);
                        index1++;
                        break;
                    }
                }
                if (index1 == i)
                {
                    break;
                }
            }
            if (group1.Count == 2 && group1.Any(x => x.CardType == CardType.Joker))
            {
                index1 -= 2;
            }
            else if (group1.Count == 1)
            {
                index1--;
            }

            var group2 = new List<Card>
            {
                cards[startIndex]
            };
            var index2 = startIndex + 1;
            for (int i = index2; i < cards.Count; i++)
            {
                var counter = 0;
                for (int k = 0; k < group2.Count; k++)
                {
                    if (cards[i].CardType == CardType.Joker && group2[group2.Count - 1].CardType != CardType.King)
                    {
                        group2.Add(cards[i]);
                        counter++;
                        index2++;
                        break;
                    }
                    if (group2[k].CardSuit == cards[i].CardSuit &&
                        (int)group2[k].CardType + (group2.Count - k) == (int)cards[i].CardType ||
                        group2[k].CardType == CardType.Joker)
                    {
                        counter++;
                    }
                    if (k == group2.Count - 1 && k == counter - 1) // 1 2 3   4
                    {                                                   // 0 1 2
                        group2.Add(cards[i]);
                        index2++;
                        break;
                    }
                }
                if (index2 == i)
                {
                    break;
                }
            }
            if (group2.Count == 2 && group2.Any(x => x.CardType == CardType.Joker))
            {
                index2 -= 2;
            }
            else if (group2.Count == 1)
            {
                index2--;
            }

            var index = startIndex + 1;
            if (index1 > startIndex || index2 > startIndex)
            {
                if (index1 > index2)
                {
                    groups.Add(group1);
                    index = index1;
                }
                else if (index2 > index1)
                {
                    groups.Add(group2);
                    index = index2;
                }
                else
                {
                    groups.Add(group1);
                    index = index1;
                }
            }
            if (index < cards.Count)
            {
                this.GetPairGroupsHelper(cards, index, groups);
            }

            return groups;
        }

        private bool CheckIfPlayerCanLightUp(string gameId, string playerId, List<List<Card>> pairGroups)
        {
            var numOfPairedCards = pairGroups.Where(x => x.Count > 2).Sum(x => x.Count);
            if (this.GetCurrentTurnPlayerId(gameId) == playerId)
            {
                var allowedMoves = this.GetCurrentPlayerTurnAllowedMoves(gameId);
                var newAllowedMoves = new PlayerTurnAllowedMoves(
                    allowedMoves.DrawFromDeck,
                    allowedMoves.DrawFromEmptyDeck,
                    allowedMoves.PutToEmptyDeck,
                    numOfPairedCards >= 9);
                this.GetGameInstance(gameId).SetCurrentPlayerAlowedMoves(newAllowedMoves);
            }

            return numOfPairedCards >= 9;
        }

        private GameInstance CreateNewGameInstance(string gameId)
        {
            if (this.GetGameInstance(gameId) != null)
            {
                throw new Exception("Game already exist.");
            }

            var room = this.roomService.GetRoom(gameId);
            if (room == null || !room.InGame)
            {
                throw new Exception("Invalid game Id.");
            }

            var players = room.Players;
            var gameInstance = new GameInstance(gameId, players, room.MaxPoints);
            this.gameInstances.Add(gameInstance);
            return gameInstance;
        }

        private void IsPlayerAllowed(string gameId, string playerId)
        {
            //if (this.userService.UserId != playerId)
            //{
            //    throw new Exception("Different player's identity.");
            //}
            if (this.GetGameInstance(gameId).CurrentTurnPlayerId != playerId)
            {
                throw new Exception("Different player's turn.");
            }
        }

        private void UpdateGameInfo(string gameId)
        {
            this.notifyService.InvokeByGroup(gameId, "UpdateGameInfo");
        }

        private GameInstance GetGameInstance(string gameId)
        {
            return this.gameInstances.FirstOrDefault(x => x.GameId == gameId);
        }

        private void DoesGameInstanceExists(string gameId)
        {
            if (this.GetGameInstance(gameId) == null)
            {
                throw new Exception("Invalid game Id.");
            }
        }
    }
}
