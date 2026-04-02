using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class ResourceManager : Node, ICoreSystem
    {
        bool ISystem.Initialize()
        {
#if !TOOLS
            return true;
#endif

            HashSet<string> mods = [];

            var targetMods = ModLoader.Singleton.EnabledMods;

            foreach (var targetMod in targetMods)
            {
                mods.Add(targetMod.ModId);
            }

            string[] directories = Directory.GetDirectories(".");

            foreach (var directory in directories)
            {
                string modId = Path.GetFileName(directory);

                if (File.Exists(modId + '/' + modId + ".tres"))
                {
                    mods.Add(modId);
                }
            }

            BuildFilesystem filesystem = BuildFilesystem.Singleton;

            ConcurrentDictionary<string, FilesystemCache.ChangeType> changes = filesystem.Filesystem.GetDelta();

            var baseTypes = Types.GetTypesOfBase(typeof(BaseResource));

            Dictionary<string, Dictionary<string, (FilesystemCache.ChangeType, string)>> relevantChanges = [];

            // Filter out relevant changes only
            foreach (var change in changes)
            {
                string[] parts = change.Key.Split(Path.DirectorySeparatorChar);

                if (parts.Length < 2)
                {
                    continue;
                }

                string modId = parts[0];

                if (!mods.Contains(modId))
                {
                    continue;
                }

                if (!parts[^1].EndsWith(".tres"))
                {
                    continue;
                }

                // Skip overrides folder
                if (parts[1] == ContentSwapper.OverrideFolder)
                {
                    continue;
                }

                if (!relevantChanges.TryGetValue(modId, out var entries))
                {
                    entries = [];
                    relevantChanges[modId] = entries;
                }

                string path = "res://" + change.Key.Replace(Path.DirectorySeparatorChar, '/');

                if (change.Value == FilesystemCache.ChangeType.Removed)
                {
                    entries[path] = (change.Value, null);
                }
                else
                {
                    string scriptType = path.GetScriptType();

                    Type namedType = Types.GetTypeOfName(scriptType);

                    if (namedType == null)
                    {
                        this.Warning($"Tried adding an unknown type \"{scriptType}\" which does not have a [GlobalClass] attribute on it to the resource manifest, skipping");

#if TOOLS
                        Debug.Assert(false);
#endif
                        continue;
                    }
                    else if (!baseTypes.Contains(namedType))
                    {
                        this.Warning($"Tried adding a type \"{namedType.FullName}\" which does not inherit Unary.Core.BaseResource to the resource manifest, skipping");

#if TOOLS
                        Debug.Assert(false);
#endif

                        continue;
                    }

                    entries[path] = (change.Value, path.GetScriptType());
                }
            }

            // Now process all the changes
            foreach (var mod in relevantChanges)
            {
                string modId = mod.Key;

                string manifestPath = modId + '/' + modId + ResourceTypesManifest.Extension;

                if (!File.Exists(manifestPath))
                {
                    ResourceTypesManifest manifest = new();

                    int readCounter = 0;

                    foreach (var change in mod.Value)
                    {
                        if (change.Value.Item1 != FilesystemCache.ChangeType.Removed)
                        {
                            readCounter++;
                        }
                    }

                    manifest.Paths = new string[readCounter];
                    manifest.Types = new string[readCounter];

                    int writeCounter = 0;

                    foreach (var change in mod.Value)
                    {
                        if (change.Value.Item1 != FilesystemCache.ChangeType.Removed)
                        {
                            string path = change.Key;
                            manifest.Paths[writeCounter] = path;
                            manifest.Types[writeCounter] = change.Value.Item2;
                            writeCounter++;
                        }
                    }

                    File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest));
                }
                else
                {
                    ResourceTypesManifest manifest = JsonSerializer.Deserialize<ResourceTypesManifest>(File.ReadAllText(manifestPath));

                    manifest.Paths ??= [];
                    manifest.Types ??= [];

                    Dictionary<string, string> previousData = [];

                    for (int i = 0; i < manifest.Paths.Length; i++)
                    {
                        previousData[manifest.Paths[i]] = manifest.Types[i];
                    }

                    foreach (var change in mod.Value)
                    {
                        if (change.Value.Item1 == FilesystemCache.ChangeType.Added ||
                        change.Value.Item1 == FilesystemCache.ChangeType.Modified)
                        {
                            if (!previousData.ContainsKey(change.Key))
                            {
                                previousData[change.Key] = change.Value.Item2;
                            }
                        }
                        // Removed
                        else
                        {
                            previousData.Remove(change.Key);
                        }
                    }

                    manifest.Paths = new string[previousData.Count];
                    manifest.Types = new string[previousData.Count];

                    int writeCounter = 0;

                    foreach (var data in previousData)
                    {
                        manifest.Paths[writeCounter] = data.Key;
                        manifest.Types[writeCounter] = data.Value;
                        writeCounter++;
                    }

                    File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest));
                }
            }

            return true;
        }
    }
}
