#if TOOLS

using System.Collections.Generic;
using System.IO;
using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginMods : IPluginSystem
    {
        [EditorSettingAction]
        private static void PrintModsList()
        {
            List<ModManifest> mods = EnabledModsList;

            foreach (var mod in mods)
            {
                Singleton.Log("MOD: " + mod.ModId);
            }
        }

        private static EditorSettingVariableBase _selectedMods = new()
        {
            VariantEditorDefault = 1,
            PropertyHint = PropertyHint.Flags,
            HintText = string.Empty,
            // Custom setter here is needed for custom "Everything" selector functionality
            CustomSetter = (previousValue, currentValue) =>
            {
                uint previous = previousValue.As<uint>();
                uint current = currentValue.As<uint>();

                // Always select "Everything" if no mods were selected
                // OR
                // If we set "Everything" for current value that was missing previously
                if (current == 0 || ((previous & 1) == 0 && (current & 1) != 0))
                {
                    current = 1;
                }
                // If we previously had only "Everything" selected but now we have another
                // value present - remove "Everything" from the value
                else if (previous == 1)
                {
                    if ((current & 1) != 0 && current != 1)
                    {
                        current &= ~(1u << 0);
                    }
                }

                return current;
            }
        };

        public static List<ModManifest> AllModsList { get; private set; } = [];

        public static List<ModManifest> EnabledModsList
        {
            get
            {
                List<ModManifest> result = [];

                uint value = _selectedMods.VariantValue.As<uint>();
                int counter = 1;

                foreach (var mod in AllModsList)
                {
                    if (value == 1)
                    {
                        result.Add(mod);
                    }
                    else if ((value & (1 << counter)) != 0)
                    {
                        result.Add(mod);
                    }

                    counter++;
                }

                return result;
            }
        }

        public static Dictionary<string, ModManifest> AllModsDictionary { get; private set; } = [];

        public static Dictionary<string, ModManifest> EnabledModsDictionary
        {
            get
            {
                List<ModManifest> targetMods = EnabledModsList;

                Dictionary<string, ModManifest> result = [];

                foreach (var mod in targetMods)
                {
                    result.Add(mod.ModId, mod);
                }

                return result;
            }
        }

        private static (uint, string) RemapFields(uint previousValue, string previousInput, List<string> currentInput)
        {
            string hintString = string.Empty;
            int counter = 1;
            int index = 0;

            foreach (var mod in currentInput)
            {
                if (index != 0)
                {
                    hintString += ',';
                }

                hintString += mod;
                hintString += ':';
                hintString += counter;

                counter <<= 1;
                index++;

                if (index >= 32)
                {
                    break;
                }
            }

            if (hintString == previousInput)
            {
                return (previousValue, hintString);
            }

            return (1, hintString);
        }

        [EditorSettingAction]
        private static void RefreshMods()
        {
            EditorFileSystem engineFilesystem = EditorInterface.Singleton.GetResourceFilesystem();

            HashSet<ModManifest> newManifests = [];
            HashSet<string> deletedManifests = [];

            string[] directories = Directory.GetDirectories(".", "*.*", SearchOption.TopDirectoryOnly);

            foreach (var directory in directories)
            {
                string path = directory.Replace("." + Path.DirectorySeparatorChar, "");

                if (path.StartsWith('.'))
                {
                    continue;
                }

                string modId = path;
                string manifestPath = modId + '/' + modId + ".tres";

                if (!ResourceLoader.Singleton.Exists(manifestPath, nameof(ModManifest)))
                {
                    deletedManifests.Add(modId);
                    continue;
                }

                string resourceType = manifestPath.GetScriptType();

                if (resourceType != nameof(ModManifest))
                {
                    deletedManifests.Add(modId);
                    continue;
                }

                if (AllModsDictionary.ContainsKey(modId))
                {
                    continue;
                }

                ModManifest manifest = (ModManifest)ResourceLoader.Singleton.Load(manifestPath);

                if (manifest != null && manifest.ModId == modId)
                {
                    newManifests.Add(manifest);
                }
            }

            bool listChanged = false;

            foreach (var newManifest in newManifests)
            {
                listChanged = true;
                AllModsList.Add(newManifest);
                AllModsDictionary.Add(newManifest.ModId, newManifest);
            }

            foreach (var deletedManifest in deletedManifests)
            {
                if (AllModsDictionary.TryGetValue(deletedManifest, out var manifest))
                {
                    listChanged = true;
                    AllModsList.Remove(manifest);
                    AllModsDictionary.Remove(deletedManifest);
                }
            }

            if (!listChanged)
            {
                return;
            }

            AllModsList.Sort((x, y) => x.ModId.CompareTo(y.ModId));

            List<string> _currentMods = ["Everything"];

            foreach (var mod in AllModsList)
            {
                _currentMods.Add(mod.ModId);
            }

            uint previousValue = _selectedMods.VariantValue.As<uint>();

            (var newValue, var newString) = RemapFields(previousValue, _selectedMods.HintText, _currentMods);

            if (_selectedMods.HintText == newString)
            {
                return;
            }

            _selectedMods.VariantValue = newValue;
            _selectedMods.HintText = newString;
            PluginDock.Singleton.UpdateInspector(_selectedMods);
        }

        bool ISystem.PostInitialize()
        {
            AllModsList = [];
            AllModsDictionary = [];
            RefreshMods();
            return true;
        }

        void ISystem.Deinitialize()
        {
            AllModsList = null;
            AllModsDictionary = null;
        }
    }
}

#endif
