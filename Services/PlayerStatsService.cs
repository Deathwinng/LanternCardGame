using LanternCardGame.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LanternCardGame.Services
{
    public class PlayerStatsService
    {
        private readonly ApplicationDbContext dbContext;

        public PlayerStatsService(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public PlayerStats GetPlayerStatsById(string id)
        {
            return this.dbContext.Users.Where(x => x.Id == id).Select(x => x.PlayerStats).FirstOrDefault();
        }

        public PlayerStats GetPlayerStatsByUsername(string username)
        {
            return this.dbContext.Users.Where(x => x.UserName == username).Select(x => x.PlayerStats).FirstOrDefault();
        }

        public async Task PlayerLeftGameAsync(string playerId)
        {
            var stats = this.GetPlayerStatsById(playerId);
            stats.GamesLeft++;
            await this.dbContext.SaveChangesAsync();
        }

        public void PlayerPlacedFirs(string playerId)
        {
            var stats = this.GetPlayerStatsById(playerId);
            stats.GamesWon++;
            this.dbContext.SaveChanges();
        }

        public void PlayerPlacedLast(string playerId)
        {
            var stats = this.GetPlayerStatsById(playerId);
            stats.GamesPlacedLast++;
            this.dbContext.SaveChanges();
        }

        public async Task PlayerFinishedGameAsync(string playerId)
        {
            var stats = this.GetPlayerStatsById(playerId);
            stats.GamesFinished++;
            await this.dbContext.SaveChangesAsync();
        }

        public void PlayersFinishedGame(IEnumerable<string> playerIds)
        {
            var stats = this.dbContext.Users.Where(x => playerIds.Contains(x.Id)).Select(x => x.PlayerStats).ToList();
            foreach (var stat in stats)
            {
                stat.GamesFinished++;
            }
            this.dbContext.SaveChanges();
        }

        public async Task SavePlayerStatsAsync(PlayerStats playerStats)
        {
            await this.dbContext.SaveChangesAsync();
        }
    }
}
