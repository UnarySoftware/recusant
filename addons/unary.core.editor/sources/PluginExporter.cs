#if TOOLS
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class ProjectExporter : IPluginSystem
    {
        private const string _editor_path = "../godot/editor.exe";
        private const string _build_logs = "../godot/editor_data/mono/build_logs";
        private const string _msbuild_issues = "msbuild_issues.csv";

        private const string _debug_preset = "Debug";
        private const string _release_preset = "Release";

        private const string _debug_preset_path = "export_debug.cfg";
        private const string _release_preset_path = "export_release.cfg";
        private const string _default_preset_path = "export_presets.cfg";

        [EditorSettingAction]
        private static void ExportDebug()
        {
            Export(true);
        }

        [EditorSettingAction]
        private static void ExportRelease()
        {
            Export(false);
        }

        private static void Export(bool debug)
        {
            PluginBootstrap.Singleton.Export(debug);
        }

        private static string ResolveImport(string path)
        {
            ConfigFile config = new();

            var error = config.Load(path);

            if (error != Error.Ok)
            {
                return string.Empty;
            }

            string result = config.GetValue("remap", "path", "").AsString();

            if (string.IsNullOrEmpty(result))
            {
                return string.Empty;
            }

            return result;
        }

        private static void AddEntries(List<string> entries, bool debug)
        {
            string text = debug ? File.ReadAllText(_debug_preset_path) : File.ReadAllText(_release_preset_path);

            StringBuilder builder = new();

            bool buildingStart = true;

            foreach (var character in text)
            {
                if (buildingStart)
                {
                    if (character == '|')
                    {
                        int counter = 0;

                        foreach (var entry in entries)
                        {
                            if (counter != 0)
                            {
                                builder.Append(", ");
                            }

                            builder.Append('"').Append("res://").Append(entry.Replace('\\', '/')).Append('"');

                            counter++;
                        }
                    }
                    else
                    {
                        builder.Append(character);
                    }
                }
                else
                {
                    builder.Append(character);
                }
            }

            File.WriteAllText(_default_preset_path, builder.ToString());
            new FileInfo(_default_preset_path).Refresh();
        }

        private static void ProcessExport(bool debug)
        {
            string preset = debug ? _debug_preset : _release_preset;
            string outputFolder = CachePath.GetDirectoryPath(preset.ToLower());

            PluginExportFilesystem filesystem = PluginExportFilesystem.Singleton;

            var delta = filesystem.Filesystem.GetDelta();

            List<string> exportList = [];

            string[] bannedExtensions = ContentSwapper.BannedExtensions.Value;

            foreach (var entry in delta)
            {
                if (entry.Value == FilesystemCache.ChangeType.Removed)
                {
                    string pathOriginal = Path.Combine(outputFolder, entry.Key);

                    if (File.Exists(pathOriginal))
                    {
                        File.Delete(pathOriginal);
                    }
                }
                else
                {
                    string entryPath = entry.Key;
                    bool gotBanned = false;

                    foreach (var banned in bannedExtensions)
                    {
                        if (entryPath.EndsWith(banned))
                        {
                            gotBanned = true;
                            break;
                        }
                    }

                    if (gotBanned)
                    {
                        continue;
                    }

                    exportList.Add(entryPath);
                }
            }

            AddEntries(exportList, debug);
        }

        bool IPluginSystem.Export()
        {
            string preset = this.IsDebug() ? _debug_preset : _release_preset;
            string outputFolder = CachePath.GetDirectoryPath(preset.ToLower());

            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true);
            }

            Directory.CreateDirectory(outputFolder);

            if (!File.Exists(_editor_path))
            {
                return this.Critical($"Failed to find Godot at path \"{_editor_path}\"");
            }

            string resultingPath = $"{outputFolder}/recusant.exe";

            ProcessExport(this.IsDebug());

            Directory.Delete(_build_logs, true);

            int result = OS.Execute(_editor_path, ["--headless", $"--export-{preset.ToLower()}", $"\"{preset}\""]);

            if (File.Exists(_default_preset_path))
            {
                File.WriteAllText(_default_preset_path, "");
                new FileInfo(_default_preset_path).Refresh();
            }

            string[] directories = Directory.GetDirectories(_build_logs);
            string[] issues = null;

            foreach (var directory in directories)
            {
                string[] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);

                    if (fileName == _msbuild_issues)
                    {
                        issues = File.ReadAllLines(file);
                    }
                }

                Directory.Delete(directory, true);
            }

            if (issues != null && issues.Length > 0)
            {
                foreach (var issue in issues)
                {
                    string[] parts = issue.Split(',');
                    string type = parts[0];

                    if (type != "error")
                    {
                        continue;
                    }

                    string path = parts[1];
                    string line = parts[2];
                    string character = parts[3];
                    string issueId = parts[4];
                    string issueMessage = parts[5];
                    string project = parts[6];

                    this.Error($"Issue: {issueMessage} At: {path} line {line}");
                }

                this.Critical($"Failed to build scripts. Check output for more info.");
                return false;
            }

            if (result != 0 || !File.Exists(resultingPath))
            {
                this.Critical($"Failed to build Core in {preset} mode");
                return false;
            }

            return true;
        }
    }
}

#endif
