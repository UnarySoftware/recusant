using System.Collections.Generic;
using Godot;

namespace Unary.Core
{
    public partial class Launcher : Node, ICoreSystem
    {
        private Dictionary<string, string> _arguments = [];

        bool ISystem.Initialize()
        {
            string[] arguments = OS.Singleton.GetCmdlineUserArgs();

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

        void ISystem.Deinitialize()
        {

        }
    }
}
