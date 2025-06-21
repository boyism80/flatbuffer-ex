namespace FlatBufferEx.Services
{
    /// <summary>
    /// Interface for file system operations
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Reads all text from a file
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>File content</returns>
        Task<string> ReadAllTextAsync(string path);

        /// <summary>
        /// Writes text to a file
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="content">Content to write</param>
        Task WriteAllTextAsync(string path, string content);

        /// <summary>
        /// Checks if a directory exists
        /// </summary>
        /// <param name="path">Directory path</param>
        /// <returns>True if directory exists</returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Creates a directory if it doesn't exist
        /// </summary>
        /// <param name="path">Directory path</param>
        void CreateDirectory(string path);

        /// <summary>
        /// Deletes a directory and all its contents
        /// </summary>
        /// <param name="path">Directory path</param>
        void DeleteDirectory(string path);

        /// <summary>
        /// Gets files in a directory matching a pattern
        /// </summary>
        /// <param name="path">Directory path</param>
        /// <param name="searchPattern">Search pattern</param>
        /// <returns>Array of file paths</returns>
        string[] GetFiles(string path, string searchPattern);

        /// <summary>
        /// Combines paths
        /// </summary>
        /// <param name="paths">Path components</param>
        /// <returns>Combined path</returns>
        string CombinePath(params string[] paths);

        /// <summary>
        /// Gets the full path
        /// </summary>
        /// <param name="path">Relative or absolute path</param>
        /// <returns>Full path</returns>
        string GetFullPath(string path);

        /// <summary>
        /// Gets the directory name from a path
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>Directory name</returns>
        string GetDirectoryName(string path);
    }
} 