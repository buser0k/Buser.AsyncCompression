using System;
using System.IO;

namespace Booser.AsyncCompression.Domain.ValueObjects
{
    public class FileInfo
    {
        public string FullPath { get; }
        public string Name { get; }
        public long Size { get; }
        public bool Exists { get; }

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

        public void Delete()
        {
            if (Exists)
            {
                System.IO.File.Delete(FullPath);
            }
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
