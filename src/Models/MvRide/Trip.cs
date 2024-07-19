using System.Text.Json.Serialization;

namespace GPXRide.Models.MvRide;

#nullable disable
public class Trip
{
    [JsonPropertyName(nameof(Id))]
    public string Id { get; set; }
    [JsonPropertyName("bikeModel")]
    public string BikeModel { get; set; }
    [JsonPropertyName("totalTimeInSeconds")]
    public int TotalTimeInSeconds { get; set; }
    [JsonPropertyName("downloadedSizeInBytes")]
    public int DownloadedSizeInBytes { get; set; }
    [JsonPropertyName("maxThrottle")]
    public int MaxThrottle { get; set; }
    [JsonPropertyName("isCompleted")]
    public bool IsCompleted { get; set; }
    [JsonPropertyName("startAddress")]
    public int StartAddress { get; set; }
    [JsonPropertyName("endDate")]
    public string EndDate { get; set; }
    [JsonPropertyName("skippedRowsForMissingData")]
    public int SkippedRowsForMissingData { get; set; }
    [JsonPropertyName("countries")]
    public string[] Countries { get; set; }
    [JsonPropertyName("maxRollAngle")]
    public double MaxRollAngle { get; set; }
    [JsonPropertyName("uom")]
    public string Uom { get; set; }
    [JsonPropertyName("title")]
    public string Title { get; set; }
    [JsonPropertyName("stopAddress")]
    public int StopAddress { get; set; }
    [JsonPropertyName("startDate")]
    public string StartDate { get; set; }
    [JsonPropertyName("maxSpeedKmh")]
    public double MaxSpeedKmh { get; set; }
    [JsonPropertyName("imageName")]
    public string ImageName { get; set; }
    [JsonPropertyName("totalDistanceInMeters")]
    public int TotalDistanceInMeters { get; set; }
    [JsonPropertyName("temporaryGpsUnitTripId")]
    public int TemporaryGpsUnitTripId { get; set; }
    [JsonPropertyName("skippedRowsForInvalidLocation")]
    public int SkippedRowsForInvalidLocation { get; set; }
    [JsonPropertyName("avgSpeedKmh")]
    public double AvgSpeedKmh { get; set; }
    [JsonPropertyName("language")]
    public string Language { get; set; }
    
    [JsonIgnore]
    public List<GpsRow> GpsRows { get; set; }

}