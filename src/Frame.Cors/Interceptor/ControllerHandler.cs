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
            if (apiAC == null)
            {
                actionExecutedContext.Response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                actionExecutedContext.Response.ReasonPhrase = "Response failed. Atleast one of Access-Control-Allow-Origin, Access-Control-Allow-Methods, Access-Control-Allow-Headers is missing from the response.";
            }
            else {
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
                                    if (!existh.Contains(AccessControlHeader.ALLOW_ORIGIN))
                                    {
                                        actionExecutedContext.Response.Headers.Add(AccessControlHeader.ALLOW_ORIGIN, reqOrigin);
                                        hasOriginMatch = true;
                                    }
                                }
                            }
                            else
                            {
                                if (apiAC.accessControlAllowOrigin.Equals("*") || apiAC.accessControlAllowOrigin.Equals("null"))
                                {
                                    actionExecutedContext.Response.Headers.Add(AccessControlHeader.ALLOW_ORIGIN, apiAC.accessControlAllowOrigin);
                                    hasOriginMatch = true;
                                }
                            }
                        }

                        if (hasOriginMatch)
                        {
                            if (apiAC.accessControlAllowCredentials.HasValue)
                            {
                                if (!existh.Contains(AccessControlHeader.ALLOW_CREDENTIALS))
                                {
                                    actionExecutedContext.Response.Headers.Add(AccessControlHeader.ALLOW_CREDENTIALS, apiAC.accessControlAllowCredentials.ToString().ToLower());
                                }
                            }

                            if (apiAC.accessControlExposeHeaders != null)
                            {
                                if (!existh.Contains(AccessControlHeader.EXPOSE_HEADERS))
                                {
                                    actionExecutedContext.Response.Headers.Add(AccessControlHeader.EXPOSE_HEADERS, apiAC.accessControlExposeHeaders.ToString());
                                }
                            }
                        }
                        else
                        {
                            actionExecutedContext.Response.Headers.Remove(AccessControlHeader.ALLOW_ORIGIN); // if any
                            actionExecutedContext.Response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                        }
                    }
                    catch (Exception resException)
                    {
                        actionExecutedContext.Response.Headers.Remove(AccessControlHeader.ALLOW_ORIGIN);
                        actionExecutedContext.Response.Headers.Remove(AccessControlHeader.ALLOW_METHODS);
                        actionExecutedContext.Response.Headers.Remove(AccessControlHeader.ALLOW_HEADERS);
                        actionExecutedContext.Response.Headers.Remove(AccessControlHeader.ALLOW_CREDENTIALS);
                        actionExecutedContext.Response.Headers.Remove(AccessControlHeader.MAX_AGE);
                        actionExecutedContext.Response.Headers.Remove(AccessControlHeader.EXPOSE_HEADERS);

                        actionExecutedContext.Response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                    }
                }
            }
        }

        public void Dispose()
        {

        }
    }
}