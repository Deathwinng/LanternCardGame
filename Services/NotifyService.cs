using System;
using System.Collections.Generic;

namespace LanternCardGame.Services
{
    public class NotifyService
    {
        private readonly object balanceLock = new object();
        private readonly IDictionary<string, IList<string>> groups;
        private readonly IDictionary<string, IDictionary<string, Action>> subscriptions; // Dictionary<playerInstanceId, Dictionary<subName, Action>>

        public NotifyService()
        {
            groups = new Dictionary<string, IList<string>>();
            subscriptions = new Dictionary<string, IDictionary<string, Action>>();
        }

        public void AddToGroup(string playerInstanceId, string groupName)
        {
            lock (balanceLock)
            {
                if (!groups.ContainsKey(groupName))
                {
                    groups.Add(groupName, new List<string>());
                }

                groups[groupName].Add(playerInstanceId);
            }
        }

        public void RemoveFromGroup(string playerInstanceId, string groupName)
        {
            lock (balanceLock)
            {
                if (groups.ContainsKey(groupName))
                {
                    groups[groupName].Remove(playerInstanceId);
                    if (groups[groupName].Count == 0)
                    {
                        groups.Remove(groupName);
                    }
                }
            }
        }

        public string[] Subscribe(string playerInstanceId, string subscriptionName, Action handler)
        {
            if (string.IsNullOrEmpty(playerInstanceId))
            {
                throw new ArgumentException($"'{nameof(playerInstanceId)}' cannot be null or empty.", nameof(playerInstanceId));
            }
            if (string.IsNullOrEmpty(subscriptionName))
            {
                throw new ArgumentException($"'{nameof(subscriptionName)}' cannot be null or empty.", nameof(subscriptionName));
            }
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (balanceLock)
            {
                if (!subscriptions.ContainsKey(playerInstanceId))
                {
                    subscriptions.Add(playerInstanceId, new Dictionary<string, Action>());
                }
                if (!subscriptions[playerInstanceId].ContainsKey(subscriptionName))
                {
                    subscriptions[playerInstanceId].Add(subscriptionName, handler);
                }

                return new string[] { playerInstanceId, subscriptionName };
            }
        }

        public void Unsubscribe(string playerInstanceId, string subscriptionName)
        {
            lock (balanceLock)
            {
                if (subscriptions.ContainsKey(playerInstanceId))
                {
                    subscriptions[playerInstanceId].Remove(subscriptionName);
                }
            }
        }

        public void Unsubscribe(string playerInstanceId)
        {
            lock (balanceLock)
            {
                subscriptions.Remove(playerInstanceId);
            }
        }

        public void InvokeAll(string subscriptionName)
        {
            foreach (var kvp in subscriptions)
            {
                InvokeByPlayer(kvp.Key, subscriptionName);
            }
        }

        public void InvokeAllExcept(string subscriptionName, string exepltPlayerInstanceId)
        {
            foreach (var kvp in subscriptions)
            {
                if (kvp.Key == exepltPlayerInstanceId)
                {
                    continue;
                }

                InvokeByPlayer(kvp.Key, subscriptionName);
            }
        }

        public void InvokeByPlayer(string playerInstanceId, string subscriptionName)
        {
            if (!subscriptions.ContainsKey(playerInstanceId))
            {
                return;
            }

            var handlerDict = subscriptions[playerInstanceId];
            if (!handlerDict.ContainsKey(subscriptionName))
            {
                return;
            }

            handlerDict[subscriptionName].Invoke();
        }

        public void InvokeByGroupExcept(string groupName, string exceptPlayerInstanceId, string subscriptionName)
        {
            if (!groups.ContainsKey(groupName))
            {
                return;
            }

            var playerInstanceIds = groups[groupName];
            foreach (var playerInstanceId in playerInstanceIds)
            {
                if (playerInstanceId == exceptPlayerInstanceId)
                {
                    continue;
                }

                InvokeByPlayer(playerInstanceId, subscriptionName);
            }
        }

        public void InvokeByGroup(string groupName, string subscriptionName)
        {
            if (!groups.ContainsKey(groupName))
            {
                return;
            }

            var playerInstanceIds = groups[groupName];
            foreach (var playerInstanceId in playerInstanceIds)
            {
                InvokeByPlayer(playerInstanceId, subscriptionName);
            }
        }
    }
}
