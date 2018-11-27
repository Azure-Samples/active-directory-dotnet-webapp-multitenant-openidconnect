using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

namespace TodoListWebApp
{
    public partial class Startup
    {
        public static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        public string appKey = ConfigurationManager.AppSettings["ida:ClientSecret"];
        public string graphResourceID = "https://graph.windows.net";

        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
