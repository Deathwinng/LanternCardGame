using LanternCardGame.Models;
using LanternCardGame.Pages;
using LanternCardGame.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LanternCardGame.Services
{
    public class GameRoomsService
    {
        private readonly NotifyService notifyService;
        private readonly OnlinePlayersService playersService;
        private readonly NotificationService notificationService;
        private readonly ICollection<GameRoomModel> rooms;

        public GameRoomsService(
            NotifyService notifyService,
            OnlinePlayersService playersService,
            NotificationService notificationService)
        {
            this.notifyService = notifyService;
            this.playersService = playersService;
            this.notificationService = notificationService;
            this.rooms = new HashSet<GameRoomModel>();
        }

        public GameRoomModel GetRoom(string roomId)
        {
            return this.rooms.FirstOrDefault(x => x.Id == roomId);
        }

        public ICollection<GameRoomModel> GetAllRooms()
        {
            return this.rooms;
        }

        public IEnumerable<GameRoomModel> GetAllRoomsNotInGame()
        {
            return this.rooms.Where(
                x => !x.InGame &&
                !x.InDeveloperMode &&
                !x.Private);
        }

        public ICollection<PlayerModel> GetAllPlayersInRoom(string roomId)
        {
            DoesRoomExist(roomId);
            var room = GetRoom(roomId);
            return room.Players.ToHashSet();
        }

        public PlayerModel GetPlayerFromRoom(string roomId, string playerId)
        {
            DoesRoomExist(roomId);
            var room = GetRoom(roomId);
            return room.Players.FirstOrDefault(x => x.Id == playerId);
        }

        public bool IsPlayerInTheRoom(string playerInstanceId, string roomId)
        {
            DoesRoomExist(roomId);
            var room = GetRoom(roomId);
            return room.Players.Any(x => x.InstanceId == playerInstanceId);
        }

        public bool IsNameAvailable(string name)
        {
            return !this.rooms.Any(x => x.Name == name);
        }

        public string CreateNewRoom(string playerId, NewRoomModel roomModel)
        {
            var player = this.playersService.GetPlayerById(playerId);
            if (player == null)
            {
                throw new Exception("Player not found.");
            }

            var room = new GameRoomModel(this.GetNewUniqueRoomId())
            {
                Name = roomModel.Name,
                MaxPlayers = roomModel.NumberOfPlayers,
                OwnerId = playerId,
                MaxPoints = roomModel.MaxPoints,
                TimePerTurn = roomModel.TimerEnabled ? TimeSpan.FromSeconds(roomModel.SecondsPerTurn) : TimeSpan.FromSeconds(0),
                Private = roomModel.Private,
                JoinUninvited = roomModel.JoinUninvited,
                InDeveloperMode = roomModel.DeveloperMode,
            };

            player.RoomId = room.Id;
            player.PlayerStatus = PlayerStatus.InRoom;
            room.Players.Add(player);
            this.rooms.Add(room);
            this.notifyService.AddToGroup(player.InstanceId, room.Id);
            if (!room.Private)
            {
                this.InvokeRefreshRooms();
            }

            this.notifyService.InvokeAll("UpdateOnlinePlayers");
            return room.Id;
        }

        public void DeleteRoom(string roomId, string playerName = null)
        {
            DoesRoomExist(roomId);
            var room = GetRoom(roomId);
            this.notifyService.InvokeByGroup(roomId, "RoomDeleted");
            foreach (var player in room.Players)
            {
                if (!room.InGame && player.Id != room.OwnerId)
                {
                    this.notificationService.AddPlayerNotification(player.InstanceId, "Room deleted!", 3, NotificationType.Warning);
                    this.notifyService.InvokeByPlayer(player.InstanceId, "ReceiveNotification");
                }
                else if (room.InGame && player.Username != playerName)
                {
                    this.notificationService.AddPlayerNotification(player.InstanceId,
                        $"Game terminated. {(string.IsNullOrEmpty(playerName) ? "A player" : playerName)} left the game!",
                        5,
                        NotificationType.Danger);
                    this.notifyService.InvokeByPlayer(player.InstanceId, "ReceiveNotification");
                }

                player.RoomId = null;
                player.PlayerStatus = PlayerStatus.Free;
                this.notifyService.RemoveFromGroup(player.InstanceId, roomId);
                room.Players.Remove(player);
            }

            this.rooms.Remove(room);
            this.notifyService.InvokeAll("UpdateOnlinePlayers");
            InvokeRefreshRooms();
        }

        public bool AddPlayerToRoom(string playerId, string roomId, bool isFriend = false)
        {
            DoesRoomExist(roomId);
            var room = GetRoom(roomId);
            if (!room.Players.Any(x => x.Id == playerId) && room.PlayerCount < room.MaxPlayers)
            {
                if (room.Private)
                {
                    if (!isFriend ||
                        (isFriend &&
                        !room.JoinUninvited &&
                        !room.InvitedPlayerIds.Any(x => x == playerId)))
                    {
                        return false;
                    }
                }
                if (room.InvitedPlayerIds.Any(x => x == playerId))
                {
                    this.RemoveInvite(playerId, roomId);
                }

                var player = this.playersService.GetPlayerById(playerId);
                player.RoomId = roomId;
                player.PlayerStatus = PlayerStatus.InRoom;
                room.Players.Add(player);
                this.notifyService.AddToGroup(player.InstanceId, room.Id);
                this.notifyService.InvokeAll("UpdateOnlinePlayers");
                foreach (var playerModel in room.Players)
                {
                    if (playerModel.Id == playerId)
                    {
                        continue;
                    }

                    this.notificationService.AddPlayerNotification(
                        playerModel.InstanceId,
                        $"\"{player.Username}\" joined.",
                        3);
                }

                this.notifyService.InvokeByGroupExcept(roomId, player.InstanceId, "ReceiveNotification");
                InvokeRefreshRoom(roomId);
                InvokeRefreshRooms();
                return true;
            }

            return false;
        }

        public void RemovePlayerFromRoom(string playerId, string roomId, bool deleteIfOwnerLeft = true)
        {
            DoesRoomExist(roomId);
            var room = GetRoom(roomId);
            var player = GetPlayerFromRoom(roomId, playerId);
            if (player != null)
            {
                room.Players.Remove(player);
                player.RoomId = null;
                player.PlayerStatus = PlayerStatus.Free;
                this.notifyService.RemoveFromGroup(player.InstanceId, roomId);
                if (room.PlayerCount == 0 || (room.OwnerId == player.Id && deleteIfOwnerLeft))
                {
                    DeleteRoom(roomId);
                }
                else
                {
                    foreach (var playerModel in room.Players)
                    {
                        if (playerModel.Id == playerId)
                        {
                            continue;
                        }

                        this.notificationService.AddPlayerNotification(
                            playerModel.InstanceId,
                            $"\"{player.Username}\" left.",
                            3);
                    }

                    this.notifyService.InvokeByGroupExcept(roomId, player.InstanceId, "ReceiveNotification");
                    this.notifyService.InvokeAll("UpdateOnlinePlayers");
                    InvokeRefreshRoom(roomId);
                    InvokeRefreshRooms();
                }
            }
        }

        public bool InvitePlayerToRoom(string inviterUsername, string inviteeInstanceId, string roomId)
        {
            this.DoesRoomExist(roomId);
            if (this.IsPlayerInTheRoom(inviteeInstanceId, roomId))
            {
                return false;
            }

            var room = this.GetRoom(roomId);
            var player = this.playersService.GetPlayerByInstanceId(inviteeInstanceId);
            if (room.InvitedPlayerIds.Any(x => x == player.Id) || !this.playersService.IsPlayerOnline(player.Id))
            {
                return false;
            }

            room.InvitedPlayerIds.Add(player.Id);
            player.RoomInviteIds[roomId] = inviterUsername;
            var owner = this.playersService.GetPlayerById(room.OwnerId);
            this.notificationService.AddPlayerNotification(
                inviteeInstanceId,
                $"\"{owner.Username}\" invited you to play!");
            this.notifyService.InvokeByPlayer(inviteeInstanceId, "ReceiveNotification");
            this.notifyService.InvokeByPlayer(inviteeInstanceId, "RoomIvite");
            return true;
        }

        public bool RemoveInvite(string playerId, string roomId)
        {
            this.DoesRoomExist(roomId);
            var room = this.GetRoom(roomId);
            var player = this.playersService.GetPlayerById(playerId);
            var playerInviteRemoveSuccess = player.RoomInviteIds.Remove(roomId);
            return room.InvitedPlayerIds.Remove(player.Id) && playerInviteRemoveSuccess;
        }

        public void AddChatMessageToRoom(ChatModel chat, string playerInstanceId, string roomId)
        {
            this.DoesRoomExist(roomId);
            var room = GetRoom(roomId);
            room.ChatList.Add(chat);
            this.notifyService.InvokeByGroupExcept(roomId, playerInstanceId, "NewChat");
        }

        public void StartGame(string roomId)
        {
            DoesRoomExist(roomId);
            var room = GetRoom(roomId);
            if (room.PlayerCount < room.MaxPlayers)
            {
                throw new Exception("Not enought players to start game.");
            }

            room.InGame = true;
            foreach (var player in room.Players)
            {
                player.PlayerStatus = PlayerStatus.InGame;
            }

            this.notifyService.InvokeAll("UpdateOnlinePlayers");
            InvokeRefreshRoom(roomId);
            InvokeRefreshRooms();
        }

        public void StopGame(string roomId)
        {
            this.DeleteRoom(roomId);
            this.InvokeRefreshRooms();
        }

        private void InvokeRefreshRoom(string roomId)
        {
            this.notifyService.InvokeByGroup(roomId, "RefreshRoom");
        }

        private void DoesRoomExist(string roomId)
        {
            if (!this.rooms.Any(x => x.Id == roomId))
            {
                throw new Exception("Room doesn't exist.");
            }
        }

        private void InvokeRefreshRooms()
        {
            this.notifyService.InvokeAll("RefreshRooms");
        }

        private string GetNewUniqueRoomId()
        {
            string roomId;
            do
            {
                roomId = Guid.NewGuid().ToString().Substring(0, 8);
            } while (GetRoom(roomId) != null);

            return roomId;
        }
    }
}
