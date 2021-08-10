using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LanternCardGame.Services
{
    public class UserService
    {
        private readonly UserManager<IdentityUser> userManager;

        public UserService(UserManager<IdentityUser> userManager)
        {
            this.userManager = userManager;
        }

        public async Task<IdentityUser> GetUserByIdAsync(string id)
        {
            return await userManager.FindByIdAsync(id);
        }

        public async Task<IdentityUser> GetUserByEmailAsync(string email)
        {
            return await userManager.FindByEmailAsync(email);
        }

        public async Task<IdentityUser> GetUserByUsernameAsync(string username)
        {
            return await userManager.FindByNameAsync(username);
        }

        public ICollection<IdentityUser> GetAllUsers()
        {
            return userManager.Users.ToList();
        }
    }
}
