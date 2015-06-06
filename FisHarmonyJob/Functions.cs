using System;
using System.Drawing;
using System.IO;
using Microsoft.Azure.WebJobs;

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

    public static void LoadImage([BlobTrigger("image/{name}")] Stream inputImage,
      TextWriter log)
    {
      // North and East indicate positive, South and West indicate negative directions

      Image image = Image.FromStream(inputImage);

      System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
      
      foreach (var propertyItem in image.PropertyItems)
      {
        if (!Enum.IsDefined(typeof (Type), propertyItem.Id))
          continue;

        string value = "";
        switch (propertyItem.Type)
        {
          case 1:
            value = BitConverter.ToBoolean(propertyItem.Value, 0).ToString();
            break;
          case 2:
           value = encoding.GetString(propertyItem.Value);
            break;
          case 3:
            value = BitConverter.ToInt16(propertyItem.Value, 0).ToString();
            break;
          case 4:
          case 9:
            value = BitConverter.ToInt32(propertyItem.Value, 0).ToString();
            break;
          case 5:
          case 10:
            UInt32 numberator = BitConverter.ToUInt32(propertyItem.Value, 0);
            UInt32 denominator = BitConverter.ToUInt32(propertyItem.Value, 4);

            if(denominator != 0)
              value = ((double)numberator / (double)denominator).ToString();
            else
              value = "0";

            if (propertyItem.ToString() == "NaN")
              value = "0";
            break;
          default:
            value = "default";
            break;
        }
        
        log.WriteLine(((Type)propertyItem.Id).ToString() + ": " + value);
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
    GPSDestDistance = 0x001a
  }
}
