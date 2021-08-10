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
            notifications = new List<NotificationModel>();
        }
        public IEnumerable<NotificationModel> GetPlayerNotifications(string playerInstanceId)
        {
            return notifications.Where(x => x.PlayerInstanceId == playerInstanceId);
        }

        public string AddPlayerNotification(
            string playerInstanceId,
            string message,
            double dissmissTimerInSeconds = 0,
            NotificationType severity = NotificationType.Primary,
            bool isDissmissable = true)
        {
            var notification = new NotificationModel(
                playerInstanceId,
                message,
                severity,
                isDissmissable,
                dissmissTimerInSeconds);
            notifications.Add(notification);
            return notification.Id;
        }

        public void RemovePlayerNotification(string notificationId)
        {
            lock (balanceLock)
            {
                var notification = notifications.FirstOrDefault(x => x.Id == notificationId);
                notifications.Remove(notification);
            }
        }

        public void RemoveAllPlayerNotifications(string playerInstanceId)
        {
            lock (balanceLock)
            {
                var notifications = this.notifications.Where(x => x.Id == playerInstanceId);
                this.notifications = this.notifications.Except(notifications).ToList();
            }
        }
    }
}
