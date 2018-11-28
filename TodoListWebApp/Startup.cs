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
        public static string appKey = ConfigurationManager.AppSettings["ida:ClientSecret"];
        public static string graphResourceID = ConfigurationManager.AppSettings["GraphAPIEndpoint"];
        public static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        
        public static string aadInstance = EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance"]);

        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
