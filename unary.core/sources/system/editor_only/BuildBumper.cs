using System;
using Godot;

namespace Unary.Core
{
    public partial class BuildBumper : Node, ICoreSystem
    {
        bool ISystem.Initialize()
        {
#if !TOOLS
            return true;
#endif

            BuildFilesystem filesystem = BuildFilesystem.Singleton;

            var changedMods = filesystem.Filesystem.GetChangedMods();

            var mods = ModLoader.Singleton.AllMods;

            foreach (var changedId in changedMods)
            {
                if (!mods.TryGetValue(changedId, out var manifest))
                {
                    continue;
                }

                string buildPath = "res://" + changedId + '/' + EditorPaths.BuildManifestPath;
                BuildManifest buildManifest;

                if (!ResourceLoader.Exists(buildPath))
                {
                    ResourceSaver.Save(new BuildManifest(), buildPath);
                }

                buildManifest = (BuildManifest)ResourceLoader.Load(buildPath, nameof(BuildManifest));
                buildManifest.BuildNumber++;
                buildManifest.BuildData = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                ResourceSaver.Save(buildManifest, buildPath);

                if (manifest.BuildManifest != buildManifest)
                {
                    manifest.BuildManifest = buildManifest;
                    ResourceSaver.Save(manifest, manifest.ResourcePath);
                }
            }

            return true;
        }

        bool ISystem.PostInitialize()
        {
            return true;
        }
    }
}
