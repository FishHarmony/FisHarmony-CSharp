using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Text;
using Dapper;
using Microsoft.Azure.WebJobs;
using Npgsql;

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
        if (!Enum.IsDefined(typeof (Type), propertyItem.Id))
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
            int iterations = propertyItem.Len/8;
            List<decimal> rational = new List<decimal>();
            for (int i = 0; i < iterations; i++)
            {
              if (i > 0)
                value.Append(" ");

              UInt32 numberator = BitConverter.ToUInt32(propertyItem.Value, i*8);
              UInt32 denominator = BitConverter.ToUInt32(propertyItem.Value, (i*8)+4);

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

              var m = minutes + (seconds/60);
              var d = m/60;
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
        values[Type.GPSLatitude] = (decimal.Parse(values[Type.GPSLatitude])*-1).ToString();
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

      NpgsqlConnection conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["PostgreSQL"].ConnectionString);
      conn.Open();
      try
      {
        conn.Execute(
        "UPDATE reports SET latitude = @latitude, longitude = @longitude, compass_direction = @compass_direction, picture_taken_at = @picture_taken_At WHERE id = @id",
        new
        {
          latitude = values[Type.GPSLatitude],
          longitude = values[Type.GPSLongitude],
          compass_direction = values[Type.GPSImgDirection].ToString(),
          picture_taken_at = dateTime,
          id = blobInfo.ReportId
        });
      }
      finally
      {
        conn.Close();
      }


    }
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
