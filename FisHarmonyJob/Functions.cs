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
      Image image = Image.FromStream(inputImage);

      System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
      
      int count = 0;
      foreach (var propertyItem in image.PropertyItems)
      {
        log.WriteLine("Property Item " + count.ToString());
        log.WriteLine("ID: 0x" + propertyItem.Id.ToString("x"));
        log.WriteLine("type: " + propertyItem.Type.ToString());
        switch (propertyItem.Type)
        {
          case 2:
            log.WriteLine("value: " + encoding.GetString(propertyItem.Value));
            break;
          case 3:
            log.WriteLine("value: " + BitConverter.ToInt16(propertyItem.Value, 0));
            break;
          case 4:
            log.WriteLine("value: " + BitConverter.ToInt32(propertyItem.Value, 0));
            break;
          case 7:
            log.WriteLine("value: undefined");
            break;
          default:
            log.WriteLine("value: " + propertyItem.Value.ToString());
            break;
        }
        
        log.WriteLine("length:" + propertyItem.Len.ToString() + " bytes" );
        count++;
      }
    }
  }
}
