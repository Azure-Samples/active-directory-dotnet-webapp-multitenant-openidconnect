using System.Web;
using System.Web.Mvc;

namespace dotnet_webapp_multitenant_oidc
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
