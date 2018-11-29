/************************************************************************************************
The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
***********************************************************************************************/

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Linq;
using System.IdentityModel.Tokens;
using System.Threading.Tasks;
using System.Web;
using TodoListWebApp.DAL;
using TodoListWebApp.Models;
using System.IdentityModel.Claims;

namespace TodoListWebApp
{
    public partial class Startup
    {
        private string authority = aadInstance + "common";
        private const string objectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";

        //private ApplicationDbContext db = new ApplicationDbContext();
        private TodoListWebAppContext db = new TodoListWebAppContext();

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions { });

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = clientId,
                    Authority = authority,
                    RedirectUri = redirectUri,
                    PostLogoutRedirectUri = redirectUri,
                    TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        // instead of using the default validation (validating against a single issuer value, as we do in line of business apps),
                        // we inject our own multi-tenant validation logic
                        ValidateIssuer = false,
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        RedirectToIdentityProvider = (context) =>
                        {
                            // This ensures that the address used for sign in and sign out is picked up dynamically from the request
                            // this allows you to deploy your app (to Azure Web Sites, for example)without having to change settings
                            // Remember that the base URL of the address used here must be provisioned in Azure AD beforehand.
                            string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase + "/";
                            context.ProtocolMessage.RedirectUri = appBaseUrl;
                            context.ProtocolMessage.PostLogoutRedirectUri = appBaseUrl;
                            return Task.FromResult(0);
                        },
                        SecurityTokenValidated = (context) =>
                        {
                            // retrieve caller data from the incoming principal
                            string issuer = context.AuthenticationTicket.Identity.FindFirst("iss").Value;
                            string UPN = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.Name).Value;
                            string tenantID = context.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

                            if (
                                // the caller comes from an admin-consented, recorded issuer
                                (db.Tenants.FirstOrDefault(a => ((a.IssValue == issuer) && (a.AdminConsented))) == null)
                                // the caller is recorded in the db of users who went through the individual on-boarding
                                && (db.Users.FirstOrDefault(b => ((b.UPN == UPN) && (b.TenantID == tenantID))) == null)
                                )
                                // the caller was neither from a trusted issuer or a registered user - throw to block the authentication flow
                                throw new SecurityTokenValidationException();

                            return Task.FromResult(0);
                        },
                        AuthorizationCodeReceived = (context) =>
                        {
                            var code = context.Code;
                            ClientCredential credential = new ClientCredential(clientId, appKey);
                            string tenantID = context.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
                            string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(System.IdentityModel.Claims.ClaimTypes.NameIdentifier).Value;

                            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext authContext = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(aadInstance + tenantID, new ADALTokenCache(signedInUserID));
                            AuthenticationResult result = authContext.AcquireTokenByAuthorizationCodeAsync(
                                code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, graphResourceID).Result;

                            return Task.FromResult(0);
                        },
                        AuthenticationFailed = (context) =>
                        {
                            context.Response.Redirect("/Error/ShowError?signIn=true&errorMessage=" + context.Exception.Message);
                            context.HandleResponse(); // Suppress the exception
                            return Task.FromResult(0);
                        }
                    }
                });
        }
    }
}