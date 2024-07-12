using GPXRide.Enums;
using Microsoft.AspNetCore.Components.Forms;

namespace GPXRide.Interfaces;

public interface IConvertTask
{
    public int? Id { get; init; }
    
    public IBrowserFile InputFile { get; init; }
    public FileSourceType FileSourceType { get; init; }
    public string FileName { get; init; }
    public ConvertState State { get; set; }
}