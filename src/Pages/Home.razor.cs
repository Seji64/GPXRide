using System.IO.Compression;
using System.Text.Json;
using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using GPXRide.Enums;
using GPXRide.Helpers;
using GPXRide.Interfaces;
using GPXRide.Models;
using GPXRide.Models.MvRide;
using KristofferStrube.Blazor.FileSystem;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Serilog;

namespace GPXRide.Pages;

public partial class Home
{
    
    private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full";
    private string _dragClass = DefaultDragClass;
    private readonly List<IBrowserFile> _browserFiles = [];
    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;
    private bool _uploading;
    private readonly List<IConvertTask> _convertTasks = [];
    private bool _pwaFileHandlingSupported;

    private Task OpenFilePickerAsync()
        => _fileUpload?.OpenFilePickerAsync() ?? Task.CompletedTask;
    
    private void SetDragClass()
        => _dragClass = $"{DefaultDragClass} mud-border-primary";
    
    private void ClearDragClass()
        => _dragClass = DefaultDragClass;
    private async Task ClearAllFilesAsync()
    {
        await (_fileUpload?.ClearAsync() ?? Task.CompletedTask);
        _browserFiles.Clear();
        ClearDragClass();
    }

    protected override async Task OnInitializedAsync()
    {
        _pwaFileHandlingSupported = await FileHandlingService.IsSupportedAsync();

        if (_pwaFileHandlingSupported)
        {
            await FileHandlingService.SetConsumerAsync(async (launchParams) =>
            {
                foreach (FileSystemHandle fileSystemHandle in launchParams.Files)
                {
                    try
                    {
                        if (fileSystemHandle is FileSystemFileHandle fileSystemFileHandle)
                        {
                            int id = _convertTasks.Count != 0 ? (_convertTasks[^1].Id + 1) : 0;
                        
                            KristofferStrube.Blazor.FileAPI.File file = await fileSystemFileHandle.GetFileAsync();
                            
                            MemoryStream memoryStream = new(await file.ArrayBufferAsync());
                            await memoryStream.FlushAsync();
                            memoryStream.Seek(0, SeekOrigin.Begin);
                        
                            switch (Path.GetExtension(await file.GetNameAsync()))
                            {
                                case ".gpx":
                                    _convertTasks.Add(new GpxToItineraryConvertTask
                                    {
                                        Id = id,
                                        InputStream = await file.StreamAsync(),
                                        SourceType = SourceType.Gpx,
                                        FileName = Path.GetFileNameWithoutExtension(await file.GetNameAsync()),
                                        State = ConvertState.PreparePending
                                    });
                                    break;
                                
                                case ".mvtrip":
                                    _convertTasks.Add(new TripToGpxConvertTask
                                    {
                                        Id = id,
                                        InputStream = memoryStream,
                                        SourceType = SourceType.MvTrip,
                                        FileName = Path.GetFileNameWithoutExtension(await file.GetNameAsync()),
                                        State = ConvertState.PreparePending
                                    });
                                    break;
                                default:
                                    await memoryStream.DisposeAsync();
                                    Log.Warning("Filetype not handled!");
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("[PWAFileHandle] {ErrorMessage}",ex.Message);
                    }
                }

                if (_convertTasks.Count <= 0) return;
                await PrepareConvertTasksAsync();

            });
        }
    }

    private void OnInputFileChanged(InputFileChangeEventArgs e)
    {
        ClearDragClass();
        IReadOnlyList<IBrowserFile> files = e.GetMultipleFiles();
        
        foreach (IBrowserFile file in files)
        {
            _browserFiles.Add(file);
        }
    }
    private async Task UploadAsync()
    {
        _uploading = true;
        
        try
        {
           _browserFiles.ForEach(browserFile =>
            {
                int id = _convertTasks.Count != 0 ? (_convertTasks[^1].Id + 1) : 0;

                switch (Path.GetExtension(browserFile.Name))
                {
                    case ".gpx":
                        _convertTasks.Add(new GpxToItineraryConvertTask
                        {
                            Id = id,
                            InputStream = browserFile.OpenReadStream(),
                            SourceType = SourceType.Gpx,
                            FileName = Path.GetFileNameWithoutExtension(browserFile.Name),
                            State = ConvertState.PreparePending
                        });
                        break;
                    case ".mvtrip":
                        _convertTasks.Add(new TripToGpxConvertTask
                        {
                            Id = id,
                            InputStream = browserFile.OpenReadStream(),
                            SourceType = SourceType.MvTrip,
                            FileName = Path.GetFileNameWithoutExtension(browserFile.Name),
                            State = ConvertState.PreparePending
                        });
                        break;
                    default:
                        Log.Warning("Filetype not handled!");
                        break;
                }
            });
           
            Snackbar.Add("All files successfully uploaded!", Severity.Success);
           
        }
        catch (Exception ex)
        {
            Log.Error("[Upload] {ErrorMessage}",ex.Message);
        }
        finally
        {
            await ClearAllFilesAsync();
            _uploading = false;
        }

        if (_convertTasks.Count > 0)
        {
            await PrepareConvertTasksAsync();
        }
    }

    private async Task PrepareConvertTasksAsync()
    {
        foreach (IConvertTask convertTask in _convertTasks.Where(x => x.State == ConvertState.PreparePending))
        {
            CancellationToken cancellationToken = new();
            
            try
            {
                switch (convertTask.SourceType)
                {
                    case SourceType.Gpx:
                            
                        GpxToItineraryConvertTask gpxToItineraryConvertTask = (GpxToItineraryConvertTask)convertTask;
                        GpxFile? gpxFile =
                            await GpxHelper.DeserializeAsync(
                                gpxToItineraryConvertTask.InputStream);
                        if (gpxFile == null)
                        {
                            throw new InvalidOperationException($"Failed to parse Gpx File {gpxToItineraryConvertTask.FileName}");
                        }

                        gpxToItineraryConvertTask.OriginalGpxFile = gpxFile;
                        gpxToItineraryConvertTask.ItineryGpxConvertOptions = new ItineryGpxConvertOptions
                        {
                            RouteName =  gpxToItineraryConvertTask.FileName
                        };

                        await gpxToItineraryConvertTask.InputStream.DisposeAsync();
                        gpxToItineraryConvertTask.State = ConvertState.Prepared;
                        
                        break;
                        
                    case SourceType.MvTrip:
                        
                        TripToGpxConvertTask tripToGpxConvertTask = (TripToGpxConvertTask)convertTask;
                        string extractPath = Path.Combine(Path.GetTempPath(),Path.GetRandomFileName());
                        string zipPath = Path.GetTempFileName();
                            
                        await using (Stream rs = tripToGpxConvertTask.InputStream)
                        {
                            await using (FileStream fs = new FileStream(zipPath, FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                await rs.CopyToAsync(fs, cancellationToken);
                            }
                        }
                            
                        ZipFile.ExtractToDirectory(zipPath, extractPath);

                        if (!File.Exists(Path.Combine(extractPath, "trip.json")) ||
                            !File.Exists(Path.Combine(extractPath, "gpsrows.json")))
                        {
                            throw new InvalidOperationException($"Failed to extract MVTrip File {tripToGpxConvertTask.FileName}");
                        }
                            
                        tripToGpxConvertTask.OriginalMvTripFile = await JsonSerializer.DeserializeAsync<Trip>(File.OpenRead(Path.Combine(extractPath, "trip.json")), cancellationToken: cancellationToken);

                        if (tripToGpxConvertTask.OriginalMvTripFile != null)
                        {
                            tripToGpxConvertTask.OriginalMvTripFile.GpsRows = await JsonSerializer.DeserializeAsync<List<GpsRow>>(File.OpenRead(Path.Combine(extractPath, "gpsrows.json")), cancellationToken: cancellationToken);    
                        }
                        
                        await tripToGpxConvertTask.InputStream.DisposeAsync();
                        tripToGpxConvertTask.State = ConvertState.Prepared;
                        
                        File.Delete(zipPath);
                        Directory.Delete(extractPath, true);
                        
                        break;
                    
                    default:
                        Log.Warning("Unknown Source Type");
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                Log.Error("[PrepareConvertTasks] {ErrorMessage}", ex.ToString());
                convertTask.State = ConvertState.Error;
            }
        }
        
        if (_convertTasks.Any(x => x.State == ConvertState.Error))
        {
            _convertTasks.Where(x => x.State == ConvertState.Error).ToList().ForEach( x => Snackbar.Add($"Failed to prepare/process {x.FileName}", Severity.Error));
        }
        
        _convertTasks.RemoveAll(x => x.State == ConvertState.Error);
        await InvokeAsync(StateHasChanged);
    }
    
    private void DisposeConvertTask(IConvertTask item)
    {
        _convertTasks.RemoveAll(x => x.Id == item.Id);
        StateHasChanged();
    }
}