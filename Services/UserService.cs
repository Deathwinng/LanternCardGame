using LanternCardGame.Data;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LanternCardGame.Services
{
    public class UserService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ApplicationDbContext dbContext;

        public UserService(UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
        }

        public async Task<ApplicationUser> GetUserByIdAsync(string id)
        {
            return await userManager.FindByIdAsync(id);
        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await userManager.FindByEmailAsync(email);
        }

        public async Task<ApplicationUser> GetUserByUsernameAsync(string username)
        {
            return await userManager.FindByNameAsync(username);
        }

        public ICollection<ApplicationUser> GetAllUsers()
        {
            return userManager.Users.ToList();
        }
    }
}
