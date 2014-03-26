using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TodoListWebApp.Models;
using TodoListWebApp.DAL;
using System.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace TodoListWebApp.Controllers
{
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
        public ActionResult SignUp([Bind(Include="ID,Name,AdminConsented")] Tenant tenant)
        {
            // generate a random value to identify the request
            string stateMarker = Guid.NewGuid().ToString();
            //store it in the temporary entry for the tenant
            tenant.IssValue = stateMarker;
            tenant.Created = DateTime.Now;
            db.Tenants.Add(tenant);
            db.SaveChanges();

            //create an OAuth2 request, using the web app as the client.
            //this will trigger a consent flow that will provision the app in the target tenant
            string authorizationRequest = String.Format(
                "https://login.windows.net/common/oauth2/authorize?response_type=code&client_id={0}&resource={1}&redirect_uri={2}&state={3}",
                 Uri.EscapeDataString(ConfigurationManager.AppSettings["ida:ClientID"]),
                 Uri.EscapeDataString("https://graph.windows.net"),
                 Uri.EscapeDataString(this.Request.Url.GetLeftPart(UriPartial.Authority).ToString() + "/Onboarding/ProcessCode"),
                 Uri.EscapeDataString(stateMarker)
                 );
            //if the prospect customer wants to provision the app for all users in his/her tenant, the request must be modified accordingly
            if (tenant.AdminConsented)
                authorizationRequest += String.Format("&prompt={0}", Uri.EscapeDataString("admin_consent"));
            // send the user to consent
            return new RedirectResult(authorizationRequest);
        }

         // GET: /TOnboarding/ProcessCode
        public ActionResult ProcessCode(string code, string error, string error_description, string resource, string state)
        {
            // do we like the state?
            // ---if we don't, return an error            
            if (db.Tenants.FirstOrDefault(a => a.IssValue == state) == null)
            {
                // TODO: prettify
                return View("Error");
            }
            else
            {
                // ---if we do
                // ------get a token for the graph
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                                                                   ConfigurationManager.AppSettings["ida:Password"]);
                AuthenticationContext authContext = new AuthenticationContext("https://login.windows.net/common/");
                AuthenticationResult result = authContext.AcquireTokenByAuthorizationCode(
                    code, new Uri(Request.Url.GetLeftPart(UriPartial.Path)), credential);

                var myTenant = db.Tenants.FirstOrDefault(a => a.IssValue == state);
                // if this was an admin consent, save the tenant
                if (myTenant.AdminConsented)
                {
                    // ------read the tenantID out of the Graph token and use it to create the issuer string
                    string issuer = String.Format("https://sts.windows.net/{0}/", result.TenantId);
                    myTenant.IssValue = issuer;
                }
                else
                //otherwise, remove the temporary entry and save just the user
                {
                    db.Users.Add(new User { UPN = result.UserInfo.UserId, TenantID = result.TenantId });
                    db.Tenants.Remove(myTenant);
                }

                // remove older, unclaimed entries
                DateTime tenMinsAgo = DateTime.Now.Subtract(new TimeSpan(0, 10, 0)); // workaround for Linq to entities
                var garbage = db.Tenants.Where(a => (!a.IssValue.StartsWith("https") && (a.Created < tenMinsAgo)));
                foreach (Tenant t in garbage)
                    db.Tenants.Remove(t);

                db.SaveChanges();
                // ------return a view claiming success, inviting the user to sign in
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
