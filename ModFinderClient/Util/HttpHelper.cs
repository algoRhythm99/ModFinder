using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ModFinder.Util
{
  internal static class HttpHelper
  {
    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task DownloadFileAsync(string uri, string outputPath)
    {
      Uri uriResult;

      if (!Uri.TryCreate(uri, UriKind.Absolute, out uriResult))
        throw new InvalidOperationException("URI is invalid.");

      if (!File.Exists(outputPath))
        throw new FileNotFoundException("File not found.", nameof(outputPath));

      byte[] fileBytes = await _httpClient.GetByteArrayAsync(uri);
      File.WriteAllBytes(outputPath, fileBytes);
    }

    public static string GetResponseContent(string url)
    {
      String result = null;
      int remainingTries = 3;
      int backoffMillis = 100;
      int backoffExp = 2;
      do
      {
        --remainingTries;
        try
        {
          using HttpResponseMessage response = _httpClient.GetAsync(url).Result;
          using HttpContent content = response.Content;
          result = content.ReadAsStringAsync().Result;
        }
        catch (Exception)
        {
          Thread.Sleep(backoffMillis);
          backoffMillis *= backoffExp;
        }
      }
      while (remainingTries > 0);
      return result;
    }
  }
}