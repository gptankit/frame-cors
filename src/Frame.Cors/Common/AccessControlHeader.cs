using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frame.Cors.Common
{
    internal class AccessControlHeader
    {
        public const string ALLOW_ORIGIN = "Access-Control-Allow-Origin";
        public const string ALLOW_METHODS = "Access-Control-Allow-Methods";
        public const string ALLOW_HEADERS = "Access-Control-Allow-Headers";
        public const string ALLOW_CREDENTIALS = "Access-Control-Allow-Credentials";
        public const string MAX_AGE = "Access-Control-Max-Age";
        public const string EXPOSE_HEADERS = "Access-Control-Expose-Headers";
        
        public const string ORIGIN = "Origin";
        public const string REQUEST_METHOD = "Access-Control-Request-Method";
        public const string REQUEST_HEADERS = "Access-Control-Request-Headers";        
    }
}
