using Frame.Cors.Common;
using Frame.Cors.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Routing;

namespace Frame.Cors.Interceptor
{
    internal class HttpHandler : DelegatingHandler, IDisposable
    {
        string reqOrigin;
        string reqMethod;
        ICollection<string> reqHeaders;
        APIAccessControl apiAC;
        bool isCrossOrigin;
        bool hasParseErrors;
        bool hasDuplicateRules;

        bool runLocalOnly;

        static readonly ICollection<string> simpleMethods = new List<string>(new string[] { "GET", "POST", "HEAD" });
        static readonly ICollection<string> simpleHeaders = new List<string>(new string[] { "accept", "accept-language", "content-language" });
        static readonly ICollection<string> simpleContentTypes = new List<string>(new string[] { "application/x-www-form-urlencoded", "multipart/form-data", "text/plain" });

        public HttpHandler()
        {
            this.runLocalOnly = false;
        }

        public HttpHandler(bool runLocalOnly)
        {
            this.runLocalOnly = runLocalOnly;
        }

        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            IHttpRouteData routeData = request.GetRouteData();
            object ctrl;
            routeData.Values.TryGetValue("controller", out ctrl);

            //request.Properties.TryGetValue("MS_HttpRouteData", out routedata);

            if (request.Method.Method.Equals("OPTIONS", StringComparison.InvariantCultureIgnoreCase))
            {
                HttpManager.GetParamsFrom(request,
                    out reqOrigin,
                    out reqMethod,
                    out reqHeaders,
                    out apiAC,
                    out isCrossOrigin,
                    out hasParseErrors,
                    out hasDuplicateRules,
                    (string)ctrl);

                HttpResponseMessage res = new HttpResponseMessage();

                if (apiAC == null)
                {
                    res.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                    res.ReasonPhrase = "Response failed. Atleast one of Access-Control-Allow-Origin, Access-Control-Allow-Methods, Access-Control-Allow-Headers is missing from the response.";
                }
                else {
                    // force set origin as "null" if running locally only
                    if (runLocalOnly)
                    {
                        apiAC.accessControlAllowOrigin = "null";
                    }

                    ApplyCORSLogicAndGenerateResponse(ref res);
                }

                var tcs = new TaskCompletionSource<HttpResponseMessage>();
                tcs.SetResult(res);
                return tcs.Task;

            }

            return base.SendAsync(request, cancellationToken);
        }

