using System.IO;

namespace Application
{
    public static class Directory
    {
        public static string[] GetFiles(string path, string serachPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (string.IsNullOrEmpty(path))
                return new string[] { };
            if (((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory))
                return System.IO.Directory.GetFiles(path, serachPattern, searchOption);
            else
                return new[] { path };
        }
    }
}