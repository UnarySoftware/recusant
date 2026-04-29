#if TOOLS

using Godot;
using System.IO;

namespace Unary.Core
{
    [Tool]
    public partial class CachePath
    {
        private const string _cacheFolder = ".unary";

        static void TryInitialize()
        {
            if (!Directory.Exists(_cacheFolder))
            {
                Directory.CreateDirectory(_cacheFolder);
            }
        }

        public static string GetDirectory()
        {
            TryInitialize();
            return _cacheFolder;
        }

        public static string GetDirectoryPath(string path)
        {
            TryInitialize();
            return Path.Combine(_cacheFolder, path).Replace('\\', '/');
        }

        public static string GetFilePath(string path)
        {
            TryInitialize();
            return Path.Combine(_cacheFolder, path).Replace('\\', '/');
        }
    }
}

#endif
