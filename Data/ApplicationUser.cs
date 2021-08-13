using Microsoft.AspNetCore.Identity;

namespace LanternCardGame.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string PlayerStatsId { get; set; }

        public PlayerStats PlayerStats { get; set; }
    }
}
