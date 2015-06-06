using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Web.Helpers;
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
      int count = 0;
      foreach (var propertyItem in image.PropertyItems)
      {
        log.WriteLine("Property Item " + count.ToString());
        log.WriteLine("ID: 0x" + propertyItem.Id.ToString("x"));
        log.WriteLine("type: " + propertyItem.Type.ToString());
        log.WriteLine("value: " + propertyItem.Value.ToString());
        log.WriteLine("length:" + propertyItem.Len.ToString() + " bytes" );
        count++;
      }
    }
  }
}
