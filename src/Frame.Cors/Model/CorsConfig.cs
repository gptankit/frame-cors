using Newtonsoft.Json;

namespace Frame.Cors.Model
{
    [JsonObject]
    internal class CorsConfig
    {
        [JsonProperty("controllers")]
        public string Controllers { get; set; }

        [JsonProperty("allow-origin")]
        public string AllowOrigin { get; set; }

        [JsonProperty("allow-methods")]
        public string AllowMethods { get; set; }

        [JsonProperty("allow-headers")]
        public string AllowHeaders { get; set; }

        [JsonProperty("allow-credentials")]
        public bool? AllowCredentials { get; set; }

        [JsonProperty("max-age")]
        public long? MaxAge { get; set; }

        [JsonProperty("expose-headers")]
        public string ExposeHeaders { get; set; }        
    }
}