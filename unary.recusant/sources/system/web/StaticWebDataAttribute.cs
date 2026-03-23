using System;

namespace Unary.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StaticWebDataAttribute(string path) : Attribute
    {
        public string Path = path;
    }
}
