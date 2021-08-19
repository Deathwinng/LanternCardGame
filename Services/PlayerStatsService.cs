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
        private readonly object balanceLock = new object();

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

        public void PlayerLeftGame(string playerId)
        {
            lock (this.balanceLock)
            {
                var stats = this.GetPlayerStatsById(playerId);
                stats.GamesLeft++;
                this.dbContext.SaveChanges();
            }
        }

        public void PlayerPlacedFirs(string playerId)
        {
            lock (this.balanceLock)
            {
                var stats = this.GetPlayerStatsById(playerId);
                stats.GamesWon++;
                this.dbContext.SaveChanges();
            }
        }

        public void PlayerPlacedLast(string playerId)
        {
            lock (this.balanceLock)
            {
                var stats = this.GetPlayerStatsById(playerId);
                stats.GamesPlacedLast++;
                this.dbContext.SaveChanges();
            }
        }

        public void PlayerFinishedGame(string playerId)
        {
            lock (this.balanceLock)
            {
                var stats = this.GetPlayerStatsById(playerId);
                stats.GamesFinished++;
                this.dbContext.SaveChanges();
            }
        }

        public void PlayersFinishedGame(IEnumerable<string> playerIds)
        {
            lock (this.balanceLock)
            {
                var stats = this.dbContext.Users.Where(x => playerIds.Contains(x.Id)).Select(x => x.PlayerStats).ToList();
                foreach (var stat in stats)
                {
                    stat.GamesFinished++;
                }

                this.dbContext.SaveChanges();
            }
        }
    }
}
