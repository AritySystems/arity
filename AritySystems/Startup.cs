using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AritySystems.Startup))]
namespace AritySystems
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //ConfigureAuth(app);
        }
    }
}
