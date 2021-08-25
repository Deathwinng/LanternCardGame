using LanternCardGame.Models;
using LanternCardGame.Shared.Enums;
using System.Collections.Generic;
using System.Linq;

namespace LanternCardGame.Services
{
    public class NotificationService
    {
        private IList<NotificationModel> notifications;
        private readonly object balanceLock = new object();
        private readonly NotifyService notifyService;

        public NotificationService(NotifyService notifyService)
        {
            this.notifications = new List<NotificationModel>();
            this.notifyService = notifyService;
        }

        public IEnumerable<NotificationModel> GetPlayerNotifications(string playerInstanceId)
        {
            return this.notifications.Where(x => x.PlayerInstanceId == playerInstanceId);
        }

        public string AddPlayerNotification(
            string playerInstanceId,
            string message,
            double dissmissTimerInSeconds = 0,
            NotificationType severity = NotificationType.Primary,
            string category = "General",
            bool isDissmissable = true,
            bool notifyPlayer = true)
        {
            lock (this.balanceLock)
            {
                var notification = new NotificationModel(
                    playerInstanceId,
                    message,
                    severity,
                    category,
                    isDissmissable,
                    dissmissTimerInSeconds);
                this.notifications.Add(notification);

                if (notifyPlayer)
                {
                    this.notifyService.InvokeByPlayer(playerInstanceId, "ReceiveNotification");
                }
                return notification.Id;
            }
        }

        public IEnumerable<string> AddPlayersNotification(
            IEnumerable<string> playerInstanceIds,
            string message,
            double dissmissTimerInSeconds = 0,
            string category = "General",
            NotificationType severity = NotificationType.Primary,
            bool isDissmissable = true,
            bool notifyPlayers = true)
        {
            lock (this.balanceLock)
            {
                var notificationIds = new List<string>(playerInstanceIds.Count());
                foreach (var playerInstanceId in playerInstanceIds)
                {
                    notificationIds.Add(this.AddPlayerNotification(
                        playerInstanceId,
                        message,
                        dissmissTimerInSeconds,
                        severity,
                        category,
                        isDissmissable));

                    if (notifyPlayers)
                    {
                        this.notifyService.InvokeByPlayer(playerInstanceId, "ReceiveNotification");
                    }
                }

                return notificationIds;
            }
        }

        public void RemovePlayerNotification(string notificationId)
        {
            lock (this.balanceLock)
            {
                var notification = this.notifications.FirstOrDefault(x => x.Id == notificationId);
                this.notifications.Remove(notification);
            }
        }

        public void RemovePlayerNotificationsByCategory(string playerInstanceId, string category)
        {
            var notifications = this.notifications.Where(x => x.PlayerInstanceId == playerInstanceId && x.Category == category).ToList();
            for (int i = 0; i < notifications.Count; i++)
            {
                this.notifications.Remove(notifications[i]);
            }
        }

        public void RemoveAllPlayerNotifications(string playerInstanceId)
        {
            lock (this.balanceLock)
            {
                var notifications = this.notifications.Where(x => x.Id == playerInstanceId);
                this.notifications = this.notifications.Except(notifications).ToList();
            }
        }
    }
}
