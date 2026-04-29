#if TOOLS

using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class ContentSwapper : Node, ICoreSystem
    {
        private static EditorSettingVariable<bool> _swapContent = new()
        {
            EditorDefault = true,
            // No need for runtime swapping, we only need this for editor time
            RuntimeDefault = false
        };

        public static EditorSettingVariable<string[]> BannedExtensions = new()
        {
            EditorDefault = [".gdignore", ".patch", ".fgd", ".vmt", ".vtf", "gameinfo.txt", ".vmf", ".vmx", ".cs", ".uid", ".import"],
            RuntimeDefault = [],
            PropertyHint = PropertyHint.ArrayType,
            HintText = "String"
        };

        public const string OperationsFile = "swap_operations.tres";
        public const string OperationsFileRes = "res://" + OperationsFile;
        public const string TempFile = "temp";
        public const string OverrideFolder = "overrides";

        private bool _swapped = false;

        private static bool SwapFiles(string pathA, string pathB, Func<object, string, bool> reporter)
        {
            if (!File.Exists(pathA))
            {
                return reporter(typeof(ContentSwapper), $"Failed to swap file {pathA} since it does not exist");
            }

            if (!File.Exists(pathB))
            {
                return reporter(typeof(ContentSwapper), $"Failed to swap file {pathB} since it does not exist");
            }

            File.Move(pathA, TempFile);
            File.Move(pathB, pathA);
            File.Move(TempFile, pathB);

            return true;
        }

        private static bool SwapContent(ContentSwapManifest operations, Func<object, string, bool> reporter)
        {
            ResourceSaver.Singleton.Save(operations, OperationsFile, ResourceSaver.SaverFlags.None);

            for (int i = 0; i < operations.Originals.Length; i++)
            {
                string original = operations.Originals[i];
                string destination = operations.Replacements[i];
                if (!SwapFiles(original, destination, reporter))
                {
                    return false;
                }
            }

            return true;
        }

        // Is public and globally accessible since we also want to try swap stuff on plugin initialization
        public static bool TryRevertSwap(Func<object, string, bool> reporter)
        {
            if (!ResourceLoader.Singleton.Exists(OperationsFileRes, nameof(ContentSwapManifest)))
            {
                return true;
            }

            ContentSwapManifest newOperations = (ContentSwapManifest)ResourceLoader.Singleton.Load(OperationsFileRes, nameof(ContentSwapManifest));

            if (newOperations == null)
            {
                return reporter(typeof(ContentSwapper), "Failed to load content swap manifest");
            }

            if (newOperations.Originals == null)
            {
                return reporter(typeof(ContentSwapper), $"Invalid content swap manifest, {nameof(newOperations.Originals)} was null");
            }

            if (newOperations.Originals.Length == 0)
            {
                return reporter(typeof(ContentSwapper), $"Invalid content swap manifest, {nameof(newOperations.Originals)} was empty");
            }

            if (newOperations.Replacements == null)
            {
                return reporter(typeof(ContentSwapper), $"Invalid content swap manifest, {nameof(newOperations.Replacements)} was null");
            }

            if (newOperations.Replacements.Length == 0)
            {
                return reporter(typeof(ContentSwapper), $"Invalid content swap manifest, {nameof(newOperations.Replacements)} was empty");
            }

            if (newOperations.Originals.Length != newOperations.Replacements.Length)
            {
                return reporter(typeof(ContentSwapper), $"Invalid content swap manifest, {nameof(newOperations.Originals)} had {newOperations.Originals.Length} entries while {nameof(newOperations.Replacements)} had {newOperations.Replacements.Length}");
            }

            for (int i = 0; i < newOperations.Originals.Length; i++)
            {
                string original = newOperations.Originals[i];
                string destination = newOperations.Replacements[i];
                SwapFiles(original, destination, reporter);
            }

            File.Delete(OperationsFile);

            return true;
        }

        [InitializeExplicit(typeof(ResourceManager), typeof(Resources))]
        bool ISystem.Initialize()
        {
            if (!TryRevertSwap(RuntimeLogger.Critical))
            {
                return false;
            }

            if (!_swapContent.Value)
            {
                // We dont want to swap anything, skip
                return true;
            }

            Dictionary<string, string> replacements = [];

            foreach (var mod in ModLoader.Singleton.EnabledMods)
            {
                string directory = mod.ModId + '/' + OverrideFolder;
                int replacement = directory.Length + 1;

                if (!Directory.Exists(directory))
                {
                    continue;
                }

                string[] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    string replacementFile = file.Replace('\\', '/');

                    if (BannedExtensions.Value.Contains(Path.GetExtension(replacementFile)))
                    {
                        continue;
                    }

                    string original = replacementFile[replacement..];

                    replacements[original] = replacementFile;
                }
            }

            if (replacements.Count == 0)
            {
                // Nothing to swap
                _swapped = false;
                return true;
            }

            ContentSwapManifest newOperations = new()
            {
                Originals = new string[replacements.Count],
                Replacements = new string[replacements.Count],
            };

            int counter = 0;

            foreach (var replacement in replacements)
            {
                newOperations.Originals[counter] = replacement.Key;
                newOperations.Replacements[counter] = replacement.Value;
                counter++;
            }

            SwapContent(newOperations, RuntimeLogger.Critical);

            _swapped = true;

            return true;
        }

        void ISystem.Deinitialize()
        {
            if (_swapped)
            {
                TryRevertSwap(RuntimeLogger.Critical);
            }
        }
    }
}

#endif
