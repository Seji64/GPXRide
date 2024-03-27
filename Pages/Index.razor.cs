using BlazorDownloadFile;
using GPXRide.Classes;
using GPXRide.Enums;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Serilog;
using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GPXRide.Pages
{
    public partial class Index
    {
        [Inject] IBlazorDownloadFileService BlazorDownloadFileService { get; set; }
        readonly List<ConvertTask> ConvertTasks = new();
        private bool _webShareSupported = false;

        protected override async Task OnInitializedAsync()
        {
            Snackbar.Clear();
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
            _webShareSupported = await IsWebShareSupportedAsync();
            await base.OnInitializedAsync();
        }
        private async Task ConvertToItinerary(ConvertTask convertTask)
        {

            convertTask.State = ConvertState.Working;
            convertTask.ConvertedItineraryFile = await Task.Run(() =>
            {

                try
                {
                    ItineraryFile m_ItineraryFile = new()
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
                    SourceType type = convertTask.SelectedSourceChip is null ? (SourceType)Enum.Parse(typeof(SourceType), GetSourceTypes(convertTask.OriginalGpxFile).FirstOrDefault()) : (SourceType)Enum.Parse(typeof(SourceType), convertTask.SelectedSourceChip.Text);
                    Log.Debug("Selected Route Type:{Type}", type.ToString());
                    GpxWaypoint[] waypoints = type switch
                    {
                        SourceType.Route => convertTask.OriginalGpxFile.rte[0].rtept,
                        SourceType.Track => convertTask.OriginalGpxFile.trk[0].trkseg[0].trkpt,
                        SourceType.Waypoints => convertTask.OriginalGpxFile.wpt,
                        _ => throw new InvalidOperationException()
                    };

                    if (waypoints is null || !waypoints.Any())
                    {
                        throw new ArgumentNullException("failed to get any Waypoints");
                    }

                    foreach (var point in waypoints)
                    {
                        Log.Debug("Waypoint: Latitude: {Lat} , Longitude: {Lon}", point.lat, point.lon);

                        var stop = new Stop()
                        {
                            Latitude = point.lat,
                            Longitude = point.lon,
                            Address = point.name,
                            City = point.name

                        };

                        if (convertTask.ConvertOptions.FirstWaypointAsMyPosition && !m_ItineraryFile.Itinerary.Stops.Any())
                        {
                            stop.IsMyPosition = true;
                        }

                        m_ItineraryFile.Itinerary.Stops.Add(stop);
                    }

                    return m_ItineraryFile;

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
            long MAXALLOWEDSIZE = 2097152;
            bool errorOnUpload = false;
            object _lock = new();

            files.AsParallel().AsOrdered().ForAll(async fileentry =>
            {
                int? _Id = 0;

                lock (_lock)
                {
                    _Id = ConvertTasks.Any() ? (ConvertTasks[^1].Id + 1) : 0;
                    ConvertTasks.Add(new ConvertTask()
                    {
                        Id = _Id,
                        OriginalGpxFile = null,
                        FileName = System.IO.Path.GetFileNameWithoutExtension(fileentry.Name)
                    });
                }

                StateHasChanged();

                try
                {
                    Log.Debug("Deserialize GPX File...");
                    var gpxFile = await Gpx11SerializerAsync.DeserializeAsync(fileentry.OpenReadStream(MAXALLOWEDSIZE));

                    if (gpxFile != null)
                    {
                        Log.Debug("GPX File deserialized!");
                        Log.Debug("Attaching GpxFile to ConvertTask with Id {Id}", _Id);

                        ConvertTasks.Single(x => x.Id == _Id).OriginalGpxFile = gpxFile;
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
                    Log.Debug("Removing Convert Task with Id {Id}", _Id);
                    ConvertTasks.RemoveAll(x => x.Id == _Id);
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
                if (ConvertTasks.Any())
                {
                    Snackbar.Add("Some files failed to upload", Severity.Warning);
                }
                else
                {
                    Snackbar.Add("Failed to upload files", Severity.Warning);
                }
            }
        }

        private List<string> GetSourceTypes(GpxFile gpxFile)
        {
            List<string> routeTypes = new();

            if (gpxFile.wpt != null && gpxFile.wpt.Any())
            {
                routeTypes.Add(SourceType.Waypoints.ToString());
            }
            if (gpxFile.rte != null && gpxFile.rte.Any())
            {
                routeTypes.Add(SourceType.Route.ToString());
            }
            if (gpxFile.trk != null && gpxFile.trk.Any())
            {
                routeTypes.Add(SourceType.Track.ToString());
            }

            return routeTypes;
        }

        private void DisposeConvertTask(ConvertTask convertTask)
        {
            ConvertTasks.Remove(convertTask);
        }

        private async Task<bool> IsWebShareSupportedAsync()
        {
            return (await JS.InvokeAsync<bool>("IsShareSupported"));
        }

        private async Task ShareItineraryFile(ConvertTask task)
        {
            string payload = Convert.ToBase64String(task.ConvertedItineraryFile.ToZipArchiveStream().ToArray());

            if (await JS.InvokeAsync<bool>("CanShareThisFile", $"{task.ConvertOptions.RouteName}.mvitinerary", "application/octet-stream", $"data:text/plain;base64,{payload}"))
            {
                try
                {
                    await JS.InvokeVoidAsync("ShareFile", "Share Itinerary File", task.ConvertOptions.RouteName, $"{task.ConvertOptions.RouteName}.mvitinerary", "application/octet-stream", $"data:text/plain;base64,{payload}");
                }
                catch (JSException ex)
                {
                    if (ex.Message != null && ex.Message.Contains("Permission denied"))
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
