using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TodoListWebApp.Controllers
{
    public class AccountController : Controller
    {
        // sign in triggered from the Sign In gesture in the UI
        // configured to return to the home page upon successful authentication
        public void SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }
        // sign out triggered from the Sign Out gesture in the UI
        // after sign out, it redirects to Post_Logout_Redirect_Uri (as set in Startup.Auth.cs)
        public void SignOut()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(
                OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);            
        }
        public void Manage()
        {
            // to be added
        }
	}
}