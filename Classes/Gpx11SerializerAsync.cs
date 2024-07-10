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
            using StreamReader streamReader = new StreamReader(stream);
            string data = await streamReader.ReadToEndAsync();
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(GpxFile));

            using StringReader stringReader = new StringReader(data);
            GpxFile gpxFile = (GpxFile)xmlSerializer.Deserialize(stringReader);
            return gpxFile;
        }
    }
}