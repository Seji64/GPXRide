using System.Text.Json.Serialization;

namespace GPXRide.Classes;
public class GpsRow
{
    [JsonPropertyName("rollAngle")]
    public double RollAngle { get; set; }
    [JsonPropertyName("gear")]
    public int Gear { get; set; }
    [JsonPropertyName("index")]
    public int Index { get; set; }
    [JsonPropertyName("engineSpeed")]
    public int EngineSpeed { get; set; }
    [JsonPropertyName("tripId")]
    public string TripId { get; set; }
    [JsonPropertyName("throttle")]
    public int Throttle { get; set; }
    [JsonPropertyName("fix")]
    public int Fix { get; set; }
    [JsonPropertyName("altitude")]
    public double Altitude { get; set; }
    [JsonPropertyName("time")]
    public string Time { get; set; }
    [JsonPropertyName("airTemperature")]
    public int AirTemperature { get; set; }
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }
    [JsonPropertyName("distanceInMeterFromStart")]
    public int DistanceInMeterFromStart { get; set; }
    [JsonPropertyName("rpm")]
    public int Rpm { get; set; }
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

