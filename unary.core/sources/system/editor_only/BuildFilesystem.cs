#if TOOLS

using Godot;

namespace Unary.Core
{
    public partial class BuildFilesystem : Node, ICoreSystem
    {
        public FilesystemCache Filesystem { get; private set; } = new("build_filesystem.bin", RuntimeLogger.Critical);

        bool ISystem.Initialize()
        {
            Filesystem.Initialize();
            return true;
        }

        bool ISystem.PostInitialize()
        {
            Filesystem.ResetChanges();
            return true;
        }
    }
}

#endif
