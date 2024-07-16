using GPXRide.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace GPXRide.Components;

public partial class TripToGpxCardContent : MudComponentBase
{
    [CascadingParameter]
    public required TripToGpxConvertTask Task { get; set; }
}