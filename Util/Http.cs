namespace FlatBufferEx.Util
{
    /// <summary>
    /// Utility class for HTTP operations
    /// Provides methods for downloading files from remote URLs
    /// </summary>
    public static class Http
    {
        /// <summary>
        /// Downloads a file from the specified URL and saves it to the local path
        /// </summary>
        /// <param name="url">URL of the file to download</param>
        /// <param name="path">Local file path where the downloaded file will be saved</param>
        /// <returns>Task representing the asynchronous download operation</returns>
        public static async Task DownloadFile(string url, string path)
        {
            using var client = new HttpClient();
            using var s = await client.GetStreamAsync(url);
            using var fs = new FileStream(path, FileMode.Create);
            s.CopyTo(fs);
        }
    }
}
