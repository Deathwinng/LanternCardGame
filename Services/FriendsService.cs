using LanternCardGame.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LanternCardGame.Services
{
    public class FriendsService
    {
        private readonly ApplicationDbContext dbContext;

        public FriendsService(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task SendFriendRequestAsync(string userId, string sendToUserId)
        {
            var userFriend = new UserFriend()
            {
                UserId = userId,
                FriendUserId = sendToUserId,
            };

            this.dbContext.UserFriends.Add(userFriend);
            await this.dbContext.SaveChangesAsync();
        }

        public async Task AcceptFriendRequestAsync(string senderUsername, string receiverUsername)
        {
            var userFriend = this.dbContext.UserFriends.FirstOrDefault(
                x => x.User.UserName == senderUsername &&
                x.FriendUser.UserName == receiverUsername &&
                !x.Accepted);

            if (userFriend == null)
            {
                return;
            }

            userFriend.Accepted = true;
            await this.dbContext.SaveChangesAsync();
        }

        public async Task DismissFriendRequestAsync(string senderUsername, string receiverUsername)
        {
            var userFriend = this.dbContext.UserFriends.FirstOrDefault(
                x => x.User.UserName == senderUsername &&
                x.FriendUser.UserName == receiverUsername &&
                !x.Accepted);

            if (userFriend == null)
            {
                return;
            }

            this.dbContext.Remove(userFriend);
            await this.dbContext.SaveChangesAsync();
        }

        public async Task RemoveFriend(string username1, string username2)
        {
            var userFriend = this.dbContext.UserFriends.FirstOrDefault(
                x =>
                ((x.User.UserName == username1 && x.FriendUser.UserName == username2) ||
                (x.User.UserName == username2 && x.FriendUser.UserName == username1)) &&
                x.Accepted);

            if (userFriend == null)
            {
                return;
            }

            this.dbContext.Remove(userFriend);
            await this.dbContext.SaveChangesAsync();

        }

        public bool CheckRequestExists(string username1, string username2)
        {
            return this.dbContext.UserFriends.Any(
                x =>
                ((x.User.UserName == username1 && x.FriendUser.UserName == username2) ||
                (x.User.UserName == username2 && x.FriendUser.UserName == username1)) &&
                !x.Accepted);
        }

        public bool CheckFriendsById(string id1, string id2)
        {
            return this.dbContext.UserFriends.Any(
                x =>
                ((x.User.Id == id1 && x.FriendUser.Id == id2) ||
                (x.User.Id == id2 && x.FriendUser.Id == id1)) &&
                x.Accepted);
        }

        public bool CheckFriendsByUsername(string username1, string username2)
        {
            return this.dbContext.UserFriends.Any(
                x =>
                ((x.User.UserName == username1 && x.FriendUser.UserName == username2) ||
                (x.User.UserName == username2 && x.FriendUser.UserName == username1)) && 
                x.Accepted);
        }

        public IEnumerable<string> GetPlayerFriendRequestsUsernames(string userId)
        {
            var friends = this.dbContext.UserFriends.Where(
                x => x.FriendUserId == userId && !x.Accepted)
                .Select(x => x.User.UserName)
                .ToList();

            return friends;
        }

        public IEnumerable<string> GetPendingPlayerFriendRequestsUsernames(string userId)
        {
            var friends = this.dbContext.UserFriends.Where(
                x => x.UserId == userId && !x.Accepted)
                .Select(x => x.FriendUser.UserName)
                .ToList();

            return friends;
        }

        public IEnumerable<string> GetPlayerFriendsUsernames(string userId)
        {
            var friends = this.dbContext.UserFriends.Where(
                x => x.UserId == userId &&
                x.Accepted)
                .Select(x => x.FriendUser.UserName)
                .ToList();

            friends.AddRange(this.dbContext.UserFriends.Where(
                x => x.FriendUserId == userId &&
                x.Accepted)
                .Select(x => x.User.UserName)
                .ToList());

            return friends;
        }

        public IEnumerable<ApplicationUser> GetPlayerFriends(string userId)
        {
            var friends = this.dbContext.UserFriends.Where(
                x => x.UserId == userId &&
                x.Accepted)
                .Select(x => x.FriendUser)
                .ToList();

            friends.AddRange(this.dbContext.UserFriends.Where(
                x => x.FriendUserId == userId &&
                x.Accepted)
                .Select(x => x.User)
                .ToList());

            return friends;
        }
    }
}
