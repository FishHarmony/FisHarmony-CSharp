using System;
using System.Drawing;
using System.IO;
using ExifLib;
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
      var image = ExifReader.ReadJpeg(inputImage);
      log.WriteLine("GpsLatitudeRef: " + image.GpsLatitudeRef);
      int latCount = 0;
      foreach (var d in image.GpsLatitude)
      {
        log.WriteLine("GpsLatitude " + latCount + ": " + d);
        latCount++;
      }
      

      log.WriteLine("GpsLongitudeRef: " + image.GpsLongitudeRef);
      int longCount = 0;
      foreach (var d in image.GpsLongitude)
      {
        log.WriteLine("GpsLongitude " + longCount + ": " + d);
        longCount++;
      }
    }
  }
}
