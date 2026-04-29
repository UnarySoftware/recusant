
using System;

namespace Unary.Core
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class EditorSettingActionAttribute(string customGroup = "", string customName = "") : Attribute
    {
        public string CustomGroup { get; private set; } = customGroup;
        public string CustomName { get; private set; } = customName;
    }
}
