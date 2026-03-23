using System.Collections.Generic;
using System.IO;
using Godot;

namespace Unary.Core
{
    public partial class ModLoader : Node, ICoreSystem
    {
        public const string ModLoaderManifestFile = nameof(LoadOrderManifest) + ".tres";

        public LoadOrderManifest LoadOrder { get; private set; }

        public Dictionary<string, ModManifest> AllMods { get; private set; } = [];
        public List<ModManifest> EnabledMods { get; private set; } = [];

        private bool FetchLoadOrder()
        {
            if (!File.Exists(ModLoaderManifestFile))
            {
                this.Critical($"Missing {ModLoaderManifestFile}, we cant launch a game without it.");
                return false;
            }

            LoadOrder = (LoadOrderManifest)ResourceLoader.Singleton.Load(ModLoaderManifestFile, nameof(LoadOrderManifest));

            if (LoadOrder == null || LoadOrder.Enabled.Length == 0)
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
                    this.Critical($"Failed to load mod manifest at path \"{target}\"");
                    return false;
                }

                if (AllMods.ContainsKey(manifest.ModId))
                {
                    this.Critical($"Failed to load mod \"{manifest.ModId}\" since its already loaded");
                    return false;
                }

                manifest.PathInfo = new()
                {
                    Type = ModPathInfo.ModPathType.EditorFilesystem,
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

            foreach (var mod in sortedMods)
            {
                EnabledMods.Add(AllMods[mod.Target]);
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
