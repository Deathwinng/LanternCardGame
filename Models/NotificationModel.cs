using LanternCardGame.Shared.Enums;
using System;

namespace LanternCardGame.Models
{
    public class NotificationModel
    {
        public NotificationModel(
            string playerInstanceId,
            string message,
            NotificationType type,
            bool isDissmissable,
            double dissmissTimerInSeconds)
        {
            this.Id = Guid.NewGuid().ToString();
            this.PlayerInstanceId = playerInstanceId;
            this.Message = message;
            this.Type = type;
            this.IsDissmissable = isDissmissable;
            this.DissmissTimerInSeconds = dissmissTimerInSeconds;
            this.TimerStarted = false;
        }

        public string Id { get; set; }

        public string PlayerInstanceId { get; set; }

        public string Message { get; set; }

        public NotificationType Type { get; set; }

        public bool IsDissmissable { get; set; }

        public double DissmissTimerInSeconds { get; set; }

        public bool TimerStarted { get; set; }
    }
}
