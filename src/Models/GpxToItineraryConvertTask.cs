using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using GPXRide.Enums;
using GPXRide.Interfaces;
using GPXRide.Models.MvRide;
using Microsoft.AspNetCore.Components.Forms;

namespace GPXRide.Models
{
    public class GpxToItineraryConvertTask : IConvertTask
    {
        public required int Id { get; init; }
        public required IBrowserFile InputFile { get; init; }
        public required SourceType SourceType { get; init; }
        public required string FileName { get; init; }
        public ItineryGpxConvertOptions? ItineryGpxConvertOptions { get; set; }
        public ConvertState State { get; set; } = ConvertState.None;
        public GpxFile? OriginalGpxFile { get; set; }
        public Itinerary? ConvertedItinerary { get; set; }
        public GpxSourceType? GpxSourceType { get; set; }
    }
}