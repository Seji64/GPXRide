namespace GPXRide.Classes
{
    public class ConvertOptions
    {
        public bool Motorway { get; set; } = true;
        public bool TollFree { get; set; }
        public bool DirtyRoads { get; set; }
        public bool Tunnel { get; set; } = true;
        public bool Trains { get; set; } = true;
        public bool Ferry { get; set; } = true;
        public string RouteName { get; set; }
        public bool FirstWaypointAsMyPosition { get; set; } = false;
    }
}