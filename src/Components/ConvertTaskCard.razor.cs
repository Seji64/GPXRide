using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using GPXRide.Enums;
using GPXRide.Helpers;
using GPXRide.Interfaces;
using GPXRide.Models;
using GPXRide.Models.MvRide;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog;

namespace GPXRide.Components;

public partial class ConvertTaskCard : MudComponentBase
{
    [CascadingParameter]
    public required IConvertTask ConvertTask { get; set; }
    [Parameter] public required Action<IConvertTask> CloseConvertTask { get; set; }

    private bool _isWebShareAPiSupported;

    protected override async Task OnInitializedAsync()
    {
        _isWebShareAPiSupported = await WebShareService.IsSupportedAsync();
    }

    private bool IsOriginalFileNullOrEmpty()
    {
        return ConvertTask.SourceType switch
        {
            SourceType.MvTrip => ((TripToGpxConvertTask)ConvertTask).OriginalMvTripFile == null,
            SourceType.Gpx => ((GpxToItineraryConvertTask)ConvertTask).OriginalGpxFile == null,
            _ => throw new InvalidCastException("Unknown type")
        };
    }

    private string? GetTitle()
    {
        return ConvertTask.SourceType switch
        {
            SourceType.MvTrip => ((TripToGpxConvertTask)ConvertTask).OriginalMvTripFile?.Title,
            SourceType.Gpx => ((GpxToItineraryConvertTask)ConvertTask).OriginalGpxFile?.metadata.name,
            _ => throw new InvalidCastException("Unknown type")
        };
    }
    private string GetButtonLabel(IConvertTask convertTask)
    {
        return ConvertTask.SourceType switch
        {
            SourceType.MvTrip => "Convert to Gpx",
            SourceType.Gpx => "Convert to Itinerary",
            _ => throw new InvalidCastException("Unknown type")
        };
    }
    private async Task ConvertFileAsync(IConvertTask convertTask)
    {
        switch (ConvertTask.SourceType)
        {
            case SourceType.MvTrip:
                await ConvertToGpxAsync((TripToGpxConvertTask)ConvertTask);
                break;
            case SourceType.Gpx:
                await ConvertToItineraryAsync((GpxToItineraryConvertTask)ConvertTask);
                break;
            default:
                throw new InvalidCastException("Unknown type");
        }
    }
    private static async Task ConvertToGpxAsync(TripToGpxConvertTask convertTask)
    {
        convertTask.State = ConvertState.Working;
        
        try
        {
            if (convertTask.OriginalMvTripFile?.GpsRows is null)
            {
                throw new NullReferenceException("OriginalMvTripFile is null");
            }
            
            GpxFile gpxFile = new()
            {
                wpt = convertTask.OriginalMvTripFile.GpsRows.AsParallel().AsOrdered().Select(x => new GpxWaypoint() { lat = (decimal)x.Latitude, lon = (decimal)x.Longitude, name = x.Index.ToString()}).ToArray()
            };

            convertTask.ConvertedGpxFile = gpxFile;
            convertTask.State = ConvertState.Completed;

        }
        catch (Exception ex)
        {
            Log.Error("{ErrorMessage}", ex.Message);
            convertTask.State = ConvertState.Error;
        }

        await Task.CompletedTask;
        
    }
    private static async Task ConvertToItineraryAsync(GpxToItineraryConvertTask convertTask)
    {
        convertTask.State = ConvertState.Working;
        
        try
        {
            if (convertTask.ItineryGpxConvertOptions is null ||
                convertTask.OriginalGpxFile is null)
            {
                throw new NullReferenceException("Itinerary gpx convert task is null");
            }

            Itinerary itinerary = new()
            {
                Id = Guid.NewGuid().ToString(),
                Length = 1,
                Duration = 1,
                VehicleClass = "bike",
                Name = convertTask.ItineryGpxConvertOptions.RouteName,
                Preferences =
                {
                    DirtRoads = convertTask.ItineryGpxConvertOptions.DirtyRoads,
                    Trains = convertTask.ItineryGpxConvertOptions.Trains,
                    Motorway = convertTask.ItineryGpxConvertOptions.Motorway,
                    Tunnel = convertTask.ItineryGpxConvertOptions.Tunnel,
                    TollFree = convertTask.ItineryGpxConvertOptions.TollFree,
                    Ferry = convertTask.ItineryGpxConvertOptions.Ferry
                }
            };
            
            GpxSourceType? type = convertTask.GpxSourceType;
            
            GpxWaypoint[] waypoints = type switch
            {
                GpxSourceType.Route => convertTask.OriginalGpxFile.rte[0].rtept,
                GpxSourceType.Track => convertTask.OriginalGpxFile.trk[0].trkseg[0].trkpt,
                GpxSourceType.Waypoints => convertTask.OriginalGpxFile.wpt,
                _ => throw new InvalidOperationException()
            };

            if (waypoints is null || waypoints.Length == 0)
            {
                throw new InvalidOperationException("failed to get any Waypoints");
            }

            waypoints.AsParallel().AsOrdered().ForAll(point =>
            {
                Stop stop = new()
                {
                    Latitude = point.lat,
                    Longitude = point.lon,
                    Address = point.name,
                    City = point.name

                };

                if (convertTask.ItineryGpxConvertOptions.FirstWaypointAsMyPosition &&
                    itinerary.Stops.Count == 0)
                {
                    stop.IsMyPosition = true;
                }

                itinerary.Stops.Add(stop);
            });

            convertTask.ConvertedItinerary = itinerary;
            convertTask.State = ConvertState.Completed;

        }
        catch (Exception ex)
        {
            Log.Error("{ErrorMessage}", ex.Message);
            convertTask.State = ConvertState.Error;
        }

        await Task.CompletedTask;
    }
    private static async Task<MemoryStream> GetPayloadAsync(Itinerary itinerary)
    {
        return await Task.FromResult(ItineraryHelper.SerializeAsBytes(itinerary));
    }

