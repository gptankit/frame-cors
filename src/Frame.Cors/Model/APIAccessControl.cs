namespace Frame.Cors.Model
{
    internal class APIAccessControl
    {
        public string accessControlAllowOrigin { get; set; }
        public string accessControlAllowMethods { get; set; }
        public string accessControlAllowHeaders { get; set; }
        public bool? accessControlAllowCredentials { get; set; }
        public long? accessControlMaxAge { get; set; }
        public string accessControlExposeHeaders { get; set; }
    }
}