using LanternCardGame.Shared.Enums;
using System;

namespace LanternCardGame.Models
{
    public class PlayerModel
    {
        public PlayerModel(
            string id,
            string username)
        {
            Id = id;
            InstanceId = Guid.NewGuid().ToString();
            Username = username;
            PlayerStatus = PlayerStatus.Free;
        }

        public string Id { get; set; }

        public string InstanceId { get; }

        public string Username { get; set; }

        public string RoomId { get; set; }

        public PlayerStatus PlayerStatus { get; set; }
    }
}
