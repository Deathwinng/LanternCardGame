using System;

namespace LanternCardGame.Data
{
    public class UserFriend
    {
        public UserFriend()
        {
            this.Id = Guid.NewGuid().ToString();
            this.Accepted = false;
        }

        public string Id { get; set; }

        public string UserId { get; set; }

        public virtual ApplicationUser User { get; set; }

        public string FriendUserId { get; set; }

        public virtual ApplicationUser FriendUser { get; set; }

        public bool Accepted { get; set; }
    }
}
