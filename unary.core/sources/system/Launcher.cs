using Godot;
using System.Collections.Generic;

namespace Unary.Core
{
    // TODO Rework on merge: https://github.com/godotengine/godot/pull/116611
    public partial class Launcher : Node, ICoreSystem
    {
        private Dictionary<string, string> _arguments = [];

        public bool HasArgument(string argument)
        {
            return _arguments.ContainsKey(argument);
        }

        public string GetArgument(string argument)
        {
            if (_arguments.TryGetValue(argument, out string result))
            {
                return result;
            }
            return string.Empty;
        }

        bool ISystem.Initialize()
        {
            string[] arguments = OS.Singleton.GetCmdlineArgs();

            string key = string.Empty;

            foreach (var argument in arguments)
            {
                if (argument.StartsWith("--"))
                {
                    key = argument.Substr(2, argument.Length - 2);
                    _arguments[key] = string.Empty;
                }
                else
                {
                    if (_arguments.ContainsKey(key))
                    {
                        _arguments[key] = argument;
                    }
                }
            }

            return true;
        }
    }
}
