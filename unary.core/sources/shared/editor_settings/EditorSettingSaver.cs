#if TOOLS

using Godot;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Unary.Core
{
    public class EditorSettingSaver
    {
        public static EditorSettingSaver Singleton
        {
            get
            {
                field ??= new();
                return field;
            }
        }

        public string Path = ".unary/editor.variables";

        private Dictionary<string, Variant> _variables = [];

        public EditorSettingSaver()
        {
            if (!Directory.Exists(".unary"))
            {
                Directory.CreateDirectory(".unary");
            }

            Load();
        }

        public void SetVariable(string path, Variant value, bool onlyIfMissing)
        {
            if (onlyIfMissing)
            {
                if (_variables.ContainsKey(path))
                {
                    return;
                }

                _variables[path] = value;
            }
            else
            {
                if (!_variables.ContainsKey(path))
                {
                    this.Error($"Tried setting an unknown variable at path \"{path}\"");
                    return;
                }

                _variables[path] = value;
            }
        }

        public void GetVariable(string path, out Variant result, out bool found)
        {
            if (!_variables.TryGetValue(path, out var output))
            {
                found = false;
                result = default;
            }
            else
            {
                found = true;
                result = output;
            }
        }

        public void Load()
        {
            if (File.Exists(Path))
            {
                _variables = JsonSerializer.Deserialize<Dictionary<string, Variant>>(File.ReadAllText(Path), JsonConverters.IndentedOptions);
            }
        }

        public void Save()
        {
            File.WriteAllText(Path, JsonSerializer.Serialize(_variables, JsonConverters.IndentedOptions));
        }
    }
}

#endif
