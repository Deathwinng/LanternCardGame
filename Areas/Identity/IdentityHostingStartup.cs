using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(LanternCardGame.Areas.Identity.IdentityHostingStartup))]
namespace LanternCardGame.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
            });
        }
    }
}