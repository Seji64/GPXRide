using System;
using System.Text.Json.Serialization;

namespace GPXRide.Classes
{
    public class Stop
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; } = DateTime.Now;

        [JsonPropertyName("isMyPosition")]
        public bool IsMyPosition { get; set; }

        [JsonPropertyName("latitude")]
        public decimal Latitude { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("longitude")]
        public decimal Longitude { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }
    }
}