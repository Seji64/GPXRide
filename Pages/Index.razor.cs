using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlazorDownloadFile;
using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using GPXRide.Classes;
using GPXRide.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using Serilog;

namespace GPXRide.Pages
{
    public partial class Index
    {
        [Inject] IBlazorDownloadFileService BlazorDownloadFileService { get; set; }
        readonly List<ConvertTask> _convertTasks = [];
        private bool _webShareSupported;

        protected override async Task OnInitializedAsync()
        {
            Snackbar.Clear();
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
            _webShareSupported = await IsWebShareSupportedAsync();
            await base.OnInitializedAsync();
        }
        private static async Task ConvertToItinerary(ConvertTask convertTask)
        {

            convertTask.State = ConvertState.Working;
            convertTask.ConvertedItineraryFile = await Task.Run(() =>
            {

                try
                {
                    ItineraryFile mItineraryFile = new()
                    {
                        Itinerary =
                    {
                        Id = Guid.NewGuid().ToString(),
                        Length = 1,
                        Duration = 1,
                        VehicleClass = "bike",
                        Name = convertTask.ConvertOptions.RouteName,

                        Preferences =
                        {
                            DirtRoads = convertTask.ConvertOptions.DirtyRoads,
                            Trains = convertTask.ConvertOptions.Trains,
                            Motorway = convertTask.ConvertOptions.Motorway,
                            Tunnel = convertTask.ConvertOptions.Tunnel,
                            TollFree = convertTask.ConvertOptions.TollFree,
                            Ferry = convertTask.ConvertOptions.Ferry
                        }
                    }
                    };
                    SourceType type = convertTask.SourceType;
                    Log.Debug("Selected Route Type:{Type}", type.ToString());
                    GpxWaypoint[] waypoints = type switch
                    {
                        SourceType.Route => convertTask.OriginalGpxFile.rte[0].rtept,
                        SourceType.Track => convertTask.OriginalGpxFile.trk[0].trkseg[0].trkpt,
                        SourceType.Waypoints => convertTask.OriginalGpxFile.wpt,
                        _ => throw new InvalidOperationException()
                    };

                    if (waypoints is null || waypoints.Length == 0)
                    {
                        throw new InvalidOperationException("failed to get any Waypoints");
                    }

                    foreach (GpxWaypoint point in waypoints)
                    {
                        Log.Debug("Waypoint: Latitude: {Lat} , Longitude: {Lon}", point.lat, point.lon);

                        Stop stop = new Stop
                        {
                            Latitude = point.lat,
                            Longitude = point.lon,
                            Address = point.name,
                            City = point.name

                        };

                        if (convertTask.ConvertOptions.FirstWaypointAsMyPosition && mItineraryFile.Itinerary.Stops.Count == 0)
                        {
                            stop.IsMyPosition = true;
                        }

                        mItineraryFile.Itinerary.Stops.Add(stop);
                    }

                    return mItineraryFile;

                }
                catch (Exception ex)
                {
                    Log.Error("{ErrorMessage}", ex.Message);
                    return null;
                }
            });

            await Task.Delay(500);
            convertTask.State = convertTask.ConvertedItineraryFile != null ? ConvertState.Completed : ConvertState.Error;
        }

        private void UploadFiles(IReadOnlyList<IBrowserFile> files)
        {
            long maxallowedsize = 2097152;
            bool errorOnUpload = false;
            object @lock = new();

            files.AsParallel().AsOrdered().ForAll(async fileentry =>
            {
                int? id = 0;

                lock (@lock)
                {
                    id = _convertTasks.Count != 0 ? (_convertTasks[^1].Id + 1) : 0;
                    _convertTasks.Add(new ConvertTask
                    {
                        Id = id,
                        OriginalGpxFile = null,
                        FileName = Path.GetFileNameWithoutExtension(fileentry.Name)
                    });
                }

                StateHasChanged();

                try
                {
                    Log.Debug("Deserialize GPX File...");
                    GpxFile gpxFile = await Gpx11SerializerAsync.DeserializeAsync(fileentry.OpenReadStream(maxallowedsize));

                    if (gpxFile != null)
                    {
                        Log.Debug("GPX File deserialized!");
                        Log.Debug("Attaching GpxFile to ConvertTask with Id {Id}", id);

                        _convertTasks.Single(x => x.Id == id).OriginalGpxFile = gpxFile;
                        Log.Debug(("Done"));
                    }
                    else
                    {
                        throw new InvalidOperationException($"Failed to parse Gpx File {fileentry.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("{ErrorMessage}", ex.Message);
                    errorOnUpload = true;
                    Log.Debug("Removing Convert Task with Id {Id}", id);
                    _convertTasks.RemoveAll(x => x.Id == id);
                }
                finally
                {
                    StateHasChanged();
                }
            });

            if (!errorOnUpload)
            {
                Snackbar.Add("All files successfully uploaded!", Severity.Success);
            }
            else
            {
                Snackbar.Add(_convertTasks.Count != 0 ? "Some files failed to upload" : "Failed to upload files",
                    Severity.Warning);
            }
        }

        private static List<string> GetSourceTypes(GpxFile gpxFile)
        {
            List<string> routeTypes = [];

            if (gpxFile.wpt != null && gpxFile.wpt.Length != 0)
            {
                routeTypes.Add(SourceType.Waypoints.ToString());
            }
            if (gpxFile.rte != null && gpxFile.rte.Length != 0)
            {
                routeTypes.Add(SourceType.Route.ToString());
            }
            if (gpxFile.trk != null && gpxFile.trk.Length != 0)
            {
                routeTypes.Add(SourceType.Track.ToString());
            }

            return routeTypes;
        }

        private void DisposeConvertTask(ConvertTask convertTask)
        {
            _convertTasks.Remove(convertTask);
        }

        private async Task<bool> IsWebShareSupportedAsync()
        {
            return (await Js.InvokeAsync<bool>("IsShareSupported"));
        }

        private async Task ShareItineraryFile(ConvertTask task)
        {
            string payload = Convert.ToBase64String(task.ConvertedItineraryFile.ToZipArchiveStream().ToArray());

            if (await Js.InvokeAsync<bool>("CanShareThisFile", $"{task.ConvertOptions.RouteName}.mvitinerary", "application/octet-stream", $"data:text/plain;base64,{payload}"))
            {
                try
                {
                    await Js.InvokeVoidAsync("ShareFile", "Share Itinerary File", task.ConvertOptions.RouteName, $"{task.ConvertOptions.RouteName}.mvitinerary", "application/octet-stream", $"data:text/plain;base64,{payload}");
                }
                catch (JSException ex)
                {
                    if (ex.Message.Contains("Permission denied"))
                    {
                        Snackbar.Add("Sorry! - Your Browser does not support sharing of this type of file!", Severity.Error);
                    }
                }
            }
            else
            {
                Snackbar.Add("Sorry! - Your Browser does not support sharing of this type of file!", Severity.Error);
            }
        }
    }
}
