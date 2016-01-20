using Frame.Cors.Common;
using Frame.Cors.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Frame.Cors.Interceptor
{
    internal class ControllerHandler : System.Web.Http.Filters.ActionFilterAttribute, IDisposable
    {
        string reqOrigin;
        string reqMethod;
        ICollection<string> reqHeaders;
        APIAccessControl apiAC;
        bool isCrossOrigin;
        bool hasParseErrors;
        bool hasDuplicateRules;        

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            string ctrl = actionContext.ActionDescriptor.ControllerDescriptor.ControllerName; // home                        

            HttpManager.GetParamsFrom(actionContext.Request,
                out reqOrigin,
                out reqMethod,
                out reqHeaders,
                out apiAC,
                out isCrossOrigin,
                out hasParseErrors,
                out hasDuplicateRules,
                ctrl);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (hasParseErrors)
            {
                actionExecutedContext.Response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                actionExecutedContext.Response.ReasonPhrase = "Response failed. Error occurred while parsing resource on server.";
            }
            else if (hasDuplicateRules)
            {
                actionExecutedContext.Response.StatusCode = System.Net.HttpStatusCode.Conflict;
                actionExecutedContext.Response.ReasonPhrase = "Response failed. Duplicate access control rules found on server.";
            }
            else
            {
                try
                {
                    bool hasOriginMatch = false;
                    ICollection<string> existh = actionExecutedContext.Response.Headers.Select(p => p.Key).ToList<string>();

                    if (!string.IsNullOrEmpty(apiAC.accessControlAllowOrigin))
                    {
                        if (reqOrigin != null)
                        {
                            if ((reqOrigin.Equals(apiAC.accessControlAllowOrigin, StringComparison.InvariantCultureIgnoreCase) || apiAC.accessControlAllowOrigin.Equals("*")))
                            {
                                if (!existh.Contains("Access-Control-Allow-Origin"))
                                {
                                    actionExecutedContext.Response.Headers.Add("Access-Control-Allow-Origin", reqOrigin);
                                    hasOriginMatch = true;
                                }
                            }
                        }
                        else
                        {
                            if (apiAC.accessControlAllowOrigin.Equals("*") || apiAC.accessControlAllowOrigin.Equals("null"))
                            {
                                actionExecutedContext.Response.Headers.Add("Access-Control-Allow-Origin", apiAC.accessControlAllowOrigin);
                                hasOriginMatch = true;
                            }
                        }
                    }

                    if (hasOriginMatch)
                    {
                        if (apiAC.accessControlAllowCredentials != null)
                        {
                            if (!existh.Contains("Access-Control-Allow-Credentials"))
                            {
                                actionExecutedContext.Response.Headers.Add("Access-Control-Allow-Credentials", apiAC.accessControlAllowCredentials.ToString());
                            }
                        }

                        if (apiAC.accessControlExposeHeaders != null)
                        {
                            if (!existh.Contains("Access-Control-Expose-Headers"))
                            {
                                actionExecutedContext.Response.Headers.Add("Access-Control-Expose-Headers", apiAC.accessControlExposeHeaders.ToString());
                            }
                        }
                    }
                    else
                    {
                        actionExecutedContext.Response.Headers.Remove("Access-Control-Allow-Origin"); // if any
                        actionExecutedContext.Response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    }
                }
                catch (Exception resException)
                {
                    actionExecutedContext.Response.Headers.Remove("Access-Control-Allow-Origin");
                    actionExecutedContext.Response.Headers.Remove("Access-Control-Allow-Methods");
                    actionExecutedContext.Response.Headers.Remove("Access-Control-Allow-Headers");
                    actionExecutedContext.Response.Headers.Remove("Access-Control-Allow-Credentials");
                    actionExecutedContext.Response.Headers.Remove("Access-Control-Max-Age");
                    actionExecutedContext.Response.Headers.Remove("Access-Control-Expose-Headers");

                    actionExecutedContext.Response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                }
            }
        }

        public void Dispose()
        {

        }
    }
}