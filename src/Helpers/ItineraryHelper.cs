using System.IO.Compression;
using System.Text;
using System.Text.Json;
using GPXRide.JsonConverters;
using GPXRide.Models.MvRide;

namespace GPXRide.Helpers;

public static class ItineraryHelper
{
    public static MemoryStream SerializeAsBytes(Itinerary itinerary)
    {
        MemoryStream memoryStream = new();

        using ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);
        ZipArchiveEntry zipArchiveEntry = archive.CreateEntry("itinerary.json");

        using Stream entryStream = zipArchiveEntry.Open();
        string itineraryText = JsonSerializer.Serialize(itinerary, new JsonSerializerOptions() { Converters = { new JsonDateTimeConverter() }});
        MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(itineraryText));
        stream.WriteTo(entryStream);

        return memoryStream;
    }
}