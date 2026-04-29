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

        private JsonSerializerOptions _options;

        public EditorSettingSaver()
        {
            if (!Directory.Exists(".unary"))
            {
                Directory.CreateDirectory(".unary");
            }

            _options = new()
            {
                WriteIndented = true
            };

            foreach (var converter in JsonConverters.Value)
            {
                _options.Converters.Add(converter);
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
                    SharedLogger.Error(this, $"Tried setting an unknown variable at path \"{path}\"");
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
                _variables = JsonSerializer.Deserialize<Dictionary<string, Variant>>(File.ReadAllText(Path), _options);
            }
        }

        public void Save()
        {
            File.WriteAllText(Path, JsonSerializer.Serialize(_variables, _options));
        }
    }
}
