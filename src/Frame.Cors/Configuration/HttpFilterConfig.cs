using Frame.Cors.Interceptor;
using System.Web.Http.Filters;

namespace Frame.Cors.Configuration
{
    internal class HttpFilterConfig
    {
        public static void RegisterGlobalFilters(HttpFilterCollection filters)
        {
            filters.Add(new ControllerHandler());
        }
    }
}