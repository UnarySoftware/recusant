#if TOOLS
using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginTrenchbroom : IPluginSystem
    {
        const string TargetFolder = ".trenchbroom";
        const string FakeSlash = " . ";

        public static EditorSettingVariable<string[]> TargetFolders = new()
        {
            EditorDefault = ["materials"],
            RuntimeDefault = [],
            PropertyHint = PropertyHint.ArrayType,
            HintText = "String"
        };

        bool ISystem.Initialize()
        {
            PluginMods.Singleton.OnRefresh += UpdatePaths;

            return true;
        }

        void ISystem.Deinitialize()
        {
            PluginMods.Singleton.OnRefresh -= UpdatePaths;
        }

        [EditorSettingAction]
        private static void UpdatePaths()
        {
            if (Directory.Exists(TargetFolder))
            {
                Directory.Delete(TargetFolder, true);
            }

            Directory.CreateDirectory(TargetFolder);

            HashSet<string> result = [];

            foreach (var modManifest in PluginMods.Singleton.AllModsList)
            {
                foreach(var folder in TargetFolders.Value)
                {
                    string target = modManifest.ModId + '/' + folder;

                    if(Directory.Exists(target))
                    {
                        result.Add(target);
                    }
                }
            }

            string root = Directory.GetCurrentDirectory();

            foreach (var directory in result)
            {
                string fakeDirectory = directory.Replace("/", FakeSlash);
                Directory.CreateSymbolicLink(TargetFolder + '/' + fakeDirectory, root + '/' + directory);
                Singleton.Log(directory);
            }
        }
    }
}

#endif
