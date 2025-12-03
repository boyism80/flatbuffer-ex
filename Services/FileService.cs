namespace FlatBufferEx.Services
{
    /// <summary>
    /// Implementation of file system operations
    /// </summary>
    public class FileService
    {
        /// <summary>
        /// Reads all text from a file asynchronously
        /// </summary>
        public async Task<string> ReadAllTextAsync(string path)
        {
            return await File.ReadAllTextAsync(path);
        }

        /// <summary>
        /// Writes all text to a file asynchronously
        /// </summary>
        public async Task WriteAllTextAsync(string path, string content)
        {
            var directory = GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
            {
                CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(path, content);
        }

        /// <summary>
        /// Checks if a directory exists
        /// </summary>
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Creates a directory
        /// </summary>
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Deletes a directory and all its contents
        /// </summary>
        public void DeleteDirectory(string path)
        {
            if (DirectoryExists(path))
            {
                Directory.Delete(path, true);
            }
        }

        /// <summary>
        /// Gets files matching a search pattern
        /// </summary>
        public string[] GetFiles(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern);
        }

        /// <summary>
        /// Combines path components
        /// </summary>
        public string CombinePath(params string[] paths)
        {
            return Path.Combine(paths);
        }

        /// <summary>
        /// Gets the full path for a relative path
        /// </summary>
        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Gets the directory name from a file path
        /// </summary>
        public string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path) ?? string.Empty;
        }
    }
}