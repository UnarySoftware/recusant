using Godot;
using System.Collections.Generic;
using System.IO;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class ModManifest : BaseResource
    {
        [Export]
        public new string ModId;

        [Export]
        public string Version;

        public BuildManifest BuildManifest;

        [Export]
        public ulong SteamFileId;

        [Export]
        public ModManifestDependency[] Dependencies = [];

        [Export]
        public ModManifestSelector[] Incompatibilities = [];

        [Export]
        public ModManifestResolution[] Resolutions = [];

        // Resolved at game runtime
        public ModLoadInfo LoadInfo;

        /// <summary>
        /// Every mod manifest in the project, found by folder: a mod folder is one holding a manifest
        /// resource named after it. The scan runs fresh on every call and touches nothing but the
        /// filesystem, so callers keep working when a .NET domain reload has wiped the editor plugin's
        /// cached mod list. Sorted by mod id, so anything generated from it is stable between runs.
        /// </summary>
        public static List<ModManifest> ScanProject()
        {
            List<ModManifest> result = [];

            foreach (string directory in Directory.GetDirectories("."))
            {
                string modId = Path.GetFileName(directory);

                // Hidden folders, .godot above all, are never mods.
                if (modId.StartsWith('.'))
                {
                    continue;
                }

                string path = modId + '/' + modId + ".tres";

                if (!File.Exists(path) || path.GetScriptType() != nameof(ModManifest))
                {
                    continue;
                }

                if (ResourceLoader.Singleton.Load(path, nameof(ModManifest)) is not ModManifest manifest)
                {
                    GD.PushError($"Failed to load mod manifest at \"{path}\"");
                    continue;
                }

                // The folder name is the mod id everywhere else, so a manifest disagreeing with it would
                // make every path built from either one point somewhere different.
                if (manifest.ModId != modId)
                {
                    GD.PushError($"Mod manifest \"{path}\" declares mismatched mod id \"{manifest.ModId}\"");
                    continue;
                }

                result.Add(manifest);
            }

            result.Sort((left, right) => string.CompareOrdinal(left.ModId, right.ModId));

            return result;
        }

        public override void _ValidateProperty(Godot.Collections.Dictionary property)
        {
            property.MakeReadOnly(PropertyName.SteamFileId);
            base._ValidateProperty(property);
        }
    }
}
