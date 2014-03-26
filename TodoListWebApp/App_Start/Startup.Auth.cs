using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using System.Configuration;
using TodoListWebApp.DAL;
using System.IdentityModel.Claims;
using System.IdentityModel.Tokens;

namespace TodoListWebApp
{

    public partial class Startup
    {
        private TodoListWebAppContext db = new TodoListWebAppContext();
        public void ConfigureAuth(IAppBuilder app)
        {
            //https://manage.windowsazure.com/microsoft.onmicrosoft.com#Workspaces/ActiveDirectoryExtension/Directory/6c3d51dd-f0e5-4959-b4ea-a80c4e36fe5e/RegisteredApp/d71c88d1-f3d3-47e9-8313-06bc9af9a991/registeredAppConfigure
            string ClientId = ConfigurationManager.AppSettings["ida:ClientID"];
            string Authority = "https://login.windows.net/common/";

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions { });

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    Client_Id = ClientId,
                    Authority = Authority,
                    Post_Logout_Redirect_Uri = "https://localhost:44302/", // app.Properties["host.Addresses"].ToString(),
                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = false,
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        SecurityTokenValidated = (context) =>
                        {
                            string issuer = context.AuthenticationTicket.Identity.FindFirst("iss").Value;
                            string UPN = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.Name).Value;

                            if (
                                //admin consented, recorded issuer
                                (db.Tenants.FirstOrDefault(a => ((a.IssValue == issuer) && (a.AdminConsented))) == null)
                                //user consented, recorded user 
                                && (db.Users.FirstOrDefault(b => (b.UPN == UPN)) == null)
                                )
                                throw new SecurityTokenValidationException();
                            //add caller validation logic
                            return Task.FromResult(0);
                        },
                        AuthenticationFailed = (context) =>
                        {
                            context.Redirect("/Home/Error");
                            return Task.FromResult(0);
                        }
                    }
                });

        }

    }
}