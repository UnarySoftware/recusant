using Godot;

namespace Unary.Core
{
    public partial class BuildFilesystem : Node, ICoreSystem
    {
        public FilesystemCache Filesystem { get; private set; } = new("build_filesystem.bin", RuntimeLogger.Critical);

        bool ISystem.Initialize()
        {
#if !TOOLS
            return true;
#endif

            Filesystem.Initialize();
            return true;
        }

        bool ISystem.PostInitialize()
        {
#if !TOOLS
            return true;
#endif

            Filesystem.ResetChanges();
            return true;
        }
    }
}
