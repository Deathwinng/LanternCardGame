using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace LanternCardGame.Data
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            this.Friends = new List<UserFriend>();
        }

        public string PlayerStatsId { get; set; }

        public virtual PlayerStats PlayerStats { get; set; }

        public ICollection<UserFriend> Friends { get; set; }
    }
}