        private void ApplyCORSLogicAndGenerateResponse(ref HttpResponseMessage res)
        {
            bool failRequest = false;
            if (hasParseErrors)
            {
                res.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                res.ReasonPhrase = "Response failed. Error occurred while parsing resource on server.";
            }
            else if (hasDuplicateRules)
            {
                res.StatusCode = System.Net.HttpStatusCode.Conflict;
                res.ReasonPhrase = "Response failed. Duplicate access control rules found on server.";
            }
            else
            {
                try
                {
                    res.StatusCode = System.Net.HttpStatusCode.OK;
                    bool hasOriginMatch = false;

                    // adding allow-origin header
                    if (!string.IsNullOrEmpty(apiAC.accessControlAllowOrigin))
                    {
                        if (reqOrigin != null)
                        {
                            if (reqOrigin.Equals(apiAC.accessControlAllowOrigin, StringComparison.InvariantCultureIgnoreCase) || apiAC.accessControlAllowOrigin.Equals("*"))
                            {
                                res.Headers.Add(AccessControlHeader.ALLOW_ORIGIN, reqOrigin);
                                hasOriginMatch = true;
                            }
                        }
                        else
                        {
                            if (apiAC.accessControlAllowOrigin.Equals("*") || apiAC.accessControlAllowOrigin.Equals("null"))
                            {
                                res.Headers.Add(AccessControlHeader.ALLOW_ORIGIN, apiAC.accessControlAllowOrigin);
                                hasOriginMatch = true;
                            }
                        }
                    }

                    if (hasOriginMatch)
                    {
                        // adding allow-methods header
                        string[] methods = null;
                        if (!string.IsNullOrEmpty(apiAC.accessControlAllowMethods))
                        {
                            if (reqMethod != null)
                            {
                                methods = apiAC.accessControlAllowMethods.Split(',');
                                List<string> methods_t = new List<string>();
                                methods_t.AddRange(methods.Where(p => !simpleMethods.Contains(p)).ToList<string>());
                                apiAC.accessControlAllowMethods = string.Join(",", methods_t);

                                if (methods_t.Any(p => p.Equals(reqMethod, StringComparison.InvariantCultureIgnoreCase)) || apiAC.accessControlAllowMethods.Equals("*"))
                                {
                                    res.Headers.Add(AccessControlHeader.ALLOW_METHODS, reqMethod);
                                }
                                else
                                {
                                    if (!simpleMethods.Contains(reqMethod.ToUpper()))
                                    {
                                        failRequest = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (apiAC.accessControlAllowMethods == "")
                            {
                                failRequest = true;
                            }
                        }

                        // adding allow-headers header
                        // check for simple headers

                        string[] headers = null;
                        if (!string.IsNullOrEmpty(apiAC.accessControlAllowHeaders))
                        {
                            if (reqHeaders != null)
                            {
                                headers = apiAC.accessControlAllowHeaders.ToLower().Split(',');
                                List<string> headers_t = new List<string>();
                                headers_t.AddRange(headers.Where(p => !simpleHeaders.Contains(p.ToLower())).ToList<string>());
                                apiAC.accessControlAllowHeaders = string.Join(",", headers_t);

                                List<string> reqHeaders_t = new List<string>();
                                reqHeaders_t.AddRange(reqHeaders.Where(p => !simpleHeaders.Contains(p.ToLower())).ToList<string>());
                                string allReqHeaders = string.Join(",", reqHeaders_t);


                                if (reqHeaders_t.All(p => headers_t.Contains(p)) || apiAC.accessControlAllowHeaders.Equals("*"))
                                {
                                    if (!string.IsNullOrEmpty(allReqHeaders))
                                    {
                                        res.Headers.Add(AccessControlHeader.ALLOW_HEADERS, allReqHeaders);
                                    }
                                }
                                else
                                {
                                    failRequest = true;
                                }
                            }
                        }
                        else
                        {
                            if (apiAC.accessControlAllowHeaders == "")
                            {
                                failRequest = true;
                            }
                        }



                        if (apiAC.accessControlAllowCredentials.HasValue)
                        {
                            res.Headers.Add(AccessControlHeader.ALLOW_CREDENTIALS, apiAC.accessControlAllowCredentials.ToString().ToLower());
                        }

                        if (apiAC.accessControlMaxAge.HasValue)
                        {
                            res.Headers.Add(AccessControlHeader.MAX_AGE, apiAC.accessControlMaxAge.ToString());
                        }

                        // removals
                        if (failRequest)
                        {
                            SetBadResponse(ref res);
                        }
                    }
                    else
                    {
                        SetBadResponse(ref res);
                    }
                }
                catch (Exception resException)
                {

                    SetInternalError(ref res);
                }
            }
        }

        private void SetBadResponse(ref HttpResponseMessage res)
        {
            RemoveCORSHeaders(ref res);

            res.StatusCode = System.Net.HttpStatusCode.BadRequest;
        }

        private void SetInternalError(ref HttpResponseMessage res)
        {
            RemoveCORSHeaders(ref res);
            res.StatusCode = System.Net.HttpStatusCode.InternalServerError;
        }

        private void RemoveCORSHeaders(ref HttpResponseMessage res)
        {
            res.Headers.Remove(AccessControlHeader.ALLOW_ORIGIN);
            res.Headers.Remove(AccessControlHeader.ALLOW_METHODS);
            res.Headers.Remove(AccessControlHeader.ALLOW_HEADERS);
            res.Headers.Remove(AccessControlHeader.ALLOW_CREDENTIALS);
            res.Headers.Remove(AccessControlHeader.MAX_AGE);
            res.Headers.Remove(AccessControlHeader.EXPOSE_HEADERS);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}