using System;

namespace Unary.Core
{
    public struct ModLoadInfo(ModLoadInfo.ModPathType type, string path) : IEquatable<ModLoadInfo>
    {
        public enum ModPathType
        {
            Filesystem,
            Folder,
            Steam
        };

        public ModPathType Type { get; set; } = type;
        public string Path { get; set; } = path;

        public readonly bool Equals(ModLoadInfo other)
        {
            return Type == other.Type && Path == other.Path;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is ModLoadInfo other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Type, Path);
        }

        public static bool operator ==(ModLoadInfo left, ModLoadInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ModLoadInfo left, ModLoadInfo right)
        {
            return !left.Equals(right);
        }
    }
}
