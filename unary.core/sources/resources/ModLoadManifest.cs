using Godot;
using System.Collections.Generic;

namespace Unary.Core
{
    public class ModLoadManifest
    {
        public const string Extension = ".mods";
        public const string Path = $"enabled{Extension}";

        public HashSet<ModLoadInfo> Enabled { get; set; }
    }
}
