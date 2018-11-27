using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

namespace dotnet_webapp_multitenant_oidc
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
