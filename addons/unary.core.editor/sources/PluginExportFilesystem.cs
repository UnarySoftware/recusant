#if TOOLS
using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginExportFilesystem : IPluginSystem
    {
        public FilesystemCache Filesystem { get; private set; } = new("export_filesystem.bin", PluginLogger.Critical);

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
