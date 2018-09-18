using System.Web;
using System.Web.Mvc;

namespace dotnet_webapp_multitenant_openidconnect
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
