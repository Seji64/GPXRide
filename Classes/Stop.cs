using System;
using Newtonsoft.Json;

namespace GPXRide.Classes
{
    public class Stop
    {
        [JsonProperty("date")]
        public DateTime Date {get; set; } = DateTime.Now;

        [JsonProperty("isMyPosition")]
        public bool IsMyPosition {get; set; }

        [JsonProperty("latitude")]
        public decimal Latitude {get; set; }

        [JsonProperty("city")]
        public string City {get; set; }

        [JsonProperty("longitude")]
        public decimal Longitude {get; set; }

        [JsonProperty("address")]
        public string Address {get; set; }
    }
}