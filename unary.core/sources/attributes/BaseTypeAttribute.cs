
using System;

namespace Unary.Core
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class BaseTypeAttribute(Type type) : Attribute
    {
        public Type Type { get; private set; } = type;
    }
}
