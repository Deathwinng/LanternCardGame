using System.Collections.Generic;

namespace LanternCardGame.Models
{
    public class GameRoomModel
    {
        public GameRoomModel(string id)
        {
            Id = id;
            Players = new HashSet<PlayerModel>();
            ChatList = new HashSet<ChatModel>();
            InGame = false;
            InDeveloperMode = false;
        }

        public string Id { get; }

        public string Name { get; set; }

        public string OwnerId { get; set; }

        public int MaxPlayers { get; set; }

        public int MaxPoints { get; set; }

        public bool InGame { get; set; }

        public int PlayerCount => Players.Count;

        public bool InDeveloperMode { get; set; }

        public ICollection<PlayerModel> Players { get; set; }

        public ICollection<ChatModel> ChatList { get; set; }
    }
}
