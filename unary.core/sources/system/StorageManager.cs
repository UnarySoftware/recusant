using Godot;
using System;
using System.Collections.Generic;
using System.IO;

namespace Unary.Core
{
    public partial class StorageManager : Node, ICoreSystem
    {
        public byte[] FingerprintBytes { get; private set; }
        public string FingerprintString { get; private set; }

        public const string Folder = "storage";

        bool ISystem.Initialize()
        {
            FingerprintBytes = Engine.Singleton.SpecsHash();
            FingerprintString = Convert.ToHexString(FingerprintBytes);

            if (!Directory.Exists(Folder))
            {
                Directory.CreateDirectory(Folder);
            }

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

        void ISystem.Deinitialize()
        {

        }
    }
}
