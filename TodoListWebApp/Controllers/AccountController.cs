using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security;
using TodoListWebApp.Models;
using System.Security.Claims;
using Microsoft.Identity.Client;
using System.Threading.Tasks;

namespace TodoListWebApp.Controllers
{
    public class AccountController : Controller
    {
        public void SignIn()
        {
            // Send an OpenID Connect sign-in request.
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }

        public async Task SignOut()
        {
            await this.RemoveCachedTokensAsync();

            string callbackUrl = Url.Action("SignOutCallback", "Account", routeValues: null, protocol: Request.Url.Scheme);
            
            HttpContext.GetOwinContext().Authentication.SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
        }

        public ActionResult SignOutCallback()
        {
            if (Request.IsAuthenticated)
            {
                // Redirect to home page if the user is authenticated.
                return RedirectToAction("Index", "Home");
            }

            return View();
        }


        /// <summary>
        /// Called by Azure AD. Here we end the user's session, but don't redirect to AAD for sign out.
        /// </summary>
        public async Task EndSession()
        {
            await this.RemoveCachedTokensAsync();
        }

        /// <summary>
        /// Remove all cache entries for this user.
        /// </summary>
        private async Task RemoveCachedTokensAsync()
        {
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            TokenCache userTokenCache = new MSALSessionCache(signedInUserID, this.HttpContext).GetMsalCacheInstance();

            ConfidentialClientApplication app = new ConfidentialClientApplication(Startup.clientId, Startup.redirectUri, new ClientCredential(Startup.appKey), userTokenCache, null);

            var accounts = await app.GetAccountsAsync();
            while (accounts.Any())
            {
                await app.RemoveAsync(accounts.First());
                accounts = await app.GetAccountsAsync();
            }
        }
    }
}
