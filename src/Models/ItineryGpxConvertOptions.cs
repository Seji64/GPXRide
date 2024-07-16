namespace GPXRide.Models
{
    public class ItineryGpxConvertOptions
    {
        public bool Motorway { get; set; } = true;
        public bool TollFree { get; set; }
        public bool DirtyRoads { get; set; }
        public bool Tunnel { get; set; } = true;
        public bool Trains { get; set; } = true;
        public bool Ferry { get; set; } = true;
        public required string RouteName { get; set; }
        public bool FirstWaypointAsMyPosition { get; set; }
    }
}