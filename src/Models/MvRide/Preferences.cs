using System.Text.Json.Serialization;

namespace GPXRide.Models.MvRide
{
    public class Preferences
    {
        [JsonPropertyName("motorway")]
        public bool Motorway { get; set; }

        [JsonPropertyName("tunnel")]
        public bool Tunnel { get; set; }

        [JsonPropertyName("dirtRoads")]
        public bool DirtRoads { get; set; }

        [JsonPropertyName("trains")]
        public bool Trains { get; set; }

        [JsonPropertyName("tollFree")]
        public bool TollFree { get; set; }

        [JsonPropertyName("ferry")]
        public bool Ferry { get; set; }
    }
}