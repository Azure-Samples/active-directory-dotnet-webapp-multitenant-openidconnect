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
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TodoListWebApp.DAL;
using TodoListWebApp.Models;
using TodoListWebApp.Utils;

namespace TodoListWebApp
{
    public partial class Startup
    {
        private readonly string authority = AadInstance + "common";

        private TodoListWebAppContext db = new TodoListWebAppContext();

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions { });

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = ClientId,
                    Authority = this.authority,
                    RedirectUri = RedirectUri,
                    PostLogoutRedirectUri = RedirectUri,
                    TokenValidationParameters = this.BuildTokenValidationParameters(),
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
                            string Upn = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.Name).Value;
                            string tenantId = context.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

                            if (
                                // the caller comes from an admin-consented, recorded issuer
                                (this.db.Tenants.FirstOrDefault(a => ((a.IssValue == issuer) && (a.AdminConsented))) == null)
                                // the caller is recorded in the db of users who went through the individual on-boarding
                                && (this.db.Users.FirstOrDefault(b => ((b.UPN == Upn) && (b.TenantID == tenantId))) == null)
                                )
                                // the caller was neither from a trusted issuer or a registered user - throw to block the authentication flow
                                throw new SecurityTokenValidationException("Please use the Sign-up link to sign -up for the ToDo list application.");
                            
                            return Task.FromResult(0);
                        },
                        AuthorizationCodeReceived = (context) =>
                        {
                            var code = context.Code;
                            ClientCredential credential = new ClientCredential(ClientId, AppKey);
                            string tenantId = context.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
                            string signedInUserId = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;

                            AuthenticationContext authContext = new AuthenticationContext(AadInstance + tenantId, new ADALTokenCache(signedInUserId));

                            // The following operation fetches a token for Microsoft graph and caches it in the token cache
                            AuthenticationResult result = authContext.AcquireTokenByAuthorizationCodeAsync(
                                code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, GraphResourceId).Result;

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

        private TokenValidationParameters BuildTokenValidationParameters()
        {
            string defaultV2Issuer = "https://login.microsoftonline.com/{tenantid}/v2.0";

            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                // Since this is a multi-tenant app, you should ideally only accept users from a list of tenants that you want to.
                // * Instead of using the default validation (validating against a single issuer value, as we do in line of business apps), we inject our own multi-tenant validation logic through IssuerValidator.
                // * Or you can provide a static list of acceptable tenantIds, as detailed below
                // ValidIssuers = new List<string>()
                // {
                //     "https://sts.windows.net/6d9c0c36-c30e-442b-b60a-ca22d8994d14/",
                //     "https://sts.windows.net/f69b5f46-9a0d-4a5c-9e25-54e42bbbd4c3/",
                //     "https://sts.windows.net/fb674642-8965-493d-beee-2703caa74f9a/"
                //     "https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0"
                // }
                ValidateIssuer = true,
                IssuerValidator = AadIssuerValidator.ValidateAadIssuer // Use a custom Issuer validator 
            };

            // Here we store the tenantIds who've signed up for the app in a database. When the app has to validate, it builds a list of signed-up issuers from the database.
            List<String> validIssuers = this.db.Tenants.GroupBy(a => a.IssValue).Select(a => a.FirstOrDefault().IssValue).ToList();

            // Add AAD V2 default issuer
            if (!validIssuers.Contains(defaultV2Issuer))
            {
                validIssuers.Add(defaultV2Issuer);
            }

            validationParameters.ValidIssuers = validIssuers;
            return validationParameters;
        }
    }
}