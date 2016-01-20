using Frame.Cors.Configuration;
using Frame.Cors.Interceptor;
using System.Web.Http;

namespace Frame.Cors
{
    public static class Helper
    {
        public static void RegisterFrame(this HttpConfiguration config, bool runLocalOnly = false)
        {
            config.MessageHandlers.Add(new HttpHandler(runLocalOnly));
            HttpFilterConfig.RegisterGlobalFilters(config.Filters);
        }
    }
}
