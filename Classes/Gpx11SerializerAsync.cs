using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Geo.Gps.Serialization;
using Geo.Gps.Serialization.Xml.Gpx.Gpx11;

namespace GPXRide.Classes
{
    public class Gpx11SerializerAsync : Gpx11Serializer
    {
        public static async Task<GpxFile> DeserializeAsync(Stream stream)
        {
            using var streamReader = new StreamReader(stream);
            var data = await streamReader.ReadToEndAsync();
            var xmlSerializer = new XmlSerializer(typeof(GpxFile));

            using var stringReader = new StringReader(data);
            var gpxFile = (GpxFile)xmlSerializer.Deserialize(stringReader);
            return gpxFile;
        }
    }
}