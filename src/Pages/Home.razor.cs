using System.IO.Compression;
using System.Text.Json;
using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using GPXRide.Enums;
using GPXRide.Helpers;
using GPXRide.Interfaces;
using GPXRide.Models;
using GPXRide.Models.MvRide;
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
                            InputFile = browserFile,
                            SourceType = SourceType.Gpx,
                            FileName = Path.GetFileNameWithoutExtension(browserFile.Name),
                            State = ConvertState.PreparePending
                        });
                        break;
                    case ".mvtrip":
                        _convertTasks.Add(new TripToGpxConvertTask
                        {
                            Id = id,
                            InputFile = browserFile,
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
                                gpxToItineraryConvertTask.InputFile.OpenReadStream(cancellationToken: cancellationToken));
                        if (gpxFile == null)
                        {
                            throw new InvalidOperationException($"Failed to parse Gpx File {gpxToItineraryConvertTask.FileName}");
                        }

                        gpxToItineraryConvertTask.OriginalGpxFile = gpxFile;
                        gpxToItineraryConvertTask.ItineryGpxConvertOptions = new ItineryGpxConvertOptions
                        {
                            RouteName =  gpxToItineraryConvertTask.FileName
                        };
                        
                        gpxToItineraryConvertTask.State = ConvertState.Prepared;
                        
                        break;
                        
                    case SourceType.MvTrip:
                            
                        TripToGpxConvertTask tripToGpxConvertTask = (TripToGpxConvertTask)convertTask;
                        string extractPath = Path.Combine(Path.GetTempPath(),Path.GetRandomFileName());
                        string zipPath = Path.GetTempFileName();
                            
                        await using (Stream rs = tripToGpxConvertTask.InputFile.OpenReadStream(cancellationToken: cancellationToken))
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
                Log.Error("[PrepareConvertTasks] {ErrorMessage}", ex.Message);
                convertTask.State = ConvertState.Error;
            }
        }
        
        if (_convertTasks.Any(x => x.State == ConvertState.Error))
        {
            _convertTasks.Where(x => x.State == ConvertState.Error).ToList().ForEach( x => Snackbar.Add($"Failed to prepare/process {x.InputFile.Name}", Severity.Error));
        }
        
        _convertTasks.RemoveAll(x => x.State == ConvertState.Error);
    }
    
    private void DisposeConvertTask(IConvertTask item)
    {
        _convertTasks.RemoveAll(x => x.Id == item.Id);
        StateHasChanged();
    }
}