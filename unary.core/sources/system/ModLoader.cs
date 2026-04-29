using Godot;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Unary.Core
{
    public partial class ModLoader : Node, ICoreSystem
    {
        public ModLoadManifest LoadManifest { get; private set; }

        public Dictionary<string, ModManifest> AllMods { get; private set; } = [];
        public List<ModManifest> EnabledMods { get; private set; } = [];

        private bool FetchLoadOrder()
        {
            if (!File.Exists(ModLoadManifest.Path))
            {
                this.Critical($"Missing {ModLoadManifest.Path}, we cant launch the game without it.");
                return false;
            }

            LoadManifest = JsonSerializer.Deserialize<ModLoadManifest>(File.ReadAllText(ModLoadManifest.Path));

            if (LoadManifest == null || LoadManifest.Enabled.Count == 0)
            {
                return this.Critical("Failed to acquire a load order");
            }

            return true;
        }

        private bool EditorFilesystem()
        {
            string[] directories = Directory.GetDirectories(".");

            foreach (var directory in directories)
            {
                string modId = Path.GetFileName(directory);

                string target = modId + '/' + modId + ".tres";

                if (!File.Exists(target))
                {
                    continue;
                }

                if (target.GetScriptType() != nameof(ModManifest))
                {
                    continue;
                }

                ModManifest manifest = (ModManifest)ResourceLoader.Singleton.Load(target, nameof(ModManifest));

                if (manifest == null)
                {
                    return this.Critical($"Failed to load mod manifest at path \"{target}\"");
                }

                if (AllMods.ContainsKey(manifest.ModId))
                {
                    return this.Critical($"Failed to load mod \"{manifest.ModId}\" since its already loaded");
                }

                string buildPath = modId + '/' + modId + BuildManifest.Extension;

#if !TOOLS

                if (!File.Exists(buildPath))
                {
                    return this.Critical($"Mod \"{modId}\" is missing a build manifest");
                }

#else

                if (File.Exists(buildPath))
                {
                    manifest.BuildManifest = JsonSerializer.Deserialize<BuildManifest>(File.ReadAllText(buildPath));
                }

#endif

                manifest.LoadInfo = new()
                {
                    Type = ModLoadInfo.ModPathType.Filesystem,
                    Path = manifest.ModId
                };

                AllMods[manifest.ModId] = manifest;
            }

            return true;
        }

        private bool SortModList()
        {
            Dictionary<string, HashSet<string>> modToDependency = [];

            foreach (var mod in AllMods)
            {
                HashSet<string> dependencies = [];

                ModVersion modVersion = new();

                if (!modVersion.TryParse(mod.Value.Version))
                {
                    return this.Critical($"Mod \"{mod.Key}\" has an invalid mod version \"{modVersion}\"");
                }

                foreach (var dependentMod in mod.Value.Dependencies)
                {
                    if (dependencies.Contains(dependentMod.ModId))
                    {
                        return this.Critical($"Mod \"{mod.Key}\" had a duplicate dependency for \"{dependentMod.ModId}\"");
                    }

                    if (dependentMod.Required && !AllMods.ContainsKey(dependentMod.ModId))
                    {
                        return this.Critical($"Mod \"{dependentMod.ModId}\" is missing, which is a hard dependency of \"{mod.Value.ModId}\"");
                    }

                    ModVersionSelector dependencyVersion = new();

                    if (!dependencyVersion.TryParse(dependentMod.Version))
                    {
                        return this.Critical($"Dependency version for \"{dependentMod.ModId}\" within mod \"{mod.Key}\" has an invalid mod version \"{dependentMod.Version}\"");
                    }

                    if (AllMods.TryGetValue(dependentMod.ModId, out var depMod))
                    {
                        ModVersion depModVersion = new();
                        if (depModVersion.TryParse(depMod.Version) && dependencyVersion.InRange(depModVersion))
                        {
                            dependencies.Add(dependentMod.ModId);
                        }
                    }
                }

                modToDependency[mod.Key] = dependencies;
            }

            List<TopoSortItem<string>> modIds = [];

            foreach (var modData in modToDependency)
            {
                modIds.Add(new TopoSortItem<string>(modData.Key, [.. modData.Value]));
            }

            List<TopoSortItem<string>> sortedMods = [.. modIds.TopoSort(x => x.Target, x => x.Dependencies)];

            Dictionary<ModLoadInfo, string> infoToModId = [];

            foreach (var mod in sortedMods)
            {
                var targetMod = AllMods[mod.Target];

                // If we have managed to remove a mod from the load order - it is present and should be added as enabled
                if (LoadManifest.Enabled.Remove(targetMod.LoadInfo))
                {
                    EnabledMods.Add(AllMods[mod.Target]);
                }
            }

            // If some mods failed to be removed from the load manifest in the loop above - they are missing
            if (LoadManifest.Enabled.Count > 0)
            {
                StringBuilder builder = new();

                builder.Append("Failed to aquire mods listed in the load order:");

                foreach (var missing in LoadManifest.Enabled)
                {
                    builder.Append("\nPath: \"").Append(missing.Path).Append("\" from ").Append(missing.Type);
                }

                return this.Critical(builder.ToString());
            }

            return true;
        }

        private bool FetchModList()
        {
#if TOOLS
            return EditorFilesystem();
#else
            return EditorFilesystem();
#endif
        }

        bool ISystem.Initialize()
        {
            return FetchLoadOrder() && FetchModList() && SortModList();
        }

        void ISystem.Deinitialize()
        {

        }
    }
}
