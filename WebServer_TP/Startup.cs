using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(JsWebServer_CP.Startup))]
namespace JsWebServer_CP
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
