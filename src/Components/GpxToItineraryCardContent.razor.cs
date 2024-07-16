using GPXRide.Enums;
using GPXRide.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog;

namespace GPXRide.Components;

public partial class GpxToItineraryCardContent : MudComponentBase
{
    [CascadingParameter]
    public required GpxToItineraryConvertTask Task { get; set; }

    public GpxSourceType SelectedGpxSourceType
    {
        get => _selectedGpxSourceType;
        set
        {
            _selectedGpxSourceType = value;
            Task.GpxSourceType = _selectedGpxSourceType;
        }
    }

    private GpxSourceType _selectedGpxSourceType;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            SelectedGpxSourceType = GetSourceTypes().FirstOrDefault();
        }
    }
    private List<GpxSourceType> GetSourceTypes()
    {
        List<GpxSourceType> routeTypes = [];

        if (Task.OriginalGpxFile?.wpt != null && Task.OriginalGpxFile.wpt.Length != 0)
        {
            routeTypes.Add(GpxSourceType.Waypoints);
        }
        if (Task.OriginalGpxFile?.rte != null && Task.OriginalGpxFile.rte.Length != 0)
        {
            routeTypes.Add(GpxSourceType.Route);
        }
        if (Task.OriginalGpxFile?.trk != null && Task.OriginalGpxFile.trk.Length != 0)
        {
            routeTypes.Add(GpxSourceType.Track);
        }

        return routeTypes;
    }
}