using LanternCardGame.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LanternCardGame.Services
{
    public class CurrentUserService
    {
        private readonly OnlinePlayersService playersService;
        private readonly AuthenticationStateProvider authenticationStateProvider;
        private bool isAdmin;

        public CurrentUserService(
            OnlinePlayersService playersService,
            AuthenticationStateProvider authenticationStateProvider)
        {
            this.playersService = playersService;
            this.authenticationStateProvider = authenticationStateProvider;
        }

        public string UserId { get; set; }

        public string InstanceId { get; set; }

        public string Username { get; set; }

        public string UserEmail { get; set; }

        public bool IsAdmin => this.isAdmin;

        public bool IsAuthenticated { get; set; }

        public async Task InitializeAsync()
        {
            var auth = await authenticationStateProvider.GetAuthenticationStateAsync();
            this.UserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
            this.InstanceId = this.playersService.GetPlayerById(UserId)?.InstanceId;
            this.Username = auth.User.FindFirstValue(ClaimTypes.Name);
            this.UserEmail = auth.User.FindFirstValue(ClaimTypes.Email);
            this.IsAuthenticated = auth.User.Identity.IsAuthenticated;
            this.isAdmin = auth.User.IsInRole("Administrator");
        }
    }
}
