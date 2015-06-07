using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Dapper;
using Microsoft.Azure.WebJobs;
using Npgsql;
using RestSharp;

namespace FisHarmonyJob
{
  public class Functions
  {
    // This function will get triggered/executed when a new message is written 
    // on an Azure Queue called queue.
    //public static async Task ProcessBlob([BlobTrigger("test/{name}.{ext}")] Stream input, string name, string ext, TextWriter log)
    //{
    //  log.WriteLine("Blob name:" + name);
    //  log.WriteLine("Blod extension:" + ext);

    //  using (StreamReader reader = new StreamReader(input))
    //  {
    //    string blobContent = await reader.ReadToEndAsync();
    //    log.WriteLine("Blob content: {0}", blobContent);
    //  }
    //}
    public static void CopyBlob([BlobTrigger("input/{name}")] TextReader input,
    [Blob("output/{name}")] out string output,
      TextWriter log)
    {
      var text = input.ReadToEnd();
      log.WriteLine(text);
      output = text;
    }

    public static void LoadTestImage([BlobTrigger("test/{name}")] Stream inputImage,
      TextWriter log)
    {
      // North and East indicate positive, South and West indicate negative directions

      Image image = Image.FromStream(inputImage);

      System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

      Dictionary<Type, string> values = new Dictionary<Type, string>();

      foreach (var propertyItem in image.PropertyItems)
      {
        if (!Enum.IsDefined(typeof(Type), propertyItem.Id))
          continue;

        StringBuilder value = new StringBuilder();
        switch (propertyItem.Type)
        {
          case 1:
            value.Append(BitConverter.ToBoolean(propertyItem.Value, 0).ToString());
            break;
          case 2:
            value.Append(encoding.GetString(propertyItem.Value, 0, propertyItem.Len - 1));
            break;
          case 3:
            value.Append(BitConverter.ToInt16(propertyItem.Value, 0).ToString());
            break;
          case 4:
          case 9:
            value.Append(BitConverter.ToInt32(propertyItem.Value, 0).ToString());
            break;
          case 5:
          case 10:
            int iterations = propertyItem.Len / 8;
            List<decimal> rational = new List<decimal>();
            for (int i = 0; i < iterations; i++)
            {
              if (i > 0)
                value.Append(" ");

              UInt32 numberator = BitConverter.ToUInt32(propertyItem.Value, i * 8);
              UInt32 denominator = BitConverter.ToUInt32(propertyItem.Value, (i * 8) + 4);

              if (denominator != 0)
                rational.Add((decimal)numberator / (decimal)denominator);
              else
                rational.Add(0);
            }

            if (iterations == 3 && (
              (((Type)propertyItem.Id) == Type.GPSLatitude || ((Type)propertyItem.Id) == Type.GPSLongitude)))
            {
              decimal degrees = rational[0];
              decimal minutes = rational[1];
              decimal seconds = rational[2];

              var m = minutes + (seconds / 60);
              var d = m / 60;
              decimal decimalDegrees = degrees + d;
              value.Append(decimalDegrees.ToString());
            }
            else
            {
              value.Append(string.Join(":", rational));
            }

            break;
          default:
            value.Append("default");
            break;
        }
        values.Add((Type)propertyItem.Id, value.ToString());
      }

      if (values.ContainsKey(Type.GPSLatitudeRef) && values.ContainsKey(Type.GPSLatitude) && values[Type.GPSLatitudeRef] == "S")
      {
        values[Type.GPSLatitude] = (decimal.Parse(values[Type.GPSLatitude]) * -1).ToString();
      }

      if (values.ContainsKey(Type.GPSLongitudeRef) && values.ContainsKey(Type.GPSLongitude) && values[Type.GPSLongitudeRef] == "W")
      {
        values[Type.GPSLongitude] = (decimal.Parse(values[Type.GPSLongitude]) * -1).ToString();
      }

      if (values.ContainsKey(Type.GPSTimeStamp) && values.ContainsKey(Type.GPSDateStamp))
      {
        DateTime dateTime;
        if (DateTime.TryParse(values[Type.GPSDateStamp].Replace(":", "/") + " " + values[Type.GPSTimeStamp], out dateTime))
        {
          log.WriteLine(dateTime.ToString());
        }
      }

      foreach (var value in values)
      {
        log.WriteLine(value.Key.ToString() + ": " + value.Value);
      }
    }

