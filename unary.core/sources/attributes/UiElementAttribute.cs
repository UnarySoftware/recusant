
using System;

namespace Unary.Core
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class UiElementAttribute(string nodePath) : Attribute
    {
        public string NodePath { get; private set; } = nodePath;
    }
}