    private static async Task<MemoryStream> GetPayloadAsync(GpxFile gpxfile)
    {
        return await Task.FromResult(GpxHelper.SerializeAsBytes(gpxfile));
    }
    
    private async Task DownloadShareConvertedFileAsync(IConvertTask convertTask, bool shareFile = false)
    {
        string filename = string.Empty;
        MemoryStream? payload = null;
        
        try
        {
            if (ConvertTask.GetType() != typeof(TripToGpxConvertTask) &&
                ConvertTask.GetType() != typeof(GpxToItineraryConvertTask))
            {
                throw new InvalidCastException("Unknown type");
            }
            
            switch (ConvertTask.SourceType)
            {
                case SourceType.MvTrip:
                {
                    if (convertTask is TripToGpxConvertTask { ConvertedGpxFile: not null, OriginalMvTripFile: not null } task){
                        payload = await GetPayloadAsync(task.ConvertedGpxFile);

                        filename = $"{task.OriginalMvTripFile.Title}.gpx";
                    
                    }

                    break;
                }
                case SourceType.Gpx:
                {
                    if (convertTask is GpxToItineraryConvertTask { ConvertedItinerary: not null, ItineryGpxConvertOptions: not null } task){
                        payload = await GetPayloadAsync(task.ConvertedItinerary);

                        filename = $"{task.ItineryGpxConvertOptions.RouteName}.mvitinerary";
                        
                        await BlazorDownloadFileService.DownloadFile(filename, payload,"application/octet-stream");
                    }

                    break;
                }
                
                default:
                    throw new InvalidCastException("Unknown type");
            }

            if (!string.IsNullOrWhiteSpace(filename) && payload is not null)
            {
                if (shareFile)
                {
                    await WebShareService.ShareAsync(filename, filename, $"data:text/plain;base64,{Convert.ToBase64String(payload.ToArray())}");
                }
                else
                {
                    await BlazorDownloadFileService.DownloadFile(filename, payload,"application/octet-stream");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("{ErrorMessage}", ex.Message);
            Snackbar.Add("Sorry, something went wrong!",Severity.Error);
        }
    }
}