#if TOOLS

using System;
using System.IO;
using System.Text.Json;
using Unary.Core;

namespace Unary.Recusant
{
    public class DebugData<T>(object handler, Func<string> identity, string fieldName)
    {
        private readonly string _path = $".unary/debug_data/{handler.GetType().FullName}/{{0}}_{fieldName}.debug";
        private readonly Func<string> _identity = identity;

        private string Path
        {
            get
            {
                return string.Format(_path, _identity());
            }
        }

        private bool _gotValue = false;

        public T Value
        {
            get
            {
                if (_gotValue)
                {
                    return field;
                }

                if (!File.Exists(Path))
                {
                    _gotValue = true;
                    return default;
                }

                field = JsonSerializer.Deserialize<T>(File.ReadAllText(Path), JsonConverters.IndentedOptions);
                _gotValue = true;

                return field;
            }
            set
            {
                field = value;

                string directory = System.IO.Path.GetDirectoryName(Path);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(Path, JsonSerializer.Serialize(field, JsonConverters.IndentedOptions));

                _gotValue = true;
            }
        }
    }
}

#endif
