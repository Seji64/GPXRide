using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GPXRide.Classes
{
    public class Itinerary
    {
        [JsonPropertyName("length")]
        public int Length { get; set; }

        [JsonPropertyName("preferences")]
        public Preferences Preferences { get; set; } = new();

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("creationDate")]
        public DateTime CreationDate { get; set; } = DateTime.Now;

        [JsonPropertyName("nations")]
        public List<object> Nations { get; set; } = [];

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("stops")]
        public List<Stop> Stops { get; set; } = [];

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("vehicleClass")]
        public string VehicleClass { get; set; }
    }
}