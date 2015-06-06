using System;
using System.IO;

namespace FisHarmonyJob
{
  public class BlobInformation
  {
    public Uri BlobUri { get; set; }

    public string BlobName
    {
      get
      {
        return BlobUri.Segments[BlobUri.Segments.Length - 1];
      }
    }
    public string BlobNameWithoutExtension
    {
      get
      {
        return Path.GetFileNameWithoutExtension(BlobName);
      }
    }
    public int DeviceId { get; set; }
  }
}