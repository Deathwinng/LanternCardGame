using System;

namespace LanternCardGame.Models
{
    public class ChatModel
    {
        public ChatModel()
        {
            Timestamp = DateTime.UtcNow;
        }

        public string RoomId { get; set; }

        public string PlayerName { get; set; }

        public DateTime Timestamp { get; set; }

        public string Message { get; set; }
    }
}
