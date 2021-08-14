using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GPXRide.Classes
{
    public class Itinerary
    {
        [JsonProperty("length")]
        public int Length {get; set; }

        [JsonProperty("preferences")]
        public Preferences Preferences {get; set; } = new();

        [JsonProperty("id")]
        public string Id {get; set; }

        [JsonProperty("creationDate")]
        public DateTime CreationDate {get; set; } = DateTime.Now;

        [JsonProperty("nations")]
        public List<object> Nations {get; set; } = new();

        [JsonProperty("duration")]
        public int Duration {get; set; }

        [JsonProperty("stops")]
        public List<Stop> Stops {get; set; } = new();

        [JsonProperty("name")]
        public string Name {get; set; }
        
        [JsonProperty("vehicleClass")]
        public string VehicleClass {get; set; }
    }
}