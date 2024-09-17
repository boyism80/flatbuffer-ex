namespace FlatBufferEx.Util
{
    public static class Http
    {
        public static async Task DownloadFile(string url, string path)
        {
            using var client = new HttpClient();
            using var s = await client.GetStreamAsync(url);
            using var fs = new FileStream(path, FileMode.Create);
            s.CopyTo(fs);
        }
    }
}
