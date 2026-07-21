
using System;

namespace Unary.Core
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class EditorSettingActionAttribute(string group = "", string name = "", string description = "") : Attribute
    {
        public string Group { get; private set; } = group;
        public string Name { get; private set; } = name;
        public string Description { get; private set; } = description;
    }
}
