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

        public NotificationService()
        {
            this.notifications = new List<NotificationModel>();
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
            bool isDissmissable = true)
        {
            lock (this.balanceLock)
            {
                var notification = new NotificationModel(
                    playerInstanceId,
                    message,
                    severity,
                    isDissmissable,
                    dissmissTimerInSeconds);
                this.notifications.Add(notification);

                return notification.Id;
            }
        }

        public IEnumerable<string> AddPlayersNotification(
            IEnumerable<string> playerInstanceIds,
            string message,
            double dissmissTimerInSeconds = 0,
            NotificationType severity = NotificationType.Primary,
            bool isDissmissable = true)
        {
            lock (this.balanceLock)
            {
                var notificationIds = new List<string>(playerInstanceIds.Count());
                foreach (var playerInstanceId in playerInstanceIds)
                {
                    var notification = new NotificationModel(
                        playerInstanceId,
                        message,
                        severity,
                        isDissmissable,
                        dissmissTimerInSeconds);
                    this.notifications.Add(notification);
                    notificationIds.Add(notification.Id);
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
