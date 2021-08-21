using System;
using System.Collections.Generic;

namespace LanternCardGame.Models
{
    public class GameRoomModel
    {
        public GameRoomModel(string id)
        {
            this.Id = id;
            this.Players = new HashSet<PlayerModel>();
            this.ChatList = new HashSet<ChatModel>();
            this.InvitedPlayerIds = new HashSet<string>();
            this.InGame = false;
            this.InDeveloperMode = false;
        }

        public string Id { get; }

        public string Name { get; set; }

        public string OwnerId { get; set; }

        public int MaxPlayers { get; set; }

        public int MaxPoints { get; set; }

        public TimeSpan TimePerTurn { get; set; }

        public bool InGame { get; set; }

        public int PlayerCount => Players.Count;

        public bool Private { get; set; }

        public bool JoinUninvited { get; set; }

        public bool InDeveloperMode { get; set; }

        public ICollection<PlayerModel> Players { get; set; }

        public ICollection<ChatModel> ChatList { get; set; }

        public ICollection<string> InvitedPlayerIds { get; set; }
    }
}
