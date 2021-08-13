using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LanternCardGame.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>()
            .HasOne(x => x.PlayerStats)
            .WithOne(x => x.User)
            .HasForeignKey<ApplicationUser>(x => x.PlayerStatsId)
            .OnDelete(DeleteBehavior.Cascade);
            base.OnModelCreating(builder);
        }

        public DbSet<PlayerStats> PlayerStats { get; set; }
    }
}
