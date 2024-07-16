using System.Xml.Serialization;
using Geo.Gps.Serialization;
using Geo.Gps.Serialization.Xml.Gpx.Gpx11;

namespace GPXRide.Helpers
{
    public class GpxHelper : Gpx11Serializer
    {
        public static async Task<GpxFile?> DeserializeAsync(Stream stream)
        {
            using StreamReader streamReader = new StreamReader(stream);
            string data = await streamReader.ReadToEndAsync();
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(GpxFile));

            using StringReader stringReader = new StringReader(data);
            GpxFile? gpxFile = (GpxFile?)xmlSerializer.Deserialize(stringReader);
            return gpxFile;
        }
        
        public static MemoryStream SerializeAsBytes(GpxFile file)
        {
            MemoryStream memoryStream = new();
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(GpxFile));
            xmlSerializer.Serialize(memoryStream, file);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
    }
}