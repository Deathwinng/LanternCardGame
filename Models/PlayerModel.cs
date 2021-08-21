using LanternCardGame.Shared.Enums;
using System;
using System.Collections.Generic;

namespace LanternCardGame.Models
{
    public class PlayerModel
    {
        public PlayerModel(
            string id,
            string username)
        {
            this.Id = id;
            this.InstanceId = Guid.NewGuid().ToString();
            this.Username = username;
            this.PlayerStatus = PlayerStatus.Free;
            this.RoomInviteIds = new Dictionary<string, string>();
        }

        public string Id { get; set; }

        public string InstanceId { get; }

        public string Username { get; set; }

        public string RoomId { get; set; }

        public PlayerStatus PlayerStatus { get; set; }

        public IDictionary<string, string> RoomInviteIds { get; set; }
    }
}
