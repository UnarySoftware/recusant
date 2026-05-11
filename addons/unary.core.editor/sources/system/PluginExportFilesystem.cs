#if TOOLS
using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginExportFilesystem : IPluginSystem
    {
        public FilesystemCache Filesystem { get; private set; } = new("export_filesystem.bin");

        bool ISystem.Initialize()
        {
            Filesystem.Initialize();
            return true;
        }

        bool IPluginSystem.PostExport()
        {
            return Filesystem.ResetChanges();
        }
    }
}

#endif
