using Newtonsoft.Json;

namespace GPXRide.Classes
{
    public class Preferences
    {
        [JsonProperty("motorway")]
        public bool Motorway {get; set;}

        [JsonProperty("tunnel")]
        public bool Tunnel {get; set;}

        [JsonProperty("dirtRoads")]
        public bool DirtRoads {get; set;}

        [JsonProperty("trains")]
        public bool Trains {get; set;}

        [JsonProperty("tollFree")]
        public bool TollFree {get; set;}

        [JsonProperty("ferry")]
        public bool Ferry {get; set;}
    }
}