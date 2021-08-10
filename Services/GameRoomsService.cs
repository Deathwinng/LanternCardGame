﻿using LanternCardGame.Models;
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
            rooms = new HashSet<GameRoomModel>();
        }

        public GameRoomModel GetRoom(string roomId)
        {
            return this.rooms.FirstOrDefault(x => x.Id == roomId);
        }

        public ICollection<GameRoomModel> GetAllRooms()
        {
            return rooms;
        }

        public IEnumerable<GameRoomModel> GetAllRoomsNotInGame()
        {
            return this.rooms.Where(x => x.InGame == false && x.InDeveloperMode == false);
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

        public string CreateNewRoom(string playerId, NewRoomModel roomModel)
        {
            var player = this.playersService.GetPlayerById(playerId);
            if (player == null)
            {
                throw new Exception("Player not found.");
            }

            var room = new GameRoomModel(GetNewUniqueRoomId())
            {
                Name = roomModel.Name,
                MaxPlayers = roomModel.NumberOfPlayers,
                OwnerId = playerId,
                MaxPoints = roomModel.MaxPoints,
                InDeveloperMode = roomModel.DeveloperMode,
            };

            player.RoomId = room.Id;
            player.PlayerStatus = PlayerStatus.InRoom;
            room.Players.Add(player);
            this.rooms.Add(room);
            this.notifyService.AddToGroup(player.InstanceId, room.Id);
            InvokeRefreshRooms();
            this.notifyService.InvokeAll("UpdateOnlinePlayers");
            return room.Id;
        }

        public void DeleteRoom(string roomId)
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
                else if (room.InGame)
                {
                    this.notificationService.AddPlayerNotification(player.InstanceId, "Game terminated. A player left the game!", 5, NotificationType.Danger);
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

        public bool AddPlayerToRoom(string playerId, string roomId)
        {
            DoesRoomExist(roomId);
            var room = GetRoom(roomId);
            if (!room.Players.Any(x => x.Id == playerId) && room.PlayerCount < room.MaxPlayers)
            {
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
                        $"Player \"{player.Username}\" joined.",
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
                            $"Player \"{player.Username}\" left.",
                            3);
                    }

                    this.notifyService.InvokeByGroupExcept(roomId, player.InstanceId, "ReceiveNotification");
                    this.notifyService.InvokeAll("UpdateOnlinePlayers");
                    InvokeRefreshRoom(roomId);
                    InvokeRefreshRooms();
                }
            }
        }

        public void AddChatMessageToRoom(string roomId, ChatModel chat)
        {
            DoesRoomExist(roomId);
            var room = GetRoom(roomId);
            room.ChatList.Add(chat);
            this.notifyService.InvokeByGroup(roomId, "NewChat");
        }

        public void StartGame(string roomId)
        {
            DoesRoomExist(roomId);
            var room = GetRoom(roomId);
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

        public void InvokeRefreshRoom(string roomId)
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