    public static void ProcessImage(
      [QueueTrigger("processimage")] BlobInformation blobInfo,
      [Blob("image/original/{BlobName}", FileAccess.Read)] Stream input,
      TextWriter log)
    {
      // North and East indicate positive, South and West indicate negative directions

      Image image = Image.FromStream(input);

      System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

      Dictionary<Type, string> values = new Dictionary<Type, string>();

      foreach (var propertyItem in image.PropertyItems)
      {
        if (!Enum.IsDefined(typeof(Type), propertyItem.Id))
          continue;

        StringBuilder value = new StringBuilder();
        switch (propertyItem.Type)
        {
          case 1:
            value.Append(BitConverter.ToBoolean(propertyItem.Value, 0).ToString());
            break;
          case 2:
            value.Append(encoding.GetString(propertyItem.Value, 0, propertyItem.Len - 1));
            break;
          case 3:
            value.Append(BitConverter.ToInt16(propertyItem.Value, 0).ToString());
            break;
          case 4:
          case 9:
            value.Append(BitConverter.ToInt32(propertyItem.Value, 0).ToString());
            break;
          case 5:
          case 10:
            int iterations = propertyItem.Len / 8;
            List<decimal> rational = new List<decimal>();
            for (int i = 0; i < iterations; i++)
            {
              if (i > 0)
                value.Append(" ");

              UInt32 numberator = BitConverter.ToUInt32(propertyItem.Value, i * 8);
              UInt32 denominator = BitConverter.ToUInt32(propertyItem.Value, (i * 8) + 4);

              if (denominator != 0)
                rational.Add((decimal)numberator / (decimal)denominator);
              else
                rational.Add(0);
            }

            if (iterations == 3 && (
              (((Type)propertyItem.Id) == Type.GPSLatitude || ((Type)propertyItem.Id) == Type.GPSLongitude)))
            {
              decimal degrees = rational[0];
              decimal minutes = rational[1];
              decimal seconds = rational[2];

              var m = minutes + (seconds / 60);
              var d = m / 60;
              decimal decimalDegrees = degrees + d;
              value.Append(decimalDegrees.ToString());
            }
            else
            {
              value.Append(string.Join(":", rational));
            }

            break;
          default:
            value.Append("default");
            break;
        }
        values.Add((Type)propertyItem.Id, value.ToString());
      }

      if (values.ContainsKey(Type.GPSLatitudeRef) && values.ContainsKey(Type.GPSLatitude) && values[Type.GPSLatitudeRef] == "S")
      {
        values[Type.GPSLatitude] = (decimal.Parse(values[Type.GPSLatitude]) * -1).ToString();
      }

      if (values.ContainsKey(Type.GPSLongitudeRef) && values.ContainsKey(Type.GPSLongitude) && values[Type.GPSLongitudeRef] == "W")
      {
        values[Type.GPSLongitude] = (decimal.Parse(values[Type.GPSLongitude]) * -1).ToString();
      }
      DateTime dateTime = DateTime.UtcNow;
      if (values.ContainsKey(Type.GPSTimeStamp) && values.ContainsKey(Type.GPSDateStamp))
      {

        if (DateTime.TryParse(values[Type.GPSDateStamp].Replace(":", "/") + " " + values[Type.GPSTimeStamp], out dateTime))
        {
          log.WriteLine(dateTime.ToString());
        }
      }

      foreach (var value in values)
      {
        log.WriteLine(value.Key.ToString() + ": " + value.Value);
      }

      decimal val = 0.007M;

      decimal LongMod = 0;
      decimal LatMod = 0;

      var mod = val / 90;

      var orientation = decimal.Parse(values[Type.GPSDestBearing]);

      if (orientation == 0)
      {
        LongMod = val;
      }
      else if (orientation <= 90)
      {
        LongMod = val - (mod * orientation);
        LatMod = mod * orientation;
      }
      else if (orientation > 90 && orientation <= 180)
      {
        var orientationMod = orientation - 90;
        LatMod = val - (mod * orientationMod);
        LongMod = (mod * orientationMod) * -1;
      }
      else if (orientation > 180 && orientation <= 270)
      {
        var orientationMod = orientation - 180;
        LongMod = (val - (mod * orientationMod)) * -1;
        LatMod = (mod * orientationMod) * -1;
      }
      else
      {
        var orientationMod = orientation - 270;
        LatMod = (val - (mod * orientationMod)) * -1;
        LongMod = (mod * orientationMod);
      }

      var latitude = decimal.Parse(values[Type.GPSLatitude]) + LatMod;
      var longitude = decimal.Parse(values[Type.GPSLongitude]) + LongMod;
      values[Type.GPSLatitude] = latitude.ToString();
      values[Type.GPSLongitude] = longitude.ToString();

      decimal minLong = longitude - (decimal)0.005;
      decimal minLat = latitude - (decimal)0.005;

      decimal maxLong = longitude + (decimal)0.005;
      decimal maxLat = latitude + (decimal)0.005;

      var key = ConfigurationManager.ConnectionStrings["AIS"].ConnectionString;

      var req =
        string.Format(
          "http://services.marinetraffic.com/api/exportvessels/{0}/MINLAT:{1}/MAXLAT:{2}/MINLON:{3}/MAXLON:{4}/timespan:60/protocol:xml/msgtype:extended",
          key,
          minLat,
          maxLat,
          minLong,
          maxLong);

      var client = new RestClient(req);
      var request = new RestRequest(Method.GET);
      var response = client.Execute(request);
      var content = response.Content; // raw content as string

      XmlSerializer serializer = new XmlSerializer(typeof(MarineTrafficAISWrapper));
      using (StringReader reader = new StringReader(content))
      {
        MarineTrafficAISWrapper traffic = (MarineTrafficAISWrapper)(serializer.Deserialize(reader));

        NpgsqlConnection conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["PostgreSQL"].ConnectionString);
        conn.Open();
        try
        {
          conn.Execute(
          "UPDATE reports SET latitude = @latitude, longitude = @longitude, compass_direction = @compass_direction, picture_taken_at = @picture_taken_At WHERE id = @id",
          new
          {
            latitude = latitude,
            longitude = longitude,
            compass_direction = values[Type.GPSDestBearing].ToString(),
            picture_taken_at = dateTime,
            id = blobInfo.ReportId
          });
          if (traffic != null)
          {
            foreach (var ais in traffic.POS)
            {
              var entity = new MarineTrafficAISPosgreSQL
              {
                mmsi = int.Parse(ais.MMSI),
                latitude = !string.IsNullOrEmpty(ais.LAT) ? (decimal?) decimal.Parse(ais.LAT) : null,
                longitude = !string.IsNullOrEmpty(ais.LON) ? (decimal?) decimal.Parse(ais.LON) : null,
                speed = !string.IsNullOrEmpty(ais.SPEED) ? (int?) int.Parse(ais.SPEED) : null,
                course = !string.IsNullOrEmpty(ais.COURSE) ? (int?) int.Parse(ais.COURSE) : null,
                timestamp = !string.IsNullOrEmpty(ais.TIMESTAMP) ? (DateTime?) DateTime.Parse(ais.TIMESTAMP) : null,
                ship_name = ais.SHIPNAME,
                ship_type = !string.IsNullOrEmpty(ais.SHIPTYPE) ? (int?) int.Parse(ais.SHIPTYPE) : null,
                imo = !string.IsNullOrEmpty(ais.IMO) ? (int?) int.Parse(ais.IMO) : null,
                callsign = ais.CALLSIGN,
                flag = ais.FLAG,
                current_port = ais.CURRENT_PORT,
                last_port = ais.LAST_PORT,
                last_port_time =
                  !string.IsNullOrEmpty(ais.LAST_PORT_TIME) ? (DateTime?) DateTime.Parse(ais.LAST_PORT_TIME) : null,
                destination = ais.DESTINATION,
                eta = !string.IsNullOrEmpty(ais.ETA) ? (DateTime?) DateTime.Parse(ais.ETA) : null,
                length = !string.IsNullOrEmpty(ais.LENGTH) ? (decimal?) decimal.Parse(ais.LENGTH) : null,
                width = !string.IsNullOrEmpty(ais.WIDTH) ? (decimal?) decimal.Parse(ais.WIDTH) : null,
                draught = !string.IsNullOrEmpty(ais.DRAUGHT) ? (decimal?) decimal.Parse(ais.DRAUGHT) : null,
                grt = ais.GRT,
                dwt = ais.DWT,
                year_built = !string.IsNullOrEmpty(ais.YEAR_BUILT) ? (int?) int.Parse(ais.YEAR_BUILT) : null,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
              };
              var rows =
                conn.Query(
                  "INSERT INTO asi_ships (mmsi, latitude, longitude, speed, course, timestamp, ship_name, ship_type, imo, callsign, flag, current_port, last_port, last_port_time, destination, eta, length, width, draught, grt, dwt, year_built, created_at, updated_at) " +
                  "VALUES (@mmsi, @latitude, @longitude, @speed, @course, @timestamp, @ship_name, @ship_type, @imo, @callsign, @flag, @current_port, @last_port, @last_port_time, @destination, @eta, @length, @width, @draught, @grt, @dwt, @year_built, @created_at, @updated_at)" +
                  "RETURNING id",
                  new
                  {
                    mmsi = entity.mmsi,
                    latitude = entity.latitude,
                    longitude = entity.longitude,
                    speed = entity.speed,
                    course = entity.course,
                    timestamp = entity.timestamp,
                    ship_name = entity.ship_name,
                    ship_type = entity.ship_type,
                    imo = entity.imo,
                    callsign = entity.callsign,
                    flag = entity.flag,
                    current_port = entity.current_port,
                    last_port = entity.last_port,
                    last_port_time = entity.last_port_time,
                    destination = entity.destination,
                    eta = entity.eta,
                    length = entity.length,
                    width = entity.width,
                    draught = entity.draught,
                    grt = entity.grt,
                    dwt = entity.dwt,
                    year_built = entity.year_built,
                    created_at = entity.created_at,
                    updated_at = entity.updated_at
                  });
              conn.Execute("INSERT INTO in_radius_ships (report_id, asi_ship_id, created_at, updated_at) VALUES (@report_id, @asi_ship_id, @created_at, @updated_at)",
                new {report_id = blobInfo.ReportId, asi_ship_id = ((int) rows.FirstOrDefault().id), created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow});
            }
          }
        }
        finally
        {
          conn.Close();
        }
      }
    }
  }

  public class MarineTrafficAISPosgreSQL
  {
    public int mmsi { get; set; }
    public decimal? latitude { get; set; }
    public decimal? longitude { get; set; }
    public int? speed { get; set; }
    public int? course { get; set; }
    public DateTime? timestamp { get; set; }
    public string ship_name { get; set; }
    public int? ship_type { get; set; }
    public int? imo { get; set; }
    public string callsign { get; set; }
    public string flag { get; set; }
    public string current_port { get; set; }
    public string last_port { get; set; }
    public DateTime? last_port_time { get; set; }
    public string destination { get; set; }
    public DateTime? eta { get; set; }
    public decimal? length { get; set; }
    public decimal? width { get; set; }
    public decimal? draught { get; set; }
    public string grt { get; set; }
    public string dwt { get; set; }
    public int? year_built { get; set; }
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
  }

  [XmlRoot("POS")]
  public class MarineTrafficAISWrapper
  {
    public MarineTrafficAISWrapper()
    {
      POS = new List<MarineTrafficAIS>();
    }

    [XmlElement("row")]
    public List<MarineTrafficAIS> POS { get; set; }
  }

  [XmlRoot("row")]
  public class MarineTrafficAIS
  {
    [XmlAttribute]
    public string MMSI { get; set; }
    [XmlAttribute]
    public string LAT { get; set; }
    [XmlAttribute]
    public string LON { get; set; }
    [XmlAttribute]
    public string SPEED { get; set; }
    [XmlAttribute]
    public string COURSE { get; set; }
    [XmlAttribute]
    public string TIMESTAMP { get; set; }
    [XmlAttribute]
    public string SHIPNAME { get; set; }
    [XmlAttribute]
    public string SHIPTYPE { get; set; }
    [XmlAttribute]
    public string IMO { get; set; }
    [XmlAttribute]
    public string CALLSIGN { get; set; }
    [XmlAttribute]
    public string FLAG { get; set; }
    [XmlAttribute]
    public string CURRENT_PORT { get; set; }
    [XmlAttribute]
    public string LAST_PORT { get; set; }
    [XmlAttribute]
    public string LAST_PORT_TIME { get; set; }
    [XmlAttribute]
    public string DESTINATION { get; set; }
    [XmlAttribute]
    public string ETA { get; set; }
    [XmlAttribute]
    public string LENGTH { get; set; }
    [XmlAttribute]
    public string WIDTH { get; set; }
    [XmlAttribute]
    public string DRAUGHT { get; set; }
    [XmlAttribute]
    public string GRT { get; set; }
    [XmlAttribute]
    public string DWT { get; set; }
    [XmlAttribute]
    public string YEAR_BUILT { get; set; }
  }

  public enum Type
  {
    GPSLatitudeRef = 0x0001,
    GPSLatitude = 0x0002,
    GPSLongitudeRef = 0x0003,
    GPSLongitude = 0x0004,
    GPSAltitudeRef = 0x0005,
    GPSAltitude = 0x0006,
    GPSTimeStamp = 0x0007,
    GPSSatellites = 0x0008,
    GPSStatus = 0x0009,
    GPSMeasureMode = 0x000a,
    GPSDOP = 0x000b,
    GPSSpeedRef = 0x000c,
    GPSSpeed = 0x000d,
    GPSTrackRef = 0x000e,
    GPSTrack = 0x000f,
    GPSImgDirectionRef = 0x0010,
    GPSImgDirection = 0x0011,
    GPSMapDatum = 0x0012,
    GPSDestLatitudeRef = 0x0013,
    GPSDestLatitude = 0x0014,
    GPSDestLongitudeRef = 0x0015,
    GPSDestLongitude = 0x0016,
    GPSDestBearingRef = 0x0017,
    GPSDestBearing = 0x0018,
    GPSDestDistanceRef = 0x0019,
    GPSDestDistance = 0x001a,
    GPSDateStamp = 0x001d
  }
}