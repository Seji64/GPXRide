using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using GPXRide.Enums;
using GPXRide.Interfaces;
using GPXRide.Models.MvRide;
using Microsoft.AspNetCore.Components.Forms;

namespace GPXRide.Models
{
    public class TripToGpxConvertTask: IConvertTask
    {
        public required int Id { get; init; }
        public required Stream InputStream { get; init; }
        public required SourceType SourceType { get; init; }
        public required string FileName { get; init; }
        public ConvertState State { get; set; } = ConvertState.None;
        public Trip? OriginalMvTripFile { get; set; }
        public GpxFile? ConvertedGpxFile { get; set; }
    }
}