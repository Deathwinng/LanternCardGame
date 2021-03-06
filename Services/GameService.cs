using LanternCardGame.Game;
using LanternCardGame.Game.Cards;
using LanternCardGame.Models;
using LanternCardGame.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace LanternCardGame.Services
{
    public class GameService
    {
        private readonly ICollection<GameInstance> gameInstances;
        private readonly GameRoomsService roomService;
        private readonly OnlinePlayersService playersService;
        private readonly NotificationService notificationService;
        private readonly NotifyService notifyService;
        private readonly object balanceLock = new object();

        public GameService(
            GameRoomsService roomService,
            OnlinePlayersService playersService,
            NotificationService notificationService,
            NotifyService subscriptionService)
        {
            this.gameInstances = new HashSet<GameInstance>();
            this.roomService = roomService;
            this.playersService = playersService;
            this.notificationService = notificationService;
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

        public void EnterGame(string gameId)
        {
            this.CreateNewGameInstance(gameId);
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
                if (gameInstance.AllPlayersReady)
                {
                    gameInstance.ResetPlayersReady();
                    this.notifyService.InvokeByGroup(gameId, "AllPlayersReady");
                    this.DealCards(gameId);
                }
            }
        }

        public void StartGame(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            var gameInstance = this.GetGameInstance(gameId);
            var allowedMoves = new PlayerTurnAllowedMoves(true, true, false, false);
            gameInstance.SetCurrentPlayerAlowedMoves(allowedMoves);
            this.InvokeUpdateGameInfo(gameId);
            var currentPlayerId = gameInstance.CurrentTurnPlayerId;
            var currentPlayerInstanceId = this.playersService.GetPlayerById(currentPlayerId).InstanceId;
            this.InvokeMyTurn(currentPlayerInstanceId);
            this.notifyService.InvokeByGroup(gameId, "NextPlayerTurn");
            gameInstance.RestartTurnTimer();
        }

        public void DealCards(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            var gameInstance = this.GetGameInstance(gameId);
            var numberOfTimers = (9 * gameInstance.PlayersCount) + 1;
            var timers = new List<Timer>(numberOfTimers);
            const int delay = 200;
            for (int i = 1; i <= numberOfTimers; i++)
            {
                timers.Add(new Timer(i * delay)
                {
                    AutoReset = false
                });
            }

            var counter = 0;
            for (int i = 0; i < 9; i++)
            {
                foreach (var player in gameInstance.Players)
                {
                    timers[counter].Elapsed += (source, e) =>
                    {
                        var card = gameInstance.DrawNextCard();
                        gameInstance.AddCardToPlayerDeck(player.Id, card);
                        this.notifyService.InvokeByPlayer(player.InstanceId, "GetPlayerCards");
                        this.InvokeUpdateGameInfo(gameId);
                    };

                    counter++;
                }
            }

            timers[counter].Elapsed += (source, e) =>
            {
                gameInstance.PutCardInDiscardDeck(gameInstance.DrawNextCard());
                this.StartGame(gameId);
            };

            foreach (var timer in timers)
            {
                timer.Start();
            }

            var disposeTimer = new Timer(delay * (numberOfTimers + 1))
            {
                AutoReset = false
            };

            disposeTimer.Elapsed += (source, e) =>
            {
                foreach (var timer in timers)
                {
                    timer.Dispose();
                }

                disposeTimer.Dispose();
            };

            disposeTimer.Start();
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
                    gameInstance.StopTurnTimer();
                    gameInstance.StopArragingTimer();
                    if (gameInstance.AreMaxPointsReached)
                    {
                        gameInstance.ResetPlayersReady();
                        this.InvokeUpdateGameInfo(gameId);
                        var playerPoints = gameInstance.GetAllPlayerPoints().OrderBy(kvp => kvp.Value);
                        var firstPlayerInstanceId = this.playersService.GetPlayerById(playerPoints.First().Key).InstanceId;
                        var lastPlayerInstanceId = this.playersService.GetPlayerById(playerPoints.Last().Key).InstanceId;
                        this.notifyService.InvokeByPlayer(firstPlayerInstanceId, "PlacedFirst");
                        this.notifyService.InvokeByPlayer(lastPlayerInstanceId, "PlacedLast");
                        this.notifyService.InvokeByGroup(gameId, "GameOver");
                        this.notifyService.InvokeByGroup(gameId, "NumberOfReplayReadyPlayers");

                        return;
                    }

                    this.notifyService.InvokeByGroup(gameId, "RoundResults");

                    var timer = new Timer(10 * 1000)
                    {
                        AutoReset = false
                    };

                    timer.Elapsed += (source, e) =>
                    {
                        this.StartNewRound(gameId);
                        timer.Dispose();
                    };

                    timer.Start();
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
                this.notifyService.InvokeByGroup(gameId, "NumberOfReplayReadyPlayers");
                if (gameInstance.AllPlayersReady)
                {
                    gameInstance.ResetPlayersReady();
                    gameInstance.Restart();
                    this.notifyService.InvokeByGroup(gameId, "NewRoundStarting");
                    this.notificationService.AddPlayersNotification(
                        gameInstance.Players.Select(x => x.InstanceId),
                        "New game started!",
                        5);
                    this.notifyService.InvokeByGroup(gameId, "ReceiveNotification");
                    this.DealCards(gameId);
                    gameInstance.RestartTurnTimer();
                }
            }
        }

        public IEnumerable<EndRoundPlayerCards> GetEndRoundPlayerCards(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            var gameInstance = this.GetGameInstance(gameId);
            if (!gameInstance.RoundOver)
            {
                throw new Exception("Not allowed.");
            }

            var playerCardsInfo = new List<EndRoundPlayerCards>(gameInstance.PlayersCount);
            foreach (var player in gameInstance.Players)
            {
                var playerCards = gameInstance.GetPlayerCards(player.Id).ToList();
                var pairGroups = this.GetPairGroups(playerCards);
                var pairGroupsIds = new List<List<int>>();
                foreach (var group in pairGroups)
                {
                    pairGroupsIds.Add(group.Select(x => x.Id).ToList());
                }

                playerCardsInfo.Add(new EndRoundPlayerCards()
                {
                    PlayerUsername = player.Username,
                    Cards = playerCards,
                    CardsPairGroupIds = pairGroupsIds
                });
            }

            return playerCardsInfo;
        }

        public GameInfo GetGameInfo(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            var gameInstance = this.GetGameInstance(gameId);
            var playerCardsNumbers = new Dictionary<string, int>(gameInstance.PlayersCount);
            var playersPoints = new Dictionary<string, int>(gameInstance.PlayersCount);
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
                    gameInstance.PeekDiscardDeckNextCard(),
                    gameInstance.RoundsPlayed,
                    gameInstance.RotationsPerRoundsPlayed);
        }

        public IDictionary<string, int> GetLastRoundGainedPlayerPoints(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            var gameInstance = this.GetGameInstance(gameId);
            var playerLastRoundPoints = gameInstance.GetLastRoundGainedPlayerPoints();
            var playersPoints = new Dictionary<string, int>(gameInstance.PlayersCount);
            foreach (var player in gameInstance.Players)
            {
                playersPoints.Add(player.Username, playerLastRoundPoints[player.Id]);
            }

            return playersPoints;
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
            var playerInstanceId = this.playersService.GetPlayerById(gameInstance.CurrentTurnPlayerId).InstanceId;
            this.InvokeMyTurn(playerInstanceId);
            this.InvokeUpdateGameInfo(gameId);
            return playerCards;
        }

        public ICollection<Card> PlayerDrawDiscardDeckNextCard(string gameId, string playerId)
        {
            this.DoesGameInstanceExists(gameId);
            this.IsPlayerAllowed(gameId, playerId);
            var gameInstance = this.GetGameInstance(gameId);
            if (!gameInstance.CurrentPlayerTurnAllowedMoves.DrawFromDiscardDeck || gameInstance.RoundOver)
            {
                throw new Exception("Move not allowed.");
            }

            var nextCard = gameInstance.DrawDiscardDeckNextCard();
            var playerCards = gameInstance.AddCardToPlayerDeck(playerId, nextCard);
            var allowedMoves = new PlayerTurnAllowedMoves(false, false, true, false);
            gameInstance.SetCurrentPlayerAlowedMoves(allowedMoves);
            var pairGroups = this.GetPairGroups(playerCards.ToList());
            this.CheckIfPlayerCanLightUp(gameId, playerId, pairGroups);
            var playerInstanceId = this.playersService.GetPlayerById(gameInstance.CurrentTurnPlayerId).InstanceId;
            this.InvokeMyTurn(playerInstanceId);
            this.InvokeUpdateGameInfo(gameId);
            return playerCards;
        }

        public ICollection<Card> PlayerPutCardInDiscardDeck(string gameId, string playerId, int cardId)
        {
            lock (this.balanceLock)
            {
                this.DoesGameInstanceExists(gameId);
                this.IsPlayerAllowed(gameId, playerId);
                var gameInstance = this.GetGameInstance(gameId);
                if (!gameInstance.CurrentPlayerTurnAllowedMoves.PutToDiscardDeck || gameInstance.RoundOver)
                {
                    throw new Exception("Move not allowed.");
                }
                if (!gameInstance.GetPlayerCards(playerId).Any(x => x.Id == cardId))
                {
                    throw new Exception("Card not in player's deck.");
                }

                var card = gameInstance.RemoveCardFromPlayerDeck(playerId, cardId);
                gameInstance.PutCardInDiscardDeck(card);
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
                    this.EndRound(gameId);
                    gameInstance.PlayerReady(playerId);
                    gameInstance.SubstractFromPlayerPoints(playerId, 10);
                }
                else if (!drawDeckAllowed)
                {
                    this.EndRound(gameId);
                }
                else
                {
                    this.NextPlayerTurn(gameId);
                }

                this.InvokeUpdateGameInfo(gameId);
                return playerCards;
            }
        }

        public void EndRound(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            this.notifyService.InvokeByGroup(gameId, "RoundOver");
            var gameInstance = this.GetGameInstance(gameId);
            gameInstance.StopTurnTimer();
            gameInstance.RestartArragingTimer();
            gameInstance.RoundOver = true;
        }

        public List<List<Card>> PlayerArrangeCards(string gameId, string playerId, ICollection<Card> cards)
        {
            this.DoesGameInstanceExists(gameId);
            var gameInstance = this.GetGameInstance(gameId);
            gameInstance.RearrangePlayerCards(playerId, cards);
            var pairGroups = this.GetPairGroups(cards.ToList());
            if (!gameInstance.RoundOver && gameInstance.CurrentTurnPlayerId == playerId)
            {
                this.CheckIfPlayerCanLightUp(gameId, playerId, pairGroups);
                if (this.GetCurrentTurnPlayerId(gameId) == playerId)
                {
                    var playerInstanceId = this.playersService.GetPlayerById(playerId).InstanceId;
                    this.InvokeMyTurn(playerInstanceId);
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

        public Card PeekDiscardDeckNextCard(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            return this.GetGameInstance(gameId).PeekDiscardDeckNextCard();
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
                    gameInstance.DisposeTurnTimer();
                    gameInstance.DisposeArragingTimer();
                    this.gameInstances.Remove(gameInstance);
                }
            }
        }

        public void DropGame(string gameId, string playerName)
        {
            lock (this.balanceLock)
            {
                this.DoesGameInstanceExists(gameId);
                this.notifyService.InvokeByGroup(gameId, "GameDropped");
                this.roomService.DeleteRoom(gameId, playerName);
                var gameInstance = this.GetGameInstance(gameId);
                gameInstance.DisposeTurnTimer();
                gameInstance.DisposeArragingTimer();
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

        public List<List<Card>> GetPairGroups(List<Card> cards)
        {
            var groups = new List<List<Card>>();
            return this.GetPairGroupsHelper(cards, 0, groups);
        }

        private List<List<Card>> GetPairGroupsHelper(List<Card> cards, int startIndex, List<List<Card>> groups)
        {
            if (startIndex > cards.Count - 1)
            {
                return new List<List<Card>>();
            }

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
                    if (cards[i].CardType == CardType.Joker && group2[^1].CardType == CardType.King ||
                        cards[i].CardType == CardType.Ace && group2[^1].CardType == CardType.Joker)
                    {
                        break;
                    }
                    if (cards[i].CardType == CardType.Joker)
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
                    if (k == group2.Count - 1 && k == counter - 1)
                    {
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
                if (index1 >= index2)
                {
                    groups.Add(group1);
                    index = index1;
                }
                else if (index2 > index1)
                {
                    groups.Add(group2);
                    index = index2;
                }
            }
            if (index < cards.Count)
            {
                this.GetPairGroupsHelper(cards, index, groups);
            }

            return groups;
        }

        private void NextPlayerTurn(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            var gameInstance = this.GetGameInstance(gameId);
            var nextPlayerId = gameInstance.SetNextPlayerTurn();
            var playerInstanceId = playersService.GetPlayerById(nextPlayerId).InstanceId;
            this.InvokeMyTurn(playerInstanceId);
            this.notifyService.InvokeByGroup(gameId, "NextPlayerTurn");
            this.notificationService.AddPlayerNotification(playerInstanceId, "Your turn!", 3);
            gameInstance.RestartTurnTimer();
        }

        private bool CheckIfPlayerCanLightUp(string gameId, string playerId, List<List<Card>> pairGroups)
        {
            var numOfPairedCards = pairGroups.Where(x => x.Count > 2).Sum(x => x.Count);
            if (this.GetCurrentTurnPlayerId(gameId) == playerId)
            {
                var allowedMoves = this.GetCurrentPlayerTurnAllowedMoves(gameId);
                var newAllowedMoves = new PlayerTurnAllowedMoves(
                    allowedMoves.DrawFromDeck,
                    allowedMoves.DrawFromDiscardDeck,
                    allowedMoves.PutToDiscardDeck,
                    numOfPairedCards >= 9);
                this.GetGameInstance(gameId).SetCurrentPlayerAlowedMoves(newAllowedMoves);
            }

            return numOfPairedCards >= 9;
        }

        private void StartNewRound(string gameId)
        {
            this.DoesGameInstanceExists(gameId);
            var gameInstance = this.GetGameInstance(gameId);
            gameInstance.StartNewRound();
            this.notifyService.InvokeByGroup(gameId, "NewRoundStarting");
            this.notificationService.AddPlayersNotification(
                gameInstance.Players.Select(x => x.InstanceId),
                "New round started!",
                5);

            this.notifyService.InvokeByGroup(gameId, "ReceiveNotification");
            //var allowedMoves = new PlayerTurnAllowedMoves(true, true, false, false);
            //gameInstance.SetCurrentPlayerAlowedMoves(allowedMoves);
            //gameInstance.RestartTurnTimer();
            //var currentPlayerId = gameInstance.CurrentTurnPlayerId;
            //var playerInstanceId = this.playersService.GetPlayerById(currentPlayerId).InstanceId;
            //this.InvokeMyTurn(playerInstanceId);
            this.DealCards(gameId);
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
            var gameInstance = new GameInstance(gameId, players, room.MaxPoints, (int)room.TimePerTurn.TotalSeconds);
            gameInstance.AddTurnTimerElapsedEvent((source, e) =>
            {
                var currentPlayer = this.playersService.GetPlayerById(gameInstance.CurrentTurnPlayerId);
                if (this.GetNumberOfPlayerCards(gameId, currentPlayer.Id) == 9)
                {
                    this.NextPlayerTurn(gameId);
                    this.InvokeUpdateGameInfo(gameId);
                    this.notificationService.AddPlayerNotification(currentPlayer.InstanceId,
                        "Time's up! Your turn was skipped.",
                        5,
                        NotificationType.Warning);
                }
                else
                {
                    var playerCards = this.GetPlayerCards(gameId, currentPlayer.Id).ToList();
                    var random = new Random(Environment.TickCount);
                    var card = playerCards[random.Next(playerCards.Count)];
                    this.PlayerPutCardInDiscardDeck(gameId, currentPlayer.Id, card.Id);
                    this.notifyService.InvokeByPlayer(currentPlayer.InstanceId, "GetPlayerCards");
                    this.notificationService.AddPlayerNotification(currentPlayer.InstanceId,
                        "Time's up! Random card from your deck was thrown.",
                        5,
                        NotificationType.Warning);
                }
            });

            gameInstance.AddArragingTimerElapsedEvent((source, e) =>
            {
                this.notifyService.InvokeByGroup(gameId, "ForceFinishArraging");
            });

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

        private void InvokeMyTurn(string playerInstanceId)
        {
            this.notifyService.InvokeByPlayer(playerInstanceId, "MyTurn");
        }

        private void InvokeUpdateGameInfo(string gameId)
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
