using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LanternCardGame.Services
{
    public class CurrentUserService
    {
        private readonly OnlinePlayersService playersService;
        private readonly AuthenticationStateProvider authenticationStateProvider;

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

        public bool IsAuthenticated { get; set; }

        public async Task Initialize()
        {
            var auth = await authenticationStateProvider.GetAuthenticationStateAsync();
            UserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
            InstanceId = this.playersService.GetPlayerById(UserId)?.InstanceId;
            Username = auth.User.FindFirstValue(ClaimTypes.Name);
            UserEmail = auth.User.FindFirstValue(ClaimTypes.Email);
            IsAuthenticated = auth.User.Identity.IsAuthenticated;
        }
    }
}
