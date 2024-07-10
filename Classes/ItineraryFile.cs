using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace GPXRide.Classes
{
    public class ItineraryFile
    {
        public Itinerary Itinerary { get; } = new();
        public MemoryStream ToZipArchiveStream()
        {
            MemoryStream memoryStream = new();

            using ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);
            ZipArchiveEntry demoFile = archive.CreateEntry("itinerary.json");

            using Stream entryStream = demoFile.Open();
            string itineraryText = JsonSerializer.Serialize(Itinerary, new JsonSerializerOptions() { Converters = { new JsonDateTimeConverter() }});
            MemoryStream stream = new(Encoding.UTF8.GetBytes(itineraryText));
            stream.WriteTo(entryStream);

            return memoryStream;
        }
    }
}