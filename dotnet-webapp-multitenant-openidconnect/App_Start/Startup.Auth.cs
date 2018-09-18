using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IdentityModel.Claims;
using System.IdentityModel.Tokens;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

namespace dotnet_webapp_multitenant_openidconnect
{
    public partial class Startup
    {
        private string ClientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private string Authority = EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance"]) + "common";

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions { });

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = ClientId,
                    Authority = Authority,
                    TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        // instead of using the default validation (validating against a single issuer value, as we do in line of business apps), 
                        // we inject our own multitenant validation logic
                        ValidateIssuer = false,
                        // If the app needs access to the entire organization, then add the logic
                        // of validating the Issuer here.
                        // IssuerValidator
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {   
                        SecurityTokenValidated = (context) =>
                        {
                            // If your authentication logic is based on users then add your logic here
                            return Task.FromResult(0);
                        } ,                    
                        AuthenticationFailed = (context) =>
                        {
                            // Pass in the context back to the app
                            context.OwinContext.Response.Redirect("/Home/Error");
                            context.HandleResponse(); // Suppress the exception
                            return Task.FromResult(0);
                        }
                    }
                });
        }

        private static string EnsureTrailingSlash(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (!value.EndsWith("/", StringComparison.Ordinal))
            {
                return value + "/";
            }

            return value;
        }
    }
}
