using System.IO;
using System.Threading.Tasks;
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
  }
}
