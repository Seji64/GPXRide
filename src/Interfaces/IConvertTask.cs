using GPXRide.Enums;
using Microsoft.AspNetCore.Components.Forms;

namespace GPXRide.Interfaces;

public interface IConvertTask
{
    public int Id { get; init; }
    public Stream InputStream { get; init; }
    public SourceType SourceType { get; init; }
    public string FileName { get; init; }
    public ConvertState State { get; set; }
}