using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using TodoListWebApp.Models;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using System.Net.Http.Headers;

namespace TodoListWebApp.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: UserProfile
        public async Task<ActionResult> Index()
        {
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            try
            {
                Uri servicePointUri = new Uri(Startup.graphResourceID);
                Uri serviceRoot = new Uri(servicePointUri, tenantID);
                GraphServiceClient graphServiceClient = GetAuthenticatedGraphServiceClient();

                // use the token for querying the graph to get the user details

                User me = await graphServiceClient.Me.Request().GetAsync();
                
                return View(me);
            }
            catch (MsalServiceException eee)
            {
                // if the above failed, the user needs to explicitly re-authenticate for the app to obtain the required token
                ViewBag.Error = "An error has occurred. Details: " + eee.Message;
                return View("Relogin");
            }            
            catch (Exception)
            {
                // Return to error page.
                return RedirectToAction("ShowError", "Error");
            }
        }

        public void RefreshSession()
        {
            HttpContext.GetOwinContext().Authentication.Challenge(
                new AuthenticationProperties { RedirectUri = "/UserProfile" },
                OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }

        public async Task<string> GetTokenForApplicationAsync()
        {
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            // get a token for the Graph without triggering any user interaction (from the cache, via multi-resource refresh token, etc)
            TokenCache userTokenCache = new MSALSessionCache(signedInUserID, this.HttpContext).GetMsalCacheInstance();
            ConfidentialClientApplication app = new ConfidentialClientApplication(Startup.clientId, Startup.redirectUri, new ClientCredential(Startup.appKey), userTokenCache, null);            
            AuthenticationResult result = null;
            var accounts = await app.GetAccountsAsync();
            
            try
            {
                result = await app.AcquireTokenSilentAsync(Startup.Scopes, accounts.FirstOrDefault());
            }
            catch (Exception ex)
            {
                Response.Write(ex.Message);
            }

            return result?.AccessToken;
        }

        public GraphServiceClient GetAuthenticatedGraphServiceClient()
        {
            GraphServiceClient graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        string accessToken = await GetTokenForApplicationAsync();

                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
                    }));
            return graphClient;
        }
    }
}
