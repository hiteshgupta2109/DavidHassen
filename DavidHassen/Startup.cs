using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DavidHassen.Startup))]
namespace DavidHassen
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
