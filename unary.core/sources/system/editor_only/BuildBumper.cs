using System;
using System.IO;
using System.Text.Json;
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

            foreach (var mod in mods)
            {
                string buildPath = mod.Key + '/' + mod.Key + BuildManifest.Extension;

                BuildManifest buildManifest;

                if (File.Exists(buildPath))
                {
                    if (!changedMods.Contains(mod.Key))
                    {
                        continue;
                    }

                    buildManifest = JsonSerializer.Deserialize<BuildManifest>(File.ReadAllText(buildPath));
                }
                else
                {
                    buildManifest = new();
                }

                buildManifest.BuildNumber++;
                buildManifest.BuildData = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

                File.WriteAllText(buildPath, JsonSerializer.Serialize(buildManifest));

                if (mod.Value.BuildManifest == null)
                {
                    mod.Value.BuildManifest = buildManifest;
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
