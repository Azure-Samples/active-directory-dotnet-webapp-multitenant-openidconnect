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
            string ClientId = ConfigurationManager.AppSettings["ida:ClientID"];
            //fixed address for multitenant apps in the public cloud
            string Authority = "https://login.microsoftonline.com/common/";

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions { });

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = ClientId,
                    Authority = Authority,
                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
                        // instead of using the default validation (validating against a single issuer value, as we do in line of business apps), 
                        // we inject our own multitenant validation logic
                        ValidateIssuer = false,
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        RedirectToIdentityProvider = (context) =>
                        {
                            // This ensures that the address used for sign in and sign out is picked up dynamically from the request
                            // this allows you to deploy your app (to Azure Web Sites, for example)without having to change settings
                            // Remember that the base URL of the address used here must be provisioned in Azure AD beforehand.
                            string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;                         
                            context.ProtocolMessage.RedirectUri = appBaseUrl;
                            context.ProtocolMessage.PostLogoutRedirectUri = appBaseUrl;
                            return Task.FromResult(0);
                        },
                        // we use this notification for injecting our custom logic
                        SecurityTokenValidated = (context) =>
                        {
                            // retriever caller data from the incoming principal
                            string issuer = context.AuthenticationTicket.Identity.FindFirst("iss").Value;
                            string UPN = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.Name).Value;
                            string tenantID = context.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

                            if (
                                // the caller comes from an admin-consented, recorded issuer
                                (db.Tenants.FirstOrDefault(a => ((a.IssValue == issuer) && (a.AdminConsented))) == null)
                                // the caller is recorded in the db of users who went through the individual onboardoing
                                && (db.Users.FirstOrDefault(b =>((b.UPN == UPN) && (b.TenantID == tenantID))) == null)
                                )
                                // the caller was neither from a trusted issuer or a registered user - throw to block the authentication flow
                                throw new SecurityTokenValidationException();                            
                            return Task.FromResult(0);
                        },
                        AuthenticationFailed = (context) =>
                        {
                            context.OwinContext.Response.Redirect("/Home/Error?message=" + context.Exception.Message);
                            context.HandleResponse(); // Suppress the exception
                            return Task.FromResult(0);
                        }
                    }
                });

        }

    }
}