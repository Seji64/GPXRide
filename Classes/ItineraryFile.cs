using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GPXRide.Classes
{
    public class ItineraryFile
    { 
        public Itinerary Itinerary { get; set; } = new();
        public MemoryStream ToZipArchiveStream()
        {
            MemoryStream memoryStream = new MemoryStream();

            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);
            var demoFile = archive.CreateEntry("itinerary.json");
            
            using var entryStream = demoFile.Open();
            var ItineraryText = JsonConvert.SerializeObject(Itinerary, new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ" });
            MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ItineraryText));
            stream.WriteTo(entryStream);

            return memoryStream;
        }
    }
}