using System.Diagnostics;

namespace MusicFree.Helper
{
    class Request
    {
        public async Task<HttpResponseMessage> GetData(string url)
        {
            Debug.WriteLine($"url: {url}");
            var client = new HttpClient();
            return await client.GetAsync(url);
        }
        public async Task<string> GetStringData(string url)
        {
            Debug.WriteLine($"url: {url}");
            var client = new HttpClient();
            return await client.GetStringAsync(url);
        }
    }
}
