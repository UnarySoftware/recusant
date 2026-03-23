
namespace Unary.Core
{
    public struct ModPathInfo
    {
        public enum ModPathType
        {
            EditorFilesystem,
            ModsFolder,
            Steam
        };

        public ModPathType Type;
        public string Path;
    }
}
