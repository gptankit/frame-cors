using Frame.Cors.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace Frame.Cors.Common
{
    internal class HttpManager
    {
        public static void GetParamsFrom(HttpRequestMessage req,
                                        out string reqOrigin,
                                        out string reqMethod,
                                        out ICollection<string> reqHeaders,
                                        out APIAccessControl apiAC,
                                        out bool isCrossOrigin,
                                        out bool hasParseErrors,
                                        out bool hasDuplicateRules,
                                        string ctrl)
        {
            // init

            hasParseErrors = false;
            hasDuplicateRules = false;
            apiAC = new APIAccessControl();

            // read cors config and check for errors
            CorsConfig accessControlSet = ReadCorsHeaders(ctrl, out hasParseErrors, out hasDuplicateRules);

            // referrer url
            //Uri uri = req.Headers.Referrer; only for chrome
            Uri uri = req.RequestUri;
            string destination = uri.Scheme + "://" + uri.Host;
            if (uri.Host.Equals("localhost"))
            {
                destination += ":" + uri.Port;
            }

            IEnumerable<string> origins = new List<string>();
            req.Headers.TryGetValues(AccessControlHeader.ORIGIN, out origins);
            string origin = (origins != null && origins.Count() > 0) ? (origins.FirstOrDefault()) : (null);

            isCrossOrigin = (origin == null || origin.Equals(destination, StringComparison.InvariantCultureIgnoreCase)) ? (false) : (true);
            reqOrigin = (isCrossOrigin) ? (origin) : (null);

            // method

            reqMethod = req.Method.Method;
            bool isPreflight = reqMethod.Equals("OPTIONS", StringComparison.InvariantCultureIgnoreCase) ? (true) : (false);
            // if options request, then fetch from the request headers                
            if (isPreflight)
            {
                reqMethod = req.Headers.GetValues(AccessControlHeader.REQUEST_METHOD).FirstOrDefault();
            }

            // headers

            reqHeaders = req.Headers.Select(p => p.Key).ToList<string>();
            if (isPreflight)
            {
                reqHeaders.Clear();
                if (req.Headers.Contains(AccessControlHeader.REQUEST_HEADERS))
                {
                    string corsHeaders = req.Headers.GetValues(AccessControlHeader.REQUEST_HEADERS).FirstOrDefault();
                    reqHeaders = corsHeaders.Replace(" ", "").Split(',');
                }
            }

            if (accessControlSet != null)
            {
                if (accessControlSet.AllowOrigin == null || accessControlSet.AllowMethods == null || accessControlSet.AllowHeaders == null)
                {
                    apiAC = null;
                }
                else
                {
                    apiAC.accessControlAllowOrigin = accessControlSet.AllowOrigin;
                    apiAC.accessControlAllowMethods = accessControlSet.AllowMethods;
                    apiAC.accessControlAllowHeaders = accessControlSet.AllowHeaders;
                    apiAC.accessControlAllowCredentials = accessControlSet.AllowCredentials;
                    apiAC.accessControlMaxAge = accessControlSet.MaxAge;
                    apiAC.accessControlExposeHeaders = accessControlSet.ExposeHeaders;
                }
            }
        }

        private static CorsConfig ReadCorsHeaders(string ctrl, out bool hasParseErrors, out bool hasDuplicateRules)
        {
            // init

            hasParseErrors = false;
            hasDuplicateRules = false;

            // reading the json file

            string cors_js_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"App_Data\frm-cors-config.json");
            FileStream fs = new FileStream(cors_js_path, FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(fs);
            string cors_spec = reader.ReadToEnd();
            fs.Close();

            CorsConfig accessControlSet = null;

            if (!String.IsNullOrEmpty(cors_spec))
            {
                // parse the json and extract objects

                JsonSerializerSettings jsonSettings = new JsonSerializerSettings();
                jsonSettings.Formatting = Formatting.None;

                CorsConfig[] corsArr = null;
                bool match = false;
                int occurrences = 0;

                try
                {
                    corsArr = (CorsConfig[])JsonConvert.DeserializeObject(cors_spec, typeof(CorsConfig[]), jsonSettings);

                    foreach (CorsConfig cs in corsArr)
                    {
                        Clean(cs);

                        if (!cs.Controllers.Equals("*"))
                        {
                            string[] controllers = cs.Controllers.Split(',');
                            if (controllers.Any(p => p.Equals(ctrl, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                match = true;
                                occurrences++;
                                accessControlSet = cs;

                                if (occurrences > 1)
                                {
                                    hasDuplicateRules = true;
                                    match = false;
                                    accessControlSet = null;
                                    break;
                                }
                            }
                        }
                    }

                    if (!match && occurrences == 0)
                    {
                        accessControlSet = corsArr.Where(p => p.Controllers.Equals("*")).FirstOrDefault();
                        if (accessControlSet != null)
                        {
                            match = true;
                        }
                    }
                }
                catch (JsonSerializationException jsonException)
                {
                    hasParseErrors = true;
                }

                // if the controller spec is found in the set
                if (match)
                {
                    return accessControlSet;
                }
            }

            return null;
        }

        private static void Clean(CorsConfig cs)
        {
            PropertyInfo[] properties = cs.GetType().GetProperties();
            foreach (var p in properties)
            {
                object valobj = p.GetValue(cs);
                if (valobj != null && valobj.GetType().Equals(typeof(string)))
                {
                    string val = (string)valobj;
                    if (!string.IsNullOrEmpty(val))
                    {
                        val = val.Replace(" ", "");
                        p.SetValue(cs, val);
                    }
                }
            }

        }
    }
}