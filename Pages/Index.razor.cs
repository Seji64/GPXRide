using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorDownloadFile;
using Geo.Gps.Serialization;
using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using GPXRide.Classes;
using GPXRide.Enums;
using GPXRide.Interfaces;
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
        readonly List<IConvertTask> _convertTasks = [];
        private bool _webShareSupported;

        protected override async Task OnInitializedAsync()
        {
            Snackbar.Clear();
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
            _webShareSupported = await IsWebShareSupportedAsync();
            await base.OnInitializedAsync();
        }

        private static async Task ConvertToGpx(TripToGpxConvertTask convertTask)
        {
            convertTask.State = ConvertState.Working;
            convertTask.ConvertedGpxFile = await Task.Run(() =>
            {
                try
                {
                    GpxFile gpxFile = new GpxFile
                    {
                        wpt = convertTask.OriginalTripFile.GpsRows.AsParallel().AsOrdered().Select(x => new GpxWaypoint() { lat = (decimal)x.Latitude, lon = (decimal)x.Longitude, name = x.Index.ToString()}).ToArray()
                    };

                    return gpxFile;
                }
                catch (Exception ex)
                {
                    Log.Error("{ErrorMessage}", ex.Message);
                    return null;
                }
            });
            
            await Task.Delay(500);
            convertTask.State = convertTask.ConvertedGpxFile != null ? ConvertState.Completed : ConvertState.Error;

        }
        private static async Task ConvertToItinerary(GpxToItineraryConvertTask gpxToItineraryConvertTask)
        {

            gpxToItineraryConvertTask.State = ConvertState.Working;
            gpxToItineraryConvertTask.ConvertedItineraryFile = await Task.Run(() =>
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
                        Name = gpxToItineraryConvertTask.ConvertOptions.RouteName,

                        Preferences =
                        {
                            DirtRoads = gpxToItineraryConvertTask.ConvertOptions.DirtyRoads,
                            Trains = gpxToItineraryConvertTask.ConvertOptions.Trains,
                            Motorway = gpxToItineraryConvertTask.ConvertOptions.Motorway,
                            Tunnel = gpxToItineraryConvertTask.ConvertOptions.Tunnel,
                            TollFree = gpxToItineraryConvertTask.ConvertOptions.TollFree,
                            Ferry = gpxToItineraryConvertTask.ConvertOptions.Ferry
                        }
                    }
                    };
                    SourceType type = gpxToItineraryConvertTask.SourceType;
                    Log.Debug("Selected Route Type:{Type}", type.ToString());
                    GpxWaypoint[] waypoints = type switch
                    {
                        SourceType.Route => gpxToItineraryConvertTask.OriginalGpxFile.rte[0].rtept,
                        SourceType.Track => gpxToItineraryConvertTask.OriginalGpxFile.trk[0].trkseg[0].trkpt,
                        SourceType.Waypoints => gpxToItineraryConvertTask.OriginalGpxFile.wpt,
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

                        if (gpxToItineraryConvertTask.ConvertOptions.FirstWaypointAsMyPosition && mItineraryFile.Itinerary.Stops.Count == 0)
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
            gpxToItineraryConvertTask.State = gpxToItineraryConvertTask.ConvertedItineraryFile != null ? ConvertState.Completed : ConvertState.Error;
        }

        private async Task UploadFiles(IReadOnlyList<IBrowserFile> files)
        {
            long maxallowedsize = 2097152;
            bool errorOnUpload = false;
            object @lock = new();

            files.AsParallel().AsOrdered().ForAll(browserFile =>
            {
                int? id = 0;

                lock (@lock)
                {
                    id = _convertTasks.Count != 0 ? (_convertTasks[^1].Id + 1) : 0;
                    
                    try
                    {
                        if (Path.GetExtension(browserFile.Name) == ".gpx")
                        {
                            _convertTasks.Add(new GpxToItineraryConvertTask
                            {
                                Id = id,
                                InputFile = browserFile,
                                OriginalGpxFile = null,
                                FileSourceType = FileSourceType.Gpx,
                                FileName = Path.GetFileNameWithoutExtension(browserFile.Name)
                            });
                        }
                    
                        if (Path.GetExtension(browserFile.Name) == ".mvtrip")
                        {
                            _convertTasks.Add(new TripToGpxConvertTask
                            {
                                Id = id,
                                InputFile = browserFile,
                                OriginalTripFile = null,
                                FileSourceType = FileSourceType.MVTrip,
                                FileName = Path.GetFileNameWithoutExtension(browserFile.Name)
                            });
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
                }
            });

            StateHasChanged();

            foreach (IConvertTask convertTask in  _convertTasks.Where(x => x.FileSourceType == FileSourceType.Gpx))
            {
                try
                {
                    GpxToItineraryConvertTask entry = (GpxToItineraryConvertTask)convertTask;
                    Log.Debug("Deserialize GPX File...");
                    GpxFile gpxFile = await Gpx11SerializerAsync.DeserializeAsync(entry.InputFile.OpenReadStream(maxallowedsize));

                    if (gpxFile != null)
                    {
                        Log.Debug("GPX File deserialized!");
                        Log.Debug("Attaching GpxFile to ConvertTask with Id {Id}", entry.Id);

                        entry.OriginalGpxFile = gpxFile;
                        Log.Debug(("Done"));
                    }
                    else
                    {
                        throw new InvalidOperationException($"Failed to parse Gpx File {entry.FileName}");
                    }
                }
                catch (Exception ex)
                {
                    errorOnUpload = true;
                    Log.Error("{ErrorMessage}", ex.Message);
                    convertTask.State = ConvertState.Error;
                }
                finally
                {
                    StateHasChanged();
                }
               
            }

            foreach (IConvertTask convertTask in _convertTasks.Where(x => x.FileSourceType == FileSourceType.MVTrip))
            {
                try
                {
                    TripToGpxConvertTask entry = (TripToGpxConvertTask)convertTask;
                    Log.Debug("Unzipping MvTrip File...");
                    string extractPath = Path.Combine(Path.GetTempPath(),Path.GetRandomFileName());
                    string zipPath = Path.GetTempFileName();
                    
                    await using (Stream rs = entry.InputFile.OpenReadStream(maxallowedsize))
                    {
                        await using (FileStream fs = new FileStream(zipPath, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            await rs.CopyToAsync(fs);
                        }
                    }
                    
                    ZipFile.ExtractToDirectory(zipPath, extractPath);

                    if (File.Exists(Path.Combine(extractPath, "trip.json")) && File.Exists(Path.Combine(extractPath, "gpsrows.json")))
                    {
                        Log.Debug("MVTrip Archive successfully extracted!");
                        Log.Debug("Deserialize MVTrip Files...");
                        entry.OriginalTripFile = await JsonSerializer.DeserializeAsync<Trip>(File.OpenRead(Path.Combine(extractPath, "trip.json")));
                        entry.OriginalTripFile.GpsRows = await JsonSerializer.DeserializeAsync<List<GpsRow>>(File.OpenRead(Path.Combine(extractPath, "gpsrows.json")));
                        Log.Debug("MVTrip Files successfully deserialized!");
                        File.Delete(zipPath);
                        Directory.Delete(extractPath, true);
                            
                    }
                    else
                    {
                        throw new InvalidOperationException($"Failed to extract MVTrip File {entry.FileName}");
                    }
                    
                }
                catch (Exception ex)
                {
                    errorOnUpload = true;
                    Log.Error("{ErrorMessage}", ex.Message);
                    convertTask.State = ConvertState.Error;
                }
                finally
                {
                    StateHasChanged();
                }
            }
            
            _convertTasks.RemoveAll(x => x.State == ConvertState.Error);
            
            if (!errorOnUpload && files.Count == _convertTasks.Count)
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

        private void DisposeConvertTask(IConvertTask item)
        {
            _convertTasks.RemoveAll(x => x.Id == item.Id);
        }

        private async Task<bool> IsWebShareSupportedAsync()
        {
            return (await Js.InvokeAsync<bool>("IsShareSupported"));
        }

        private async Task ShareItineraryFile(GpxToItineraryConvertTask task)
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
        
        private async Task ShareGpxFile(TripToGpxConvertTask task)
        {
            string payload = await Gpx11SerializerAsync.SerializeAsync(task.ConvertedGpxFile);

            if (await Js.InvokeAsync<bool>("CanShareThisFile", $"{task.OriginalTripFile.Title}.gpx", "application/octet-stream", $"data:text/plain;base64,{payload}"))
            {
                try
                {
                    await Js.InvokeVoidAsync("ShareFile", "Share Itinerary File", task.OriginalTripFile.Title, $"{task.OriginalTripFile.Title}.mvitinerary", "application/octet-stream", $"data:text/plain;base64,{payload}");
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
