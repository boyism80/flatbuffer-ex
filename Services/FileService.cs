namespace FlatBufferEx.Services
{
    /// <summary>
    /// Implementation of file system operations
    /// </summary>
    public class FileService : IFileService
    {
        /// <inheritdoc />
        public async Task<string> ReadAllTextAsync(string path)
        {
            return await File.ReadAllTextAsync(path);
        }

        /// <inheritdoc />
        public async Task WriteAllTextAsync(string path, string content)
        {
            var directory = GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
            {
                CreateDirectory(directory);
            }
            
            await File.WriteAllTextAsync(path, content);
        }

        /// <inheritdoc />
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <inheritdoc />
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        /// <inheritdoc />
        public void DeleteDirectory(string path)
        {
            if (DirectoryExists(path))
            {
                Directory.Delete(path, true);
            }
        }

        /// <inheritdoc />
        public string[] GetFiles(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern);
        }

        /// <inheritdoc />
        public string CombinePath(params string[] paths)
        {
            return Path.Combine(paths);
        }

        /// <inheritdoc />
        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        /// <inheritdoc />
        public string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path) ?? string.Empty;
        }
    }
} 