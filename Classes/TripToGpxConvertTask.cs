using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using GPXRide.Enums;
using GPXRide.Interfaces;
using Microsoft.AspNetCore.Components.Forms;

namespace GPXRide.Classes
{
    public class TripToGpxConvertTask: IConvertTask
    {
        public int? Id { get; init; }
        public IBrowserFile InputFile { get; init; }
        public FileSourceType FileSourceType { get; init; }
        public Trip OriginalTripFile { get; set; }
        public string FileName { get; init; }
        public GpxFile ConvertedGpxFile { get; set; }
        public ConvertState State { get; set; } = ConvertState.None;
    }
}