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
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TodoListWebApp.DAL;
using TodoListWebApp.Models;

namespace TodoListWebApp.Controllers
{
    /// <summary>
    /// Controller that handles the on-boarding of new tenants and new individual users. It operates by starting an OAuth2 request on behalf of the user.
    /// During that request, the user is asked whether he/she consent for the app to gain access to the specified directory permissions.
    /// </summary>
    public class OnboardingController : Controller
    {
        private TodoListWebAppContext db = new TodoListWebAppContext();

        // GET: /Onboarding/SignUp
        public ActionResult SignUp()
        {
            return View();
        }

        // POST: /Onboarding/SignUp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SignUp([Bind(Include = "ID,Name,AdminConsented")] Tenant tenant)
        {
            // Generate a random value to identify the request
            string stateMarker = Guid.NewGuid().ToString();

            // Store it in the temporary entry for the tenant, we'll use it later to assess if the request was originated from us.
            // This is necessary if we want to prevent attackers from provisioning themselves to access our app without having gone through our onboarding process (e.g. payments, etc)
            tenant.IssValue = stateMarker;
            tenant.Created = DateTime.Now;

            db.Tenants.Add(tenant);
            db.SaveChanges();

            // Create an OAuth2 request, using the web app as the client.This will trigger a consent flow that will provision the app in the target tenant.
            string authorizationRequest = string.Format(
                "{0}common/oauth2/authorize?response_type=code&client_id={1}&resource={2}&redirect_uri={3}&state={4}",
                Startup.aadInstance,
                Uri.EscapeDataString(Startup.clientId),
                Uri.EscapeDataString(Startup.graphResourceID),
                Uri.EscapeDataString(this.Request.Url.GetLeftPart(UriPartial.Authority).ToString() + "/Onboarding/ProcessCode"),
                Uri.EscapeDataString(stateMarker));

            // If the prospect customer wants to provision the app for all users in his/her tenant, the request must be modified accordingly
            if (tenant.AdminConsented)
            {
                authorizationRequest += String.Format("&prompt={0}", Uri.EscapeDataString("admin_consent"));
            }

            // Send the user to consent
            return new RedirectResult(authorizationRequest);
        }

        // GET: /Onboarding/ProcessCode
        public async Task<ActionResult> ProcessCode(string code, string error, string error_description, string resource, string state)
        {
            if (error != null)
            {
                if (error_description.StartsWith("AADSTS65005"))
                {
                    error_description = $"Please restart the sign-up as an administrator of the home tenant and consent for app permissions first. If thats already done, please sign-up again as the administrator of the host tenant to complete the provisioning of this app in the host tenant. Also check if this app already exists in the 'Enterprise Application'. {error_description}";
                }
                return RedirectToAction("ShowError", "Error", new { errorMessage = error_description });
            }

            // Is this a response to a request we generated? Let's see if the state is carrying an ID we previously saved
            // If we don't, return an error            
            if (db.Tenants.FirstOrDefault(a => a.IssValue == state) == null)
            {
                return RedirectToAction("ShowError", "Error", new { errorMessage = "State verification failed." });
            }
            else
            {
                // If the response is indeed from a request we generated
                var myTenant = db.Tenants.FirstOrDefault(a => a.IssValue == state);

                // Get a token for the Graph, that will provide us with information abut the caller
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.appKey);
                AuthenticationContext authContext = new AuthenticationContext($"{Startup.aadInstance}common/");

                AuthenticationResult result = await authContext.AcquireTokenByAuthorizationCodeAsync(code, new Uri(Request.Url.GetLeftPart(UriPartial.Path)), credential);

                // if this was an admin consent, save the tenant
                if (myTenant.AdminConsented)
                {
                    // Read the tenantID out of the Graph token and use it to create the issuer string
                    string issuer = String.Format("{0}{1}/", Startup.issuerEndpoint, result.TenantId);
                    myTenant.IssValue = issuer;
                }
                else
                {
                    // Otherwise, remove the temporary entry and save just the user
                    if (db.Users.FirstOrDefault(a => (a.UPN == result.UserInfo.DisplayableId) && (a.TenantID == result.TenantId)) == null)
                    {
                        db.Users.Add(new User { UPN = result.UserInfo.DisplayableId, TenantID = result.TenantId });
                    }
                    db.Tenants.Remove(myTenant);
                }

                // Remove older, unclaimed entries
                DateTime tenMinsAgo = DateTime.Now.Subtract(new TimeSpan(0, 10, 0)); // workaround for Linq to entities
                var garbage = db.Tenants.Where(a => (!a.IssValue.StartsWith("https") && (a.Created < tenMinsAgo)));
                foreach (Tenant t in garbage)
                    db.Tenants.Remove(t);

                db.SaveChanges();
                // Return a view claiming success, inviting the user to sign in
                return View();
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}