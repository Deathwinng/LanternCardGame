using LanternCardGame.Models;
using LanternCardGame.Shared.Enums;
using System.Collections.Generic;
using System.Linq;

namespace LanternCardGame.Services
{
    public class OnlinePlayersService
    {
        private readonly ICollection<PlayerModel> onlinePlayers;
        private readonly NotifyService notifyService;
        private readonly NotificationService notificationService;
        private readonly object balanceLock = new object();

        public OnlinePlayersService(
            NotifyService notifyService,
            NotificationService notificationService
            )
        {
            this.onlinePlayers = new HashSet<PlayerModel>();
            this.notifyService = notifyService;
            this.notificationService = notificationService;
        }

        public IEnumerable<PlayerModel> GetPlayersOnline()
        {
            return this.onlinePlayers.Where(x => x.PlayerStatus != PlayerStatus.Duplicate);
        }

        public PlayerModel GetPlayerById(string id)
        {
            return this.onlinePlayers.FirstOrDefault(x => x.Id == id && x.PlayerStatus != PlayerStatus.Duplicate);
        }

        public PlayerModel GetPlayerByUsername(string username)
        {
            return this.onlinePlayers.FirstOrDefault(x => x.Username == username && x.PlayerStatus != PlayerStatus.Duplicate);
        }

        public PlayerModel GetPlayerByInstanceId(string instanceId)
        {
            return this.onlinePlayers.FirstOrDefault(x => x.InstanceId == instanceId);
        }

        public PlayerModel PlayerConnected(string playerId, string playerUsername)
        {
            lock (this.balanceLock)
            {
                var player = new PlayerModel(playerId, playerUsername);
                this.onlinePlayers.Add(player);
                if (!this.IsDuplicatePlayer(player.Id, player.InstanceId))
                {
                    this.notificationService.AddPlayerNotification(player.InstanceId, "You are now online!", 2);
                    this.notifyService.InvokeByPlayer(player.InstanceId, "ReceiveNotification");
                    this.notifyService.InvokeAll("UpdateOnlinePlayers");
                }
                else
                {
                    player.PlayerStatus = PlayerStatus.Duplicate;
                    this.notifyService.InvokeByPlayer(player.InstanceId, "DuplicateUser");
                }

                return player;
            }
        }

        public void PlayerDisconnected(string playerInstanceId)
        {
            lock (this.balanceLock)
            {
                var player = GetPlayerByInstanceId(playerInstanceId);
                if (player != null)
                {
                    this.onlinePlayers.Remove(player);
                    this.notifyService.Unsubscribe(player.InstanceId);
                    if (this.IsDuplicatePlayer(player.Id, playerInstanceId))
                    {
                        var duplicatePlayer = this.onlinePlayers.FirstOrDefault(x => x.Id == player.Id && x.InstanceId != player.InstanceId && x.PlayerStatus == PlayerStatus.Duplicate);

                        if (duplicatePlayer != null)
                        {
                            duplicatePlayer.PlayerStatus = PlayerStatus.Free;
                            this.notifyService.InvokeByPlayer(duplicatePlayer.InstanceId, "DuplicateUserCleared");
                        }
                    }

                    this.notifyService.InvokeAll("UpdateOnlinePlayers");
                }
            }
        }

        //public void PlayerInactive(string playerInstanceId)
        //{
        //    var player = GetPlayerByInstanceId(playerInstanceId);
        //    if (player == null)
        //    {
        //        return;
        //    }

        //    player.PlayerStatus = PlayerStatus.Inactive;
        //    this.notifyService.InvokeAll("UpdateOnlinePlayers");
        //}

        //public void PlayerActive(string playerInstanceId)
        //{
        //    var player = GetPlayerByInstanceId(playerInstanceId);
        //    if (player == null)
        //    {
        //        return;
        //    }

        //    player.PlayerStatus = PlayerStatus.Free;
        //    this.notifyService.InvokeAll("UpdateOnlinePlayers");

        //}

        public bool IsPlayerOnline(string playerId)
        {
            return this.onlinePlayers.Any(x => x.Id == playerId);
        }

        public bool IsDuplicatePlayer(string playerId, string instanceId)
        {
            return this.onlinePlayers.Any(x => x.Id == playerId && x.InstanceId != instanceId);
        }
    }
}
