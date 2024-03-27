using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using GPXRide.Enums;
using MudBlazor;

namespace GPXRide.Classes
{
    public class ConvertTask
    {
        public int? Id { get; set; } = null;
        public GpxFile OriginalGpxFile { get; set; } = null;
        public string FileName { get; set; }
        public ItineraryFile ConvertedItineraryFile { get; set; } = null;
        public ConvertState State { get; set; } = ConvertState.None;
        public ConvertOptions ConvertOptions { get; set; } = new();
        public MudChip SelectedSourceChip { get; set; }
        public SourceType SourceType { get; set; }
    }
}