using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using GPXRide.Enums;

namespace GPXRide.Classes
{
    public class ConvertTask
    {
        public int? Id { get; init; }
        public GpxFile OriginalGpxFile { get; set; }
        public string FileName { get; init; }
        public ItineraryFile ConvertedItineraryFile { get; set; }
        public ConvertState State { get; set; } = ConvertState.None;
        public ConvertOptions ConvertOptions { get; set; } = new();
        public SourceType SourceType { get; set; }
    }
}