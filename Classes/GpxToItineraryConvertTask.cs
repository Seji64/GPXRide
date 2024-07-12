using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using GPXRide.Enums;
using GPXRide.Interfaces;
using Microsoft.AspNetCore.Components.Forms;

namespace GPXRide.Classes
{
    public class GpxToItineraryConvertTask : IConvertTask
    {
        public int? Id { get; init; }
        public IBrowserFile InputFile { get; init; }
        public FileSourceType FileSourceType { get; init; }
        public GpxFile OriginalGpxFile { get; set; }
        public string FileName { get; init; }
        public ItineraryFile ConvertedItineraryFile { get; set; }
        public ConvertState State { get; set; } = ConvertState.None;
        public ConvertOptions ConvertOptions { get; set; } = new();
        public SourceType SourceType { get; set; }
    }
}