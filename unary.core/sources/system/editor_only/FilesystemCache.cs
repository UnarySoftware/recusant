using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;

namespace Unary.Core
{
    [Tool]
    public class FilesystemCache(string path, Func<object, string, bool> reporter)
    {
        private Func<object, string, bool> _reporter = reporter;

        public string Path { get; private set; } = path;

        public enum ChangeType
        {
            Added,
            Removed,
            Modified
        };

        public static readonly string DirectorySearchStart = "." + System.IO.Path.DirectorySeparatorChar + ".";

        public void Initialize()
        {
            bool missing = false;

            if (!File.Exists(CachePath.GetFilePath(Path)))
            {
                missing = true;
            }

            if (missing)
            {
                ConcurrentDictionary<string, ChangeType> result = new();
                ConcurrentDictionary<string, ulong> state = GetState();
                using (var cacheFile = new FileStream(CachePath.GetFilePath(Path), FileMode.OpenOrCreate))
                using (var binaryWriter = new BinaryWriter(cacheFile))
                {
                    WriteState(state, binaryWriter);
                }

                _changes = [];
                foreach (var change in state)
                {
                    _changes[change.Key] = ChangeType.Added;
                }

                Multi.Thread(state, entry =>
                {
                    result.TryAdd(entry.Key, ChangeType.Added);
                });
            }
        }

        private ConcurrentDictionary<string, ulong> GetState()
        {
            string root = Directory.GetCurrentDirectory();

            ConcurrentHashSet<string> targetFiles = [];

            {
                ConcurrentHashSet<string> unsortedFiles = [];
                List<string> ignoreDirectory = [];
                object locker = new();

                string[] directories = Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly);

                Multi.Thread(directories, directory =>
                {
                    if (System.IO.Path.GetFileName(directory).StartsWith('.'))
                    {
                        return;
                    }

                    string[] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

                    foreach (var file in files)
                    {
                        string fileName = System.IO.Path.GetFileName(file);

                        if (fileName == ".gdignore")
                        {
                            lock (locker)
                            {
                                ignoreDirectory.Add(System.IO.Path.GetDirectoryName(file));
                                continue;
                            }
                        }

                        unsortedFiles.Add(file);
                    }
                });

                Multi.Thread(unsortedFiles, file =>
                {
                    foreach (var ignore in ignoreDirectory)
                    {
                        if (file.StartsWith(ignore))
                        {
                            return;
                        }
                    }

                    targetFiles.Add(file);
                });
            }

            var finalResults = new ConcurrentDictionary<string, ulong>();
            object lockObj = new();

            root += System.IO.Path.DirectorySeparatorChar;
            int rootLength = root.Length;

            Multi.Thread(targetFiles, filePath =>
            {
                try
                {
                    using var md5 = MD5.Create();
                    using var stream = File.OpenRead(filePath);

                    finalResults.TryAdd(filePath.Substr(rootLength, filePath.Length - rootLength), BitConverter.ToUInt64(md5.ComputeHash(stream).AsSpan()));
                }
                catch (Exception ex)
                {
                    lock (lockObj)
                    {
                        _reporter(this, ex.Message);
                    }
                }

            });

            return finalResults;
        }

        private static void WriteState(ConcurrentDictionary<string, ulong> entries, BinaryWriter writer)
        {
            writer.Write(entries.Count);

            foreach (var kvp in entries)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        private static ConcurrentDictionary<string, ulong> ReadState(BinaryReader reader)
        {
            ConcurrentDictionary<string, ulong> result = [];

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadString();
                ulong value = reader.ReadUInt64();
                result[key] = value;
            }

            return result;
        }

        public bool ResetChanges()
        {
            _changes = null;
            return true;
        }

        private ConcurrentDictionary<string, ChangeType> _changes;

        public ConcurrentDictionary<string, ChangeType> GetDelta()
        {
            _changes ??= CalculateDelta();
            return _changes;
        }

        public ConcurrentHashSet<string> GetChangedMods()
        {
            ConcurrentHashSet<string> result = [];
            ConcurrentDictionary<string, ChangeType> changes = GetDelta();

            Multi.Thread(changes, entry =>
            {
                string[] parts = entry.Key.Split(System.IO.Path.DirectorySeparatorChar);

                if (parts.Length < 1)
                {
                    return;
                }

                string modId = parts[0];

                if (!modId.Contains('.'))
                {
                    return;
                }

                string type = entry.Key.GetScriptType();

                if (type == nameof(ModManifest))
                {
                    return;
                }

                result.Add(modId);
            });

            return result;
        }

        private ConcurrentDictionary<string, ChangeType> CalculateDelta()
        {
            ConcurrentDictionary<string, ChangeType> result = new();

            ConcurrentDictionary<string, ulong> state = GetState();

            ConcurrentDictionary<string, ulong> previousState;

            using (var cacheFile = new FileStream(CachePath.GetFilePath(Path), FileMode.Open))
            using (var binaryReader = new BinaryReader(cacheFile))
            {
                previousState = ReadState(binaryReader);
            }

            Multi.Thread(state, entry =>
            {
                if (previousState.TryGetValue(entry.Key, out ulong previousMd5))
                {
                    if (entry.Value != previousMd5)
                    {
                        result.TryAdd(entry.Key, ChangeType.Modified);
                    }
                }
                else
                {
                    result.TryAdd(entry.Key, ChangeType.Added);
                }
            });

            Multi.Thread(previousState, previousEntry =>
            {
                if (!state.ContainsKey(previousEntry.Key))
                {
                    result.TryAdd(previousEntry.Key, ChangeType.Removed);
                }
            });

            using (var cacheFile = new FileStream(CachePath.GetFilePath(Path), FileMode.OpenOrCreate))
            using (var binaryWriter = new BinaryWriter(cacheFile))
            {
                WriteState(state, binaryWriter);
            }

            return result;
        }

    }
}
