using System;
using System.IO;

namespace Buser.AsyncCompression.Domain.ValueObjects
{
    /// <summary>
    /// Represents information about a file. This is a value object that encapsulates file metadata.
    /// </summary>
    public class FileInfo
    {
        /// <summary>
        /// Gets the full path of the file.
        /// </summary>
        public string FullPath { get; }
        
        /// <summary>
        /// Gets the name of the file (without path).
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets the size of the file in bytes. Returns 0 if the file does not exist.
        /// </summary>
        public long Size { get; }
        
        /// <summary>
        /// Gets a value indicating whether the file exists.
        /// </summary>
        public bool Exists { get; }

        /// <summary>
        /// Initializes a new instance of the FileInfo class.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <exception cref="ArgumentException">Thrown when path is null or empty.</exception>
        public FileInfo(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("File path cannot be null or empty", nameof(path));

            FullPath = System.IO.Path.GetFullPath(path);
            Name = System.IO.Path.GetFileName(path);
            
            var fileInfo = new System.IO.FileInfo(FullPath);
            Exists = fileInfo.Exists;
            Size = Exists ? fileInfo.Length : 0;
        }

        public override bool Equals(object? obj)
        {
            return obj is FileInfo other && FullPath.Equals(other.FullPath, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return FullPath.GetHashCode();
        }

        public override string ToString()
        {
            return FullPath;
        }
    }
}
