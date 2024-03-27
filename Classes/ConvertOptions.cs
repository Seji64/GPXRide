namespace GPXRide.Classes
{
    public class ConvertOptions
    {
        public bool Motorway { get; set; } = true;
        public bool TollFree { get; set; } = false;
        public bool DirtyRoads { get; set; } = false;
        public bool Tunnel { get; set; } = true;
        public bool Trains { get; set; } = true;
        public bool Ferry { get; set; } = true;
        public string RouteName { get; set; }
        public bool FirstWaypointAsMyPosition { get; set; } = false;
    }
}