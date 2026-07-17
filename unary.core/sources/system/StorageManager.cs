using Godot;
using System;
using System.Collections.Generic;
using System.IO;

namespace Unary.Core
{
    public partial class StorageManager : Node, ICoreSystem
    {
        // !!! DISCLAIMER !!!
        // Hardware fingerprinting is used exclusively for stuff like bound graphics settings per specific hardware configuration.
        // !!! DISCLAIMER !!!

        public byte[] FingerprintBytes { get; private set; }
        public string FingerprintString { get; private set; }

        // !!! DISCLAIMER !!!
        // Unique Id is used exclusively as a "safe to pass over the internet" unique identity token that gets consumed per-use and yet 
		// could NOT be traced back to the original holder directly while still being able to target them for things like developer gifted rewards.
        // !!! DISCLAIMER !!!

        public Guid UniqueId { get; private set; }
        public string UniqueIdString { get; private set; }
        // Set this to true after unique id could not be considered as consumed
        public bool UniqueIdConsumed { get; set; } = false;

        public const string Folder = "storage";
        public static string IdPath { get; } = Folder + '/' + nameof(StorageManager) + ".id";

        private void RerollUniqueId()
        {
            File.WriteAllText(IdPath, Guid.CreateVersion7().ToString());
        }

        [InitializeExplicit(typeof(ModLoader))]
        bool ISystem.Initialize()
        {
            FingerprintBytes = Engine.Singleton.SpecsHash();
            FingerprintString = Convert.ToHexString(FingerprintBytes);

            if (!Directory.Exists(Folder))
            {
                Directory.CreateDirectory(Folder);
                File.Create(Folder + "/.gdignore");
            }

            if (!File.Exists(IdPath))
            {
                RerollUniqueId();
            }

            UniqueIdString = File.ReadAllText(IdPath);

            if (!Guid.TryParse(UniqueIdString, out Guid id))
            {
                return this.Critical($"Failed parsing Guid \"{UniqueIdString}\" from file " + IdPath);
            }

            UniqueId = id;

            foreach (var mod in ModLoader.Singleton.EnabledMods)
            {
                string targetPath = Folder + '/' + mod.ModId;

                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }
            }

            return true;
        }

        void ISystem.Deinitialize()
        {
            if (UniqueIdConsumed)
            {
                RerollUniqueId();
            }
        }

        private static string TouchFolder(string modId, string type)
        {
            string result = Folder + '/' + modId + '/' + type;

            if (!Directory.Exists(result))
            {
                Directory.CreateDirectory(result);
            }

            return result;
        }

        private static string TouchFolder(string modId)
        {
            string result = Folder + '/' + modId;

            if (!Directory.Exists(result))
            {
                Directory.CreateDirectory(result);
            }

            return result;
        }

        public List<string> GetEntries(string modId, string type, string searchPattern = "*.*")
        {
            string path = TouchFolder(modId, type);

            string[] files = Directory.GetFiles(path, searchPattern);

            List<string> result = [];

            foreach (var file in files)
            {
                result.Add(Path.GetFileName(file));
            }

            return result;
        }

        public List<string> GetEntriesLocal(string modId, string searchPattern = "*.*")
        {
            return GetEntries(modId, FingerprintString, searchPattern);
        }

        public string ReadEntryText(string modId, string entry)
        {
            string path = TouchFolder(modId) + '/' + entry;

            if (!File.Exists(path))
            {
                return string.Empty;
            }

            return File.ReadAllText(path);
        }

        public string ReadEntryText(string modId, string type, string entry)
        {
            string path = TouchFolder(modId, type) + '/' + entry;

            if (!File.Exists(path))
            {
                return string.Empty;
            }

            return File.ReadAllText(path);
        }

        public string ReadEntryTextLocal(string modId, string entry)
        {
            return ReadEntryText(modId, FingerprintString, entry);
        }

        public byte[] ReadEntryBytes(string modId, string type, string entry)
        {
            string path = TouchFolder(modId, type) + '/' + entry;

            if (!File.Exists(path))
            {
                return [];
            }

            return File.ReadAllBytes(path);
        }

        public byte[] ReadEntryBytes(string modId, string entry)
        {
            string path = TouchFolder(modId) + '/' + entry;

            if (!File.Exists(path))
            {
                return [];
            }

            return File.ReadAllBytes(path);
        }

        public byte[] ReadEntryBytesLocal(string modId, string type, string entry)
        {
            return ReadEntryBytes(modId, FingerprintString, entry);
        }

        public void WriteEntryText(string modId, string type, string entry, string text)
        {
            string path = TouchFolder(modId, type) + '/' + entry;
            File.WriteAllText(path, text);
        }

        public void WriteEntryText(string modId, string entry, string text)
        {
            string path = TouchFolder(modId) + '/' + entry;
            File.WriteAllText(path, text);
        }

        public void WriteEntryTextLocal(string modId, string entry, string text)
        {
            WriteEntryText(modId, FingerprintString, entry, text);
        }

        public void WriteEntryBytes(string modId, string type, string entry, byte[] bytes)
        {
            string path = TouchFolder(modId, type) + '/' + entry;
            File.WriteAllBytes(path, bytes);
        }

        public void WriteEntryBytes(string modId, string entry, byte[] bytes)
        {
            string path = TouchFolder(modId) + '/' + entry;
            File.WriteAllBytes(path, bytes);
        }

        public void WriteEntryBytesLocal(string modId, string entry, byte[] bytes)
        {
            WriteEntryBytes(modId, FingerprintString, entry, bytes);
        }
    }
}
