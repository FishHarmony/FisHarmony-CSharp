using System.IO;
using Microsoft.Azure.WebJobs;

namespace FisHarmonyJob
{
  public class Functions
  {
    // This function will get triggered/executed when a new message is written 
    // on an Azure Queue called queue.
    public static void ProcessBlob([BlobTrigger("test/{name}")] TextReader input, TextWriter log)
    {
      log.WriteLine(input.ReadToEnd());
    }
  }
}
