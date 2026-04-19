
using System;

namespace Unary.Recusant
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class Node3DIconAttribute(string path) : Attribute
    {
        public string Path { get; private set; } = path;
    }
}
